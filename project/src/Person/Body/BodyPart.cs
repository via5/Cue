using System.Collections.Generic;

namespace Cue
{
	public class BodyPart
	{
		private Person person_;
		private int type_;
		private Logger log_;
		private Sys.IBodyPart part_;
		private Sys.TriggerInfo[] triggers_ = null;
		private List<Sys.TriggerInfo> forcedTriggers_ = new List<Sys.TriggerInfo>();
		private float triggerCheckInterval_;
		private float updateTriggersElapsed_ = 0;
		private BodyPartLocker locker_;
		private bool staleTriggers_ = true;
		private List<Sys.TriggerInfo> tempList_ = null;

		public BodyPart(Person p, int type, Sys.IBodyPart part)
		{
			Cue.Assert(part != null, $"{BP.ToString(type)} is null");
			person_ = p;
			type_ = type;
			log_ = new Logger(Logger.Object, p, $"body.{BP.ToString(type)}");
			part_ = part;
			locker_ = new BodyPartLocker(this);
			triggerCheckInterval_ = U.RandomFloat(0.9f, 1.1f);
		}

		public bool Render
		{
			set { part_.Render = value; }
		}

		public Person Person
		{
			get { return person_; }
		}

		public Logger Log
		{
			get { return log_; }
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
			get { return part_.Exists; }
		}

		public bool IsPhysical
		{
			get { return part_.IsPhysical; }
		}

		public string Name
		{
			get { return BP.ToString(type_); }
		}

		public string Source
		{
			get { return part_.ToString(); }
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

				return part_.CanTrigger;
			}
		}

		public bool CanGrab
		{
			get { return part_.CanGrab; }
		}

		public Vector3 ControlPosition
		{
			get { return part_.ControlPosition; }
			set { part_.ControlPosition = value; }
		}

		public Quaternion ControlRotation
		{
			get { return part_.ControlRotation; }
			set { part_.ControlRotation = value; }
		}

		public Vector3 Position
		{
			get { return part_.Position; }
		}

		public Quaternion Rotation
		{
			get { return part_.Rotation; }
		}

		public BodyPartLocker Locker
		{
			get { return locker_; }
		}


		public void Update(float s)
		{
			updateTriggersElapsed_ += s;
			if (updateTriggersElapsed_ >= triggerCheckInterval_)
				staleTriggers_ = true;
		}

		public BodyPartLock Lock(int lockType, string why, bool strong = true)
		{
			return locker_.Lock(lockType, why, strong);
		}

		public bool LockedFor(int lockType, ulong key = BodyPartLock.NoKey)
		{
			return locker_.LockedFor(lockType, key);
		}

		public void AddForcedTrigger(
			int sourcePersonIndex, int sourceBodyPart, float value = 1)
		{
			var ti = new Sys.TriggerInfo(
				sourcePersonIndex, sourceBodyPart, value, true);

			Log.Verbose($"adding forced trigger for {this}: {ti}");
			forcedTriggers_.Add(ti);
		}

		public void RemoveForcedTrigger(int sourcePersonIndex, int sourceBodyPart)
		{
			for (int i = 0; i < forcedTriggers_.Count; ++i)
			{
				if (forcedTriggers_[i].Is(sourcePersonIndex, sourceBodyPart))
				{
					Log.Verbose($"removing forced trigger: {forcedTriggers_[i]}");
					forcedTriggers_.RemoveAt(i);
					return;
				}
			}

			Log.Error(
				$"{this}: RemoveForcedTrigger: not found for " +
				$"pi={sourcePersonIndex} bp={sourceBodyPart}");
		}

		public Sys.TriggerInfo[] GetTriggers()
		{
			if (staleTriggers_)
				UpdateTriggers();

			return triggers_;
		}

		private void UpdateTriggers()
		{
			updateTriggersElapsed_ = 0;
			staleTriggers_ = false;

			triggers_ = part_.GetTriggers();

			if (forcedTriggers_.Count > 0)
			{
				if (triggers_ == null)
					triggers_ = forcedTriggers_.ToArray();
				else
					MergeForcedTriggers();
			}
		}

		private void MergeForcedTriggers()
		{
			if (tempList_ == null)
				tempList_ = new List<Sys.TriggerInfo>();
			else
				tempList_.Clear();

			tempList_.AddRange(triggers_);

			for (int i = 0; i < forcedTriggers_.Count; ++i)
			{
				if (forcedTriggers_[i].IsExternal && tempList_.Count > 0)
					continue;

				bool found = false;

				for (int j = 0; j < triggers_.Length; ++j)
				{
					if (triggers_[j].SameAs(forcedTriggers_[i]))
					{
						found = true;
						break;
					}
				}

				if (!found)
					tempList_.Add(forcedTriggers_[i]);
			}

			triggers_ = tempList_.ToArray();
		}

		public bool Triggered
		{
			get
			{
				var ts = GetTriggers();
				if (ts == null)
					return false;

				return (ts.Length > 0);
			}
		}

		public Sys.GrabInfo[] GetGrabs()
		{
			// todo, cache it
			return part_.GetGrabs();
		}

		public bool Grabbed
		{
			get { return part_.Grabbed; }
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

			return DistanceToSurface(other) <= BodyParts.CloseToDistance;
		}

		public float DistanceToSurface(BodyPart other, bool debug = false)
		{
			if (!Exists || !other.Exists)
				return float.MaxValue;

			return Sys.DistanceToSurface(other.Sys, debug);
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

		public void UnlinkFrom(Person to)
		{
			var toParts = to.Body.Parts;

			for (int i = 0; i < toParts.Length; ++i)
			{
				if (IsLinkedTo(toParts[i]))
				{
					Unlink();
					return;
				}
			}
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

		public bool CanApplyForce()
		{
			return part_.CanApplyForce();
		}

		public void AddRelativeForce(Vector3 v)
		{
			part_.AddRelativeForce(v);
		}

		public void AddRelativeTorque(Vector3 v)
		{
			part_.AddRelativeTorque(v);
		}

		public void AddForce(Vector3 v)
		{
			part_.AddForce(v);
		}

		public void AddTorque(Vector3 v)
		{
			part_.AddTorque(v);
		}

		public override string ToString()
		{
			return $"{part_} ({BP.ToString(type_)})";
		}
	}
}
