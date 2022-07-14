using System;
using System.Collections.Generic;

namespace Cue
{
	public abstract class ExcitementSource
	{
		protected readonly Person person_;
		private int enabledForOthers_ = 0;
		private int enabledForPlayer_ = 0;
		private int enabledForExternal_ = 0;

		protected ExcitementSource(Person p)
		{
			person_ = p;
		}

		public virtual bool Physical
		{
			get { return true; }
		}

		public bool EnabledForOthers
		{
			get { return true || (enabledForOthers_ > 0); }
		}

		public bool EnabledForPlayer
		{
			get { return true || (enabledForPlayer_ > 0); }
		}

		public bool EnabledForToys
		{
			get { return true; }
		}

		public bool EnabledForExternal
		{
			get { return (enabledForExternal_ > 0); }
		}

		public virtual bool AllowDecay
		{
			get { return true; }
		}

		public void AddEnabledFor(Person p)
		{
			if (p == null)
				AddEnabledForExternal();
			else if (p.IsPlayer)
				AddEnabledForPlayer();
			else
				AddEnabledForOthers();
		}

		public void RemoveEnabledFor(Person p)
		{
			if (p == null)
				RemoveEnabledForExternal();
			else if (p.IsPlayer)
				RemoveEnabledForPlayer();
			else
				RemoveEnabledForOthers();
		}

		public void ForceEnabledForOthers()
		{
			AddEnabledForOthers();
		}

		public void ForceEnabledForPlayer()
		{
			AddEnabledForPlayer();
		}


		private void AddEnabledForOthers()
		{
			++enabledForOthers_;
		}

		private void RemoveEnabledForOthers()
		{
			Cue.Assert(enabledForOthers_ > 0);
			enabledForOthers_ = Math.Max(enabledForOthers_ - 1, 0);
		}

		private void AddEnabledForPlayer()
		{
			++enabledForPlayer_;
		}

		private void RemoveEnabledForPlayer()
		{
			Cue.Assert(enabledForPlayer_ > 0);
			enabledForPlayer_ = Math.Max(enabledForPlayer_ - 1, 0);
		}

		private void AddEnabledForExternal()
		{
			++enabledForExternal_;
		}

		private void RemoveEnabledForExternal()
		{
			Cue.Assert(enabledForExternal_ > 0);
			enabledForExternal_ = Math.Max(enabledForExternal_ - 1, 0);
		}


		public virtual void Update()
		{
			// no-op
		}

		public abstract float GetMaximum();
		public abstract float GetRate(float penDampen);

		public abstract void Debug(DebugLines debug);
	}


	public class ZoneExcitementSource : ExcitementSource
	{
		private ZoneType zone_;

		public ZoneExcitementSource(Person p, ZoneType ss)
			: base(p)
		{
			zone_ = ss;
		}

		public override bool AllowDecay
		{
			get
			{
				// don't allow decay if something is active, even if its mag is
				// too low

				var z = person_.Body.Zone(zone_);

				if (z != null)
				{
					for (int i = 0; i < z.Sources.Length; ++i)
					{
						if (z.Sources[i].StrictlyActiveCount > 0)
							return false;
					}
				}

				return true;
			}
		}

		public override float GetMaximum()
		{
			var z = person_.Body.Zone(zone_);
			if (z == null)
				return 0;

			float max = 0;

			for (int j = 0; j < z.Sources.Length; ++j)
			{
				var src = z.Sources[j];

				if (src.Active)
					max = Math.Max(max, src.Maximum);
			}

			return max;
		}

		public override float GetRate(float penDampen)
		{
			var z = person_.Body.Zone(zone_);
			if (z == null)
				return 0;

			float rate = 0;

			for (int j = 0; j < z.Sources.Length; ++j)
			{
				var src = z.Sources[j];

				if (UsableSource(src))
				{
					float thisRate = src.Rate * src.Modifier;

					if (zone_ != SS.Penetration)
						thisRate *= penDampen;

					rate += thisRate;
				}
			}

			return rate;
		}

		public bool UsableSource(ErogenousZoneSource s)
		{
			if (s.Active && (person_.Mood.Get(MoodType.Excited) <= s.Maximum))
			{
				switch (s.Type)
				{
					case Sys.TriggerInfo.PersonType:
					{
						if (EnabledForPlayer && s.IsPlayer)
							return true;

						if (EnabledForOthers && !s.IsPlayer)
							return true;

						return false;
					}

					case Sys.TriggerInfo.ToyType:
					{
						return EnabledForToys;
					}

					case Sys.TriggerInfo.NoneType:
					default:
					{
						return EnabledForExternal;
					}
				}
			}

			return false;
		}

