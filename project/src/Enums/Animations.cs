// auto generated from AnimationsEnums.tt

using System.Collections.Generic;

namespace Cue
{
	class Animations
	{
		public const int None = -1;
		public const int Idle = 0;
		public const int Sex = 1;
		public const int Frottage = 2;
		public const int Orgasm = 3;
		public const int Smoke = 4;
		public const int SuckFinger = 5;
		public const int Penetrated = 6;
		public const int RightFinger = 7;
		public const int LeftFinger = 8;
		public const int Kiss = 9;
		public const int HandjobBoth = 10;
		public const int HandjobLeft = 11;
		public const int HandjobRight = 12;
		public const int Blowjob = 13;

		public const int Count = 14;
		public int GetCount() { return 14; }


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

		public static int FromString(string s)
		{
			for (int i = 0; i<names_.Length; ++i)
			{
				if (names_[i] == s)
					return i;
			}

			return -1;
		}

		public static int[] FromStringMany(string s)
		{
			var list = new List<int>();
			var ss = s.Split(' ');

			foreach (string p in ss)
			{
				string tp = p.Trim();
				if (tp == "")
					continue;

				var i = FromString(tp);
				if (i != -1)
					list.Add(i);
			}

			return list.ToArray();
		}

		public string GetName(int i)
		{
			return ToString(i);
		}

		public static string ToString(int i)
		{
			if (i >= 0 && i < names_.Length)
				return names_[i];
			else
				return $"?{i}";
		}

		public static string[] Names
		{
			get { return names_; }
		}
	}
}
