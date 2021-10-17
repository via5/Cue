using SimpleJSON;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Cue
{
	interface IObject
	{
		int ObjectIndex { get; }
		string ID { get; }
		bool IsPlayer { get; }
		bool Visible { get; set; }
		Sys.IAtom Atom { get; }
		Vector3 Position { get; set; }
		Quaternion Rotation { get; set; }
		Vector3 EyeInterest { get; }
		bool Possessed { get; }

		bool HasTrait(string name);
		string[] Traits { get; set; }

		void Destroy();
		void FixedUpdate(float s);
		void Update(float s);
		void OnPluginState(bool b);
		void SetPaused(bool b);

		bool InteractWith(IObject o);
		void MoveTo(IObject o, Vector3 to, float bearing);
		void MoveToManual(IObject o, Vector3 to, float bearing);
		void MakeIdle();
		void MakeIdleForMove();

		void TeleportTo(Vector3 to, float bearing);
		bool HasTarget { get; }
		IObject MoveTarget{ get; }

		Slots Slots { get; }

		JSONNode ToJSON();
		void Load(JSONClass r);

		string GetParameter(string key);
	}


	class BasicObject : IObject
	{
		public const float NoBearing = float.MaxValue;

		private const int NoMoveState = 0;
		private const int TentativeMoveState = 1;
		private const int MovingState = 2;

		private readonly int objectIndex_;
		private readonly Sys.IAtom atom_;
		protected readonly Logger log_;
		private Sys.ObjectParameters ps_ = null;

		private Vector3 targetPos_ = Vector3.Zero;
		private float targetBearing_ = NoBearing;
		private float targetStoppingDistance_ = 0;
		private int moveState_ = NoMoveState;
		private IObject moveTarget_ = null;

		private Slots slots_;
		private Slot locked_ = null;

		private string[] traits_ = new string[0];


		public BasicObject(int index, Sys.IAtom atom, Sys.ObjectParameters ps = null)
		{
			objectIndex_ = index;
			atom_ = atom;
			log_ = new Logger(Logger.Object, this, "");
			slots_ = new Slots(this);
			ps_ = ps;
		}

		public static BasicObject TryCreateFromSlot(int index, Sys.IAtom a)
		{
			return null;
			//var re = new Regex(@"cue!([a-zA-Z]+)#?.*");
			//var m = re.Match(a.ID);
			//
			//if (m == null || !m.Success)
			//	return null;
			//
			//var typeName = m.Groups[1].Value;
			//
			//var type = Slot.TypeFromString(typeName);
			//if (type == Slot.NoType)
			//{
			//	Cue.LogError("bad object type '" + typeName + "'");
			//	return null;
			//}
			//
			//BasicObject o = new BasicObject(index, a);
			//o.Slots.Add(type);
			//
			//return o;
		}

		public string GetParameter(string key)
		{
			if (ps_ == null)
				return "";
			else
				return ps_.Get(key);
		}

		public void Destroy()
		{
			atom_?.Destroy();
		}

		public int ObjectIndex
		{
			get { return objectIndex_; }
		}

		public Logger Log
		{
			get { return log_; }
		}

		public Sys.Vam.VamAtom VamAtom
		{
			get { return atom_ as Sys.Vam.VamAtom; }
		}

		public Sys.IAtom Atom
		{
			get { return atom_; }
		}

		public string ID
		{
			get { return atom_.ID; }
		}

		public bool IsPlayer
		{
			get { return (this == Cue.Instance.Player); }
		}

		public bool Visible
		{
			get { return atom_.Visible; }
			set { atom_.Visible = value; }
		}

		public Vector3 Position
		{
			get { return atom_.Position; }
			set { atom_.Position = value; }
		}

		public Quaternion Rotation
		{
			get { return atom_.Rotation; }
			set { atom_.Rotation = value; }
		}

		public virtual Vector3 EyeInterest
		{
			get { return Position; }
		}

		public Slots Slots
		{
			get { return slots_; }
		}

		public Slot LockedSlot
		{
			get { return locked_; }
			set { locked_ = value; }
		}

		public bool Possessed
		{
			get { return Atom.Possessed; }
		}

		public bool HasTrait(string name)
		{
			for (int i = 0; i < traits_.Length; ++i)
			{
				if (traits_[i] == name)
					return true;
			}

			return false;
		}

		public string[] Traits
		{
			get
			{
				return traits_;
			}

			set
			{
				traits_ = value;
				Cue.Instance.Save();
			}
		}

		public bool TryLockSlot(IObject o)
		{
			Slot s = o.Slots.GetLockedBy(this);

			// object is already locked by this person, reuse it
			if (s != null)
			{
				log_.Info($"slot {s} already locked by self, reusing it");
				return true;
			}

			if (o.Slots.AnyLocked)
			{
				// a slot is already locked, fail
				log_.Info($"can't lock {o}, already has locked slot {o.Slots.AnyLocked}");
				return false;
			}

			// take a random slot
			s = o.Slots.RandomUnlocked();

			if (s == null)
			{
				// no free slots
				log_.Info($"can't lock {o}, no free slots");
				return false;
			}

			return TryLockSlot(s);
		}

		public bool TryLockSlot(Slot s)
		{
			if (!s.Lock(this))
			{
				// this object can't lock this slot
				log_.Info($"can't lock {s}");
				return false;
			}

			// slot has been locked successfully, unlock the current slot,
			// if any
			if (locked_ != null)
			{
				log_.Info($"found slot to lock, unlocking current {locked_}");
				locked_.Unlock(this);
			}

			log_.Info($"locked {s}");
			locked_ = s;

			return true;
		}

		public void UnlockSlot()
		{
			if (locked_ != null)
			{
				log_.Info($"unlocking {locked_}");
				locked_.Unlock(this);
				locked_ = null;
			}
		}

		public virtual bool InteractWith(IObject o)
		{
			// no-op
			return false;
		}

		public string MoveStateString()
		{
			switch (moveState_)
			{
				case TentativeMoveState:
					return $"tentative to {targetPos_} {U.BearingToString(targetBearing_)}";

				case MovingState:
					return $"moving, nav {Sys.NavStates.ToString(Atom.NavState)}";

				case NoMoveState:
				default:
					return "(none)";
			}
		}

		public virtual void Update(float s)
		{
			I.Start(I.UpdateObjectsAtoms);
			{
				Atom.Update(s);
			}
			I.End();


			I.Start(I.UpdateObjectsMove);
			{
				if (moveState_ == TentativeMoveState)
				{
					if (StartMove())
					{
						Atom.NavTo(targetPos_, targetBearing_, targetStoppingDistance_);
						moveState_ = MovingState;
					}
				}

				if (moveState_ == MovingState)
				{
					if (Atom.NavState == Sys.NavStates.None)
					{
						moveState_ = NoMoveState;
						Atom.NavStop("nav state is none");
					}
				}
			}
			I.End();
		}

		public void LateUpdate(float s)
		{
			Atom.LateUpdate(s);
		}

		public virtual void Load(JSONClass r)
		{
			var ts = new List<string>();
			foreach (JSONNode n in r["traits"].AsArray)
				ts.Add(n.Value);
			traits_ = ts.ToArray();
		}

		public virtual JSONNode ToJSON()
		{
			var o = new JSONClass();

			if (traits_.Length > 0)
			{
				var a = new JSONArray();

				for (int i = 0; i < traits_.Length; ++i)
					a.Add(new JSONData(traits_[i]));

				o.Add("traits", a);
			}

			return o;
		}

		public virtual void FixedUpdate(float s)
		{
		}

		public virtual void OnPluginState(bool b)
		{
			atom_.OnPluginState(b);
		}

		public virtual void SetPaused(bool b)
		{
		}

		public override string ToString()
		{
			return atom_.ID;
		}

		public void MoveTo(IObject o, Vector3 to, float bearing)
		{
			moveTarget_ = o;
			targetStoppingDistance_ = 0;
			targetPos_ = to;

			if (bearing == NoBearing)
				targetBearing_ = NoBearing;
			else
				targetBearing_ = Quaternion.NormalizeAngle(bearing);

			moveState_ = TentativeMoveState;
		}

		public void MoveToManual(IObject o, Vector3 to, float bearing)
		{
			UnlockSlot();
			MakeIdleForMove();
			MoveTo(o, to, bearing);

			// todo
			targetStoppingDistance_ = 0.1f;
		}

		public virtual void MakeIdle()
		{
			// no-op
		}

		public virtual void MakeIdleForMove()
		{
			// no-op
		}

		public void TeleportTo(Vector3 to, float bearing)
		{
			Atom.TeleportTo(to, bearing);
		}

		public bool HasTarget
		{
			get { return (moveState_ != NoMoveState); }
		}

		public IObject MoveTarget
		{
			get { return moveTarget_; }
		}

		protected virtual bool StartMove()
		{
			// no-op
			return true;
		}
	}
}
