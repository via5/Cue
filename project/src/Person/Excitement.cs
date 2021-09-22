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
					$"{value_:0.00}=" +
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
		private PartValue[] parts_ = new PartValue[BP.Count];

		private Reason[] reasons_ = new Reason[ReasonCount];
		private ForceableFloat physicalRate_ = new ForceableFloat();
		private ForceableFloat emotionalRate_ = new ForceableFloat();
		private float totalRate_ = 0;
		private float max_ = 0;

		public Excitement(Person p)
		{
			person_ = p;

			reasons_[Mouth] = new Reason("Mouth", true);
			reasons_[Breasts] = new Reason("Breasts", true);
			reasons_[Genitals] = new Reason("Genitals", true);
			reasons_[Penetration] = new Reason("Penetration", true);
			reasons_[OtherSex] = new Reason("Other sex", false);
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
			get { return physicalRate_.Value; }
		}

		public ForceableFloat ForceablePhysicalRate
		{
			get { return physicalRate_; }
		}

		public float EmotionalRate
		{
			get { return emotionalRate_.Value; }
		}

		public ForceableFloat ForceableEmotionalRate
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
		}

		private void UpdateParts(float s)
		{
			var pp = person_.Physiology;

			for (int i = 0; i < BP.Count; ++i)
			{
				var ts = person_.Body.Get(i).GetTriggers();

				parts_[i].specificModifier = 1;

				if (ts == null || ts.Length == 0)
				{
					// todo, decay
					parts_[i].value = Math.Max(parts_[i].value - s, 0);
				}
				else
				{
					parts_[i].value = 0;

					for (int j = 0; j < ts.Length; ++j)
					{
						parts_[i].value += ts[j].value;
						parts_[i].specificModifier += pp.GetSpecificModifier(i, ts[j].sourcePartIndex);
					}
				}
			}
		}

		private void UpdateReasonValues(float s)
		{
			var ps = person_.Personality;
			var pg = person_.Physiology;

			reasons_[Mouth].Value =
				parts_[BP.Lips].value * pg.Get(PE.LipsFactor) +
				parts_[BP.Mouth].value * pg.Get(PE.MouthFactor);

			reasons_[Mouth].SpecificSensitivityModifier =
				parts_[BP.Lips].specificModifier +
				parts_[BP.Mouth].specificModifier;


			reasons_[Breasts].Value =
				parts_[BP.LeftBreast].value * pg.Get(PE.LeftBreastFactor) +
				parts_[BP.RightBreast].value * pg.Get(PE.RightBreastFactor);

			reasons_[Breasts].SpecificSensitivityModifier =
				parts_[BP.LeftBreast].specificModifier +
				parts_[BP.RightBreast].specificModifier;


			reasons_[Genitals].Value =
				parts_[BP.Labia].value * pg.Get(PE.LabiaFactor);

			reasons_[Genitals].SpecificSensitivityModifier =
				parts_[BP.Labia].specificModifier;


			reasons_[Penetration].Value =
				GetFixedPenetrationValue() +
				parts_[BP.DeepVagina].value * pg.Get(PE.DeepVaginaFactor) +
				parts_[BP.DeeperVagina].value * pg.Get(PE.DeeperVaginaFactor);

			reasons_[Penetration].SpecificSensitivityModifier =
				parts_[BP.Vagina].specificModifier +
				parts_[BP.DeepVagina].specificModifier +
				parts_[BP.DeeperVagina].specificModifier;


			reasons_[OtherSex].Value = 0;
			reasons_[OtherSex].SpecificSensitivityModifier = 1;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_ || p.Body.HavingSexWith(person_))
					continue;

				reasons_[OtherSex].Value += p.Excitement.physicalRate_.Value;
			}
		}

		private float GetFixedPenetrationValue()
		{
			var pg = person_.Physiology;

			if (parts_[BP.Vagina].value > 0)
			{
				return
					parts_[BP.Vagina].value *
					pg.Get(PE.VaginaFactor);
			}

			if (parts_[BP.Penis].value > 0)
			{
				// todo
				return
					parts_[BP.Penis].value *
					pg.Get(PE.VaginaFactor);
			}

			// if penetration is shallow, the vagina trigger doesn't activate
			// at all, it seems rather deep
			//
			// if the labia is triggered by a genital, assume penetration

			var ts = person_.Body.Get(BP.Labia).GetTriggers();
			if (ts != null)
			{
				for (int i = 0; i < ts.Length; i++)
				{
					if (ts[i].sourcePartIndex == BP.Penis)
					{
						return
							parts_[BP.Labia].value *
							pg.Get(PE.VaginaFactor);
					}
				}
			}

			return 0;
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

			physicalRate_.Value = 0;
			emotionalRate_.Value = 0;

			for (int i = 0; i < ReasonCount; ++i)
			{
				reasons_[i].Rate =
					reasons_[i].Value *
					reasons_[i].GlobalSensitivityRate *
					reasons_[i].SpecificSensitivityModifier;

				if (reasons_[i].Physical)
					physicalRate_.Value += reasons_[i].Rate;
				else
					emotionalRate_.Value += reasons_[i].Rate;
			}

			totalRate_ = physicalRate_.Value + emotionalRate_.Value;

			if (totalRate_ == 0)
				totalRate_ = pp.Get(PE.ExcitementDecayRate);
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
	}
}
