// auto generated from AnimationsEnums.tt

using System.Collections.Generic;

namespace Cue
{
	public struct AnimationTypes
	{
		public static readonly AnimationTypes None = AnimationTypes.CreateInternal(-1);
		public static readonly AnimationTypes Idle = AnimationTypes.CreateInternal(0);
		public static readonly AnimationTypes Sex = AnimationTypes.CreateInternal(1);
		public static readonly AnimationTypes Frottage = AnimationTypes.CreateInternal(2);
		public static readonly AnimationTypes Orgasm = AnimationTypes.CreateInternal(3);
		public static readonly AnimationTypes Smoke = AnimationTypes.CreateInternal(4);
		public static readonly AnimationTypes SuckFinger = AnimationTypes.CreateInternal(5);
		public static readonly AnimationTypes Penetrated = AnimationTypes.CreateInternal(6);
		public static readonly AnimationTypes RightFinger = AnimationTypes.CreateInternal(7);
		public static readonly AnimationTypes LeftFinger = AnimationTypes.CreateInternal(8);
		public static readonly AnimationTypes Kiss = AnimationTypes.CreateInternal(9);
		public static readonly AnimationTypes HandjobBoth = AnimationTypes.CreateInternal(10);
		public static readonly AnimationTypes HandjobLeft = AnimationTypes.CreateInternal(11);
		public static readonly AnimationTypes HandjobRight = AnimationTypes.CreateInternal(12);
		public static readonly AnimationTypes Blowjob = AnimationTypes.CreateInternal(13);

		public const int Count = 14;
		public int GetCount() { return 14; }


		private static AnimationTypes[] values_ = new AnimationTypes[]
		{
			AnimationTypes.CreateInternal(0),
			AnimationTypes.CreateInternal(1),
			AnimationTypes.CreateInternal(2),
			AnimationTypes.CreateInternal(3),
			AnimationTypes.CreateInternal(4),
			AnimationTypes.CreateInternal(5),
			AnimationTypes.CreateInternal(6),
			AnimationTypes.CreateInternal(7),
			AnimationTypes.CreateInternal(8),
			AnimationTypes.CreateInternal(9),
			AnimationTypes.CreateInternal(10),
			AnimationTypes.CreateInternal(11),
			AnimationTypes.CreateInternal(12),
			AnimationTypes.CreateInternal(13),
		};

		public static AnimationTypes[] Values
		{
			get { return values_; }
		}

		private static string[] names_ = new string[]
		{
			"idle",
			"sex",
			"frottage",
			"orgasm",
			"smoke",
			"suckFinger",
			"penetrated",
			"rightFinger",
			"leftFinger",
			"kiss",
			"handjobBoth",
			"handjobLeft",
			"handjobRight",
			"blowjob",
		};

		public static AnimationTypes FromString(string s)
		{
			for (int i = 0; i<names_.Length; ++i)
			{
				if (names_[i] == s)
					return AnimationTypes.CreateInternal(i);
			}

			return None;
		}

		public static AnimationTypes[] FromStringMany(string s)
		{
			var list = new List<AnimationTypes>();
			var ss = s.Split(' ');

			foreach (string p in ss)
			{
				string tp = p.Trim();
				if (tp == "")
					continue;

				var i = FromString(tp);
				if (i != None)
					list.Add(i);
			}

			return list.ToArray();
		}

		public string GetName(AnimationTypes i)
		{
			return ToString(i);
		}

		public static string ToString(AnimationTypes i)
		{
			if (i.v_ >= 0 && i.v_ < names_.Length)
				return names_[i.v_];
			else
				return $"?{i.v_}";
		}

		public static string[] Names
		{
			get { return names_; }
		}



		private int v_;

		private AnimationTypes(int value)
		{
			v_ = value;
		}

		public static AnimationTypes CreateInternal(int value)
		{
			return new AnimationTypes(value);
		}

		public int Int
		{
			get { return v_; }
		}

		public static bool operator==(AnimationTypes a, AnimationTypes b)
		{
			return (a.v_ == b.v_);
		}

		public static bool operator!=(AnimationTypes a, AnimationTypes b)
		{
			return (a.v_ != b.v_);
		}

		public override bool Equals(object o)
		{
			return (o is AnimationTypes) && (((AnimationTypes)o).v_ == v_);
		}

		public override int GetHashCode()
		{
			return v_;
		}
	}
}
