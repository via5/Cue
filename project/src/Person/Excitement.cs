using System;

namespace Cue
{
	class Excitement
	{
		public class Reason
		{
			private string name_;
			private bool physical_;
			private float value_ = 0;
			private float rate_ = 0;
			private float personalityModifier_ = 0;
			private float personalityMax_ = 0;

			public Reason(string name, bool physical)
			{
				name_ = name;
				physical_ = physical;
			}

			public string Name
			{
				get { return name_; }
			}

			public bool Physical
			{
				get { return physical_; }
			}

			public float Value
			{
				get { return value_; }
				set { value_ = value; }
			}

			public float Rate
			{
				get { return rate_; }
				set { rate_ = value; }
			}

			public float PersonalityModifier
			{
				get { return personalityModifier_; }
				set { personalityModifier_ = value; }
			}

			public float PersonalityMax
			{
				get { return personalityMax_; }
				set { personalityMax_ = value; }
			}

			public override string ToString()
			{
				if (rate_ == 0)
					return "0";
				else
					return $"{value_:0.000000} {rate_:0.000000}";
			}
		}


		public const int Mouth = 0;
		public const int Breasts = 1;
		public const int Genitals = 2;
		public const int Penetration = 3;
		public const int OtherSex = 4;
		public const int ReasonCount = 5;

		private Person person_;
		private float[] parts_ = new float[BodyParts.Count];

		private Reason[] reasons_ = new Reason[ReasonCount];
		private float physicalRate_ = 0;
		private float emotionalRate_ = 0;
		private float totalRate_ = 0;
		private float max_ = 0;
		private float flatValue_ = 0;
		private IEasing easing_ = new SinusoidalEasing();


		public Excitement(Person p)
		{
			person_ = p;

			reasons_[Mouth] = new Reason("Mouth", true);
			reasons_[Breasts] = new Reason("Breasts", true);
			reasons_[Genitals] = new Reason("Genitals", true);
			reasons_[Penetration] = new Reason("Penetration", true);
			reasons_[OtherSex] = new Reason("Other sex", false);
		}

		public float Value
		{
			get { return easing_.Magnitude(flatValue_); }
		}

		public float FlatValue
		{
			get { return flatValue_; }
			set { flatValue_ = value; }
		}

		public float Max
		{
			get { return max_; }
		}

		public Reason GetReason(int i)
		{
			return reasons_[i];
		}

		public float PhysicalRate
		{
			get { return physicalRate_; }
		}

		public float EmotionalRate
		{
			get { return emotionalRate_; }
		}

		public float TotalRate
		{
			get { return totalRate_; }
		}

		public void Update(float s)
		{
			UpdateParts(s);
			UpdateReasonValues(s);
			UpdateReasonRates(s);
			UpdateMax(s);
			UpdateValue(s);
		}

		public override string ToString()
		{
			return
				$"{Value:0.000000} " +
				$"(flat {flatValue_:0.000000}, max {max_:0.000000})";
		}

		private void UpdateParts(float s)
		{
			for (int i = 0; i < BodyParts.Count; ++i)
			{
				var ts = person_.Body.Get(i).GetTriggers();

				if (ts != null && ts.Length > 0)
					parts_[i] = ts[0].value; // todo
				else
					parts_[i] = Math.Max(parts_[i] - s, 0);
			}
		}

		private void UpdateReasonValues(float s)
		{
			var ps = person_.Personality;

			reasons_[Mouth].Value =
				parts_[BodyParts.Lips] * 0.1f +
				parts_[BodyParts.Mouth] * 0.9f;

			reasons_[Breasts].Value =
				parts_[BodyParts.LeftBreast] * 0.5f +
				parts_[BodyParts.RightBreast] * 0.5f;

			reasons_[Genitals].Value = Math.Min(1,
				parts_[BodyParts.Labia]);

			reasons_[Penetration].Value = Math.Min(1,
				parts_[BodyParts.Vagina] * 0.3f +
				parts_[BodyParts.DeepVagina] * 1 +
				parts_[BodyParts.DeeperVagina] * 1);


			reasons_[OtherSex].Value = 0;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				reasons_[OtherSex].Value += p.Excitement.physicalRate_;
			}
		}

		private void UpdateReasonRates(float s)
		{
			var ss = person_.Physiology.Sensitivity;
			var ps = person_.Personality;

			reasons_[Mouth].PersonalityModifier = ss.MouthRate;
			reasons_[Breasts].PersonalityModifier = ss.BreastsRate;
			reasons_[Genitals].PersonalityModifier = ss.GenitalsRate;
			reasons_[Penetration].PersonalityModifier = ss.PenetrationRate;
			reasons_[OtherSex].PersonalityModifier = ps.OtherSexExcitementRate;

			reasons_[Mouth].PersonalityMax = ss.MouthMax;
			reasons_[Breasts].PersonalityMax = ss.BreastsMax;
			reasons_[Genitals].PersonalityMax = ss.GenitalsMax;
			reasons_[Penetration].PersonalityMax = ss.PenetrationMax;
			reasons_[OtherSex].PersonalityMax = ps.MaxOtherSexExcitement;

			physicalRate_ = 0;
			emotionalRate_ = 0;

			for (int i = 0; i < ReasonCount; ++i)
			{
				reasons_[i].Rate = reasons_[i].Value * reasons_[i].PersonalityModifier * s;

				if (reasons_[i].Physical)
					physicalRate_ += reasons_[i].Rate;
				else
					emotionalRate_ += reasons_[i].Rate;
			}

			totalRate_ =
				physicalRate_ +
				emotionalRate_;

			totalRate_ *= ss.RateAdjustment;

			if (totalRate_ == 0)
				totalRate_ = ss.DecayPerSecond * s;
		}

		private void UpdateMax(float s)
		{
			var ss = person_.Physiology.Sensitivity;
			var ps = person_.Personality;

			max_ = 0;

			for (int i = 0; i < ReasonCount; ++i)
			{
				if (reasons_[i].Rate > 0)
					max_ = Math.Max(max_, reasons_[i].PersonalityMax);
			}
		}

		private void UpdateValue(float s)
		{
			var ss = person_.Physiology.Sensitivity;

			if (flatValue_ > max_)
			{
				flatValue_ = Math.Max(flatValue_ + ss.DecayPerSecond * s, max_);
			}
			else
			{
				flatValue_ = U.Clamp(flatValue_ + totalRate_, 0, max_);
			}
		}
	}
}
