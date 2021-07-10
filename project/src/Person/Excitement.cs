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
			private float specificSensitivityModifier_ = 0;
			private float globalSensitivityRate_ = 0;
			private float sensitivityMax_ = 0;

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

			public float SpecificSensitivityModifier
			{
				get { return specificSensitivityModifier_; }
				set { specificSensitivityModifier_ = value; }
			}

			public float GlobalSensitivityRate
			{
				get { return globalSensitivityRate_; }
				set { globalSensitivityRate_ = value; }
			}

			public float SensitivityMax
			{
				get { return sensitivityMax_; }
				set { sensitivityMax_ = value; }
			}

			public override string ToString()
			{
				if (rate_ == 0)
					return "0";

				return
					$"{value_:0.000000}*" +
					$"{globalSensitivityRate_:0.000000}*" +
					$"{specificSensitivityModifier_:0.000000}=" +
					$"{rate_:0.000000}";
			}
		}

		private struct PartValue
		{
			public float value;
			public float specificModifier;
		}


		public const int Mouth = 0;
		public const int Breasts = 1;
		public const int Genitals = 2;
		public const int Penetration = 3;
		public const int OtherSex = 4;
		public const int ReasonCount = 5;

		private Person person_;
		private PartValue[] parts_ = new PartValue[BodyParts.Count];

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
			var pp = person_.Physiology;

			for (int i = 0; i < BodyParts.Count; ++i)
			{
				var ts = person_.Body.Get(i).GetTriggers();

				if (ts == null || ts.Length == 0)
				{
					// todo, decay
					parts_[i].value = Math.Max(parts_[i].value - s, 0);
				}
				else
				{
					parts_[i].value = 0;
					parts_[i].specificModifier = 0;

					for (int j = 0; j < ts.Length; ++j)
					{
						parts_[i].value += ts[j].value;
						parts_[i].specificModifier += pp.GetSpecificModifier(i, ts[j]);
					}
				}
			}
		}

		private void UpdateReasonValues(float s)
		{
			var ps = person_.Personality;

			reasons_[Mouth].Value =
				parts_[BodyParts.Lips].value * 0.1f +
				parts_[BodyParts.Mouth].value * 0.9f;

			reasons_[Mouth].SpecificSensitivityModifier =
				parts_[BodyParts.Lips].specificModifier +
				parts_[BodyParts.Mouth].specificModifier;


			reasons_[Breasts].Value =
				parts_[BodyParts.LeftBreast].value * 0.5f +
				parts_[BodyParts.RightBreast].value * 0.5f;

			reasons_[Breasts].SpecificSensitivityModifier =
				parts_[BodyParts.LeftBreast].specificModifier +
				parts_[BodyParts.RightBreast].specificModifier;


			reasons_[Genitals].Value = Math.Min(1,
				parts_[BodyParts.Labia].value);

			reasons_[Genitals].SpecificSensitivityModifier =
				parts_[BodyParts.Labia].specificModifier;


			reasons_[Penetration].Value = Math.Min(1,
				parts_[BodyParts.Vagina].value * 0.3f +
				parts_[BodyParts.DeepVagina].value * 1 +
				parts_[BodyParts.DeeperVagina].value * 1);

			reasons_[Penetration].SpecificSensitivityModifier =
				parts_[BodyParts.Vagina].specificModifier +
				parts_[BodyParts.DeepVagina].specificModifier +
				parts_[BodyParts.DeeperVagina].specificModifier;


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
			var pp = person_.Physiology;
			var ps = person_.Personality;

			reasons_[Mouth].GlobalSensitivityRate = pp.Get(PE.MouthRate);
			reasons_[Breasts].GlobalSensitivityRate = pp.Get(PE.BreastsRate);
			reasons_[Genitals].GlobalSensitivityRate = pp.Get(PE.GenitalsRate);
			reasons_[Penetration].GlobalSensitivityRate = pp.Get(PE.PenetrationRate);
			reasons_[OtherSex].GlobalSensitivityRate = ps.Get(PSE.OtherSexExcitementRateFactor);

			reasons_[Mouth].SensitivityMax = pp.Get(PE.MouthMax);
			reasons_[Breasts].SensitivityMax = pp.Get(PE.BreastsMax);
			reasons_[Genitals].SensitivityMax = pp.Get(PE.GenitalsMax);
			reasons_[Penetration].SensitivityMax = pp.Get(PE.PenetrationMax);
			reasons_[OtherSex].SensitivityMax = ps.Get(PSE.MaxOtherSexExcitement);

			physicalRate_ = 0;
			emotionalRate_ = 0;

			for (int i = 0; i < ReasonCount; ++i)
			{
				reasons_[i].Rate =
					reasons_[i].Value *
					reasons_[i].GlobalSensitivityRate *
					reasons_[i].SpecificSensitivityModifier;

				if (reasons_[i].Physical)
					physicalRate_ += reasons_[i].Rate;
				else
					emotionalRate_ += reasons_[i].Rate;
			}

			totalRate_ = physicalRate_ + emotionalRate_;

			totalRate_ *= pp.Get(PE.RateAdjustment);

			if (totalRate_ == 0)
				totalRate_ = pp.Get(PE.DecayPerSecond);
		}

		private void UpdateMax(float s)
		{
			var ps = person_.Personality;

			max_ = 0;

			for (int i = 0; i < ReasonCount; ++i)
			{
				if (reasons_[i].Rate > 0)
					max_ = Math.Max(max_, reasons_[i].SensitivityMax);
			}
		}

		private void UpdateValue(float s)
		{
			var pp = person_.Physiology;

			if (flatValue_ > max_)
			{
				flatValue_ = Math.Max(flatValue_ + pp.Get(PE.DecayPerSecond) * s, max_);
			}
			else
			{
				flatValue_ = U.Clamp(flatValue_ + totalRate_ * s, 0, max_);
			}
		}
	}
}
