using System;

namespace Cue
{
	public class ErogenousZones
	{
		private const float UpdateInterval = 0.5f;

		private Person person_;
		private ErogenousZone[] zones_ = new ErogenousZone[SS.Count];
		private float elapsed_ = 0;

		public ErogenousZones(Person p)
		{
			person_ = p;
		}

		public ErogenousZone Get(int i)
		{
			return zones_[i];
		}

		public void Init()
		{
			zones_[SS.Penetration] = new ErogenousZone(
				person_, SS.Penetration, new ErogenousZone.Part[]
				{
					new ErogenousZone.Part(BP.Labia, BP.Penis),
					new ErogenousZone.Part(BP.Vagina),
					new ErogenousZone.Part(BP.DeepVagina),
					new ErogenousZone.Part(BP.DeeperVagina),
					new ErogenousZone.Part(BP.Penis, BP.Labia),
					new ErogenousZone.Part(BP.Penis, BP.DeepVagina),
					new ErogenousZone.Part(BP.Penis, BP.DeeperVagina),
				});

			zones_[SS.Mouth] = new ErogenousZone(
				person_, SS.Mouth, new ErogenousZone.Part[]
				{
					new ErogenousZone.Part(BP.Lips),
					new ErogenousZone.Part(BP.Mouth)
				});

			zones_[SS.Breasts] = new ErogenousZone(
				person_, SS.Breasts, new ErogenousZone.Part[]
				{
					new ErogenousZone.Part(BP.LeftBreast),
					new ErogenousZone.Part(BP.RightBreast)
				});

			zones_[SS.Genitals] = new ErogenousZone(
				person_, SS.Genitals, new ErogenousZone.Part[]
				{
					new ErogenousZone.Part(BP.Labia),
					new ErogenousZone.Part(BP.Penis)
				});
		}

		public void Update(float s)
		{
			elapsed_ += s;
			if (elapsed_ < UpdateInterval)
				return;

			elapsed_ = 0;

			for (int i = 0; i < zones_.Length; ++i)
			{
				if (zones_[i] != null)
					zones_[i].Update(s);
			}

			int[] ignore = new int[]
			{
				BP.Penis, BP.Labia, BP.Vagina, BP.DeepVagina, BP.DeeperVagina
			};

			for (int j = 0; j < zones_[SS.Penetration].Sources.Length; ++j)
			{
				bool ignoreAll = false;

				for (int i = 0; i < ignore.Length; ++i)
				{
					int targetBodyPart = ignore[i];

					if (zones_[SS.Penetration].Sources[j].IsAnyActiveForTarget(targetBodyPart))
					{
						ignoreAll = true;
						break;
					}
				}

				if (ignoreAll)
				{
					for (int i = 0; i < ignore.Length; ++i)
					{
						zones_[SS.Genitals].Ignore(j, ignore[i]);
					}
				}
			}
		}
	}


	public class ErogenousZoneSource
	{
		class Part
		{
			public int bodyPart;
			public bool active = false;
			public float elapsed = 0;
			public bool ignored = false;
			public int targetBodyPart = BP.None;

			public Part(int i)
			{
				bodyPart = i;
			}
		}

		private int totalActive_ = 0;
		private int validActive_ = 0;
		private readonly int personIndex_;
		private readonly Part[] parts_ = new Part[BP.Count + 1];

		private float rate_ = 0;
		private float mod_ = 0;
		private float max_ = 0;

		public ErogenousZoneSource(int personIndex)
		{
			personIndex_ = personIndex;

			for (int i = 0; i < parts_.Length; ++i)
				parts_[i] = new Part(i);
		}

		public int PersonIndex
		{
			get { return personIndex_; }
		}

		public bool Active
		{
			get { return (validActive_ > 0); }
		}

		public int StrictlyActiveCount
		{
			get { return totalActive_; }
		}

		public float Rate
		{
			get { return rate_; }
		}

		public float Modifier
		{
			get { return mod_; }
		}

		public float Maximum
		{
			get { return max_; }
		}

		private Part GetPart(int bodyPart)
		{
			if (bodyPart == BP.None)
				return parts_[parts_.Length - 1];
			else
				return parts_[bodyPart];
		}

		public bool IsActive(int bodyPart)
		{
			return IsStrictlyActive(bodyPart) && !IsIgnored(bodyPart);
		}

		public bool IsStrictlyActive(int bodyPart)
		{
			return GetPart(bodyPart).active;
		}

		public bool IsIgnored(int bodyPart)
		{
			return GetPart(bodyPart).ignored;
		}

		public int TargetBodyPart(int bodyPart)
		{
			return GetPart(bodyPart).targetBodyPart;
		}

		public bool IsAnyActiveForTarget(int targetBodyPart)
		{
			for (int i = 0; i < parts_.Length; ++i)
			{
				if (IsActive(i) && parts_[i].targetBodyPart == targetBodyPart)
					return true;
			}

			return false;
		}