		public override void Debug(DebugLines debug)
		{
			string damp = "";
			if (person_.Excitement.NeedsPenetrationDamper() && zone_ != SS.Penetration)
				damp = $" (damp={person_.Personality.Get(PS.PenetrationDamper):0.00})";

			string enabled = "";

			if (EnabledForPlayer)
				enabled += "player|";

			if (EnabledForOthers)
				enabled += "other|";

			if (EnabledForToys)
				enabled += "toys|";

			if (EnabledForExternal)
				enabled += "external|";

			if (enabled.Length == 0)
				enabled = "enabled:none";
			else
				enabled = "enabled:" + enabled.Substring(0, enabled.Length - 1);

			debug.Add($"{ZoneType.ToString(zone_)}:{damp} {enabled}");
			DebugZone(zone_, debug);
		}

		private void DebugZone(ZoneType zoneType, DebugLines debug)
		{
			var z = person_.Body.Zone(zoneType);
			var srcs = z.Sources;

			for (int i = 0; i < srcs.Length; ++i)
			{
				var s = srcs[i];
				if (s.StrictlyActiveCount == 0)
					continue;

				bool usable = UsableSource(s) && !person_.IsPlayer;

				string line = "  ";
				if (!usable)
					line += "(";

				line += $"{s}";
				line += $" rate={s.Rate} mag={s.Magnitude:0.00} mod={s.Modifier} max={s.Maximum}";

				string parts = "";

				foreach (BodyPartType j in BodyPartType.Values)
				{
					if (s.IsStrictlyActive(j))
					{
						if (parts != "")
							parts += ", ";

						parts += DebugMakePart(s, j);
					}
				}

				if (s.IsStrictlyActive(BP.None))
				{
					if (parts != "")
						parts += ", ";

					parts += DebugMakePart(s, BP.None);
				}

				if (parts != "")
					line += ", parts: " + parts;

				if (s.Active && !s.IsPhysical)
					line += $" (not physical)";

				if (!usable)
					line += ")";

				debug.Add(line);
			}
		}

		private string DebugMakePart(ErogenousZoneSource src, BodyPartType part)
		{
			string s = "";

			if (part == BP.None)
				s += "unknown";
			else
				s += BodyPartType.ToString(part);

			s += "=>";

			if (src.TargetBodyPart(part) == BP.None)
				s += "unknown";
			else
				s += BodyPartType.ToString(src.TargetBodyPart(part));

			return s;
		}
	}

	public class OthersExcitementSource : ExcitementSource
	{
		class Other
		{
			public float rate;
			public float mod;
			public float max;
			public bool active;
		}

		private Other[] others_;

		public OthersExcitementSource(Person p)
			: base(p)
		{
			others_ = new Other[Cue.Instance.ActivePersons.Length];
			for (int i = 0; i < others_.Length; ++i)
				others_[i] = new Other();
		}

		public override bool Physical
		{
			get { return false; }
		}

		public override void Update()
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
				if (p == person_)
					continue;

				var ss = person_.Personality.Sensitivities.Get(SS.OthersExcitement);
				var o = others_[p.PersonIndex];

				o.rate = p.Excitement.CalculatePhysicalRate();
				o.mod = ss.PhysicalRate;
				o.max = ss.PhysicalMaximum;

				if (person_.Mood.Get(MoodType.Excited) <= o.max)
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

		public override float GetMaximum()
		{
			float max = 0;

			for (int i = 0; i < others_.Length; ++i)
				max = Math.Max(max, others_[i].max);

			return max;
		}

		public override float GetRate(float penDampen)
		{
			float rate = 0;

			for (int i = 0; i < others_.Length; ++i)
			{
				if (others_[i].active)
					rate += others_[i].rate * others_[i].mod;
			}

			return rate;
		}

		public override void Debug(DebugLines debug)
		{
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
		}
	}


	public class Excitement
	{
		private const float UpdateInterval = 1.0f;

		private Person person_;
		private ExcitementSource[] sources_ = new ExcitementSource[SS.Count];
		private ForceableFloat physicalRate_ = new ForceableFloat();
		private ForceableFloat emotionalRate_ = new ForceableFloat();
		private float subtotalRate_ = 0;
		private float totalRate_ = 0;
		private float max_ = 0;
		private float elapsed_ = 0;
		private DebugLines debug_ = null;

