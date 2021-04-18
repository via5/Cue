using Leap.Unity;
using System;
using System.Collections.Generic;

namespace Cue
{
	class Slot
	{
		public const int NoType = 0;
		public const int Stand = 1;
		public const int Sit = 2;
		public const int Lie = 3;
		public const int Toilet = 4;

		private IObject self_;
		private int type_;
		private IObject lockedBy_ = null;

		public Slot(IObject self, int type)
		{
			self_ = self;
			type_ = type;
		}

		public int Type
		{
			get { return type_; }
		}

		public bool Lock(IObject by)
		{
			if (lockedBy_ == null)
			{
				lockedBy_ = by;
				return true;
			}

			return false;
		}

		public bool Unlock(IObject by)
		{
			if (lockedBy_ == by)
			{
				lockedBy_ = null;
				return true;
			}

			return false;
		}

		public bool Locked
		{
			get { return (lockedBy_ != null); }
		}

		public IObject LockedBy
		{
			get { return lockedBy_; }
		}

		public Vector3 Position
		{
			get { return self_.Position; }
		}

		public float Bearing
		{
			get { return self_.Bearing; }
		}

		private static string[] GetTypes()
		{
			return new string[]
			{
				"none", "stand", "sit", "lie", "toilet"
			};
		}

		public static string TypeToString(int t)
		{
			var types = GetTypes();
			if (t >= 0 && t < types.Length)
				return types[t];
			else
				return "?" + t.ToString();
		}

		public static int TypeFromString(string s)
		{
			var types = GetTypes();

			for (int i = 0; i < types.Length; ++i)
			{
				if (types[i] == s)
					return i;
			}

			return NoType;
		}

		public override string ToString()
		{
			return self_.ToString() + " slot " + TypeToString(type_);
		}
	}


	class Slots
	{
		private IObject self_;
		private List<Slot> slots_ = new List<Slot>();

		public Slots(IObject self)
		{
			self_ = self;
		}

		public void Add(int type)
		{
			slots_.Add(new Slot(self_, type));
		}

		public Slot Get(int type)
		{
			for (int i = 0; i < slots_.Count; ++i)
			{
				if (slots_[i].Type == type)
					return slots_[i];
			}

			return null;
		}

		public bool Has(int type)
		{
			return (Get(type) != null);
		}

		public bool AnyLocked
		{
			get
			{
				for (int i = 0; i < slots_.Count; ++i)
				{
					if (slots_[i].Locked)
						return true;
				}

				return false;
			}
		}

		public Slot GetLockedBy(IObject o)
		{
			for (int i = 0; i < slots_.Count; ++i)
			{
				if (slots_[i].LockedBy == o)
					return slots_[i];
			}

			return null;
		}

		public Slot RandomUnlocked()
		{
			var unlocked = new List<int>();

			for (int i = 0; i < slots_.Count; ++i)
			{
				if (!slots_[i].Locked)
					unlocked.Add(i);
			}

			if (unlocked.Count == 0)
				return null;

			unlocked.Shuffle();
			return slots_[unlocked[0]];
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
			targetBearing_ = bearing;
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
