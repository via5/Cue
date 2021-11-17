using SimpleJSON;
using System;
using System.Threading;
using UnityEngine;

namespace Cue
{
#if MOCK
	public class CueToken
	{
		private int time_;

		public CueToken()
		{
			time_ = System.Environment.TickCount;
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


	public class CueMain
	{
		static private CueMain instance_ = null;

		private readonly CueToken token_ = new CueToken();
		private Sys.Mock.MockSys sys_ = null;
		private Cue cue_ = null;

		public CueMain()
		{
			instance_ = this;

			sys_ = new Sys.Mock.MockSys();
			cue_ = new Cue();
			cue_.Init();
		}

		public void Run()
		{
			float deltaTime = 0;
			long last = 0;

			for (; ; )
			{
				var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

				if (last > 0)
				{
					deltaTime = (float)((now - deltaTime) / 1000.0);

					sys_.Update(deltaTime);
					cue_.FixedUpdate(deltaTime);
					cue_.Update(deltaTime);
					cue_.LateUpdate(deltaTime);
				}

				Thread.Sleep(1);
				sys_.Tick();
				last = now;
			}
		}

		public static void Main()
		{
			new CueMain().Run();
		}

		static public CueMain Instance
		{
			get { return instance_; }
		}

		public CueToken Token
		{
			get { return token_; }
		}

		public UnityEngine.Transform UITransform
		{
			get { return null; }
		}

		public MVRScriptUI MVRScriptUI
		{
			get { return null; }
		}

		public MVRPluginManager MVRPluginManager
		{
			get { return null; }
		}

		public string PluginPath
		{
			get { return ""; }
		}

		public Sys.ISys Sys
		{
			get { return sys_; }
		}

		public void DisablePlugin()
		{
		}
	}
#else
	class CueToken
	{
		private float time_;

		public CueToken()
		{
			time_ = UnityEngine.Time.realtimeSinceStartup;
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


	class CueMain : MVRScript
	{
		static private CueMain instance_ = null;

		private readonly CueToken token_ = new CueToken();
		private Sys.ISys sys_ = null;
		private Cue cue_ = null;
		private bool inited_ = false;

		private static float lastErrorTime_ = 0;
		private static int errorCount_ = 0;
		private const int MaxErrors = 3;

		public CueMain()
		{
			instance_ = this;
		}

		static public CueMain Instance
		{
			get { return instance_; }
		}

		public CueToken Token
		{
			get { return token_; }
		}

		public Sys.ISys Sys
		{
			get { return sys_; }
		}

		public string PluginID
		{
			get { return name; }
		}

		public override void Init()
		{
			base.Init();

			try
			{
				sys_ = new Sys.Vam.VamSys(this);
				cue_ = new Cue();
				sys_.OnReady(DoInit);
			}
			catch(PluginGone e)
			{
				SuperController.LogError("plugin disabled");
			}
			catch (Exception e)
			{
				SuperController.LogError(e.ToString());
				SuperController.LogError("cue: Init failed, disabling");
				DisablePlugin();
			}
		}

		private void DoInit()
		{
			try
			{
				cue_.Init();
				inited_ = true;
			}
			catch(PluginGone e)
			{
				SuperController.LogError("plugin disabled");
			}
			catch(Exception e)
			{
				SuperController.LogError(e.ToString());
				SuperController.LogError("cue: DoInit failed, disabling");
				DisablePlugin();
			}
		}

		public void FixedUpdate()
		{
			if (!inited_)
				return;

			try
			{
				cue_.FixedUpdate(Time.deltaTime);
			}
			catch(PluginGone e)
			{
				SuperController.LogError("plugin disabled");
			}
			catch(Exception e)
			{
				OnException(e);
			}
		}

		public void Update()
		{
			if (!inited_)
				return;

			try
			{
				sys_.Update(Time.deltaTime);
				cue_.Update(Time.deltaTime);
			}
			catch(PluginGone e)
			{
				SuperController.LogError("plugin disabled");
			}
			catch(Exception e)
			{
				OnException(e);
			}
		}

		public void LateUpdate()
		{
			if (!inited_)
				return;

			try
			{
				cue_.LateUpdate(Time.deltaTime);
			}
			catch(PluginGone e)
			{
				SuperController.LogError("plugin disabled");
			}
			catch(Exception e)
			{
				OnException(e);
			}
		}

		public void OnEnable()
		{
			if (!inited_)
				return;

			try
			{
				if (cue_ != null)
					cue_.OnPluginState(true);
			}
			catch(PluginGone e)
			{
				SuperController.LogError("plugin disabled");
			}
			catch(Exception e)
			{
				OnException(e);
			}
		}

		public void OnDisable()
		{
			if (!inited_)
				return;

			try
			{
				if (cue_ != null)
					cue_.OnPluginState(false);
			}
			catch(PluginGone e)
			{
				SuperController.LogError("plugin disabled");
			}
			catch(Exception e)
			{
				OnException(e);
			}
		}

		public MVRScriptUI MVRScriptUI
		{
			get
			{
				return UITransform.GetComponentInChildren<MVRScriptUI>();
			}
		}

		public MVRPluginManager MVRPluginManager
		{
			get { return manager; }
		}

		public string PluginPath
		{
			get
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
		}

		public void DisablePlugin()
		{
			enabledJSON.val = false;
			throw new PluginGone();
		}

		private void OnException(Exception e)
		{
			SuperController.LogError(e.ToString());

			var now = Cue.Instance.Sys.RealtimeSinceStartup;

			if (now - lastErrorTime_ < 1)
			{
				++errorCount_;
				if (errorCount_ > MaxErrors)
				{
					SuperController.LogError(
						$"more than {MaxErrors} errors in the last " +
						"second, disabling plugin");

					Cue.Instance.DisablePlugin();
				}
			}
			else
			{
				errorCount_ = 0;
			}

			lastErrorTime_ = now;
		}
	}
#endif
}
