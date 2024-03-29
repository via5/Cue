﻿using System;
using System.Collections.Generic;

namespace Cue
{
	public class ErogenousZones
	{
		private const float UpdateInterval = 0.5f;

		private Person person_;
		private ErogenousZone[] zones_ = null;
		private float elapsed_ = 0;

		public ErogenousZones(Person p)
		{
			person_ = p;
		}

		public ErogenousZone[] All
		{
			get { return zones_; }
		}

		public ErogenousZone Get(ZoneType i)
		{
			return zones_[i.Int];
		}

		public void Init()
		{
			zones_ = new ErogenousZone[4];

			zones_[SS.Penetration.Int] = new ErogenousZone(
				person_, SS.Penetration, new ErogenousZone.Part[]
				{
					new ErogenousZone.Part(BP.Vagina, ErogenousZone.Part.SourceToy),
					new ErogenousZone.Part(BP.Vagina, ErogenousZone.Part.SourceExternal),
					new ErogenousZone.Part(BP.Vagina, BP.Penis),
					new ErogenousZone.Part(BP.Penis, BP.Vagina),

					new ErogenousZone.Part(BP.Anus, ErogenousZone.Part.SourceToy),
					new ErogenousZone.Part(BP.Anus, ErogenousZone.Part.SourceExternal),
					new ErogenousZone.Part(BP.Anus, BP.Penis),
					new ErogenousZone.Part(BP.Penis, BP.Anus),
				});

			zones_[SS.Mouth.Int] = new ErogenousZone(
				person_, SS.Mouth, new ErogenousZone.Part[]
				{
					new ErogenousZone.Part(BP.Lips),
					new ErogenousZone.Part(BP.Mouth)
				});

			zones_[SS.Breasts.Int] = new ErogenousZone(
				person_, SS.Breasts, new ErogenousZone.Part[]
				{
					new ErogenousZone.Part(BP.LeftBreast),
					new ErogenousZone.Part(BP.RightBreast)
				});

			zones_[SS.Genitals.Int] = new ErogenousZone(
				person_, SS.Genitals, new ErogenousZone.Part[]
				{
					new ErogenousZone.Part(BP.Vagina),
					new ErogenousZone.Part(BP.Anus),
					new ErogenousZone.Part(BP.Penis)
				});
		}

		public void Update(float s)
		{
			Instrumentation.Start(I.ZoneDecay);
			{
				for (int i = 0; i < zones_.Length; ++i)
					zones_[i].Decay(s);
			}
			Instrumentation.End();


			elapsed_ += s;
			if (elapsed_ < UpdateInterval)
				return;
			elapsed_ = 0;

			Instrumentation.Start(I.ZoneUpdate);
			{
				for (int i = 0; i < zones_.Length; ++i)
					zones_[i].Update();
			}
			Instrumentation.End();


			Instrumentation.Start(I.ZoneIgnore);
			{
				BodyPartType[] ignore = BodyParts.GenitalParts;

				for (int j = 0; j < Get(SS.Penetration).Sources.Length; ++j)
				{
					bool ignoreAll = false;

					for (int i = 0; i < ignore.Length; ++i)
					{
						BodyPartType targetBodyPart = ignore[i];

						if (Get(SS.Penetration).Sources[j].IsAnyActiveForTarget(targetBodyPart))
						{
							ignoreAll = true;
							break;
						}
					}

					if (ignoreAll)
					{
						for (int i = 0; i < ignore.Length; ++i)
						{
							Get(SS.Genitals).Ignore(j, ignore[i]);
						}
					}
				}
			}
			Instrumentation.End();
		}
	}


	public class ErogenousZoneSource
	{
		public class Part
		{
			public BodyPartType bodyPart;
			public bool active = false;
			public float elapsed = 0;
			public float magnitude = 0;
			public bool ignored = false;
			public BodyPartType targetBodyPart = BP.None;

			public Part(BodyPartType i)
			{
				bodyPart = i;
			}
		}

		private readonly BodyPartType ToyPartIndex = BodyPartType.CreateInternal(BP.Count);
		private readonly BodyPartType ExternalPartIndex = BodyPartType.CreateInternal(BP.Count + 1);

		private List<Part> activeParts_ = new List<Part>();
		private List<Part> validParts_ = new List<Part>();
		private List<Part> physicalParts_ = new List<Part>();

		private Person person_;
		private int type_;  // Sys.TriggerInfo types
		private readonly int sourcePersonIndex_;
		private readonly float minMag_;
		private readonly Part[] parts_ = new Part[BP.Count + 2];

		private float rate_ = 0;
		private float mod_ = 0;
		private float max_ = 0;
		private float mag_ = 0;

