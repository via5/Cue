using System;

namespace Cue
{
	class Optional<T>
	{
		private T value_;
		private bool hasValue_;

		public Optional()
		{
			hasValue_ = false;
		}

		public Optional(T t)
		{
			value_ = t;
			hasValue_ = true;
		}

		public bool HasValue
		{
			get { return hasValue_; }
		}

		public T Value
		{
			get { return value_; }
		}
	}


	class Slot
	{
		public Vector3 positionOffset;
		public float bearingOffset;

		public Slot()
			: this(Vector3.Zero, 0)
		{
		}

		public Slot(Vector3 positionOffset, float bearingOffset)
		{
			this.positionOffset = positionOffset;
			this.bearingOffset = bearingOffset;
		}
	}


	interface IObject
	{
		W.IAtom Atom { get; }
		Vector3 Position { get; set; }
		Vector3 Direction { get; set; }
		float Bearing { get; }
		void Update(float s);
		void FixedUpdate(float s);
		void MoveTo(Vector3 to, float bearing);
		bool HasTarget { get; }
		void OnPluginState(bool b);
		void SetPaused(bool b);

		Slot StandSlot { get; }
		Slot SitSlot { get; }
		Slot SleepSlot { get; }
		Slot ToiletSlot { get; }
	}


	class BasicObject : IObject
	{
		public const float NoBearing = float.MaxValue;

		private const int NoMoveState = 0;
		private const int MoveTowardsTargetState = 2;

		protected const int MoveNone = 0;
		protected const int MoveTentative = 1;
		protected const int MoveWalk = 2;
		protected const int MoveTurnLeft = 3;
		protected const int MoveTurnRight = 4;

		private readonly W.IAtom atom_;

		private float moveElapsed_ = 0;
		private float turnElapsed_ = 0;

		private Vector3 targetPos_ = Vector3.Zero;
		private float targetBearing_ = NoBearing;
		private int moveState_ = NoMoveState;
		private bool canMove_ = false;
		private bool moving_ = false;

		private Slot standSlot_ = null;
		private Slot sitSlot_ = null;
		private Slot sleepSlot_ = null;
		private Slot toiletSlot_ = null;

		public BasicObject(W.IAtom atom)
		{
			atom_ = atom;
		}

		public W.IAtom Atom
		{
			get { return atom_; }
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

		public float Bearing
		{
			get
			{
				return Vector3.Angle(Vector3.Zero, Direction);
			}

			set
			{
				Direction = Vector3.Rotate(0, value, 0);
			}
		}

		public bool HasTarget
		{
			get { return (moveState_ != NoMoveState); }
		}

		public Slot StandSlot
		{
			get { return standSlot_; }
			set { standSlot_ = value; }
		}

		public Slot SitSlot
		{
			get { return sitSlot_; }
			set { sitSlot_ = value; }
		}

		public Slot SleepSlot
		{
			get { return sleepSlot_; }
			set { sleepSlot_ = value; }
		}

		public Slot ToiletSlot
		{
			get { return toiletSlot_; }
			set { toiletSlot_ = value; }
		}

		public virtual void Update(float s)
		{
			Atom.Update(s);

			if (moveState_ != NoMoveState)
			{
				if (!canMove_)
					canMove_ = SetMoving(MoveTentative);

				if (canMove_)
				{
					moveState_ = MoveTowardsTargetState;

					if (!moving_)
					{
						Atom.NavTo(targetPos_, targetBearing_);
						moving_ = true;
					}

					if (Atom.NavActive)
					{
						SetMoving(MoveWalk);
					}
					else
					{
						moveState_ = NoMoveState;
						Atom.NavStop();
						SetMoving(MoveNone);
						moving_ = false;
					}
				}
			}
			else
			{
				moveElapsed_ = Math.Max(0, moveElapsed_ - s);
				turnElapsed_ = Math.Max(0, turnElapsed_ - s);
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

		public void MoveTo(Vector3 to, float bearing)
		{
			targetPos_ = to;
			targetBearing_ = bearing;
			moveState_ = MoveTowardsTargetState;
			canMove_ = SetMoving(MoveTentative);
			moving_ = false;
		}

		protected virtual bool SetMoving(int i)
		{
			// no-op
			return true;
		}
	}
}
