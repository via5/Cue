using System;
using System.Collections;
using System.Collections.Generic;

namespace Cue.W
{
	class MockSys : ISys
	{
		private static MockSys instance_ = null;
		private readonly MockTime time_ = new MockTime();
		private readonly MockLog log_ = new MockLog();
		private readonly MockNav nav_ = new MockNav();

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
			time_.Tick();
		}

		public ITime Time
		{
			get { return time_; }
		}

		public ILog Log
		{
			get { return log_; }
		}

		public INav Nav
		{
			get { return nav_; }
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

		public void OnPluginState(bool b)
		{
			// no-op
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
	}

	class MockTime : ITime
	{
		private float dt_ = 0;
		private long last_ = 0;

		public void Tick()
		{
			var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

			if (last_ > 0)
				dt_ = (float)((now - last_) / 1000.0);

			last_ = now;
		}

		public float deltaTime
		{
			get { return dt_; }
		}
	}

	class MockLog : ILog
	{
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

	class MockAtom : IAtom
	{
		private string id_;

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
			Cue.LogError(id_ + " says '" + s + "'");
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

		public bool NavActive
		{
			get { return false; }
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
