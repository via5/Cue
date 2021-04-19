﻿using System;
using System.Threading;

namespace Cue
{
#if MOCK
	class CueMain
	{
		static private CueMain instance_ = null;

		private W.MockSys sys_ = null;
		private Cue cue_ = null;

		public CueMain()
		{
			instance_ = this;

			sys_ = new W.MockSys();
			cue_ = new Cue(this);
			cue_.Init();

			for (; ; )
			{
				cue_.FixedUpdate();
				cue_.Update();
				Thread.Sleep(1);
				sys_.Tick();
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

		public W.ISys Sys
		{
			get { return sys_; }
		}
	}
#else
	class CueMain : MVRScript
	{
		static private CueMain instance_ = null;

		private W.ISys sys_ = null;
		private Cue cue_ = null;
		private UI.ScriptUI sui_ = null;

		public CueMain()
		{
			instance_ = this;
		}

		static public CueMain Instance
		{
			get { return instance_; }
		}

		public W.ISys Sys
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

			cue_ = new Cue(this);

			try
			{
				sys_ = new W.VamSys(this);
				sui_ = new UI.ScriptUI();
				sys_.OnReady(DoInit);
			}
			catch (Exception e)
			{
				SuperController.LogError(e.Message);
			}
		}

		private void DoInit()
		{
			U.Safe(() =>
			{
				sui_.Init();
				cue_.Init();
			});
		}

		public void FixedUpdate()
		{
			U.Safe(() =>
			{
				cue_.FixedUpdate();
			});
		}

		public void Update()
		{
			U.Safe(() =>
			{
				cue_.Update();
				sui_.Update();
			});
		}

		public void OnEnable()
		{
			U.Safe(() =>
			{
				cue_.OnPluginState(true);
			});
		}

		public void OnDisable()
		{
			U.Safe(() =>
			{
				cue_.OnPluginState(false);
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
	}
#endif
}