		public ErogenousZoneSource(Person p, int type, int sourcePersonIndex, float minMag)
		{
			type_ = type;
			person_ = p;
			sourcePersonIndex_ = sourcePersonIndex;
			minMag_ = minMag;

			foreach (BodyPartType i in BodyPartType.Values)
				parts_[i.Int] = new Part(i);

			// hack
			parts_[ToyPartIndex.Int] = new Part(ToyPartIndex);
			parts_[ExternalPartIndex.Int] = new Part(ExternalPartIndex);
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
			get { return (validParts_.Count > 0); }
		}

		public bool IsPhysical
		{
			get { return (physicalParts_.Count > 0); }
		}

		public int StrictlyActiveCount
		{
			get { return activeParts_.Count; }
		}

		public float Rate
		{
			get { return rate_; }
		}

		public float Magnitude
		{
			get { return mag_; }
		}

		public float Modifier
		{
			get { return mod_; }
		}

		public float Maximum
		{
			get { return max_; }
		}

		public Part GetExternalPart()
		{
			return parts_[ExternalPartIndex.Int];
		}

		public Part GetToyPart()
		{
			return parts_[ToyPartIndex.Int];
		}

		public Part GetPart(BodyPartType bodyPart)
		{
			if (bodyPart == BP.None)
				return GetExternalPart();
			else
				return parts_[bodyPart.Int];
		}

		public bool IsActive(BodyPartType bodyPart)
		{
			return IsStrictlyActive(bodyPart) && !IsIgnored(bodyPart);
		}

		public bool IsStrictlyActive(BodyPartType bodyPart)
		{
			return GetPart(bodyPart).active;
		}

		public bool IsIgnored(BodyPartType bodyPart)
		{
			return GetPart(bodyPart).ignored;
		}

		public BodyPartType TargetBodyPart(BodyPartType bodyPart)
		{
			return GetPart(bodyPart).targetBodyPart;
		}

		public float Elapsed(BodyPartType bodyPart)
		{
			return GetPart(bodyPart).elapsed;
		}

		public bool IsAnyActiveForTarget(BodyPartType targetBodyPart)
		{
			for (int i = 0; i < parts_.Length; ++i)
			{
				if (IsActive(parts_[i].bodyPart) && parts_[i].targetBodyPart == targetBodyPart)
					return true;
			}

			return false;
		}

		public void IgnoreTarget(BodyPartType targetBodyPart)
		{
			for (int i = 0; i < parts_.Length; ++i)
			{
				if (IsActive(parts_[i].bodyPart) && parts_[i].targetBodyPart == targetBodyPart)
					Ignore(parts_[i].bodyPart);
			}
		}

		public void Decay(float s)
		{
			int i = 0;

			while (i < activeParts_.Count)
			{
				var part = activeParts_[i];

				// todo, decay speed
				part.elapsed = Math.Max(0, part.elapsed - (s * 2));
				part.active = (part.elapsed > 0);

				if (part.active)
				{
					++i;
				}
				else
				{
					part.magnitude = 0;

					activeParts_.RemoveAt(i);
					validParts_.Remove(part);

					if (person_.Body.Get(part.targetBodyPart).IsPhysical)
						physicalParts_.Remove(part);
				}
			}
		}

		public void Check(Person p, ZoneType zoneType)
		{
			rate_ = 0;
			mod_ = 0;
			max_ = 0;
			mag_ = 0;

			if (activeParts_.Count > 0)
			{
				var ss = p.Personality.Sensitivities.Get(zoneType);

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

				foreach (var i in BodyPartType.Values)
					CheckBodyPart(p, ss, i);

				CheckBodyPart(p, ss, ToyPartIndex);
				CheckBodyPart(p, ss, ExternalPartIndex);

				if (GetToyPart().active)
					mag_ = Math.Max(mag_, GetToyPart().magnitude);

				if (GetExternalPart().active)
					mag_ = Math.Max(mag_, GetExternalPart().magnitude);
			}

			if (mod_ == 0)
				mod_ = 1;
		}

		private void CheckBodyPart(Person p, Sensitivity ss, BodyPartType bp)
		{
			var part = parts_[bp.Int];

			if (bp.Int >= BP.Count)
				bp = BP.None;

			if (part.active)
			{
				mod_ = Math.Max(
					mod_, ss.GetModifier(p, type_, sourcePersonIndex_, bp));

				mag_ = Math.Max(mag_, part.magnitude);
			}
		}

		public void SetFromPerson(BodyPartType sourceBodyPart, BodyPartType targetBodyPart, float mag)
		{
			SetInternal(GetPart(sourceBodyPart), targetBodyPart, mag);
		}

		public void SetFromToy(BodyPartType targetBodyPart, float mag)
		{
			SetInternal(GetToyPart(), targetBodyPart, mag);
		}

		public void SetFromExternal(BodyPartType targetBodyPart, float mag)
		{
			SetInternal(GetExternalPart(), targetBodyPart, mag);
		}

