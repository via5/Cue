using System;
using System.Configuration;

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
		void MoveTo(Vector3 to, float finalBearing = float.MaxValue);
		bool HasTarget { get; }

		Slot StandSlot { get; }
		Slot SitSlot { get; }
		Slot SleepSlot { get; }
		Slot ToiletSlot { get; }
	}


	class BasicObject : IObject
	{
		public const float NoBearing = float.MaxValue;

		private const int NoMoveState = 0;
		private const int TurnTowardsTargetState = 1;
		private const int MoveTowardsTargetState = 2;
		private const int TurnTowardsFinalState = 3;

		private readonly W.IAtom atom_;

		private float maxMoveSpeed_ = 1;
		private float moveSpeedRampTime_ = 1;
		private float maxTurnSpeed_ = 300;
		private float turnSpeedRampTime_ = 1;
		private float moveDistanceThreshold_ = 0.01f;
		private float turnAngleThreshold_ = 0.1f;

		private float moveElapsed_ = 0;
		private float turnElapsed_ = 0;

		private Vector3 targetPos_ = Vector3.Zero;
		private float targetBearing_ = NoBearing;
		private int moveState_ = NoMoveState;
		private bool canMove_ = false;

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
			if (moveState_ != NoMoveState)
			{
				if (!canMove_)
					canMove_ = SetMoving(true);

				if (canMove_)
					MoveToTarget(s);
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

		private float AngleBetweenBearings(float bearing1, float bearing2)
		{
			return ((((bearing2 - bearing1) % 360) + 540) % 360) - 180;
		}

		private void MoveToTarget(float s)
		{
			// todo: if the target is slightly above the current position, the
			// direction can go entirely vertical, which rotates the object
			// completely
			//
			// always set the Y position first, assume it's not too far away;
			// this will need to change to support vertical movement
			atom_.Position = new Vector3(
				atom_.Position.X,
				targetPos_.Y,
				atom_.Position.Z);

			switch (moveState_)
			{
				case TurnTowardsTargetState:
				{
					if (TurnTowardsTarget(s))
					{
						moveState_ = MoveTowardsTargetState;
						return;
					}

					break;
				}

				case MoveTowardsTargetState:
				{
					if (MoveTowardsTarget(s))
					{
						moveState_ = TurnTowardsFinalState;
						return;
					}

					break;
				}

				case TurnTowardsFinalState:
				{
					if (TurnTowardsFinal(s))
					{
						moveState_ = NoMoveState;
						SetMoving(false);
					}

					break;
				}
			}
		}

		private bool TurnTowardsTarget(float s)
		{
			if (Vector3.Distance(Position, targetPos_) < moveDistanceThreshold_)
				return true;

			var dirToTarget = (targetPos_ - Position).Normalized;
			var bearingToTarget = Vector3.Angle(Vector3.Zero, dirToTarget);
			return TurnTowards(s, bearingToTarget);
		}

		private bool MoveTowardsTarget(float s)
		{
			if (Vector3.Distance(Position, targetPos_) < moveDistanceThreshold_)
			{
				atom_.Position = targetPos_;
				return true;
			}
			else
			{
				turnElapsed_ = Math.Max(0, turnElapsed_ - s);

				moveElapsed_ += s;
				var moveSpeed = Math.Min(moveElapsed_ / moveSpeedRampTime_, 1) * maxMoveSpeed_;

				atom_.Position += (Direction * s * moveSpeed);
				return false;
			}
		}

		private bool TurnTowardsFinal(float s)
		{
			if (targetBearing_ == NoBearing)
				return true;

			return TurnTowards(s, targetBearing_);
		}

		private bool TurnTowards(float s, float bearingToTarget)
		{
			var currentBearing = Bearing;
			var a = AngleBetweenBearings(bearingToTarget, currentBearing);

			if (Math.Abs(a) <= turnAngleThreshold_ || turnSpeedRampTime_ == 0)
			{
				Bearing = bearingToTarget;
				return true;
			}
			else
			{
				moveElapsed_ = Math.Max(0, moveElapsed_ - s);
				turnElapsed_ += s;
				var turnSpeed = Math.Min(turnElapsed_ / turnSpeedRampTime_, 1) * maxTurnSpeed_;

				float bearingDiff;

				if (a < 0)
					bearingDiff = Math.Max(a, -s * turnSpeed);
				else
					bearingDiff = Math.Min(a, s * turnSpeed);

				Bearing -= bearingDiff;

				return false;
			}
		}

		public void MoveTo(Vector3 to, float bearing = NoBearing)
		{
			targetPos_ = to;
			targetBearing_ = bearing;
			moveState_ = TurnTowardsTargetState;
			canMove_ = SetMoving(true);
		}

		protected virtual bool SetMoving(bool b)
		{
			// no-op
			return true;
		}
	}
}
