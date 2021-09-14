// auto generated from AnimationsEnums.tt

namespace Cue
{
	class Animations
	{
		public const int None = -1;
		public const int Walk = 0;
		public const int TurnLeft = 1;
		public const int TurnRight = 2;
		public const int Transition = 3;
		public const int Sex = 4;
		public const int Idle = 5;
		public const int Orgasm = 6;
		public const int Smoke = 7;
		public const int Suck = 8;
		public const int Penetrated = 9;

		public const int Count = 10;
		public int GetCount() { return 10; }


		private static string[] names_ = new string[]
		{
			"walk",
			"turnLeft",
			"turnRight",
			"transition",
			"sex",
			"idle",
			"orgasm",
			"smoke",
			"suck",
			"penetrated",
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
