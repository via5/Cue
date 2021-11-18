using System;
using System.Collections.Generic;

namespace Cue
{
	public class Excitement
	{
		private Person person_;
		private ExcitementBodyPart[] parts_ = new ExcitementBodyPart[BP.Count];
		private IExcitementReason[] reasons_;
		private ForceableFloat physicalRate_ = new ForceableFloat();
		private ForceableFloat emotionalRate_ = new ForceableFloat();
		private float subtotalRate_ = 0;
		private float totalRate_ = 0;
		private float max_ = 0;

		public Excitement(Person p)
		{
			person_ = p;

			for (int i = 0; i < parts_.Length; ++i)
				parts_[i] = new ExcitementBodyPart(i);

			var rs = new List<IExcitementReason>();

			// penetration must be first, it's used by the other reasons to
			// check for the damper
			rs.Add(new BodyPartExcitementReason(
				"penetration", new BodyPartExcitementReason.Source[]
				{
					new BodyPartExcitementReason.Source(BP.Vagina, PS.VaginaFactor),
					new BodyPartExcitementReason.Source(BP.DeepVagina, PS.DeepVaginaFactor),
					new BodyPartExcitementReason.Source(BP.DeeperVagina, PS.DeeperVaginaFactor)
				}, PS.PenetrationRate, PS.PenetrationMax, false, false));

			rs.Add(new BodyPartExcitementReason(
				"mouth", new BodyPartExcitementReason.Source[]
				{
					new BodyPartExcitementReason.Source(BP.Lips, PS.LipsFactor),
					new BodyPartExcitementReason.Source(BP.Mouth, PS.MouthFactor)
				}, PS.MouthRate, PS.MouthMax, false, true));

			rs.Add(new BodyPartExcitementReason(
				"breasts", new BodyPartExcitementReason.Source[]
				{
					new BodyPartExcitementReason.Source(BP.LeftBreast, PS.LeftBreastFactor),
					new BodyPartExcitementReason.Source(BP.RightBreast, PS.RightBreastFactor)
				}, PS.BreastsRate, PS.BreastsMax, false, true));

			rs.Add(new BodyPartExcitementReason(
				"genitals", new BodyPartExcitementReason.Source[]
				{
					new BodyPartExcitementReason.Source(BP.Labia, PS.LabiaFactor)
				}, PS.GenitalsRate, PS.GenitalsMax, true, true));

			rs.Add(new OtherSexExcitementReason());

			reasons_ = rs.ToArray();
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
			//UpdateParts(s);
			//UpdateReasonValues(s);
			//UpdateReasonRates(s);
		}

		private void UpdateParts(float s)
		{
			for (int i = 0; i < BP.Count; ++i)
			{
				var ts = person_.Body.Get(i).GetTriggers();
				parts_[i].Update(s, person_, ts);
			}
		}

		private void UpdateReasonValues(float s)
		{
			for (int i = 0; i < reasons_.Length; ++i)
				reasons_[i].UpdateValue(person_, parts_);
		}

		private void UpdateReasonRates(float s)
		{
			var ps = person_.Personality;

			// todo
			bool isPenetrated = (reasons_[0].Rate > 0);

			for (int i = 0; i < reasons_.Length; ++i)
				reasons_[i].UpdateRate(person_, isPenetrated);

			physicalRate_.Value = 0;
			emotionalRate_.Value = 0;
			max_ = 0;

			for (int i = 0; i < reasons_.Length; ++i)
			{
				if (reasons_[i].Physical)
					physicalRate_.Value += reasons_[i].Rate;
				else
					emotionalRate_.Value += reasons_[i].Rate;

				if (reasons_[i].Rate > 0)
					max_ = Math.Max(max_, reasons_[i].MaximumExcitement);
			}

			subtotalRate_ = physicalRate_.Value + emotionalRate_.Value;
			totalRate_ = subtotalRate_ * ps.Get(PS.RateAdjustment);

			if (totalRate_ == 0)
				totalRate_ = ps.Get(PS.ExcitementDecayRate);
		}

		private string DebugMakeSource(Source src, int part)
		{
			string s = $"  {src} ";

			if (part == BP.None)
				s += "unknown";
			else
				s += BP.ToString(part);

			s += "=>";

			if (src.TargetBodyPart(part) == BP.None)
				s += "unknown";
			else
				s += BP.ToString(src.TargetBodyPart(part));

			if (src.IsIgnored(part))
				s += " (ignored)";

			return s;
		}

		private void DebugZone(ErogenousZone z, List<string> debug)
		{
			var srcs = z.Sources;

			debug.Add($"  active sources: {z.ActiveSources}");

			for (int i = 0; i < srcs.Length; ++i)
			{
				var s = srcs[i];

				for (int j = 0; j < BP.Count; ++j)
				{
					if (s.IsStrictlyActive(j))
						debug.Add(DebugMakeSource(s, j));
				}

				if (s.IsStrictlyActive(BP.None))
					debug.Add(DebugMakeSource(s, BP.None));
			}
		}

		public string[] Debug()
		{
			var debug = new List<string>();

			debug.Add($"penetration:");
			DebugZone(person_.Status.Penetration, debug);

			debug.Add($"mouth:");
			DebugZone(person_.Status.Mouth, debug);

			debug.Add($"breasts:");
			DebugZone(person_.Status.Breasts, debug);

			debug.Add($"genitals:");
			DebugZone(person_.Status.Genitals, debug);


			var ps = person_.Personality;
			debug.Add("");
			debug.Add("");
			debug.Add("parts:");
			for (int i = 0; i < parts_.Length; ++i)
			{
				var s = parts_[i].Debug();
				if (s != null)
					debug.Add("  " + s);
			}

			debug.Add("reasons:");
			for (int i = 0; i < reasons_.Length; ++i)
			{
				var s = reasons_[i].Debug(person_, parts_);
				if (s != null)
					debug.Add("  " + s);
			}

			debug.Add("rates:");

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

				if (r.DampenModifier != 1)
					s += $"damp={r.DampenModifier} ";

				s +=
					$"rate={r.Rate} " +
					$"max={r.MaximumExcitement} ";

				debug.Add(s);
			}

			debug.Add("total rates:");
			debug.Add($"  physical: {physicalRate_.Value:0.00000}");
			debug.Add($"  emotional: {emotionalRate_.Value:0.00000}");
			debug.Add($"  subtotal: {subtotalRate_:0.00000}");

			if (ps.Get(PS.RateAdjustment) != 1)
				debug.Add($"  rate adjustement: {ps.Get(PS.RateAdjustment)}");

			if (totalRate_ < 0)
				debug.Add($"  total: {totalRate_:0.00000} (decaying)");
			else
				debug.Add($"  total: {totalRate_:0.00000} (rising)");

			return debug.ToArray();
		}
	}
}
