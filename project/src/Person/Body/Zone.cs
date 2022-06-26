﻿using System;

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
			for (int i = 0; i < zones_.Length; ++i)
			{
				if (zones_[i] != null)
					zones_[i].Decay(s);
			}


			elapsed_ += s;
			if (elapsed_ < UpdateInterval)
				return;

			elapsed_ = 0;

			for (int i = 0; i < zones_.Length; ++i)
			{
				if (zones_[i] != null)
					zones_[i].Update();
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

		private const int ToyPartIndex = BP.Count;
		private const int ExternalPartIndex = BP.Count + 1;

		private int totalActive_ = 0;
		private int validActive_ = 0;
		private int physicalActive_ = 0;

		private Person person_;
		private int type_;  // Sys.TriggerInfo types
		private readonly int sourcePersonIndex_;
		private readonly Part[] parts_ = new Part[BP.Count + 2];

		private float rate_ = 0;
		private float mod_ = 0;
		private float max_ = 0;

		public ErogenousZoneSource(Person p, int type, int sourcePersonIndex)
		{
			type_ = type;
			person_ = p;
			sourcePersonIndex_ = sourcePersonIndex;

			for (int i = 0; i < parts_.Length; ++i)
				parts_[i] = new Part(i);
		}

		public int PersonIndex
		{
			get { return sourcePersonIndex_; }
		}

		public int Type
		{
			get { return type_; }
		}

		public bool IsPlayer
		{
			get { return (sourcePersonIndex_ == Cue.Instance.Player.PersonIndex); }
		}

		public bool Active
		{
			get { return (validActive_ > 0); }
		}

		public bool IsPhysical
		{
			get { return (physicalActive_ > 0); }
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

		private Part GetExternalPart()
		{
			return parts_[ExternalPartIndex];
		}

		private Part GetToyPart()
		{
			return parts_[ToyPartIndex];
		}

		private Part GetPart(int bodyPart)
		{
			if (bodyPart == BP.None)
				return GetExternalPart();
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

		public float Elapsed(int bodyPart)
		{
			return GetPart(bodyPart).elapsed;
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
			physicalActive_ = 0;

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

				if (IsPhysical)
				{
					rate_ = ss.PhysicalRate;
					max_ = ss.PhysicalMaximum;
				}
				else
				{
					rate_ = ss.NonPhysicalRate;
					max_ = ss.NonPhysicalMaximum;
				}

				for (int i = 0; i < parts_.Length; ++i)
				{
					if (parts_[i].active && !parts_[i].ignored)
					{
						mod_ = Math.Max(
							mod_, ss.GetModifier(p, sourcePersonIndex_, i));
					}
				}
			}
		}

		private void DoDecay(float s, Part part)
		{
			// todo, decay speed
			part.elapsed = Math.Max(0, part.elapsed - (s / 2));
			part.active = (part.elapsed > 0);

			if (part.active)
				Activated(part);
		}

		public void SetFromPerson(int sourceBodyPart, int targetBodyPart)
		{
			SetInternal(GetPart(sourceBodyPart), targetBodyPart);
		}

		public void SetFromToy(int targetBodyPart)
		{
			SetInternal(GetToyPart(), targetBodyPart);
		}

		public void SetFromExternal(int targetBodyPart)
		{
			SetInternal(GetExternalPart(), targetBodyPart);
		}

		private void SetInternal(Part p, int targetBodyPart)
		{
			p.elapsed = 1.0f;
			p.ignored = false;
			p.targetBodyPart = targetBodyPart;

			if (!p.active)
			{
				p.active = true;
				Activated(p);
			}
		}

		public void Ignore(int bodyPart)
		{
			var p = GetPart(bodyPart);

			if (!p.ignored)
			{
				p.ignored = true;
				if (p.active)
					Ignored(p);
			}
		}

		private void Activated(Part p)
		{
			++totalActive_;
			++validActive_;

			if (p.targetBodyPart != BP.None)
			{
				if (person_.Body.Get(p.targetBodyPart).IsPhysical)
					++physicalActive_;
			}
		}

		private void Ignored(Part p)
		{
			--validActive_;

			if (p.targetBodyPart != BP.None)
			{
				if (person_.Body.Get(p.targetBodyPart).IsPhysical)
					--physicalActive_;
			}

		}

		public override string ToString()
		{
			switch (type_)
			{
				case Sys.TriggerInfo.PersonType:
				{
					var p = Cue.Instance.GetPerson(sourcePersonIndex_);
					if (p == null)
						return $"?{sourcePersonIndex_}";

					string s = p.ID;

					if (p.IsPlayer)
						s += "(player)";

					return s;
				}

				case Sys.TriggerInfo.ToyType:
				{
					return "toy";
				}

				case Sys.TriggerInfo.NoneType:
				default:
				{
					return $"external";
				}
			}
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

		private int ToySourceIndex = -1;
		private int ExternalSourceIndex = -1;

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

			ToySourceIndex = Cue.Instance.ActivePersons.Length;
			ExternalSourceIndex = Cue.Instance.ActivePersons.Length + 1;

			// include a toy and external ones at the end
			sources_ = new ErogenousZoneSource[Cue.Instance.ActivePersons.Length + 2];

			for (int i = 0; i < Cue.Instance.ActivePersons.Length; ++i)
				sources_[i] = new ErogenousZoneSource(person_, Sys.TriggerInfo.PersonType, i);

			sources_[ToySourceIndex] = new ErogenousZoneSource(person_, Sys.TriggerInfo.ToyType, - 1);
			sources_[ExternalSourceIndex] = new ErogenousZoneSource(person_, Sys.TriggerInfo.NoneType, -1);
		}

		public override string ToString()
		{
			return SS.ToString(type_);
		}

		public bool Active
		{
			get { return (activeSources_ > 0); }
		}

		public int ActiveSources
		{
			get { return activeSources_; }
		}

		public BodyPart MainBodyPart
		{
			get
			{
				for (int i = 0; i < parts_.Length; ++i)
				{
					var bp = person_.Body.Get(parts_[i].target);

					if (bp.Exists)
						return bp;
				}

				return null;
			}
		}

		public ErogenousZoneSource[] Sources
		{
			get { return sources_; }
		}

		public void Update()
		{
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

		public void Decay(float s)
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

				if (sourceCheck == BP.None || sourceCheck == t.BodyPart)
				{
					switch (t.Type)
					{
						case Sys.TriggerInfo.PersonType:
						{
							sources_[t.PersonIndex].SetFromPerson(t.BodyPart, targetBodyPart);
							break;
						}

						case Sys.TriggerInfo.ToyType:
						{
							sources_[ToySourceIndex].SetFromToy(targetBodyPart);
							break;
						}

						case Sys.TriggerInfo.NoneType:
						default:
						{
							sources_[ExternalSourceIndex].SetFromExternal(targetBodyPart);
							break;
						}
					}
				}
			}
		}
	}
}
