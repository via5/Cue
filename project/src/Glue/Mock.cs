using System;

namespace Cue.W
{
	class MockSys : ISys
	{
		private static MockSys instance_ = null;
		private readonly MockTime time_ = new MockTime();
		private readonly MockLog log_ = new MockLog();

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

		public IAtom ContainingAtom
		{
			get { return null; }
		}

		public IAtom GetAtom(string id)
		{
			return new MockAtom(id);
		}

		public bool Paused
		{
			get { return false; }
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

		public bool IsPerson
		{
			get { return true; }
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
	}
}
