using UnityEngine;

namespace Cue.Proc
{
	class Force : ITarget
	{
		private string rbId_;
		private Rigidbody rb_ = null;
		private Vector3 min_, max_;
		private Vector3 last_, current_;
		private Duration duration_;
		private IEasing easing_ = new SinusoidalEasing();

		public Force(string rbId, Vector3 min, Vector3 max, Duration d)
		{
			rbId_ = rbId;
			min_ = min;
			max_ = max;
			duration_ = d;
		}

		public bool Done
		{
			get { return false; }
		}

		public ITarget Clone()
		{
			return new Force(rbId_, min_, max_, duration_);
		}

		public void Start(Person p)
		{
			rb_ = Cue.Instance.VamSys.FindRigidbody(p.VamAtom.Atom, rbId_);
			if (rb_ == null)
			{
				Cue.LogError($"Force: rigidbody {rbId_} not found");
				return;
			}

			last_ = Vector3.Zero;
			Next();
		}

		public void Reset()
		{
			duration_.Reset();
		}

		public void FixedUpdate(float s)
		{
			duration_.Update(s);

			var p = easing_.Magnitude(duration_.Progress);
			rb_.AddForce(W.VamU.ToUnity(Vector3.Lerp(last_, current_, p)));

			if (duration_.Finished)
				Next();
		}

		public void Update(float s)
		{
		}

		public override string ToString()
		{
			return rb_.name;
		}

		private void Next()
		{
			last_ = current_;

			current_ = new Vector3(
				U.RandomFloat(min_.X, max_.X),
				U.RandomFloat(min_.Y, max_.Y),
				U.RandomFloat(min_.Z, max_.Z));
		}
	}
}
