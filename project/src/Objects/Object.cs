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

		public Slot(Vector3 positionOffset, float bearingOffset)
		{
			this.positionOffset = positionOffset;
			this.bearingOffset = bearingOffset;
		}
	}


	interface IObject
	{
		Vector3 Position { get; set; }
		Vector3 Direction { get; set; }
		float Bearing { get; }
		void Update(float s);
		void FixedUpdate(float s);
		void MoveTo(Vector3 to);
		bool HasTarget { get; }
		bool Animating { get; }
		void PlayAnimation(int i, bool loop);

		Slot SitSlot { get; }
		Slot SleepSlot { get; }
		Slot ToiletSlot { get; }
	}


	class BasicObject : IObject
	{
		private readonly W.IAtom atom_;

		private float maxMoveSpeed_ = 1;
		private float moveSpeedRampTime_ = 1;
		private float maxTurnSpeed_ = 300;
		private float turnSpeedRampTime_ = 0;
		private float moveDistanceThreshold_ = 0.01f;
		private float turnAngleThreshold_ = 0.1f;

		private float moveElapsed_ = 0;
		private float turnElapsed_ = 0;

		private Vector3 target_ = Vector3.Zero;
		private bool hasTarget_ = false;
		private bool canMove_ = false;

		private Slot sitSlot_ = null;
		private Slot sleepSlot_ = null;
		private Slot toiletSlot_ = null;

		public BasicObject(W.IAtom atom)
		{
			atom_ = atom;
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
			get { return hasTarget_; }
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
			if (hasTarget_)
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
			var dirToTarget = (target_ - Position).Normalized;
			var bearingToTarget = Vector3.Angle(Vector3.Zero, dirToTarget);
			var currentBearing = Bearing;
			var a = AngleBetweenBearings(bearingToTarget, currentBearing);

			if (Math.Abs(a) < turnAngleThreshold_ || turnSpeedRampTime_ == 0)
			{
				atom_.Direction = dirToTarget;
				turnElapsed_ = Math.Max(0, turnElapsed_ - s);
			}
			else
			{
				turnElapsed_ += s;
				var turnSpeed = Math.Min(turnElapsed_ / turnSpeedRampTime_, 1) * maxTurnSpeed_;
				var bearingDiff = Math.Min(a, Math.Sign(a) * s * turnSpeed);

				Bearing -= bearingDiff;
			}

			moveElapsed_ += s;
			var moveSpeed = Math.Min(moveElapsed_ / moveSpeedRampTime_, 1) * maxMoveSpeed_;

			atom_.Position += (Direction * s * moveSpeed);

			if (Vector3.Distance(Position, target_) < moveDistanceThreshold_)
			{
				atom_.Position = target_;
				hasTarget_ = false;
				SetMoving(false);
			}
		}

		public void MoveTo(Vector3 to)
		{
			if (Vector3.Distance(Position, to) < moveDistanceThreshold_)
			{
				atom_.Position = to;
				hasTarget_ = false;
				SetMoving(false);
			}
			else
			{
				target_ = to;
				hasTarget_ = true;
				canMove_ = SetMoving(true);
			}
		}

		public virtual bool Animating
		{
			get { return false; }
		}

		public virtual void PlayAnimation(int i, bool loop)
		{
			// no-op
		}

		protected virtual bool SetMoving(bool b)
		{
			// no-op
			return true;
		}
	}
}
