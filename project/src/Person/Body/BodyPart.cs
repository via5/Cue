using System.Collections.Generic;

namespace Cue
{
	class BodyPartLock
	{
		public const int Move = 0x01;
		public const int Morph = 0x02;
		public const int Anim = Move | Morph;

		private BodyPart bp_;
		private int type_;

		public BodyPartLock(BodyPart bp, int type)
		{
			bp_ = bp;
			type_ = type;
		}

		public int Type
		{
			get { return type_; }
		}

		public bool Is(int type)
		{
			return Bits.IsAnySet(type_, type);
		}

		public void Unlock()
		{
			bp_.UnlockInternal(this);
		}

		public static string TypeToString(int type)
		{
			if (type == 0)
				return "";

			var list = new List<string>();

			if (Bits.IsSet(type, Move))
				list.Add("move");

			if (Bits.IsSet(type, Morph))
				list.Add("morph");

			if (list.Count == 0)
				return $"?{type}";

			return string.Join("|", list.ToArray());
		}

		public override string ToString()
		{
			return $"{bp_}/{TypeToString(type_)}";
		}
	}


	class BodyPart
	{
		private const float TriggerCheckDelay = 1;

		private Person person_;
		private int type_;
		private Sys.IBodyPart part_;
		private Sys.IGraphic render_ = null;
		private Sys.TriggerInfo[] triggers_ = null;
		private List<Sys.TriggerInfo> forcedTriggers_ = new List<Sys.TriggerInfo>();
		private float lastTriggerCheck_ = 0;
		private List<BodyPartLock> locks_ = new List<BodyPartLock>();

		public BodyPart(Person p, int type, Sys.IBodyPart part)
		{
			person_ = p;
			type_ = type;
			part_ = part;
		}

		public bool Render
		{
			set
			{
				if (value)
				{
					if (render_ == null)
					{
						render_ = Cue.Instance.Sys.CreateBoxGraphic(
							Name + "_render",
							Vector3.Zero, new Vector3(0.005f, 0.005f, 0.005f),
							new Color(0, 0, 1, 0.1f));
					}

					render_.Visible = true;
					++person_.Body.RenderingParts;
				}
				else
				{
					if (render_ != null)
					{
						render_.Visible = false;
						--person_.Body.RenderingParts;
					}
				}
			}
		}

		public void UpdateRender()
		{
			if (render_ != null)
				render_.Position = part_.Position;
		}

		public Person Person
		{
			get { return person_; }
		}

		public Sys.IBodyPart Sys
		{
			get { return part_; }
		}

		public Sys.Vam.VamBodyPart VamSys
		{
			get { return part_ as Sys.Vam.VamBodyPart; }
		}

		public bool Exists
		{
			get { return part_?.Exists ?? false; }
		}

		public string Name
		{
			get { return BP.ToString(type_); }
		}

		public string Source
		{
			get { return part_?.ToString() ?? ""; }
		}

		public int Type
		{
			get { return type_; }
		}

		public bool CanTrigger
		{
			get
			{
				if (forcedTriggers_.Count > 0)
					return true;

				return part_?.CanTrigger ?? false;
			}
		}

		public bool CanGrab
		{
			get { return part_?.CanGrab ?? false; }
		}

		public void AddForcedTrigger(
			int sourcePersonIndex, int sourceBodyPart, float value = 1)
		{
			var ti = new Sys.TriggerInfo(
				sourcePersonIndex, sourceBodyPart, value, true);

			Person.Log.Info($"adding forced trigger for {this}: {ti}");
			forcedTriggers_.Add(ti);
		}

		public void RemoveForcedTrigger(int sourcePersonIndex, int sourceBodyPart)
		{
			for (int i = 0; i < forcedTriggers_.Count; ++i)
			{
				if (forcedTriggers_[i].personIndex == sourcePersonIndex)
				{
					if (forcedTriggers_[i].sourcePartIndex == sourceBodyPart)
					{
						Person.Log.Info($"removing forced trigger: {forcedTriggers_[i]}");
						forcedTriggers_.RemoveAt(i);
						return;
					}
				}
			}

			Cue.LogError(
				$"{this} RemoveForcedTrigger: not found for " +
				$"pi={sourcePersonIndex} bp={sourceBodyPart}");
		}

