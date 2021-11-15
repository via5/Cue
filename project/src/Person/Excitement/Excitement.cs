using System;
using System.Collections.Generic;

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
			public float fromPenisValue;
			public bool fromPenisOnly;
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

		private List<string> debug_ = new List<string>();
		private bool debugEnabled_ = false;


		public Excitement(Person p)
		{
			person_ = p;

			reasons_[Mouth] = new Reason("Mouth", true);
			reasons_[Breasts] = new Reason("Breasts", true);
			reasons_[Genitals] = new Reason("Genitals", true);
			reasons_[Penetration] = new Reason("Penetration", true);
			reasons_[OtherSex] = new Reason("Other sex", false);
		}

		public bool DebugEnabled
		{
			set { debugEnabled_ = value; }
		}

		public List<string> Debug
		{
			get { return debug_; }
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
			if (debugEnabled_)
				debug_.Clear();

			UpdateParts(s);
			UpdateReasonValues(s);
			UpdateReasonRates(s);
			UpdateMax(s);

			debugEnabled_ = false;
		}

		private void UpdateParts(float s)
		{
			if (debugEnabled_)
				debug_.Add("parts:");

			var ps = person_.Personality;

			for (int i = 0; i < BP.Count; ++i)
			{
				var ts = person_.Body.Get(i).GetTriggers();
				string debug = null;

				parts_[i].specificModifier = 0;
				parts_[i].fromPenisValue = 0;
				parts_[i].fromPenisOnly = false;

				if (ts == null || ts.Length == 0)
				{
					// todo, decay
					if (parts_[i].value > 0)
					{
						parts_[i].value = Math.Max(parts_[i].value - s, 0);

						if (debugEnabled_)
							debug = $"  {BP.ToString(i)}: {parts_[i].value:0.0} (decaying)";
					}
				}
				else
				{
					parts_[i].value = 0;
					bool sawUnknown = false;

					parts_[i].fromPenisValue = 0;
					parts_[i].fromPenisOnly = true;

					for (int j = 0; j < ts.Length; ++j)
					{
						if (!ts[j].IsPerson())
						{
							if (sawUnknown)
							{
								continue;
							}
							else
							{
								sawUnknown = true;
								parts_[i].fromPenisOnly = false;
							}
						}

						if (ts[j].sourcePartIndex == BP.Penis)
							parts_[i].fromPenisValue += ts[j].value;
						else
							parts_[i].fromPenisOnly = false;

						float mod = ps.GetSpecificModifier(
							ts[j].personIndex, ts[j].sourcePartIndex,
							person_.PersonIndex, i);

						parts_[i].value += ts[j].value;
						parts_[i].specificModifier += mod;

						if (debugEnabled_)
						{
							if (debug == null)
								debug = $"  {BP.ToString(i)}: ";
							else
								debug += " ";

							debug += $"+{ts[j]}@{ts[j].value:0.0}";

							if (mod > 0)
								debug += $"*{mod}";
						}
					}

					if (debugEnabled_ && debug != null)
						debug_.Add(debug);
				}
			}
		}

		private float GetReasonValue(
			int reason, int part,
			BasicEnumValues.FloatIndex factor, ref string debug)
		{
			if (part == BP.Vagina)
			{
				return GetFixedPenetrationValue(ref debug);
			}
			else
			{
				float value = parts_[part].value;
				bool allIgnored = false;
				bool someIgnored = false;

				if (reason == Genitals && value > 0)
				{
					value -= parts_[part].fromPenisValue;

					if (Math.Abs(value) < 0.001f)
					{
						allIgnored = true;
						value = 0;
					}
					else
					{
						someIgnored = true;
					}
				}

				if (debugEnabled_)
				{
					var p = parts_[part];

					if (p.value > 0)
					{
						var f = person_.Personality.Get(factor);
						debug += $"{BP.ToString(part)}={value:0.0}*{f}*{p.specificModifier}";

						if (allIgnored)
							debug += "(ignored)";
						else if (someIgnored)
							debug += "(some ignored)";

						debug += " ";
					}
				}

				return value * person_.Personality.Get(factor);
			}
		}

		private void SetReason(
			int reason,
			int part1, BasicEnumValues.FloatIndex factor1)
		{
			string debug = null;
			if (debugEnabled_)
				debug = "";

			reasons_[reason].Value =
				GetReasonValue(reason, part1, factor1, ref debug);

			reasons_[reason].SpecificSensitivityModifier =
				parts_[part1].specificModifier;

			if (reasons_[reason].SpecificSensitivityModifier == 0)
				reasons_[reason].SpecificSensitivityModifier = 1;

			if (debugEnabled_)
			{
				if (parts_[part1].value > 0)
				{
					var r = reasons_[reason];
					string s = $"  {r.Name}: {debug}";
					debug_.Add(s);
				}
			}
		}

		private void SetReason(
			int reason,
			int part1, BasicEnumValues.FloatIndex factor1,
			int part2, BasicEnumValues.FloatIndex factor2)
		{
			string debug = null;
			if (debugEnabled_)
				debug = "";

			reasons_[reason].Value =
				GetReasonValue(reason, part1, factor1, ref debug) +
				GetReasonValue(reason, part2, factor2, ref debug);

			reasons_[reason].SpecificSensitivityModifier =
				parts_[part1].specificModifier +
				parts_[part2].specificModifier;

			if (reasons_[reason].SpecificSensitivityModifier == 0)
				reasons_[reason].SpecificSensitivityModifier = 1;

			if (debugEnabled_)
			{
				if (parts_[part1].value > 0 || parts_[part2].value > 0)
				{
					debug = $"  {reasons_[reason].Name}: " + debug;
					debug_.Add(debug);
				}
			}
		}

		private void SetReason(
			int reason,
			int part1, BasicEnumValues.FloatIndex factor1,
			int part2, BasicEnumValues.FloatIndex factor2,
			int part3, BasicEnumValues.FloatIndex factor3)
		{
			string debug = null;
			if (debugEnabled_)
				debug = "";

			reasons_[reason].Value =
				GetReasonValue(reason, part1, factor1, ref debug) +
				GetReasonValue(reason, part2, factor2, ref debug) +
				GetReasonValue(reason, part3, factor3, ref debug);

			reasons_[reason].SpecificSensitivityModifier =
				parts_[part1].specificModifier +
				parts_[part2].specificModifier +
				parts_[part3].specificModifier;

			if (reasons_[reason].SpecificSensitivityModifier == 0)
				reasons_[reason].SpecificSensitivityModifier = 1;

			if (debugEnabled_)
			{
				if (parts_[part1].value > 0 || parts_[part2].value > 0 || parts_[part3].value > 0)
				{
					debug = $"  {reasons_[reason].Name}: " + debug;
					debug_.Add(debug);
				}
			}
		}

		private void UpdateReasonValues(float s)
		{
			if (debugEnabled_)
				debug_.Add("reasons:");

			var ps = person_.Personality;

			SetReason(
				Mouth,
				BP.Lips, PS.LipsFactor,
				BP.Mouth, PS.MouthFactor);

			SetReason(
				Breasts,
				BP.LeftBreast, PS.LeftBreastFactor,
				BP.RightBreast, PS.RightBreastFactor);

			SetReason(Genitals,
				BP.Labia, PS.LabiaFactor);

			SetReason(Penetration,
				BP.Vagina, PS.VaginaFactor,
				BP.DeepVagina, PS.DeepVaginaFactor,
				BP.DeeperVagina, PS.DeeperVaginaFactor);

			reasons_[OtherSex].Value = 0;
			reasons_[OtherSex].SpecificSensitivityModifier = 1;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_ || p.Body.HavingSexWith(person_))
					continue;

				if (debugEnabled_)
				{
					if (p.Excitement.physicalRate_.Value > 0)
						debug_.Add($"  other physical: {p.ID}@{p.Excitement.physicalRate_.Value}");
				}

				reasons_[OtherSex].Value += p.Excitement.physicalRate_.Value;
			}
		}

		private float GetFixedPenetrationValue(ref string debug)
		{
			var ps = person_.Personality;

			if (parts_[BP.Vagina].value > 0)
			{
				if (debugEnabled_)
				{
					var p = parts_[BP.Vagina];
					debug += $"vagina={p.value:0.0}*{ps.Get(PS.VaginaFactor)}*{p.specificModifier} ";
				}

				return
					parts_[BP.Vagina].value *
					ps.Get(PS.VaginaFactor);
			}

			if (parts_[BP.Penis].value > 0)
			{
				if (debugEnabled_)
				{
					var p = parts_[BP.Penis];
					debug += $"penis={p.value:0.0}*{ps.Get(PS.VaginaFactor)}*{p.specificModifier} ";
				}

				// todo
				return
					parts_[BP.Penis].value *
					ps.Get(PS.VaginaFactor);
			}

			// if penetration is shallow, the vagina trigger doesn't activate
			// at all, it seems rather deep
			//
			// if the labia is triggered by a genital, assume penetration

			if (parts_[BP.Labia].fromPenisValue > 0)
			{
				if (debugEnabled_)
					debug += $"vagina={parts_[BP.Labia].value:0.0}*{ps.Get(PS.VaginaFactor)}(guess) ";

				return
					parts_[BP.Labia].value *
					ps.Get(PS.VaginaFactor);
			}

			return 0;
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

			if (reason != Penetration)
			{
				if (reasons_[Penetration].Rate > 0)
					reasons_[reason].Rate *= person_.Personality.Get(PS.PenetrationDamper);
			}

			if (reasons_[reason].Physical)
				physicalRate_.Value += reasons_[reason].Rate;
			else
				emotionalRate_.Value += reasons_[reason].Rate;

			if (debugEnabled_ && reasons_[reason].Value > 0)
			{
				var r = reasons_[reason];

				string s = $"  {r.Name}: " +
					$"v={r.Value} " +
					$"mod={r.GlobalSensitivityRate} ";

				if (r.SpecificSensitivityModifier != 1)
					s += $"smod={r.SpecificSensitivityModifier} ";

				if (reason != Penetration && reasons_[Penetration].Rate > 0)
					s += $"pdamp={person_.Personality.Get(PS.PenetrationDamper)} ";

				s +=
					$"rate={r.Rate} " +
					$"max={r.SensitivityMax} ";

				debug_.Add(s);
			}
		}

		private void UpdateReasonRates(float s)
		{
			if (debugEnabled_)
				debug_.Add("rates:");

			var ps = person_.Personality;

			// penetration must be first, it's used by the other reasons to
			// check for the damper
			SetReasonPersonalitySettings(Penetration, PS.PenetrationRate, PS.PenetrationMax);
			SetReasonPersonalitySettings(Mouth, PS.MouthRate, PS.MouthMax);
			SetReasonPersonalitySettings(Breasts, PS.BreastsRate, PS.BreastsMax);
			SetReasonPersonalitySettings(Genitals, PS.GenitalsRate, PS.GenitalsMax);
			SetReasonPersonalitySettings(OtherSex, PS.OtherSexExcitementRateFactor, PS.MaxOtherSexExcitement);

			physicalRate_.Value = 0;
			emotionalRate_.Value = 0;

			for (int i = 0; i < ReasonCount; ++i)
				SetReasonRate(i);

			float subtotal = physicalRate_.Value + emotionalRate_.Value;

			if (debugEnabled_)
			{
				debug_.Add("total rates:");
				debug_.Add($"  physical: {physicalRate_.Value:0.00000}");
				debug_.Add($"  emotional: {emotionalRate_.Value:0.00000}");
				debug_.Add($"  subtotal: {subtotal:0.00000}");

				if (ps.Get(PS.RateAdjustment) != 1)
				{
					debug_.Add($"  rate adjustement: {ps.Get(PS.RateAdjustment)}");
				}
			}

			totalRate_ = subtotal * ps.Get(PS.RateAdjustment);

			if (totalRate_ == 0)
			{
				totalRate_ = ps.Get(PS.ExcitementDecayRate);

				if (debugEnabled_)
					debug_.Add($"  total: {totalRate_:0.00000} (decaying)");
			}
			else
			{
				if (debugEnabled_)
					debug_.Add($"  total: {totalRate_:0.00000} (rising)");
			}
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
