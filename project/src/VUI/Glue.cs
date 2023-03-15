using System.Diagnostics;

namespace VUI
{
	public interface IIconProvider
	{
		Icon ResizeWE { get; }
	}

	public class NullIconProvider : IIconProvider
	{
		public Icon ResizeWE { get { return null; } }
	}


	public class Glue
	{
		private static string prefix_;

		public delegate MVRPluginManager PluginManagerDelegate();
		private static PluginManagerDelegate getPluginManager_;

		public delegate string StringDelegate(string s, params object[] ps);
		private static StringDelegate getString_;

		public delegate void LogDelegate(string s);
		private static LogDelegate logInfo_, logWarning_, logError_, logVerbose_;

		public delegate IIconProvider IconProviderDelegate();
		public static IconProviderDelegate iconProvider_;
		private static NullIconProvider nullIcons_ = new NullIconProvider();

		private static bool inited_ = false;

		public static void InitInternal(
			string prefix,
			PluginManagerDelegate getPluginManager,
			StringDelegate getString = null,
			LogDelegate logVerbose = null,
			LogDelegate logInfo = null,
			LogDelegate logWarning = null,
			LogDelegate logError = null,
			IconProviderDelegate icons = null)
		{
			inited_ = true;
			prefix_ = prefix;
			getPluginManager_ = getPluginManager;
			getString_ = getString;
			logVerbose_ = logVerbose;
			logInfo_ = logInfo;
			logWarning_ = logWarning;
			logError_ = logError;

			if (icons == null)
				iconProvider_ = () => nullIcons_;
			else
				iconProvider_ = icons;
		}

		public static bool Initialized
		{
			get { return inited_; }
		}

		public static string Prefix
		{
			get { return prefix_; }
		}

		public static MVRPluginManager PluginManager
		{
			get
			{
				if (getPluginManager_ == null)
					return null;
				else
					return getPluginManager_();
			}
		}

		public static IIconProvider IconProvider
		{
			get
			{
				if (iconProvider_ == null)
					return null;
				else
					return iconProvider_();
			}
		}

		public static string GetString(string s, params object[] ps)
		{
			if (getString_ == null)
				return string.Format(s, ps);
			else
				return getString_(s, ps);
		}

		public static void LogVerbose(string s)
		{
			// disabled by default
			if (logVerbose_ != null)
				logVerbose_(s);
		}

		public static void LogInfo(string s)
		{
			if (logInfo_ == null)
				SuperController.LogMessage(s);
			else
				logInfo_(s);
		}

		public static void LogWarning(string s)
		{
			if (logWarning_ == null)
				SuperController.LogMessage(s);
			else
				logWarning_(s);
		}

		public static void LogWarningST(string s)
		{
			LogWarning(s + "\n" + new StackTrace(1).ToString());
		}

		public static void LogError(string s)
		{
			if (logError_ == null)
				SuperController.LogMessage(s);
			else
				logError_(s);
		}

		public static void LogErrorST(string s)
		{
			LogError(s + "\n" + new StackTrace(1).ToString());
		}
	}
}
