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
		public const int Spawn = 5;

		private IObject self_;
		private Logger log_;
		private int type_;
		private IObject lockedBy_ = null;

		public Slot(IObject self, int type)
		{
			self_ = self;
			log_ = new Logger(Logger.Slots, self_, TypeToString(type_));
			type_ = type;
		}

		public IObject ParentObject
		{
			get { return self_; }
		}

		public int Type
		{
			get { return type_; }
		}

		public bool Interactable
		{
			get
			{
				switch (type_)
				{
					case NoType:
					case Spawn:
						return false;

					default:
						return true;
				}
			}
		}

		public bool Lock(IObject by)
		{
			if (lockedBy_ == null)
			{
				log_.Info($"locked by {by}");
				lockedBy_ = by;
				return true;
			}

			return false;
		}

		public bool Unlock(IObject by)
		{
			if (lockedBy_ == by)
			{
				log_.Info($"unlocked by {by}");
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
				"none", "stand", "sit", "lie", "toilet", "spawn"
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
			string s = $"{self_}.{TypeToString(type_)}";

			if (lockedBy_ == null)
				s += "@free";
			else
				s += $"@{lockedBy_}";

			return s;
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

		public Slot GetAny(int type)
		{
			for (int i = 0; i < slots_.Count; ++i)
			{
				if (slots_[i].Type == type)
					return slots_[i];
			}

			return null;
		}

		public List<Slot> GetAll(int type)
		{
			var list = new List<Slot>();

			for (int i = 0; i < slots_.Count; ++i)
			{
				if (slots_[i].Type == type)
					list.Add(slots_[i]);
			}

			return list;
		}

		public bool Has(int type)
		{
			return (GetAny(type) != null);
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

		public bool AnyInteractable
		{
			get
			{
				for (int i = 0; i < slots_.Count; ++i)
				{
					if (slots_[i].Interactable)
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

			int ri = U.RandomInt(0, unlocked.Count - 1);
			if (ri < 0 || ri >= unlocked.Count)
			{
				Cue.LogError(
					"RandomUnlocked: bad " +
					"ri=" + ri.ToString() + ", " +
					"count=" + unlocked.Count.ToString());

				return null;
			}

			return slots_[unlocked[ri]];
		}
	}
}