		private void SetInternal(Part p, BodyPartType targetBodyPart, float mag)
		{
			p.targetBodyPart = targetBodyPart;
			p.magnitude = Math.Max(p.magnitude, mag);
			p.elapsed = 1.0f;
			p.ignored = (p.magnitude < minMag_);

			if (!p.active)
			{
				p.active = true;
				Activated(p);
			}
		}

		public void Ignore(BodyPartType bodyPart)
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
			activeParts_.Add(p);

			if (!p.ignored)
				validParts_.Add(p);

			if (p.targetBodyPart != BP.None)
			{
				if (person_.Body.Get(p.targetBodyPart).IsPhysical)
					physicalParts_.Add(p);
			}
		}

		private void Ignored(Part p)
		{
			validParts_.Remove(p);

			if (p.targetBodyPart != BP.None)
			{
				if (person_.Body.Get(p.targetBodyPart).IsPhysical)
					physicalParts_.Remove(p);
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
			public const int SourcePerson = 0x01;
			public const int SourceToy = 0x02;
			public const int SourceExternal = 0x04;
			public const int SourceAny = SourcePerson | SourceToy | SourceExternal;

			public BodyPartType target;
			public BodyPartType source;
			public int sourceType;

			public Part(BodyPartType target, int sourceType = SourceAny)
				: this(target, BP.None, sourceType)
			{
			}

			public Part(BodyPartType target, BodyPartType source, int sourceType = SourceAny)
			{
				this.source = source;
				this.target = target;
				this.sourceType = sourceType;
			}
		}

		public readonly int ToySourceIndex;
		public readonly int ExternalSourceIndex;

		private Person person_;
		private ZoneType type_;
		private Part[] parts_;
		private ErogenousZoneSource[] sources_;
		private int activeSources_ = 0;

		public ErogenousZone(Person p, ZoneType type, Part[] bodyParts)
		{
			person_ = p;
			type_ = type;
			parts_ = bodyParts;

			ToySourceIndex = Cue.Instance.ActivePersons.Length;
			ExternalSourceIndex = Cue.Instance.ActivePersons.Length + 1;

			float minMag = GetMinMag(type);

			// include a toy and external ones at the end
			sources_ = new ErogenousZoneSource[Cue.Instance.ActivePersons.Length + 2];

			for (int i = 0; i < Cue.Instance.ActivePersons.Length; ++i)
				sources_[i] = new ErogenousZoneSource(person_, Sys.TriggerInfo.PersonType, i, minMag);

			sources_[ToySourceIndex] = new ErogenousZoneSource(person_, Sys.TriggerInfo.ToyType, -1, minMag);
			sources_[ExternalSourceIndex] = new ErogenousZoneSource(person_, Sys.TriggerInfo.NoneType, -1, minMag);
		}

		private float GetMinMag(ZoneType type)
		{
			var ps = person_.Personality;

			if (type == SS.Penetration)
				return ps.Get(PS.MinCollisionMagPenetration);
			else
				return ps.Get(PS.MinCollisionMag);
		}

		public override string ToString()
		{
			return ZoneType.ToString(type_);
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

		public ErogenousZoneSource GetPersonSource(Person p)
		{
			return sources_[p.PersonIndex];
		}

		public ErogenousZoneSource GetToySource()
		{
			return sources_[ToySourceIndex];
		}

		public void Update()
		{
			for (int i = 0; i < parts_.Length; ++i)
			{
				var bp = parts_[i];
				var ts = person_.Body.Get(bp.target).GetTriggers();

				if (ts != null)
					CheckTriggers(ts, bp);
			}

			activeSources_ = 0;
			for (int i = 0; i < sources_.Length; ++i)
			{
				sources_[i].Check(person_, type_);

				if (sources_[i].Active)
					++activeSources_;
			}
		}

		public void Ignore(int source, BodyPartType bodyPart)
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

		private void CheckTriggers(Sys.TriggerInfo[] ts, Part part)
		{
			for (int i = 0; i < ts.Length; ++i)
			{
				var t = ts[i];

				if (part.source == BP.None || part.source == t.BodyPart)
				{
					switch (t.Type)
					{
						case Sys.TriggerInfo.PersonType:
						{
							if (Bits.IsSet(part.sourceType, Part.SourcePerson))
								sources_[t.PersonIndex].SetFromPerson(t.BodyPart, part.target, t.Magnitude);

							break;
						}

						case Sys.TriggerInfo.ToyType:
						{
							if (Bits.IsSet(part.sourceType, Part.SourceToy))
								sources_[ToySourceIndex].SetFromToy(part.target, t.Magnitude);

							break;
						}

						case Sys.TriggerInfo.NoneType:
						default:
						{
							if (Bits.IsSet(part.sourceType, Part.SourceExternal))
								sources_[ExternalSourceIndex].SetFromExternal(part.target, t.Magnitude);

							break;
						}
					}
				}
			}
		}
	}
}
