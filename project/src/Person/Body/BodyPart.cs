﻿using System.Collections.Generic;

namespace Cue
{
	public class BodyPart
	{
		private Person person_;
		private BodyPartType type_;
		private Logger log_;
		private Sys.IBodyPart part_;
		private Sys.TriggerInfo[] triggers_ = null;
		private List<Sys.TriggerInfo> forcedTriggers_ = new List<Sys.TriggerInfo>();
		private float triggerCheckInterval_;
		private float updateTriggersElapsed_ = 0;
		private BodyPartLocker locker_;
		private bool staleTriggers_ = true;
		private List<Sys.TriggerInfo> tempList_ = null;

		public BodyPart(Person p, BodyPartType type, Sys.IBodyPart part)
		{
			Cue.Assert(part != null, $"{BodyPartType.ToString(type)} is null");
			person_ = p;
			type_ = type;
			log_ = new Logger(Logger.Object, p, $"body.{BodyPartType.ToString(type)}");
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

		public bool IsAvailable
		{
			get { return part_.IsAvailable; }
		}

		public string Name
		{
			get { return BodyPartType.ToString(type_); }
		}

		public string Source
		{
			get { return part_.ToString(); }
		}

		public BodyPartType Type
		{
			get { return type_; }
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

		public Vector3 Center
		{
			get { return part_.Center; }
		}

		public Vector3 Extremity
		{
			get { return part_.Extremity; }
		}

		public Quaternion Rotation
		{
			get { return part_.Rotation; }
		}

		public Quaternion CenterRotation
		{
			get { return part_.CenterRotation; }
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

		public BodyPartLock Lock(int lockType, string why, int strengthType)
		{
			return locker_.Lock(lockType, why, strengthType);
		}

		public bool LockedFor(int lockType, ulong key = BodyPartLock.NoKey)
		{
			return locker_.LockedFor(lockType, key);
		}

		public void AddForcedTrigger(
			int sourcePersonIndex, BodyPartType sourceBodyPart, float value = 1)
		{
			global::Cue.Sys.TriggerInfo ti;

			if (sourcePersonIndex >= 0)
			{
				ti = global::Cue.Sys.TriggerInfo.FromPerson(
					sourcePersonIndex, sourceBodyPart, value, true);
			}
			else
			{
				ti = global::Cue.Sys.TriggerInfo.FromExternal(
					global::Cue.Sys.TriggerInfo.NoneType, null, value, true);
			}

			Log.Verbose($"adding forced trigger for {this}: {ti}");
			forcedTriggers_.Add(ti);
		}

		public void RemoveForcedTrigger(int sourcePersonIndex, BodyPartType sourceBodyPart)
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

		public Sys.TriggerInfo[] GetTriggers(bool forceUpdate = false)
		{
			if (forceUpdate || (Exists && staleTriggers_))
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
				if (forcedTriggers_[i].Type != global::Cue.Sys.TriggerInfo.PersonType && tempList_.Count > 0)
					continue;

				bool found = false;

				for (int j = 0; j < tempList_.Count; ++j)
				{
					if (tempList_[j].SameAs(forcedTriggers_[i]))
					{
						tempList_[j] = tempList_[j].MergeFrom(forcedTriggers_[i]);
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

		public PersonStatus.PartResult GrabbedByPlayer
		{
			get
			{
				if (Grabbed)
				{
					var p = Cue.Instance.Player;

					if (IsLinkedTo(p.Body.Get(BP.LeftHand)))
						return new PersonStatus.PartResult(Type, p.ObjectIndex, BP.LeftHand);
					else if (IsLinkedTo(p.Body.Get(BP.RightHand)))
						return new PersonStatus.PartResult(Type, p.ObjectIndex, BP.RightHand);
				}

				return PersonStatus.PartResult.None;
			}
		}

		public bool CloseTo(BodyPart other)
		{
			if (!Exists || !other.Exists)
				return false;

			// can't use DistanceToSurface() here, CloseTo() is used to check
			// for personal space and it's way too slow
			float d = Vector3.Distance(Position, other.Position);

			return (d < BodyParts.PersonalSpaceDistance);
		}

		public float DistanceToSurface(BodyPart other, bool debug = false)
		{
			if (!Exists || !other.Exists)
				return float.MaxValue;

			return Sys.DistanceToSurface(other.Sys, debug);
		}

		public float DistanceToSurface(Vector3 pos, bool debug = false)
		{
			if (!Exists)
				return float.MaxValue;

			return Sys.DistanceToSurface(pos, debug);
		}

		public Sys.BodyPartRegionInfo ClosestBodyPartRegion(Vector3 pos)
		{
			if (!Exists)
				return global::Cue.Sys.BodyPartRegionInfo.None;

			return Sys.ClosestBodyPartRegion(pos);
		}

		public void LinkTo(Sys.IBodyPartRegion other)
		{
			if (!Exists)
				return;

			Sys.LinkTo(other);
		}

		public void Unlink()
		{
			Sys?.Unlink();
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

		public bool IsLinked
		{
			get
			{
				if (!Exists)
					return false;

				return Sys.IsLinked;
			}
		}

		public BodyPart Link
		{
			get
			{
				if (!Exists)
					return null;

				var link = Sys.Link;
				if (link == null)
					return null;

				var p = Cue.Instance.PersonForAtom(link.BodyPart.Atom);
				if (p == null)
					return null;

				return p.Body.Get(link.BodyPart.Type);
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
			return $"{person_.ID}.{BodyPartType.ToString(type_)}";
		}
	}
}
