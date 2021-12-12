using System;
using System.Diagnostics;

namespace ClothingManager
{
	static class Log
	{
		public const int ErrorLevel = 0;
		public const int WarningLevel = 1;
		public const int InfoLevel = 2;
		public const int VerboseLevel = 3;

		public static string LevelToShortString(int i)
		{
			switch (i)
			{
				case ErrorLevel: return "E";
				case WarningLevel: return "W";
				case InfoLevel: return "I";
				case VerboseLevel: return "V";
				default: return $"?{i}";
			}
		}

		public static void Out(int level, string s)
		{
			var t = DateTime.Now.ToString("hh:mm:ss.fff");
			string p = LevelToShortString(level);

			if (level == ErrorLevel)
				SuperController.LogError($"{t} !![{p}] {s}");
			else
				SuperController.LogError($"{t}   [{p}] {s}");
		}

		public static void Verbose(string s)
		{
			//Out(VerboseLevel, s);
		}

		public static void Info(string s)
		{
			Out(InfoLevel, s);
		}

		public static void Warning(string s)
		{
			Out(WarningLevel, s);
		}

		public static void Error(string s)
		{
			Out(ErrorLevel, s);
		}

		public static void ErrorST(string s)
		{
			Out(ErrorLevel, $"{s}\n{new StackTrace(1)}");
		}
	}
}
