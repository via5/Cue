// auto generated from AnimationsEnums.tt

namespace Cue
{
	class Animations
	{
		public const int None = -1;
		public const int Sex = 0;
		public const int Idle = 1;
		public const int Expressions = 2;
		public const int Orgasm = 3;
		public const int Smoke = 4;
		public const int Suck = 5;
		public const int Penetrated = 6;
		public const int RightFinger = 7;
		public const int LeftFinger = 8;

		public const int Count = 9;
		public int GetCount() { return 9; }


		private static string[] names_ = new string[]
		{
			"sex",
			"idle",
			"expressions",
			"orgasm",
			"smoke",
			"suck",
			"penetrated",
			"rightFinger",
			"leftFinger",
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
