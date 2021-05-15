using System.Collections.Generic;

namespace Cue.Proc
{
	class Step
	{
		private readonly List<ITarget> targets_ = new List<ITarget>();
		private bool done_ = false;

		public Step Clone()
		{
			var s = new Step();

			foreach (var c in targets_)
				s.targets_.Add(c.Clone());

			return s;
		}

		public bool Done
		{
			get { return done_; }
		}

		public void Start(Person p)
		{
			done_ = false;
			for (int i = 0; i < targets_.Count; ++i)
				targets_[i].Start(p);
		}

		public void AddController(string name, Vector3 pos, Vector3 rot)
		{
			targets_.Add(new Controller(name, pos, rot));
		}

		public void AddForce(string rbId, Vector3 min, Vector3 max, Duration d)
		{
			targets_.Add(new Force(rbId, min, max, d));
		}

		public void Reset()
		{
			for (int i = 0; i < targets_.Count; ++i)
				targets_[i].Reset();
		}

		public void Update(float s)
		{
			for (int i = 0; i < targets_.Count; ++i)
			{
				targets_[i].Update(s);
				done_ = done_ || targets_[i].Done;
			}
		}

		public void FixedUpdate(float s)
		{
			for (int i = 0; i < targets_.Count; ++i)
			{
				targets_[i].FixedUpdate(s);
				done_ = done_ || targets_[i].Done;
			}
		}
	}
}
