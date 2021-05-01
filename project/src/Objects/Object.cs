using System.Text.RegularExpressions;

namespace Cue
{
	interface IObject
	{
		string ID { get; }
		W.IAtom Atom { get; }
		Vector3 Position { get; set; }
		Vector3 Direction { get; set; }
		Vector3 EyeInterest { get; }
		float Bearing { get; }

		void FixedUpdate(float s);
		void Update(float s);
		void OnPluginState(bool b);
		void SetPaused(bool b);

		void MoveTo(Vector3 to, float bearing);
		void TeleportTo(Vector3 to, float bearing);
		bool HasTarget { get; }

		Slots Slots { get; }
	}


	class BasicObject : IObject
	{
		public const float NoBearing = float.MaxValue;

		private const int NoMoveState = 0;
		private const int TentativeMoveState = 1;
		private const int MovingState = 2;

		private readonly W.IAtom atom_;

		private Vector3 targetPos_ = Vector3.Zero;
		private float targetBearing_ = NoBearing;
		private int moveState_ = NoMoveState;

		private Slots slots_;

		public BasicObject(W.IAtom atom)
		{
			atom_ = atom;
			slots_ = new Slots(this);
		}

		public static BasicObject TryCreateFromSlot(W.IAtom a)
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

			BasicObject o = new BasicObject(a);
			o.Slots.Add(type);

			return o;
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

		public virtual void Update(float s)
		{
			Atom.Update(s);

			if (moveState_ == TentativeMoveState)
			{
				if (StartMove())
				{
					Atom.NavTo(targetPos_, targetBearing_);
					moveState_ = MovingState;
				}
				else
				{
					Cue.LogInfo($"{ID}: waiting to start move");
				}
			}

			if (moveState_ == MovingState)
			{
				if (Atom.NavState == W.NavStates.None)
				{
					moveState_ = NoMoveState;
					Atom.NavStop();
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

		public void MoveTo(Vector3 to, float bearing)
		{
			targetPos_ = to;

			if (bearing == NoBearing)
				targetBearing_ = NoBearing;
			else
				targetBearing_ = Vector3.NormalizeAngle(bearing);

			moveState_ = TentativeMoveState;
		}

		public void TeleportTo(Vector3 to, float bearing)
		{
			Atom.TeleportTo(to, bearing);
		}

		public bool HasTarget
		{
			get { return (moveState_ != NoMoveState); }
		}

		protected virtual bool StartMove()
		{
			// no-op
			return true;
		}
	}
}
