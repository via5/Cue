using System;
using System.Diagnostics;

namespace Cue
{
	public class Logger
	{
		public const int Animation = 0x001;
		public const int Action = 0x002;
		public const int Event = 0x004;
		public const int AI = 0x008;
		public const int Command = 0x010;
		public const int Integration = 0x020;
		public const int Object = 0x040;
		public const int Slots = 0x080;
		public const int Sys = 0x100;
		public const int Clothing = 0x200;
		public const int Resources = 0x400;
		public const int Main = 0x800;
		public const int UI = 0x1000;

		public const int All = int.MaxValue;

		public const int ErrorLevel = 0;
		public const int WarningLevel = 1;
		public const int InfoLevel = 2;
		public const int VerboseLevel = 3;

		private static int sEnabledTypes =
			Action | Event | AI | Command |
			Object | Sys | Main;

		private static int sLevel = ErrorLevel;

		private static Logger global_ = new Logger(Main, "cue");

		private int type_;
		private Func<string> prefix_;
		private int level_ = InfoLevel;
		private bool forceEnabled_ = false;

		public Logger(int type, string prefix)
		{
			type_ = type;
			prefix_ = () => prefix;
		}

		public Logger(int type, Func<string> prefix)
		{
			type_ = type;
			prefix_ = prefix;
		}

		public Logger(int type, IObject o, string prefix)
		{
			type_ = type;
			prefix_ = MakePrefix(o, prefix);
		}

		public Logger(int type, Sys.IAtom a, string prefix)
		{
			type_ = type;
			prefix_ = MakePrefix(a, prefix);
		}

		static public Logger Global
		{
			get { return global_; }
		}

		public int Level
		{
			get { return level_; }
			set { level_ = value; }
		}

		public bool ForceEnabled
		{
			get { return forceEnabled_; }
			set { forceEnabled_ = value; }
		}

		public void Set(IObject o, string prefix)
		{
			prefix_ = MakePrefix(o, prefix);
		}

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

		private static Func<string> MakePrefix(IObject o, string prefix)
		{
			if (o == null)
				return () => prefix;
			else
				return () => o.ID + (prefix == "" ? "" : "." + prefix);
		}

		private static Func<string> MakePrefix(Sys.IAtom a, string prefix)
		{
			if (a == null)
				return () => prefix;
			else
				return () => a.ID + (prefix == "" ? "" : "." + prefix);
		}

		public static int EnabledTypes
		{
			get { return sEnabledTypes; }
			set { sEnabledTypes = value; }
		}

		public static int GlobalLevel
		{
			get { return sLevel; }
			set { sLevel = value; }
		}

		public static string[] Names
		{
			get
			{
				return new string[]
				{
					"animation",
					"action",
					"event",
					"ai",
					"command",
					"integration",
					"object",
					"slots",
					"sys",
					"clothing",
					"resources",
					"main",
					"ui"
				};
			}
		}

		public string Prefix
		{
			get { return prefix_(); }
		}

		public void Verbose(string s)
		{
			Log(VerboseLevel, s);
		}

		public void Info(string s)
		{
			Log(InfoLevel, s);
		}

		public void Warning(string s)
		{
			Log(WarningLevel, s);
		}

		public void Error(string s)
		{
			Log(ErrorLevel, s);
		}

		public void ErrorST(string s)
		{
			Log(ErrorLevel, s + "\n" + new StackTrace(1).ToString());
		}

		public void Log(int level, string s)
		{
			if (IsEnabled(level))
			{
				if (Cue.Instance == null)
				{
					if (level == ErrorLevel || level == WarningLevel)
						SafeLogError($"{Prefix}: {s}");
					else
						SafeLogInfo($"{Prefix}: {s}");
				}
				else
				{
					Cue.Instance.Sys.LogLines($"{Prefix}: {s}", level);
				}
			}
		}

		public static void SafeLogError(string s)
		{
			SuperController.LogMessage(s);
		}

		public static void SafeLogInfo(string s)
		{
			SuperController.LogMessage(s);
		}

		private bool IsEnabled(int level)
		{
			if (level == ErrorLevel)
				return true;

			if (!forceEnabled_ && !Bits.IsSet(sEnabledTypes, type_))
				return false;

			if (level > level_ && level > sLevel)
				return false;

			return true;
		}
	}
}
