using System;
using System.Threading;

namespace Cue
{
#if (VAM_GT_1_20)
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

		public void Update()
		{
			U.Safe(() =>
			{
				cue_.Update();
				sui_.Update();
			});
		}

		public void FixedUpdate()
		{
			U.Safe(() =>
			{
				cue_.FixedUpdate();
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
	}

#else
	class CueMain
	{
		static private CueMain instance_ = null;

		private W.ISys sys_ = null;
		private Cue cue_ = null;

		public CueMain()
		{
			instance_ = this;

			var sys = new W.MockSys();
			cue_ = new Cue(this);
			cue_.Init();

			for (; ; )
			{
				cue_.Update();
				Thread.Sleep(1);
				sys.Tick();
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

		public W.ISys Sys
		{
			get { return sys_; }
		}

	}
#endif
}
