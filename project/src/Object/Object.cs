using System.Text.RegularExpressions;

namespace Cue
{
	interface IObject
	{
		int ObjectIndex { get; }
		string ID { get; }
		W.IAtom Atom { get; }
		Vector3 Position { get; set; }
		Vector3 Direction { get; set; }
		Vector3 Rotation { get; set; }
		Vector3 EyeInterest { get; }
		float Bearing { get; }
		bool Possessed { get; }

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
	}


	class BasicObject : IObject
	{
		public const float NoBearing = float.MaxValue;

		private const int NoMoveState = 0;
		private const int TentativeMoveState = 1;
		private const int MovingState = 2;

		private readonly int objectIndex_;
		private readonly W.IAtom atom_;
		protected readonly Logger log_;

		private Vector3 targetPos_ = Vector3.Zero;
		private float targetBearing_ = NoBearing;
		private float targetStoppingDistance_ = 0;
		private int moveState_ = NoMoveState;
		private IObject moveTarget_ = null;

		private Slots slots_;
		private Slot locked_ = null;

		public BasicObject(int index, W.IAtom atom)
		{
			objectIndex_ = index;
			atom_ = atom;
			log_ = new Logger(Logger.Object, this, "");
			slots_ = new Slots(this);
		}

		public static BasicObject TryCreateFromSlot(int index, W.IAtom a)
		{
			var re = new Regex(@"cue!([a-zA-Z]+)#?.*");
			var m = re.Match(a.ID);

			if (m == null || !m.Success)
				return null;

			var typeName = m.Groups[1].Value;

			var type = Slot.TypeFromString(typeName);
			if (type == Slot.NoType)
			{
				Cue.LogError("bad object type '" + typeName + "'");
				return null;
			}

			BasicObject o = new BasicObject(index, a);
			o.Slots.Add(type);

			return o;
		}

		public int ObjectIndex
		{
			get { return objectIndex_; }
		}

		public Logger Log
		{
			get { return log_; }
		}

		public W.VamAtom VamAtom
		{
			get { return (W.VamAtom)atom_; }
		}

		public W.IAtom Atom
		{
			get { return atom_; }
		}

		public string ID
		{
			get { return atom_.ID; }
		}

		public Vector3 Position
		{
			get { return atom_.Position; }
			set { atom_.Position = value; }
		}

		public Vector3 Direction
		{
			get { return atom_.Direction; }
			set { atom_.Direction = value; }
		}

		public Vector3 Rotation
		{
			get { return atom_.Rotation; }
			set { atom_.Rotation = value; }
		}

		public virtual Vector3 EyeInterest
		{
			get { return Position; }
		}

		public float Bearing
		{
			get { return Vector3.Angle(Vector3.Zero, Direction); }
			set { Direction = Vector3.Rotate(0, value, 0); }
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
					return $"moving, nav {W.NavStates.ToString(Atom.NavState)}";

				case NoMoveState:
				default:
					return "(none)";
			}
		}

		public virtual void Update(float s)
		{
			Atom.Update(s);

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
				if (Atom.NavState == W.NavStates.None)
				{
					moveState_ = NoMoveState;
					Atom.NavStop("nav state is none");
				}
			}
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
				targetBearing_ = Vector3.NormalizeAngle(bearing);

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
