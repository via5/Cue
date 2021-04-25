using System.Collections.Generic;
using System.Security;
using UnityEngine;

namespace Cue
{
	class ProceduralPlayer : IPlayer
	{
		private ProceduralAnimation anim_ = null;

		public bool Playing
		{
			get
			{
				return (anim_ != null);
			}
		}

		public bool Play(IAnimation a, int flags)
		{
			if (anim_ == a)
				return true;

			anim_ = a as ProceduralAnimation;
			if (anim_ == null)
				return false;

			anim_.Reset();
			return true;
		}

		public void Stop(bool rewind)
		{
			anim_ = null;
		}

		public void FixedUpdate(float s)
		{
			if (anim_ != null)
				anim_.FixedUpdate(s);
		}

		public void Update(float s)
		{
			if (anim_ != null)
				anim_.Update(s);
		}

		public override string ToString()
		{
			return "Procedural: " + (anim_ == null ? "(none)" : anim_.ToString());
		}
	}


	class ProceduralAnimation : BasicAnimation
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

				rb_.AddForce(Vector3.ToUnity(f));
			}

			public override string ToString()
			{
				return rb_.name;
			}
		}


		private Person person_;
		private string name_;
		private readonly List<Part> parts_ = new List<Part>();

		public ProceduralAnimation(Person p, string name)
		{
			person_ = p;
			name_ = name;
		}

		public void Add(string rbId, Vector3 f, float time)
		{
			var rb = Cue.Instance.VamSys.FindRigidbody(
				((W.VamAtom)person_.Atom).Atom, rbId);

			parts_.Add(new Part(rb, f, time));
		}

		public void Reset()
		{
			for (int i = 0; i < parts_.Count; ++i)
				parts_[i].Reset();
		}

		public void Update(float s)
		{
			for (int i = 0; i < parts_.Count; ++i)
				parts_[i].Update(s);
		}

		public void FixedUpdate(float s)
		{
		}

		public override string ToString()
		{
			string s = name_;

			if (parts_.Count == 0)
				s += " (empty)";
			else
				s += " (" + parts_[0].ToString() + ")";

			return s;
		}
	}
}
