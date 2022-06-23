// auto generated from MoodsEnums.tt

using System.Collections.Generic;

namespace Cue
{
	public class Moods
	{
		public const int None = -1;
		public const int Happy = 0;
		public const int Playful = 1;
		public const int Excited = 2;
		public const int Angry = 3;
		public const int Tired = 4;
		public const int Orgasm = 5;

		public const int Count = 6;
		public int GetCount() { return 6; }


		private static string[] names_ = new string[]
		{
			"happy",
			"playful",
			"excited",
			"angry",
			"tired",
			"orgasm",
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
