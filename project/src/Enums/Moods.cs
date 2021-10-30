// auto generated from MoodsEnums.tt

namespace Cue
{
	class Moods
	{
		public const int Happy = 0;
		public const int Excited = 1;
		public const int Angry = 2;
		public const int Tired = 3;

		public const int Count = 4;
		public int GetCount() { return 4; }


		private static string[] names_ = new string[]
		{
			"happy",
			"excited",
			"angry",
			"tired",
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
