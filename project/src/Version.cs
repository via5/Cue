namespace Cue
{
	class Version
	{
		public static readonly int Major = 3;
		public static readonly int Minor = 1;
		public static readonly int Patch = 0;

		public static string String
		{
			get
			{
				string s = $"{Major}.{Minor}";

				if (Patch != 0)
					s += $".{Patch}";

				return s;
			}
		}

		public static string DisplayString
		{
			get
			{
				return "Cue " + String;
			}
		}
	}
}
