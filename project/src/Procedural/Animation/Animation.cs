using System.Collections.Generic;
using UnityEngine;

namespace Cue.Proc
{
	class ProcAnimation : IAnimation
	{
		class Part
		{
			private Rigidbody rb_ = null;
			private Vector3 force_;
			private float time_;
			private float elapsed_ = 0;
			private bool fwd_ = true;

			public Part(Rigidbody rb, Vector3 f, float t)
			{
				rb_ = rb;
				force_ = f;
				time_ = t;
			}

			public void Reset()
			{
				elapsed_ = 0;
				fwd_ = true;
			}

			public void Update(float s)
			{
				elapsed_ += s;
				if (elapsed_ >= time_)
				{
					elapsed_ = 0;
					fwd_ = !fwd_;
				}

				float p = (fwd_ ? (elapsed_ / time_) : ((time_ - elapsed_) / time_));
				var f = force_ * p;

				rb_.AddForce(W.VamU.ToUnity(f));
			}

			public override string ToString()
			{
				return rb_.name;
			}
		}


		//private Person person_ = null;
		private readonly string name_;
		private readonly List<Step> steps_ = new List<Step>();
		//private readonly List<Part> parts_ = new List<Part>();

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

		//public void Add(string rbId, Vector3 f, float time)
		//{
		//	var rb = Cue.Instance.VamSys.FindRigidbody(
		//		((W.VamAtom)person_.Atom).Atom, rbId);
		//
		//	parts_.Add(new Part(rb, f, time));
		//}

		public void Reset()
		{
			for (int i = 0; i < steps_.Count; ++i)
				steps_[i].Reset();

			//for (int i = 0; i < parts_.Count; ++i)
			//	parts_[i].Reset();
		}

		public void Update(float s)
		{
			steps_[0].Update(s);

			//for (int i = 0; i < parts_.Count; ++i)
			//	parts_[i].Update(s);
		}

		public void FixedUpdate(float s)
		{
		}

		public override string ToString()
		{
			string s = name_;

			//if (parts_.Count == 0)
			//	s += " (empty)";
			//else
			//	s += " (" + parts_[0].ToString() + ")";

			return s;
		}
	}
}
