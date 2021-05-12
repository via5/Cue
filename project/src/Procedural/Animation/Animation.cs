using System.Collections.Generic;
using UnityEngine;

namespace Cue.Proc
{
	class ProcAnimation : IAnimation
	{
		private readonly string name_;
		private readonly List<Step> steps_ = new List<Step>();

		public ProcAnimation(string name)
		{
			name_ = name;
		}

		public ProcAnimation Clone()
		{
			var a = new ProcAnimation(name_);

			foreach (var s in steps_)
				a.steps_.Add(s.Clone());

			return a;
		}

		public bool Done
		{
			get { return steps_[0].Done; }
		}

		// todo
		public float InitFrame { get { return -1; } }
		public float FirstFrame { get { return -1; } }
		public float LastFrame { get { return -1; } }

		public Step AddStep()
		{
			var s = new Step();
			steps_.Add(s);
			return s;
		}

		public void Start(Person p)
		{
			for (int i = 0; i < steps_.Count; ++i)
				steps_[i].Start(p);
		}

		public void Reset()
		{
			for (int i = 0; i < steps_.Count; ++i)
				steps_[i].Reset();
		}

		public void Update(float s)
		{
			steps_[0].Update(s);
		}

		public void FixedUpdate(float s)
		{
		}

		public override string ToString()
		{
			return name_;
		}
	}
}
