using System;

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

		public const int All = int.MaxValue;

		private static int enabled_ =
			Action | Event | AI | Command |
			Object | Sys;

		private int type_;
		private Func<string> prefix_;

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

		public void Set(IObject o, string prefix)
		{
			prefix_ = MakePrefix(o, prefix);
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

		public static int Enabled
		{
			get { return enabled_; }
			set { enabled_ = value; }
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
					"resources"
				};
			}
		}

		public string Prefix
		{
			get { return prefix_(); }
		}

		public void Verbose(string s)
		{
			if (IsEnabled() && Cue.LogVerboseEnabled)
				Cue.LogVerbose($"{Prefix}: {s}");
		}

		public void Info(string s)
		{
			if (IsEnabled())
				Cue.LogInfo($"{Prefix}: {s}");
		}

		public void Warning(string s)
		{
			Cue.LogWarning($"{Prefix}: {s}");
		}

		public void Error(string s)
		{
			Cue.LogError($"{Prefix}: {s}");
		}

		public void ErrorST(string s)
		{
			Cue.LogErrorST($"{Prefix}: {s}");
		}

		private bool IsEnabled()
		{
			return Bits.IsSet(enabled_, type_);
		}
	}
}
