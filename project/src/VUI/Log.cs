using System;
using System.Diagnostics;

namespace VUI
{
	public class Logger
	{
		public const int ErrorLevel = 0;
		public const int WarningLevel = 1;
		public const int InfoLevel = 2;
		public const int VerboseLevel = 3;

		private Func<string> prefix_;
		private bool enabled_ = true;
		private static Logger global_ = null;

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

		public Logger(string prefix)
		{
			prefix_ = () => prefix;
		}

		public Logger(Func<string> prefix)
		{
			prefix_ = prefix;
		}

		public Logger(Atom a, string prefix)
		{
			prefix_ = () => a.uid + (prefix == "" ? "" : " " + prefix);
		}

		public static Logger Global
		{
			get
			{
				if (global_ == null)
					global_ = new Logger("vui.global");

				return global_;
			}
		}

		public bool Enabled
		{
			get { return enabled_; }
			set { enabled_ = value; }
		}

		public string Prefix
		{
			get { return prefix_(); }
		}

		public void Verbose(string s)
		{
			//if (IsEnabled())
			//	Out(VerboseLevel, $"{Prefix}: {s}");
		}

		public void Info(string s)
		{
			if (IsEnabled())
				Out(InfoLevel, $"{Prefix}: {s}");
		}

		public void InfoST(string s)
		{
			if (IsEnabled())
				Out(InfoLevel, $"{Prefix}: {s}\n{new StackTrace(1)}");
		}

		public void Warning(string s)
		{
			Out(WarningLevel, $"{Prefix}: {s}");
		}

		public void WarningST(string s)
		{
			Out(WarningLevel, $"{Prefix}: {s}\n{new StackTrace(1)}");
		}

		public void Error(string s)
		{
			Out(ErrorLevel, $"{Prefix}: {s}");
		}

		public void ErrorST(string s)
		{
			Out(ErrorLevel, $"{Prefix}: {s}\n{new StackTrace(1)}");
		}

		private bool IsEnabled()
		{
			return enabled_;
		}

		public static void Out(int level, string s)
		{
			var t = DateTime.Now.ToString("hh:mm:ss.fff");
			string p = LevelToShortString(level);

			switch (level)
			{
				case ErrorLevel:
				{
					Glue.LogError(s);
					break;
				}

				case WarningLevel:
				{
					Glue.LogWarning(s);
					break;
				}

				case InfoLevel:
				{
					Glue.LogInfo(s);
					break;
				}

				case VerboseLevel:
				{
					Glue.LogVerbose(s);
					break;
				}
			}
		}
	}
}
