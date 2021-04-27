using System;
using System.Collections;
using System.Collections.Generic;

namespace Cue.W
{
	class MockSys : ISys
	{
		private static MockSys instance_ = null;
		private readonly MockLog log_ = new MockLog();
		private readonly MockNav nav_ = new MockNav();
		private readonly MockInput input_ = new MockInput();

		public MockSys()
		{
			instance_ = this;
		}

		static public MockSys Instance
		{
			get { return instance_; }
		}

		public void Tick()
		{
		}

		public ILog Log
		{
			get { return log_; }
		}

		public INav Nav
		{
			get { return nav_; }
		}

		public IInput Input
		{
			get { return input_; }
		}

		public IAtom ContainingAtom
		{
			get { return null; }
		}

		public IAtom GetAtom(string id)
		{
			return new MockAtom(id);
		}

		public List<IAtom> GetAtoms(bool alsoOff)
		{
			return new List<IAtom>();
		}

		public bool Paused
		{
			get { return false; }
		}

		public bool IsVR
		{
			get { return false; }
		}

		public bool IsPlayMode
		{
			get { return true; }
		}

		public void OnPluginState(bool b)
		{
		}

		public void ReloadPlugin()
		{
		}

		public void OnReady(Action f)
		{
			f?.Invoke();
		}

		public string ReadFileIntoString(string path)
		{
			return "";
		}

		public string GetResourcePath(string path)
		{
			return path;
		}

		public ICanvas CreateHud(Vector3 offset, Point pos, Size size)
		{
			return null;
		}

		public ICanvas CreateAttached(Vector3 offset, Point pos, Size size)
		{
			return null;
		}

		public ICanvas Create2D()
		{
			return null;
		}
	}

	class MockInput : IInput
	{
		public bool ReloadPlugin { get { return false; } }
		public bool MenuToggle { get { return false; } }
		public bool Select { get { return false; } }
		public bool Action { get { return false; } }
		public bool ShowControls { get { return false; } }

		public IObject GetHovered()
		{
			return null;
		}
	}

	class MockLog : ILog
	{
		public void Clear()
		{
		}

		public void Verbose(string s)
		{
			Write("V", s);
		}

		public void Info(string s)
		{
			Write("I", s);
		}

		public void Error(string s)
		{
			Write("E", s);
		}

		private void Write(string p, string s)
		{
			Console.WriteLine("[" + p + "] " + s);
		}
	}

	class MockTriggers : ITriggers
	{
		public bool Lip { get { return false; } }
		public bool Mouth { get { return false; } }
		public bool LeftBreast { get { return false; } }
		public bool RightBreast { get { return false; } }
		public bool Labia { get { return false; } }
		public bool Vagina { get { return false; } }
		public bool DeepVagina { get { return false; } }
		public bool DeeperVagina { get { return false; } }
	}

	class MockAtom : IAtom
	{
		private string id_;
		private MockTriggers triggers_ = new MockTriggers();

		public MockAtom(string id)
		{
			id_ = id;
		}

		public string ID
		{
			get { return id_; }
		}

		public bool IsPerson
		{
			get { return true; }
		}

		public int Sex
		{
			get { return Sexes.Any; }
		}

		public ITriggers Triggers
		{
			get { return triggers_; }
		}

		public Vector3 Position
		{
			get { return Vector3.Zero; }
			set { }
		}

		public Vector3 Direction
		{
			get { return Vector3.Zero; }
			set { }
		}

		public Vector3 HeadPosition
		{
			get { return Vector3.Zero; }
		}

		public void Say(string s)
		{
			Cue.LogInfo(id_ + " says '" + s + "'");
		}

		public void SetDefaultControls()
		{
		}

		public void OnPluginState(bool b)
		{
		}

		public void Update(float s)
		{
		}

		public void TeleportTo(Vector3 v, float bearing)
		{
		}

		public bool NavEnabled
		{
			get { return false; }
			set { }
		}

		public bool NavPaused
		{
			get { return false; }
			set { }
		}

		public int NavState
		{
			get { return NavStates.None; }
		}

		public void NavTo(Vector3 v, float bearing)
		{
		}

		public void NavStop()
		{
		}
	}

	class MockNav : INav
	{
		public void Update()
		{
		}

		public List<Vector3> Calculate(Vector3 from, Vector3 to)
		{
			return new List<Vector3>();
		}

		public bool Render
		{
			get { return false; }
			set { }
		}
	}
}
