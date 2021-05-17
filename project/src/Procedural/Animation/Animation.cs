using System.Collections.Generic;
using UnityEngine;

namespace Cue.Proc
{
	class ProcAnimation : IAnimation
	{
		private readonly string name_;
		private bool forcesOnly_;
		private readonly List<Step> steps_ = new List<Step>();

		public ProcAnimation(string name, bool forcesOnly=false)
		{
			name_ = name;
			forcesOnly_= forcesOnly;
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

		public bool ForcesOnly
		{
			get { return forcesOnly_; }
		}

		public Step AddStep()
		{
			var s = new Step();
			steps_.Add(s);
			return s;
		}

		public List<Step> Steps
		{
			get { return steps_; }
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

		public void FixedUpdate(float s)
		{
			steps_[0].FixedUpdate(s);
		}

		public void Update(float s)
		{
			steps_[0].Update(s);
		}

		public override string ToString()
		{
			string s = name_;

			if (forcesOnly_)
				s += " (forces only)";

			return s;
		}
	}
}
