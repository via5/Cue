using UnityEngine;

namespace Cue.Proc
{
	class Force : ITarget
	{
		private string rbId_;
		private Rigidbody rb_ = null;
		private Vector3 force_;
		private float time_;
		private float elapsed_ = 0;
		private bool fwd_ = true;

		public Force(string rbId, Vector3 f, float t)
		{
			rbId_ = rbId;
			force_ = f;
			time_ = t;
		}

		public bool Done
		{
			get { return false; }
		}

		public ITarget Clone()
		{
			return new Force(rbId_, force_, time_);
		}

		public void Start(Person p)
		{
			rb_ = Cue.Instance.VamSys.FindRigidbody(p.VamAtom.Atom, rbId_);
			if (rb_ == null)
			{
				Cue.LogError($"Force: rigidbody {rbId_} not found");
				return;
			}
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
}
