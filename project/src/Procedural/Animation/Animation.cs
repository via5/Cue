using System.Collections.Generic;

namespace Cue.Proc
{
	class ProcAnimation : IAnimation
	{
		private readonly string name_;
		private bool forcesOnly_;
		private ConcurrentTargetGroup root_ = new ConcurrentTargetGroup("root");

		public ProcAnimation(string name, bool forcesOnly=false)
		{
			name_ = name;
			forcesOnly_= forcesOnly;
		}

		public ProcAnimation Clone()
		{
			var a = new ProcAnimation(name_, forcesOnly_);
			a.root_ = (ConcurrentTargetGroup)root_.Clone();
			return a;
		}

		public bool Done
		{
			get { return root_.Done; }
		}

		// todo
		public float InitFrame { get { return -1; } }
		public float FirstFrame { get { return -1; } }
		public float LastFrame { get { return -1; } }

		public bool ForcesOnly
		{
			get { return forcesOnly_; }
		}

		public void AddTarget(ITarget t)
		{
			root_.AddTarget(t);
		}

		public List<ITarget> Targets
		{
			get { return root_.Targets; }
		}

		public void Start(Person p)
		{
			root_.Start(p);
		}

		public void Reset()
		{
			root_.Reset();
		}

		public void FixedUpdate(float s)
		{
			root_.FixedUpdate(s);
		}

		public override string ToString()
		{
			return name_ + " " + root_.ToString();
		}

		public string ToDetailedString()
		{
			return ToString();
		}
	}
}