		public void IgnoreTarget(int targetBodyPart)
		{
			for (int i = 0; i < parts_.Length; ++i)
			{
				if (IsActive(i) && parts_[i].targetBodyPart == targetBodyPart)
					Ignore(i);
			}
		}

		public void Decay(float s)
		{
			totalActive_ = 0;
			validActive_ = 0;

			for (int i = 0; i < parts_.Length; ++i)
			{
				DoDecay(s, parts_[i]);
				parts_[i].ignored = false;
			}
		}

		public void Check(Person p, int sensitivityIndex)
		{
			rate_ = 0;
			mod_ = 0;
			max_ = 0;

			if (Active)
			{
				var ss = p.Personality.Sensitivities.Get(sensitivityIndex);

				rate_ = ss.Rate;
				mod_ = ss.GetModifier(p, personIndex_);
				max_ = ss.Maximum;
			}
		}

		private void DoDecay(float s, Part part)
		{
			// todo, decay speed
			part.elapsed = Math.Max(0, part.elapsed - (s / 2));
			part.active = (part.elapsed > 0);

			if (part.active)
			{
				++totalActive_;
				++validActive_;
			}
		}

		public void Set(int sourceBodyPart, int targetBodyPart)
		{
			var p = GetPart(sourceBodyPart);

			p.elapsed = 1.0f;
			p.ignored = false;
			p.targetBodyPart = targetBodyPart;

			if (!p.active)
			{
				p.active = true;
				++totalActive_;
				++validActive_;
			}
		}

		public void Ignore(int bodyPart)
		{
			var p = GetPart(bodyPart);

			if (!p.ignored)
			{
				p.ignored = true;
				if (p.active)
					--validActive_;
			}
		}

		public override string ToString()
		{
			if (personIndex_ == -1)
				return $"external";

			var p = Cue.Instance.GetPerson(personIndex_);
			if (p == null)
				return $"?{personIndex_}";

			string s = p.ID;

			if (p.IsPlayer)
				s += "(player)";

			return s;
		}
	}


	public class ErogenousZone
	{
		public struct Part
		{
			public int target;
			public int source;

			public Part(int target, int source = BP.None)
			{
				this.source = source;
				this.target = target;
			}
		}

		private Person person_;
		private int type_;
		private Part[] parts_;
		private ErogenousZoneSource[] sources_;
		private int activeSources_ = 0;

		public ErogenousZone(Person p, int type, Part[] bodyParts)
		{
			person_ = p;
			type_ = type;
			parts_ = bodyParts;

			// include an external one at the end
			sources_ = new ErogenousZoneSource[Cue.Instance.ActivePersons.Length + 1];

			for (int i = 0; i < Cue.Instance.ActivePersons.Length; ++i)
				sources_[i] = new ErogenousZoneSource(i);

			sources_[sources_.Length - 1] = new ErogenousZoneSource(-1);
		}

		public bool Active
		{
			get { return (activeSources_ > 0); }
		}

		public int ActiveSources
		{
			get { return activeSources_; }
		}

		public ErogenousZoneSource[] Sources
		{
			get { return sources_; }
		}

		private ErogenousZoneSource External
		{
			get { return sources_[sources_.Length - 1]; }
		}

		public void Update(float s)
		{
			Decay(s);

			for (int i = 0; i < parts_.Length; ++i)
			{
				var bp = parts_[i];
				var ts = person_.Body.Get(bp.target).GetTriggers();

				if (ts != null)
					CheckTriggers(ts, bp.target, bp.source);
			}

			activeSources_ = 0;
			for (int i = 0; i < sources_.Length; ++i)
			{
				sources_[i].Check(person_, type_);

				if (sources_[i].Active)
					++activeSources_;
			}
		}

		public void Ignore(int source, int bodyPart)
		{
			bool wasActive = sources_[source].Active;
			sources_[source].IgnoreTarget(bodyPart);

			if (wasActive && !sources_[source].Active)
				--activeSources_;
		}

		private void Decay(float s)
		{
			activeSources_ = 0;

			for (int i = 0; i < sources_.Length; ++i)
			{
				sources_[i].Decay(s);
				if (sources_[i].Active)
					++activeSources_;
			}
		}

		private void CheckTriggers(
			Sys.TriggerInfo[] ts, int targetBodyPart, int sourceCheck)
		{
			for (int i = 0; i < ts.Length; ++i)
			{
				var t = ts[i];

				if (sourceCheck == BP.None || sourceCheck == t.sourcePartIndex)
				{
					if (t.IsPerson())
						sources_[t.personIndex].Set(t.sourcePartIndex, targetBodyPart);
					else
						External.Set(-1, targetBodyPart);
				}
			}
		}
	}
}