		public Sys.TriggerInfo[] GetTriggers()
		{
			// todo
			if (UnityEngine.Time.realtimeSinceStartup >= (lastTriggerCheck_ + TriggerCheckDelay))
			{
				lastTriggerCheck_ = UnityEngine.Time.realtimeSinceStartup;
				triggers_ = part_?.GetTriggers();

				if (forcedTriggers_.Count > 0)
				{
					if (triggers_ == null)
					{
						triggers_ = forcedTriggers_.ToArray();
					}
					else
					{
						var copy = new List<Sys.TriggerInfo>(triggers_);

						for (int i = 0; i < forcedTriggers_.Count; ++i)
						{
							bool found = false;

							for (int j = 0; j < triggers_.Length; ++j)
							{
								if (triggers_[j].personIndex == forcedTriggers_[i].personIndex)
								{
									if (triggers_[j].sourcePartIndex == forcedTriggers_[i].sourcePartIndex)
									{
										found = true;
										break;
									}
								}
							}

							if (!found)
								copy.Add(forcedTriggers_[i]);
						}

						triggers_ = copy.ToArray();
					}
				}
			}

			return triggers_;
		}

		public bool Triggered
		{
			get
			{
				if (part_ == null)
					return false;

				var ts = GetTriggers();
				if (ts == null)
					return false;

				return (ts.Length > 0);
			}
		}

		public bool Grabbed
		{
			get { return part_?.Grabbed ?? false; }
		}

		public bool GrabbedByPlayer
		{
			get
			{
				if (Grabbed)
				{
					var p = Cue.Instance.Player;

					return
						IsLinkedTo(p.Body.Get(BP.LeftHand)) ||
						IsLinkedTo(p.Body.Get(BP.RightHand));
				}

				return false;
			}
		}

		public bool CloseTo(BodyPart other)
		{
			if (!Exists || !other.Exists)
				return false;

			return DistanceToSurface(other) < 0.1f;
		}

		public float DistanceToSurface(BodyPart other)
		{
			if (!Exists || !other.Exists)
				return float.MaxValue;

			return Sys.DistanceToSurface(other.Sys);
		}

		public void LinkTo(BodyPart other)
		{
			if (!Exists)
				return;

			if (other != null && !other.Exists)
				return;

			Sys.LinkTo(other?.Sys);
		}

		public void Unlink()
		{
			LinkTo(null);
		}

		public bool Linked
		{
			get
			{
				if (!Exists)
					return false;

				return Sys.Linked;
			}
		}

		public bool IsLinkedTo(BodyPart other)
		{
			if (!Exists || other == null || !other.Exists)
				return false;

			return Sys.IsLinkedTo(other.Sys);
		}

		public BodyPartLock Lock(int lockType)
		{
			if (LockedFor(lockType))
				return null;

			var lk = new BodyPartLock(this, lockType);
			locks_.Add(lk);

			return lk;
			// todo
			//return
			//	person_.Kisser.IsBusy(type_) ||
			//	person_.Blowjob.IsBusy(type_) ||
			//	person_.Handjob.IsBusy(type_);
		}

		public void UnlockInternal(BodyPartLock lk)
		{
			for (int i = 0; i < locks_.Count; ++i)
			{
				if (locks_[i] == lk)
				{
					locks_.RemoveAt(i);
					return;
				}
			}

			person_.Log.Error($"can't unlock {lk}, not in list");
		}

		public bool LockedFor(int lockType)
		{
			for (int i = 0; i < locks_.Count; ++i)
			{
				if (locks_[i].Is(lockType))
					return true;
			}

			return false;
		}

		public string DebugLockString()
		{
			int lockType = 0;

			for (int i = 0; i < locks_.Count; ++i)
				lockType |= locks_[i].Type;

			if (lockType == 0 && locks_.Count > 0)
				return "?";

			return BodyPartLock.TypeToString(lockType);
		}

		public Vector3 ControlPosition
		{
			get
			{
				return part_?.ControlPosition ?? Vector3.Zero;

			}

			set
			{
				if (part_ != null)
					part_.ControlPosition = value;
			}
		}

		public Quaternion ControlRotation
		{
			get
			{
				return part_?.ControlRotation ?? Quaternion.Zero;
			}

			set
			{
				if (part_ != null)
					part_.ControlRotation = value;
			}
		}

		public Vector3 Position
		{
			get
			{
				return part_?.Position ?? Vector3.Zero;

			}
		}

		public Quaternion Rotation
		{
			get
			{
				return part_?.Rotation ?? Quaternion.Zero;
			}
		}

		public void AddRelativeForce(Vector3 v)
		{
			part_?.AddRelativeForce(v);
		}

		public void AddRelativeTorque(Vector3 v)
		{
			part_?.AddRelativeTorque(v);
		}

		public override string ToString()
		{
			string s = "";

			if (part_ == null)
				s += "null";
			else
				s += part_.ToString();

			s += $" ({BP.ToString(type_)})";

			return s;
		}
	}
}
