// auto generated from AnimationsEnums.tt

using System.Collections.Generic;

namespace Cue
{
	class Animations
	{
		public const int None = -1;
		public const int Sex = 0;
		public const int Idle = 1;
		public const int Orgasm = 2;
		public const int Smoke = 3;
		public const int Suck = 4;
		public const int Penetrated = 5;
		public const int RightFinger = 6;
		public const int LeftFinger = 7;
		public const int Kiss = 8;

		public const int Count = 9;
		public int GetCount() { return 9; }


		private static string[] names_ = new string[]
		{
			"sex",
			"idle",
			"orgasm",
			"smoke",
			"suck",
			"penetrated",
			"rightFinger",
			"leftFinger",
			"kiss",
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
