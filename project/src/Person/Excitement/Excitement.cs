using System;
using System.Collections.Generic;

namespace Cue
{
	public class Excitement
	{
		private Person person_;
		private ForceableFloat physicalRate_ = new ForceableFloat();
		private ForceableFloat emotionalRate_ = new ForceableFloat();
		private float subtotalRate_ = 0;
		private float totalRate_ = 0;
		private float max_ = 0;

		public Excitement(Person p)
		{
			person_ = p;
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
			physicalRate_.Value = 0;
			max_ = 0;

			CheckRate(person_.Status.Penetration, SS.Penetration);
			CheckRate(person_.Status.Mouth, SS.Mouth);
			CheckRate(person_.Status.Breasts, SS.Breasts);
			CheckRate(person_.Status.Genitals, SS.Genitals);
		}

		/*
	class OtherSexExcitementReason : BasicExcitementReason
	{
		public OtherSexExcitementReason()
			: base("otherSex", PS.OtherSexExcitementRateFactor, PS.MaxOtherSexExcitement, false)
		{
		}

		protected override void DoUpdateValue(Person p, ExcitementBodyPart[] parts)
		{
			value_ = 0;
			specificSensitivityModifier_ = 1;

			foreach (var op in Cue.Instance.ActivePersons)
			{
				if (op == p || op.Status.PenetratedBy(p) || p.Status.PenetratedBy(op))
					continue;

				// todo, missing in debug
				//if (op.Excitement.physicalRate_.Value > 0)
				//	debug_.Add($"  other physical: {p.ID}@{p.Excitement.physicalRate_.Value}");

				value_ += op.Excitement.PhysicalRate;
			}
		}

		protected override void DoUpdateRate(Person p, bool isPenetrated)
		{
			// no-op
		}

		protected override string DoDebug(Person p, ExcitementBodyPart[] parts)
		{
			return null;
		}
		*/

		private void CheckRate(ErogenousZone zone, int sensitivityIndex)
		{
			for (int i = 0; i < zone.Sources.Length; ++i)
			{
				var src = zone.Sources[i];
				if (!src.Active)
					continue;

				var ss = person_.Personality.Sensitivities.Get(sensitivityIndex);
				physicalRate_.Value += ss.Rate * ss.GetModifier(person_, src.PersonIndex);
				max_ = Math.Max(max_, ss.Maximum);
			}
		}

/*		private void UpdateReasonRates(float s)
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
*/
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

			string sources = "";
			for (int i = 0; i < z.Sources.Length; ++i)
			{
				if (z.Sources[i].Active)
				{
					if (sources != "")
						sources += ", ";

					sources += z.Sources[i].ToString();
				}
			}

			if (sources == "")
				sources = "none";

			debug.Add($"  active sources: {sources}");

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

			debug.Add("values:");
			debug.Add($"  max: {max_:0.00000}");
			debug.Add($"  physical: {physicalRate_.Value:0.00000}");
			debug.Add($"  emotional: {emotionalRate_.Value:0.00000}");
			debug.Add($"  subtotal: {subtotalRate_:0.00000}");

			//if (ps.Get(PS.RateAdjustment) != 1)
			//	debug.Add($"  rate adjustement: {ps.Get(PS.RateAdjustment)}");

			if (totalRate_ < 0)
				debug.Add($"  total: {totalRate_:0.00000} (decaying)");
			else
				debug.Add($"  total: {totalRate_:0.00000} (rising)");

			return debug.ToArray();
		}
	}
}
