using System;
using System.Collections.Generic;

namespace Cue
{
	class Body
	{
		public struct PartResult
		{
			public int ownBodyPart;
			public int byBodyPart;
			public int byObjectIndex;

			public PartResult(int ownBodyPart, int byObjectIndex, int byBodyPart)
			{
				this.ownBodyPart = ownBodyPart;
				this.byObjectIndex = byObjectIndex;
				this.byBodyPart = byBodyPart;
			}

			public static PartResult None
			{
				get { return new PartResult(-1, -1, -1); }
			}

			public bool Valid
			{
				get { return (ownBodyPart != -1); }
			}

			public override string ToString()
			{
				string s = "";

				s +=
					$"{BP.ToString(ownBodyPart)} by " +
					$"{Cue.Instance.GetObject(byObjectIndex)?.ID ?? "?"}" +
					$"." +
					$"{BP.ToString(byBodyPart)}";

				return s;
			}

			public static implicit operator bool(PartResult pr)
			{
				return pr.Valid;
			}
		}


		public const int CloseDelay = 2;
		private const float MaxMorphs = 1.0f;

		private Person person_;
		private readonly BodyPart[] all_;
		private Hand leftHand_, rightHand_;
		private DampedFloat temperature_;
		private int renderingParts_ = 0;
		private float[] morphsRemaining_ = new float[BP.Count];

		public Body(Person p)
		{
			person_ = p;
			temperature_ = new DampedFloat(OnTemperatureChanged);

			var parts = p.Atom.Body.GetBodyParts();
			var all = new List<BodyPart>();

			for (int i = 0; i < BP.Count; ++i)
			{
				if (parts[i] != null && parts[i].Type != i)
					Cue.LogError($"mismatched body part type {parts[i].Type} {i}");

				all.Add(new BodyPart(person_, i, parts[i]));
			}

			all_ = all.ToArray();

			leftHand_ = new Hand(p, "left", p.Atom.Body.GetLeftHand(), BP.LeftHand);
			rightHand_ = new Hand(p, "right", p.Atom.Body.GetRightHand(), BP.RightHand);
		}

		public void Init()
		{
		}

		public BodyPart[] Parts
		{
			get { return all_; }
		}

		public int RenderingParts
		{
			get { return renderingParts_; }
			set { renderingParts_ = value; }
		}

		public bool Exists
		{
			get { return person_.Atom.Body.Exists; }
		}

		public bool HasPenis
		{
			get
			{
				return Get(BP.Penis).Exists;
			}
		}

		public int GenitalsBodyPart
		{
			get
			{
				if (HasPenis)
					return BP.Penis;
				else
					return BP.Labia;
			}
		}

		public bool Strapon
		{
			get { return person_.Atom.Body.Strapon; }
			set { person_.Atom.Body.Strapon = value; }
		}

		public void DebugAllLocks(List<string> list)
		{
			list.Clear();

			for (int i = 0; i < all_.Length; ++i)
				all_[i].DebugAllLocks(list);
		}

		public BodyPartLock[] LockMany(
			int[] bodyParts, int lockType, string why, bool strong = true)
		{
			List<BodyPartLock> list = null;
			bool failed = false;

			ulong key = BodyPartLock.NextKey();

			for (int i = 0; i < bodyParts.Length; ++i)
			{
				var lk = Get(bodyParts[i]).LockInternal(lockType, why, strong, key);
				if (lk == null)
				{
					failed = true;
					break;
				}

				if (list == null)
					list = new List<BodyPartLock>();

				list.Add(lk);
			}

			if (failed)
			{
				if (list != null)
				{
					for (int i = 0; i < list.Count; ++i)
						list[i].Unlock();
				}

				return null;
			}

			return list.ToArray();
		}

		public bool AnyInsidePersonalSpace()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				if (InsidePersonalSpace(p))
					return true;
			}

