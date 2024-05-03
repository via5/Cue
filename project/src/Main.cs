using System;
using System.Threading;

namespace Cue
{
	public class CueMain
	{
		const float MaxDeltaTime = 0.1f;

		static private CueMain instance_ = null;

		private readonly CueToken token_ = new CueToken();
		private CueMainImpl impl_;
		private string pluginId_;
		private string pluginPath_;
		private Cue cue_ = null;
		private Sys.ISys sys_ = null;
		private bool enabled_ = true;
		private bool inited_ = false;

		private float lastUpdateTime_ = 0;
		private float lastFixedUpdateTime_ = 0;
		private float lastLateUpdateTime_ = 0;

		private static float lastErrorTime_ = 0;
		private static int errorCount_ = 0;
		private const int MaxErrors = 3;

		public CueMain(CueMainImpl i, string pluginId, string pluginPath)
		{
			instance_ = this;

			impl_ = i;
			pluginId_ = pluginId;
			pluginPath_ = pluginPath;
			cue_ = new Cue();

			Logger.SafeLogInfo($"cue {Version.String}");
		}

		public void Init(Sys.ISys sys)
		{
			inited_ = true;
			sys_ = sys;
			cue_.Init();
			lastUpdateTime_ = CueMainImpl.RealtimeSinceStartup;
			lastFixedUpdateTime_ = CueMainImpl.RealtimeSinceStartup;
			lastLateUpdateTime_ = CueMainImpl.RealtimeSinceStartup;
		}

		static public CueMain Instance
		{
			get { return instance_; }
		}

		public static string PluginCslist
		{
			get { return "Cue.cslist"; }
		}

		public Cue Cue
		{
			get { return cue_; }
		}

		public CueToken Token
		{
			get { return token_; }
		}

		public Sys.ISys Sys
		{
			get { return sys_; }
		}

		public CueMainImpl Impl
		{
			get { return impl_; }
		}

		public string PluginID
		{
			get { return pluginId_; }
		}

		public string PluginPath
		{
			get { return pluginPath_; }
		}

		public bool PluginEnabled
		{
			get { return enabled_; }
		}

		public void DisablePlugin()
		{
			impl_.DisablePlugin();
		}

		public void PluginStateChanged(bool b)
		{
			if (enabled_ != b)
			{
				enabled_ = b;
				cue_?.OnPluginState(enabled_);
			}
		}

		public void FixedUpdate()
		{
			if (!inited_)
				return;

			float s = GetFixedUpdateTime();
			if (s == 0)
				return;

			cue_.FixedUpdate(s);
		}

		public void Update()
		{
			if (!inited_)
				return;

			float s = GetUpdateTime();
			if (s == 0)
				return;

			cue_.Update(s);
		}

		public void LateUpdate()
		{
			if (!inited_)
				return;

			cue_.LateUpdate(GetLateUpdateTime());
		}

		public static void OnException(Exception e)
		{
			Logger.SafeLogError(e.ToString());

			var now = Cue.Instance.Sys.RealtimeSinceStartup;

			if (now - lastErrorTime_ < 1)
			{
				++errorCount_;
				if (errorCount_ > MaxErrors)
				{
					Logger.SafeLogError(
						$"more than {MaxErrors} errors in the last " +
						"second, disabling plugin");

					Instance.impl_.DisablePlugin();
				}
			}
			else
			{
				errorCount_ = 0;
			}

			lastErrorTime_ = now;
		}

		private float GetUpdateTime()
		{
			// Time.deltaTime is capped at a pretty low value, controlled by
			// Time.maximumDeltaTime, which cannot be changed since it also
			// controls FixedUpdate()
			//
			// maximumDeltaTime depends on the physics rate setting, but it
			// seems to be 0.033 at 72hz, and so everything slow down if the fps
			// is lower than 30
			//
			// since cue relies on accurate timing for lots of things, like
			// raising excitement over time, it has its own deltaTime, capped
			// at a much higher value

			if (lastUpdateTime_ == 0)
			{
				lastUpdateTime_ = CueMainImpl.RealtimeSinceStartup;
				return 0;
			}

			float now = CueMainImpl.RealtimeSinceStartup;
			float d = Math.Min(now - lastUpdateTime_, MaxDeltaTime);

			lastUpdateTime_ = now;

			return d * CueMainImpl.TimeScale;
		}

		private float GetFixedUpdateTime()
		{
			// see GetUpdateTime()

			if (lastFixedUpdateTime_ == 0)
			{
				lastFixedUpdateTime_ = CueMainImpl.RealtimeSinceStartup;
				return 0;
			}

			float now = CueMainImpl.RealtimeSinceStartup;
			float d = Math.Min(now - lastFixedUpdateTime_, MaxDeltaTime);

			lastFixedUpdateTime_ = now;

			return d * CueMainImpl.TimeScale;
		}

