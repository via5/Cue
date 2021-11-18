using System;
using System.Collections.Generic;

namespace Cue
{
	public class Excitement
	{
		class Other
		{
			public float rate;
			public float mod;
			public float max;
			public bool active;
		}


		private Person person_;
		private Other[] others_ = null;
		private ForceableFloat physicalRate_ = new ForceableFloat();
		private ForceableFloat emotionalRate_ = new ForceableFloat();
		private float subtotalRate_ = 0;
		private float totalRate_ = 0;
		private float max_ = 0;

		public Excitement(Person p)
		{
			person_ = p;
		}

		public void Init()
		{
			others_ = new Other[Cue.Instance.ActivePersons.Length];
			for (int i = 0; i < others_.Length; ++i)
				others_[i] = new Other();
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
			CheckOthersExcitement();

			physicalRate_.Value = GetPhysicalRate();
			emotionalRate_.Value = GetEmotionalRate();
			max_ = GetMaximum();

			subtotalRate_ = physicalRate_.Value + emotionalRate_.Value;
			totalRate_ = subtotalRate_ * person_.Personality.Get(PS.RateAdjustment);

			if (totalRate_ == 0)
				totalRate_ = person_.Personality.Get(PS.ExcitementDecayRate);
		}

		private float GetMaximum()
		{
			float max = 0;

			for (int i = 0; i < SS.Count; ++i)
			{
				var z = person_.Status.Zone(i);
				if (z == null)
					continue;

				for (int j = 0; j < z.Sources.Length; ++j)
				{
					var src = z.Sources[j];

					if (src.Active)
						max = Math.Max(max, src.Maximum);
				}
			}

			for (int i = 0; i < others_.Length; ++i)
				max = Math.Max(max, others_[i].max);

			return max;
		}

		private float GetPhysicalRate()
		{
			float rate = 0;

			float dampen = 1;
			if (person_.Status.Zone(SS.Penetration).Active)
				dampen = person_.Personality.Get(PS.PenetrationDamper);

			for (int i = 0; i < SS.Count; ++i)
			{
				var z = person_.Status.Zone(i);
				if (z == null)
					continue;

				for (int j = 0; j < z.Sources.Length; ++j)
				{
					var src = z.Sources[j];

					if (src.Active && (person_.Mood.Get(Moods.Excited) <= src.Maximum))
					{
						float thisRate = src.Rate * src.Modifier;

						if (i != SS.Penetration)
							thisRate *= dampen;

						rate += thisRate;
					}
				}
			}

			return rate;
		}

		private float GetEmotionalRate()
		{
			float rate = 0;

			for (int i = 0; i < others_.Length; ++i)
			{
				if (others_[i].active)
					rate += others_[i].rate * others_[i].mod;
			}

			return rate;
		}

		private void CheckOthersExcitement()
		{
			for (int i = 0; i < others_.Length; ++i)
			{
				others_[i].rate = 0;
				others_[i].mod = 0;
				others_[i].max = 0;
				others_[i].active = false;
			}

			int highest = -1;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_ || p.Status.PenetratedBy(person_) || person_.Status.PenetratedBy(p))
					continue;

				var ss = person_.Personality.Sensitivities.Get(SS.OthersExcitement);
				var o = others_[p.PersonIndex];

				o.rate = p.Excitement.GetPhysicalRate();
				o.mod = ss.Rate;
				o.max = ss.Maximum;

				if (person_.Mood.Get(Moods.Excited) <= o.max)
				{
					if (highest == -1)
						highest = p.PersonIndex;
					else if (o.rate > others_[highest].rate)
						highest = p.PersonIndex;
				}
			}

			if (highest >= 0)
				others_[highest].active = true;
		}

		private string DebugMakeSource(Source src, int part)
		{
			string s = "";

			if (part == BP.None)
				s += "unknown";
			else
				s += BP.ToString(part);

			s += "=>";

			if (src.TargetBodyPart(part) == BP.None)
				s += "unknown";
			else
				s += BP.ToString(src.TargetBodyPart(part));

			return s;
		}

		private void DebugZone(int sensitivityIndex, List<string> debug)
		{
			var z = person_.Status.Zone(sensitivityIndex);
			var srcs = z.Sources;

			for (int i = 0; i < srcs.Length; ++i)
			{
				var s = srcs[i];
				if (s.StrictlyActiveCount == 0)
					continue;

				bool active = (s.Active && person_.Mood.Get(Moods.Excited) <= s.Maximum);

				string line = "  ";
				if (!active)
					line += "(";

				line += $"{s}";
				line += $" rate={s.Rate} mod={s.Modifier} max={s.Maximum}";

				string parts = "";

				for (int j = 0; j < BP.Count; ++j)
				{
					if (s.IsStrictlyActive(j))
					{
						if (parts != "")
							parts += ", ";

						parts += DebugMakeSource(s, j);
					}
				}

				if (s.IsStrictlyActive(BP.None))
				{
					if (parts != "")
						parts += ", ";

					parts += DebugMakeSource(s, BP.None);
				}

				if (parts != "")
					line += ", parts: " + parts;

				if (!active)
					line += ")";

				debug.Add(line);
			}
		}

		public string[] Debug()
		{
			var debug = new List<string>();

			string damp = "";
			if (person_.Status.Zone(SS.Penetration).Active)
				damp = $" (dampened, mod={person_.Personality.Get(PS.PenetrationDamper)})";

			debug.Add($"penetration:");
			DebugZone(SS.Penetration, debug);

			debug.Add($"mouth:{damp}");
			DebugZone(SS.Mouth, debug);

			debug.Add($"breasts:{damp}");
			DebugZone(SS.Breasts, debug);

			debug.Add($"genitals:{damp}");
			DebugZone(SS.Genitals, debug);

			debug.Add($"others excitement:");
			for (int i = 0; i < others_.Length; ++i)
			{
				var o = others_[i];
				if (o.rate > 0)
				{
					if (o.active)
						debug.Add($"  {Cue.Instance.GetPerson(i).ID} rate={o.rate} mod={o.mod} max={o.max}");
					else
						debug.Add($"  ({Cue.Instance.GetPerson(i).ID} rate={o.rate} mod={o.mod} max={o.max})");
				}
			}

			debug.Add("values:");
			debug.Add($"  max: {max_:0.00000}");
			debug.Add($"  physical: {physicalRate_.Value:0.00000}");
			debug.Add($"  emotional: {emotionalRate_.Value:0.00000}");

			if (subtotalRate_ != totalRate_)
				debug.Add($"  subtotal: {subtotalRate_:0.00000}");

			if (person_.Personality.Get(PS.RateAdjustment) != 1)
				debug.Add($"  rate adjustement: {person_.Personality.Get(PS.RateAdjustment)}");

			if (totalRate_ < 0)
				debug.Add($"  total: {totalRate_:0.00000} (decaying)");
			else
				debug.Add($"  total: {totalRate_:0.00000} (rising)");

			return debug.ToArray();
		}
	}
}
