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

		public List<ITarget> Targets
		{
			get { return targets_; }
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

		public void AddTarget(ITarget t)
		{
			targets_.Add(t);
		}

		public void Reset()
		{
			for (int i = 0; i < targets_.Count; ++i)
				targets_[i].Reset();
		}

		public void FixedUpdate(float s)
		{
			for (int i = 0; i < targets_.Count; ++i)
				targets_[i].FixedUpdate(s);
		}

		public void Update(float s)
		{
			done_ = true;

			for (int i = 0; i < targets_.Count; ++i)
			{
				targets_[i].Update(s);
				if (!targets_[i].Done)
					done_ = false;
			}
		}

		public override string ToString()
		{
			return $"step, {targets_.Count} targets";
		}
	}
}
