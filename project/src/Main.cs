﻿using System;
using System.Threading;
using UnityEngine;

namespace Cue
{
#if MOCK
	class CueMain
	{
		static private CueMain instance_ = null;

		private Sys.Mock.MockSys sys_ = null;
		private Cue cue_ = null;

		public CueMain()
		{
			instance_ = this;

			sys_ = new Sys.Mock.MockSys();
			cue_ = new Cue();
			cue_.Init();

			float deltaTime = 0;
			long last = 0;

			for (; ; )
			{
				var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

				if (last > 0)
				{
					deltaTime = (float)((now - deltaTime) / 1000.0);

					sys_.Update(Time.deltaTime);
					cue_.FixedUpdate(deltaTime);
					cue_.Update(deltaTime);
				}

				Thread.Sleep(1);
				sys_.Tick();
				last = now;
			}
		}

		public static void Main()
		{
			new CueMain();
		}

		static public CueMain Instance
		{
			get { return instance_; }
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
	class CueMain : MVRScript
	{
		static private CueMain instance_ = null;

		private Sys.ISys sys_ = null;
		private Cue cue_ = null;
		private ScriptUI sui_ = null;
		private bool inited_ = false;

		public CueMain()
		{
			instance_ = this;
		}

		static public CueMain Instance
		{
			get { return instance_; }
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
				sui_ = new ScriptUI();
				sys_.OnReady(DoInit);
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
				sui_.Init();
				inited_ = true;
			}
			catch (Exception e)
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

			U.Safe(() =>
			{
				cue_.FixedUpdate(Time.deltaTime);
			});
		}

		public void Update()
		{
			if (!inited_)
				return;

			U.Safe(() =>
			{
				try
				{
					sys_.Update(Time.deltaTime);
					cue_.Update(Time.deltaTime);
					sui_.Update(Time.deltaTime, cue_.Tickers);
				}
				catch(PluginGone e)
				{
				}
			});
		}

		public void OnEnable()
		{
			if (!inited_)
				return;

			U.Safe(() =>
			{
				if (cue_ != null)
					cue_.OnPluginState(true);

				if (sui_ != null)
					sui_.OnPluginState(true);
			});
		}

		public void OnDisable()
		{
			if (!inited_)
				return;

			U.Safe(() =>
			{
				if (cue_ != null)
					cue_.OnPluginState(false);

				if (sui_ != null)
					sui_.OnPluginState(false);
			});
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
		}
	}
#endif
}
