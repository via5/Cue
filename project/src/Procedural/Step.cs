using System.Collections.Generic;

namespace Cue.Proc
{
	class Step
	{
		private readonly List<Controller> cs_ = new List<Controller>();
		private bool done_ = false;

		public Step Clone()
		{
			var s = new Step();

			foreach (var c in cs_)
				s.cs_.Add(c.Clone());

			return s;
		}

		public bool Done
		{
			get { return done_; }
		}

		public void Start(Person p)
		{
			done_ = false;
			for (int i = 0; i < cs_.Count; ++i)
				cs_[i].Start(p);
		}

		public void AddController(string name, Vector3 pos, Vector3 rot)
		{
			cs_.Add(new Controller(name, pos, rot));
		}

		public void Reset()
		{
			for (int i = 0; i < cs_.Count; ++i)
				cs_[i].Reset();
		}

		public void Update(float s)
		{
			for (int i = 0; i < cs_.Count; ++i)
			{
				cs_[i].Update(s);
				done_ = done_ || cs_[i].Done;
			}
		}
	}
}
