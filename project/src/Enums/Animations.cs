// auto generated from AnimationsEnums.tt

using System.Collections.Generic;

namespace Cue
{
	public struct AnimationType
	{
		public static readonly AnimationType None = AnimationType.CreateInternal(-1);
		public static readonly AnimationType Idle = AnimationType.CreateInternal(0);
		public static readonly AnimationType Sex = AnimationType.CreateInternal(1);
		public static readonly AnimationType Frottage = AnimationType.CreateInternal(2);
		public static readonly AnimationType Orgasm = AnimationType.CreateInternal(3);
		public static readonly AnimationType Smoke = AnimationType.CreateInternal(4);
		public static readonly AnimationType SuckFinger = AnimationType.CreateInternal(5);
		public static readonly AnimationType Penetrated = AnimationType.CreateInternal(6);
		public static readonly AnimationType RightFinger = AnimationType.CreateInternal(7);
		public static readonly AnimationType LeftFinger = AnimationType.CreateInternal(8);
		public static readonly AnimationType Kiss = AnimationType.CreateInternal(9);
		public static readonly AnimationType HandjobBoth = AnimationType.CreateInternal(10);
		public static readonly AnimationType HandjobLeft = AnimationType.CreateInternal(11);
		public static readonly AnimationType HandjobRight = AnimationType.CreateInternal(12);
		public static readonly AnimationType Blowjob = AnimationType.CreateInternal(13);

		public const int Count = 14;
		public int GetCount() { return 14; }


		private static AnimationType[] values_ = new AnimationType[]
		{
			AnimationType.CreateInternal(0),
			AnimationType.CreateInternal(1),
			AnimationType.CreateInternal(2),
			AnimationType.CreateInternal(3),
			AnimationType.CreateInternal(4),
			AnimationType.CreateInternal(5),
			AnimationType.CreateInternal(6),
			AnimationType.CreateInternal(7),
			AnimationType.CreateInternal(8),
			AnimationType.CreateInternal(9),
			AnimationType.CreateInternal(10),
			AnimationType.CreateInternal(11),
			AnimationType.CreateInternal(12),
			AnimationType.CreateInternal(13),
		};

		public static AnimationType[] Values
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

		public static AnimationType FromString(string s)
		{
			for (int i = 0; i<names_.Length; ++i)
			{
				if (names_[i] == s)
					return AnimationType.CreateInternal(i);
			}

			return None;
		}

		public static AnimationType[] FromStringMany(string s)
		{
			var list = new List<AnimationType>();
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

		public string GetName(AnimationType i)
		{
			return ToString(i);
		}

		public static string ToString(AnimationType i)
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

		private AnimationType(int value)
		{
			v_ = value;
		}

		public static AnimationType CreateInternal(int value)
		{
			return new AnimationType(value);
		}

		public int Int
		{
			get { return v_; }
		}

		public static bool operator==(AnimationType a, AnimationType b)
		{
			return (a.v_ == b.v_);
		}

		public static bool operator!=(AnimationType a, AnimationType b)
		{
			return (a.v_ != b.v_);
		}

		public override bool Equals(object o)
		{
			return (o is AnimationType) && (((AnimationType)o).v_ == v_);
		}

		public override int GetHashCode()
		{
			return v_;
		}
	}
}
