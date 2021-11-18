// auto generated from SensitivitiesEnums.tt

using System.Collections.Generic;

namespace Cue
{
	public class SS
	{
		public const int None = -1;
		public const int Penetration = 0;
		public const int Mouth = 1;
		public const int Breasts = 2;
		public const int Genitals = 3;
		public const int OthersExcitement = 4;

		public const int Count = 5;
		public int GetCount() { return 5; }


		private static string[] names_ = new string[]
		{
			"penetration",
			"mouth",
			"breasts",
			"genitals",
			"othersExcitement",
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