			return false;
		}

		public bool InsidePersonalSpace(Person other)
		{
			var checkParts = BodyParts.PersonalSpaceParts;

			for (int i = 0; i < checkParts.Length; ++i)
			{
				var a = person_.Body.Get(checkParts[i]);

				for (int j = 0; j < checkParts.Length; ++j)
				{
					var b = other.Body.Get(checkParts[j]);
					if (a.CloseTo(b))
						return true;
				}
			}

			return false;
		}

		public bool InteractingWith(Person other)
		{
			if (InsidePersonalSpace(other) || PenetratedBy(other) || GropedBy(other))
				return true;

			// special case for unpossessed, because it's just the camera and
			// the mouse pointer grab is not handled
			if (!other.Body.Exists && other.IsPlayer)
			{
				for (int i = 0; i < all_.Length; ++i)
				{
					if (all_[i].GrabbedByPlayer)
						return true;
				}
			}

			return false;
		}

		public bool Groped()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (GropedBy(p, BodyParts.GropedParts))
					return true;
			}

			return false;
		}

		public Person PenetratedBy()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (PenetratedBy(p))
					return p;
			}

			return null;
		}

		public bool Penetrated()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (PenetratedBy(p))
					return true;
			}

			if (Get(BP.Vagina).Triggered)
				return true;

			return false;
		}

		public PartResult GropedByAny(int triggerBodyPart)
		{
			return GropedByAny(new int[] { triggerBodyPart });
		}

		public PartResult GropedByAny(int[] triggerBodyParts)
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				var pr = GropedBy(p, triggerBodyParts);
				if (pr.Valid)
					return pr;
			}

			return PartResult.None;
		}

		public PartResult GropedBy(Person p)
		{
			return GropedBy(p, BodyParts.GropedParts);
		}

		public PartResult GropedBy(Person p, int triggerBodyPart)
		{
			return GropedBy(p, new int[] { triggerBodyPart });
		}

		public PartResult GropedBy(Person p, int[] triggerBodyParts)
		{
			if (p == person_)
				return PartResult.None;

			return CheckParts(p, triggerBodyParts, BodyParts.GropedByParts);
		}

		public bool PenetratedBy(Person p)
		{
			if (p == person_)
				return false;

			return CheckParts(
				p, BodyParts.PenetratedParts, BodyParts.PenetratedByParts);
		}

		public bool HavingSexWith(Person p)
		{
			return PenetratedBy(p) || p.Body.PenetratedBy(person_);
		}

		private PartResult CheckParts(Person by, int[] triggerParts, int[] checkParts)
		{
			for (int i = 0; i < triggerParts.Length; ++i)
			{
				var triggerPart = Get(triggerParts[i]);

				for (int j = 0; j < checkParts.Length; ++j)
				{
					var byPart = by.Body.Get(checkParts[j]);

					if (triggerPart.CanTrigger)
					{
						var pr = TriggeredBy(triggerPart, byPart);
						if (pr.Valid)
							return pr;
					}
					else
					{
						if (triggerPart.CloseTo(byPart))
						{
							return new PartResult(
								triggerPart.Type, by.ObjectIndex, byPart.Type);
						}
					}
				}
			}

			return PartResult.None;
		}

		private PartResult TriggeredBy(BodyPart p, BodyPart by)
		{
			if (!p.Exists || !by.Exists)
				return PartResult.None;

			var ts = p.GetTriggers();

			if (ts != null)
			{
				for (int i = 0; i < ts.Length; ++i)
				{
					if (ts[i].sourcePartIndex >= 0)
					{
						var pp = Cue.Instance.GetPerson(ts[i].personIndex);
						var bp = pp.Body.Get(ts[i].sourcePartIndex);

						if (bp == by)
						{
							return new PartResult(
								p.Type,
								pp.ObjectIndex, ts[i].sourcePartIndex);
						}
					}
				}
			}

			return PartResult.None;
		}

		public float Temperature
		{
			get { return temperature_.Target; }
		}

		public DampedFloat DampedTemperature
		{
			get { return temperature_; }
		}

		public BodyPart Get(int type)
		{
			if (type < 0 || type >= all_.Length)
			{
				Cue.LogError($"bad part type {type}");
				return null;
			}

			return all_[type];
		}

		public Hand LeftHand
		{
			get { return leftHand_; }
		}

		public Hand RightHand
		{
			get { return rightHand_; }
		}

		public Box TopBox
		{
			get
			{
				Vector3 topPos = person_.EyeInterest + new Vector3(0, 0.2f, 0);
				Vector3 bottomPos;

				var hips = Get(BP.Hips);

				// this happens for the camera pseudo-person
				if (hips.Exists)
					bottomPos = hips.Position;
				else
					bottomPos = topPos - new Vector3(0, 0.5f, 0);

				return new Box(
					bottomPos + (topPos - bottomPos) / 2,
					new Vector3(0.5f, (topPos - bottomPos).Y, 0.5f));
			}
		}

		public Box FullBox
		{
			get
			{
				var avoidHead = Get(BP.Head).Position + new Vector3(0, 0.2f, 0);
				var avoidFeet = person_.Position;

				var b = new Box(
					avoidFeet + (avoidHead - avoidFeet) / 2,
					new Vector3(0.5f, (avoidHead - avoidFeet).Y, 0.35f));

				b.center.Y -= b.size.Y / 2;

				return b;
			}
		}

		public Frustum ClosenessFrustum
		{
			get
			{
				var f = new Frustum(
					new Vector3(0.45f, 2, 0.3f),
					new Vector3(0.2f, 2, 0.4f));

				return f;
			}
		}

		public void ResetMorphLimits()
		{
			for (int i = 0; i < morphsRemaining_.Length; ++i)
				morphsRemaining_[i] = 1.1f;
		}

		public float UseMorphs(int[] bodyParts, float use)
		{
			if (bodyParts == null || bodyParts.Length == 0)
				return use;

			float smallestAv = float.MaxValue;

			for (int i = 0; i < bodyParts.Length; ++i)
			{
				var bp = bodyParts[i];
				if (bp != BP.None && morphsRemaining_[bp] < smallestAv)
					smallestAv = morphsRemaining_[bp];
			}

			float av = Math.Min(use, smallestAv);

			for (int i = 0; i < bodyParts.Length; ++i)
			{
				var bp = bodyParts[i];
				if (bp != BP.None)
					morphsRemaining_[bp] -= av;
			}

			return av;
		}

		public void Update(float s)
		{
			var ps = person_.Personality;

			temperature_.UpRate = person_.Mood.Get(Moods.Excited) * ps.Get(PS.TemperatureExcitementRate);
			temperature_.DownRate = ps.Get(PS.TemperatureDecayRate);

			temperature_.Target = U.Clamp(
				person_.Mood.Get(Moods.Excited) / ps.Get(PS.TemperatureExcitementMax),
				0, 1);

			temperature_.Update(s);

			person_.Breathing.Intensity = person_.Mood.MovementEnergy;

			if (renderingParts_ > 0)
			{
				for (int i = 0; i < all_.Length; ++i)
					all_[i].UpdateRender();
			}
		}

		private void OnTemperatureChanged(float f)
		{
			var ps = person_.Personality;

			person_.Atom.Body.Sweat = f * ps.Get(PS.MaxSweat);
			person_.Atom.Body.Flush = f * ps.Get(PS.MaxFlush);
			person_.Atom.Hair.Loose = f;
		}
	}
}
