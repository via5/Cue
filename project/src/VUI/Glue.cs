using System.Diagnostics;

namespace VUI
{
	public class Glue
	{
		public delegate MVRPluginManager PluginManagerDelegate();
		private static PluginManagerDelegate getPluginManager_;

		public delegate string StringDelegate(string s, params object[] ps);
		private static StringDelegate getString_;

		public delegate void LogDelegate(string s);
		private static LogDelegate logInfo_, logWarning_, logError_, logVerbose_;

		public static void Set(
			PluginManagerDelegate getPluginManager,
			StringDelegate getString,
			LogDelegate logVerbose, LogDelegate logInfo,
			LogDelegate logWarning, LogDelegate logError)
		{
			getPluginManager_ = getPluginManager;
			getString_ = getString;
			logVerbose_ = logVerbose;
			logInfo_ = logInfo;
			logWarning_ = logWarning;
			logError_ = logError;
		}

		public static MVRPluginManager PluginManager
		{
			get { return getPluginManager_(); }
		}

		public static string GetString(string s, params object[] ps)
		{
			return getString_(s, ps);
		}

		public static void LogVerbose(string s)
		{
			logVerbose_(s);
		}

		public static void LogInfo(string s)
		{
			logInfo_(s);
		}

		public static void LogWarning(string s)
		{
			logWarning_(s);
		}

		public static void LogWarningST(string s)
		{
			logWarning_(s + "\n" + new StackTrace(1).ToString());
		}

		public static void LogError(string s)
		{
			logError_(s);
		}

		public static void LogErrorST(string s)
		{
			logError_(s + "\n" + new StackTrace(1).ToString());
		}
	}
}