		public Excitement(Person p)
		{
			person_ = p;
		}

		public void Init()
		{
			foreach (ZoneType z in ZoneType.Values)
			{
				if (z == SS.OthersExcitement)
					sources_[z.Int] = new OthersExcitementSource(person_);
				else
					sources_[z.Int] = new ZoneExcitementSource(person_, z);
			}

			GetSource(SS.Mouth).ForceEnabledForOthers();
			GetSource(SS.Breasts).ForceEnabledForOthers();

			GetSource(SS.Mouth).ForceEnabledForPlayer();
			GetSource(SS.Breasts).ForceEnabledForPlayer();
			GetSource(SS.Genitals).ForceEnabledForPlayer();
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

		public bool NeedsPenetrationDamper()
		{
			var z = person_.Body.Zone(SS.Penetration);

			if (z.Active)
			{
				var pen = GetSource(SS.Penetration) as ZoneExcitementSource;

				for (int i = 0; i < z.Sources.Length; ++i)
				{
					var s = z.Sources[i];

					if (pen.UsableSource(s))
						return true;
				}
			}

			return false;
		}

		public ExcitementSource GetSource(ZoneType ss)
		{
			return sources_[ss.Int];
		}

		public void Update(float s)
		{
			elapsed_ += s;

			if (elapsed_ >= UpdateInterval)
			{
				elapsed_ = 0;

				for (int i = 0; i < sources_.Length; ++i)
					sources_[i].Update();

				physicalRate_.Value = CalculatePhysicalRate();
				emotionalRate_.Value = GetEmotionalRate();
				max_ = GetMaximum();

				subtotalRate_ = physicalRate_.Value + emotionalRate_.Value;
				totalRate_ = subtotalRate_ * person_.Personality.Get(PS.RateAdjustment);

				if (totalRate_ == 0)
				{
					bool allowDecay = true;

					for (int i = 0; i < sources_.Length; ++i)
					{
						if (!sources_[i].AllowDecay)
						{
							allowDecay = false;
							break;
						}
					}

					if (allowDecay)
						totalRate_ = person_.Personality.Get(PS.ExcitementDecayRate);
				}
			}
		}

		private float GetMaximum()
		{
			float max = 0;
			for (int i=0; i<sources_.Length; ++i)
				max = Math.Max(max, sources_[i].GetMaximum());

			return max;
		}

		public float CalculatePhysicalRate()
		{
			float rate = 0;

			float dampen = 1;
			if (NeedsPenetrationDamper())
				dampen = person_.Personality.Get(PS.PenetrationDamper);

			for (int i = 0; i < sources_.Length;++i)
			{
				if (!sources_[i].Physical)
					continue;

				rate += sources_[i].GetRate(dampen);
			}

			return rate;
		}

		private float GetEmotionalRate()
		{
			float rate = 0;

			float dampen = 1;
			if (NeedsPenetrationDamper())
				dampen = person_.Personality.Get(PS.PenetrationDamper);

			for (int i = 0; i < sources_.Length; ++i)
			{
				if (sources_[i].Physical)
					continue;

				rate += sources_[i].GetRate(dampen);
			}

			return rate;
		}


		public string[] Debug()
		{
			if (debug_ == null)
				debug_ = new DebugLines();

			debug_.Clear();

			if (person_.IsPlayer)
			{
				debug_.Add(">>>> excitement is disabled for player");
				debug_.Add("");
			}

			for (int i = 0; i < sources_.Length; ++i)
				sources_[i].Debug(debug_);

			debug_.Add("values:");
			debug_.Add($"  max: {max_:0.00000}");
			debug_.Add($"  physical: {physicalRate_.Value:0.00000}");
			debug_.Add($"  emotional: {emotionalRate_.Value:0.00000}");

			if (subtotalRate_ != totalRate_)
				debug_.Add($"  subtotal: {subtotalRate_:0.00000}");

			if (person_.Personality.Get(PS.RateAdjustment) != 1)
				debug_.Add($"  rate adjustement: {person_.Personality.Get(PS.RateAdjustment)}");

			if (totalRate_ < 0)
				debug_.Add($"  total: {totalRate_:0.00000} (decaying)");
			else if (totalRate_ == 0)
				debug_.Add($"  total: {totalRate_:0.00000} (stable)");
			else
				debug_.Add($"  total: {totalRate_:0.00000} (rising)");

			return debug_.MakeArray();
		}
	}
}
