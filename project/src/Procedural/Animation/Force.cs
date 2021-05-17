using UnityEngine;

namespace Cue.Proc
{
	abstract class BasicForce : ITarget
	{
		protected int bodyPart_;
		protected string rbId_;
		protected Rigidbody rb_ = null;
		protected Vector3 min_, max_;
		private Vector3 last_, current_;
		protected Duration duration_;
		protected Duration delay_;
		private IEasing easing_ = new SinusoidalEasing();
		private Person person_ = null;
		private bool wasBusy_ = false;
		private bool inDelay_ = false;

		public BasicForce(
			int bodyPart, string rbId, Vector3 min, Vector3 max,
			Duration d, Duration delay)
		{
			bodyPart_ = bodyPart;
			rbId_ = rbId;
			min_ = min;
			max_ = max;
			duration_ = d;
			delay_ = delay;
		}

		public bool Done
		{
			get { return false; }
		}

		public abstract ITarget Clone();

		public void Start(Person p)
		{
			person_ = p;

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
			current_ = Vector3.Zero;
			Next();
		}

		public void FixedUpdate(float s)
		{
			if (person_.Body.Get(bodyPart_).Busy)
			{
				wasBusy_ = true;
				return;
			}
			else if (wasBusy_)
			{
				Reset();
				wasBusy_ = false;
			}

			if (inDelay_)
			{
				delay_.Update(s);
				if (delay_.Finished)
					inDelay_ = false;
			}
			else
			{
				duration_.Update(s);
				Apply(Lerped());

				if (duration_.Finished)
				{
					if (delay_.Enabled)
						inDelay_ = true;
					else
						Next();
				}
			}
		}

		private float Magnitude
		{
			get { return easing_.Magnitude(duration_.Progress); }
		}

		private Vector3 Lerped()
		{
			return Vector3.Lerp(last_, current_, Magnitude);
		}

		public void Update(float s)
		{
		}

		public override string ToString()
		{
			return
				$"{rbId_} ({BodyParts.ToString(bodyPart_)})\n" +
				$"min={min_} max={max_}\n" +
				$"last={last_} current={current_}\n" +
				$"dur={duration_} delay={delay_} p={duration_.Progress:0.00}\n" +
				$"mag={Magnitude:0.00} lerped={Lerped()}";
		}

		protected abstract void Apply(Vector3 v);

		private void Next()
		{
			last_ = current_;

			current_ = new Vector3(
				U.RandomFloat(min_.X, max_.X),
				U.RandomFloat(min_.Y, max_.Y),
				U.RandomFloat(min_.Z, max_.Z));
		}
	}


	class Force : BasicForce
	{
		public Force(
			int bodyPart, string rbId, Vector3 min, Vector3 max,
			Duration d, Duration delay)
				: base(bodyPart, rbId, min, max, d, delay)
		{
		}

		public override ITarget Clone()
		{
			return new Force(bodyPart_, rbId_, min_, max_, duration_, delay_);
		}

		protected override void Apply(Vector3 v)
		{
			rb_.AddRelativeForce(W.VamU.ToUnity(v));
		}

		public override string ToString()
		{
			return "force " + base.ToString();
		}
	}


	class Torque : BasicForce
	{
		public Torque(
			int bodyPart, string rbId, Vector3 min, Vector3 max,
			Duration d, Duration delay)
				: base(bodyPart, rbId, min, max, d, delay)
		{
		}

		public override ITarget Clone()
		{
			return new Torque(bodyPart_, rbId_, min_, max_, duration_, delay_);
		}

		protected override void Apply(Vector3 v)
		{
			rb_.AddRelativeTorque(W.VamU.ToUnity(v));
		}

		public override string ToString()
		{
			return "torque " + base.ToString();
		}
	}
}