		private float GetLateUpdateTime()
		{
			// see GetUpdateTime()

			if (lastLateUpdateTime_ == 0)
			{
				lastLateUpdateTime_ = CueMainImpl.RealtimeSinceStartup;
				return 0;
			}

			float now = CueMainImpl.RealtimeSinceStartup;
			float d = Math.Min(now - lastLateUpdateTime_, MaxDeltaTime);

			lastLateUpdateTime_ = now;

			return d * CueMainImpl.TimeScale;
		}
	}

	public class CueToken
	{
		private float time_;

		public CueToken()
		{
			time_ = CueMainImpl.RealtimeSinceStartup;
		}

		public bool Same(CueToken other)
		{
			return (time_ == other.time_);
		}

		public override string ToString()
		{
			return $"{time_}";
		}
	}


#if MOCK
	public class CueMainImpl
	{
		private CueMain main_;
		private Sys.Mock.MockSys sys_ = null;

		public CueMainImpl()
		{
			main_ = new CueMain(this, "", "");
		}

		public static void Main()
		{
			new CueMainImpl().Run();
		}

		public void Run()
		{
			sys_ = new Sys.Mock.MockSys();
			main_.Init(sys_);

			for (; ; )
			{
				main_.FixedUpdate();
				main_.Update();
				main_.LateUpdate();

				Thread.Sleep(1);
				sys_.Tick();
			}
		}

		public static float RealtimeSinceStartup
		{
			get
			{
				return (float)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
			}
		}

		public static float TimeScale
		{
			get { return 1.0f; }
		}

		public void DisablePlugin()
		{
		}
	}
#else
	public class CueMainImpl : MVRScript
	{
		private CueMain main_ = null;
		private Sys.Vam.VamSys sys_ = null;
		private MVRScriptUI scriptUI_ = null;

		public CueMainImpl()
		{
		}

		public bool PluginEnabled
		{
			get { return enabledJSON.val; }
		}

		public static float RealtimeSinceStartup
		{
			get { return Time.realtimeSinceStartup; }
		}

		public static float TimeScale
		{
			get { return Time.timeScale; }
		}


		public override void Init()
		{
			base.Init();

			try
			{
				main_ = new CueMain(this, name, GetPluginPath());

				sys_ = new Sys.Vam.VamSys(this);
				sys_.OnReady(OnSysReady);
			}
			catch(PluginGone)
			{
				Logger.SafeLogError("plugin disabled");
			}
			catch (Exception e)
			{
				Logger.SafeLogError(e.ToString());
				Logger.SafeLogError("cue: Init failed, disabling");
				DisablePlugin();
			}
		}

		private void OnSysReady()
		{
			try
			{
				main_.Init(sys_);
			}
			catch (PluginGone)
			{
				Logger.SafeLogError("plugin disabled");
			}
			catch(Exception e)
			{
				Logger.SafeLogError(e.ToString());
				Logger.SafeLogError("cue: DoInit failed, disabling");
				DisablePlugin();
			}
		}

		public void FixedUpdate()
		{
			try
			{
				main_.FixedUpdate();
			}
			catch(PluginGone)
			{
				Logger.SafeLogError("plugin disabled");
			}
			catch(Exception e)
			{
				CueMain.OnException(e);
			}
		}

		public void Update()
		{
			try
			{
				main_.Update();
			}
			catch(PluginGone)
			{
				Logger.SafeLogError("plugin disabled");
			}
			catch(Exception e)
			{
				CueMain.OnException(e);
			}
		}

		public void LateUpdate()
		{
			try
			{
				main_.LateUpdate();
			}
			catch(PluginGone)
			{
				Logger.SafeLogError("plugin disabled");
			}
			catch(Exception e)
			{
				CueMain.OnException(e);
			}
		}

		public void OnEnable()
		{
			try
			{
				main_?.PluginStateChanged(true);
			}
			catch(PluginGone)
			{
				Logger.SafeLogError("plugin disabled");
			}
			catch(Exception e)
			{
				CueMain.OnException(e);
			}
		}

		public void OnDisable()
		{
			try
			{
				main_.PluginStateChanged(false);
			}
			catch(PluginGone)
			{
				Logger.SafeLogError("plugin disabled");
			}
			catch(Exception e)
			{
				CueMain.OnException(e);
			}
		}

		public MVRScriptUI MVRScriptUI
		{
			get
			{
				if (scriptUI_ == null)
				{
					scriptUI_ = UITransform
						?.GetComponentInChildren<MVRScriptUI>();
				}

				return scriptUI_;
			}
		}

		public MVRPluginManager MVRPluginManager
		{
			get { return manager; }
		}

		private string GetPluginPath()
		{
			// based on MacGruber, which was based on VAMDeluxe, which was
			// in turn based on Alazi

			string id = name.Substring(0, name.IndexOf('_'));
			string filename = manager.GetJSON()["plugins"][id].Value;

			var path = filename.Substring(
				0, filename.LastIndexOfAny(new char[] { '/', '\\' }));

			path = path.Replace('/', '\\');
			if (path.EndsWith("\\"))
				path = path.Substring(0, path.Length - 1);

			return path;
		}

		public void DisablePlugin()
		{
			enabledJSON.val = false;
			throw new PluginGone();
		}
	}
#endif
}
