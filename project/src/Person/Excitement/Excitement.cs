using System;
using System.Collections.Generic;

namespace Cue
{
	class Excitement
	{
		private Person person_;
		private ExcitementBodyPart[] parts_ = new ExcitementBodyPart[BP.Count];
		private ExcitementReason[] reasons_ = new ExcitementReason[ExcitementReason.Count];
		private ForceableFloat physicalRate_ = new ForceableFloat();
		private ForceableFloat emotionalRate_ = new ForceableFloat();
		private float subtotalRate_ = 0;
		private float totalRate_ = 0;
		private float max_ = 0;

		private List<string> debug_ = new List<string>();
		private bool debugEnabled_ = false;


		public Excitement(Person p)
		{
			person_ = p;

			for (int i = 0; i < parts_.Length; ++i)
				parts_[i] = new ExcitementBodyPart(i);

			reasons_[ExcitementReason.Mouth] = new ExcitementReason(
				ExcitementReason.Mouth, new ExcitementReason.Source[]
				{
					new ExcitementReason.Source(BP.Lips, PS.LipsFactor),
					new ExcitementReason.Source(BP.Mouth, PS.MouthFactor)
				});

			reasons_[ExcitementReason.Breasts] = new ExcitementReason(
				ExcitementReason.Breasts, new ExcitementReason.Source[]
				{
					new ExcitementReason.Source(BP.LeftBreast, PS.LeftBreastFactor),
					new ExcitementReason.Source(BP.RightBreast, PS.RightBreastFactor)
				});

			reasons_[ExcitementReason.Genitals] = new ExcitementReason(
				ExcitementReason.Genitals, new ExcitementReason.Source[]
				{
					new ExcitementReason.Source(BP.Labia, PS.LabiaFactor)
				});

			reasons_[ExcitementReason.Penetration] = new ExcitementReason(
				ExcitementReason.Penetration, new ExcitementReason.Source[]
				{
					new ExcitementReason.Source(BP.Vagina, PS.VaginaFactor),
					new ExcitementReason.Source(BP.DeepVagina, PS.DeepVaginaFactor),
					new ExcitementReason.Source(BP.DeeperVagina, PS.DeeperVaginaFactor)
				});

			reasons_[ExcitementReason.OtherSex] = new ExcitementReason(
				ExcitementReason.OtherSex, new ExcitementReason.Source[]
				{
				});
		}

		public bool DebugEnabled
		{
			set { debugEnabled_ = value; }
		}

		public List<string> Debug
		{
			get
			{
				MakeDebug();
				return debug_;
			}
		}

		public float Max
		{
			get { return max_; }
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

			debugEnabled_ = false;
		}

		private void MakeDebug()
		{
			var ps = person_.Personality;

			debug_.Clear();

			debug_.Add("parts:");
			for (int i = 0; i < parts_.Length; ++i)
			{
				var s = parts_[i].Debug();
				if (s != null)
					debug_.Add("  " + s);
			}

			debug_.Add("reasons:");
			for (int i = 0; i < reasons_.Length; ++i)
			{
				var s = reasons_[i].Debug(person_, parts_);
				if (s != null)
					debug_.Add("  " + s);
			}

			debug_.Add("rates:");

			for (int i = 0; i < reasons_.Length; ++i)
			{
				var r = reasons_[i];
				if (r.Value <= 0)
					continue;

				string s =
					$"  {r.Name}: " +
					$"v={r.Value} " +
					$"mod={r.GlobalSensitivityRate} ";

				if (r.SpecificSensitivityModifier != 1)
					s += $"smod={r.SpecificSensitivityModifier} ";

				if (i != ExcitementReason.Penetration && reasons_[ExcitementReason.Penetration].Rate > 0)
					s += $"pdamp={ps.Get(PS.PenetrationDamper)} ";

				s +=
					$"rate={r.Rate} " +
					$"max={r.SensitivityMax} ";

				debug_.Add(s);
			}

			debug_.Add("total rates:");
			debug_.Add($"  physical: {physicalRate_.Value:0.00000}");
			debug_.Add($"  emotional: {emotionalRate_.Value:0.00000}");
			debug_.Add($"  subtotal: {subtotalRate_:0.00000}");

			if (ps.Get(PS.RateAdjustment) != 1)
				debug_.Add($"  rate adjustement: {ps.Get(PS.RateAdjustment)}");

			if (totalRate_ < 0)
				debug_.Add($"  total: {totalRate_:0.00000} (decaying)");
			else
				debug_.Add($"  total: {totalRate_:0.00000} (rising)");
		}

		private void UpdateParts(float s)
		{
			var ps = person_.Personality;

			for (int i = 0; i < BP.Count; ++i)
			{
				var ts = person_.Body.Get(i).GetTriggers();
				parts_[i].Update(s, person_, ts);
			}
		}

		private void UpdateReasonValues(float s)
		{
			var ps = person_.Personality;

			for (int i = 0; i < reasons_.Length; ++i)
				reasons_[i].Update(person_, parts_);
		}

		private void SetReasonPersonalitySettings(
			int reason,
			BasicEnumValues.FloatIndex rate, BasicEnumValues.FloatIndex max)
		{
			reasons_[reason].GlobalSensitivityRate = person_.Personality.Get(rate);
			reasons_[reason].SensitivityMax = person_.Personality.Get(max);
		}

		private void SetReasonRate(int reason)
		{
			reasons_[reason].Rate =
				reasons_[reason].Value *
				reasons_[reason].GlobalSensitivityRate *
				reasons_[reason].SpecificSensitivityModifier;

			if (reason != ExcitementReason.Penetration)
			{
				if (reasons_[ExcitementReason.Penetration].Rate > 0)
					reasons_[reason].Rate *= person_.Personality.Get(PS.PenetrationDamper);
			}

			if (reasons_[reason].Physical)
				physicalRate_.Value += reasons_[reason].Rate;
			else
				emotionalRate_.Value += reasons_[reason].Rate;
		}

		private void UpdateReasonRates(float s)
		{
			var ps = person_.Personality;

			// penetration must be first, it's used by the other reasons to
			// check for the damper
			SetReasonPersonalitySettings(ExcitementReason.Penetration, PS.PenetrationRate, PS.PenetrationMax);
			SetReasonPersonalitySettings(ExcitementReason.Mouth, PS.MouthRate, PS.MouthMax);
			SetReasonPersonalitySettings(ExcitementReason.Breasts, PS.BreastsRate, PS.BreastsMax);
			SetReasonPersonalitySettings(ExcitementReason.Genitals, PS.GenitalsRate, PS.GenitalsMax);
			SetReasonPersonalitySettings(ExcitementReason.OtherSex, PS.OtherSexExcitementRateFactor, PS.MaxOtherSexExcitement);

			physicalRate_.Value = 0;
			emotionalRate_.Value = 0;

			for (int i = 0; i < ExcitementReason.Count; ++i)
				SetReasonRate(i);

			subtotalRate_ = physicalRate_.Value + emotionalRate_.Value;
			totalRate_ = subtotalRate_ * ps.Get(PS.RateAdjustment);

			if (totalRate_ == 0)
				totalRate_ = ps.Get(PS.ExcitementDecayRate);
		}

		private void UpdateMax(float s)
		{
			var ps = person_.Personality;

			max_ = 0;

			for (int i = 0; i < ExcitementReason.Count; ++i)
			{
				if (reasons_[i].Rate > 0)
					max_ = Math.Max(max_, reasons_[i].SensitivityMax);
			}
		}
	}
}
