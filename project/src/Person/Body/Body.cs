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
		private const float MaxMorphs = 1.1f;

		private Person person_;
		private Logger log_;
		private readonly BodyPart[] all_;
		private Hand leftHand_, rightHand_;
		private DampedFloat temperature_;
		private int renderingParts_ = 0;
		private float[] morphsRemaining_ = new float[BP.Count];

		public Body(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Object, p, $"body");
			temperature_ = new DampedFloat();

			var parts = p.Atom.Body.GetBodyParts();
			var all = new List<BodyPart>();

			for (int i = 0; i < BP.Count; ++i)
			{
				if (parts[i] != null && parts[i].Type != i)
					Log.Error($"mismatched body part type {parts[i].Type} {i}");

				all.Add(new BodyPart(person_, i, parts[i]));
			}

			all_ = all.ToArray();

			leftHand_ = new Hand(p, "left", p.Atom.Body.GetLeftHand(), BP.LeftHand);
			rightHand_ = new Hand(p, "right", p.Atom.Body.GetRightHand(), BP.RightHand);
		}

		public Logger Log
		{
			get { return log_; }
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
			get { return Get(BP.Penis).Exists; }
		}

		public int GenitalsBodyPart
		{
			get { return (HasPenis ? BP.Penis : BP.Labia); }
		}

		public bool Strapon
		{
			get { return person_.Atom.Body.Strapon; }
			set { person_.Atom.Body.Strapon = value; }
		}

		public float Temperature
		{
			get { return temperature_.Target; }
		}

		public DampedFloat DampedTemperature
		{
			get { return temperature_; }
		}

		public Hand LeftHand
		{
			get { return leftHand_; }
		}

		public Hand RightHand
		{
			get { return rightHand_; }
		}

		public BodyPart Get(int type)
		{
			if (type < 0 || type >= all_.Length)
			{
				Log.Error($"bad part type {type}");
				return null;
			}

			return all_[type];
		}

		public void Update(float s)
		{
			for (int i = 0; i < all_.Length; ++i)
				all_[i].Update(s);

			var ps = person_.Personality;

			temperature_.UpRate = person_.Mood.Get(Moods.Excited) * ps.Get(PS.TemperatureExcitementRate);
			temperature_.DownRate = ps.Get(PS.TemperatureDecayRate);

			temperature_.Target = U.Clamp(
				person_.Mood.Get(Moods.Excited) / ps.Get(PS.TemperatureExcitementMax),
				0, 1);

			if (temperature_.Update(s))
			{
				person_.Atom.Body.Sweat = temperature_.Value * ps.Get(PS.MaxSweat);
				person_.Atom.Body.Flush = temperature_.Value * ps.Get(PS.MaxFlush);
				person_.Atom.Hair.Loose = temperature_.Value;
			}

			person_.Breathing.Intensity = person_.Mood.MovementEnergy;

			if (renderingParts_ > 0)
			{
				for (int i = 0; i < all_.Length; ++i)
					all_[i].UpdateRender();
			}
		}

		public void DebugAllLocks(List<string> list)
		{
			list.Clear();

			for (int i = 0; i < all_.Length; ++i)
				all_[i].Locker.DebugAllLocks(list);
		}

		public PartResult CheckParts(Person by, int[] triggerParts, int[] checkParts)
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

		public PartResult TriggeredBy(BodyPart p, BodyPart by)
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

		public Box GetUpperBodyBox()
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

		public void ResetMorphLimits()
		{
			for (int i = 0; i < morphsRemaining_.Length; ++i)
				morphsRemaining_[i] = MaxMorphs;
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
	}
}
