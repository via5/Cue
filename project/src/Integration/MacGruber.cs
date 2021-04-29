using System;

namespace Cue
{
	class MacGruberBreather : IBreather
	{
		private Person person_;
		private W.VamFloatParameter intensity_;
		private float lastIntensity_ = 0;

		public MacGruberBreather(Person p)
		{
			person_ = p;
			intensity_ = new W.VamFloatParameter(p, "MacGruber.Breathing", "Intensity");
		}

		public float Intensity
		{
			get
			{
				lastIntensity_ = intensity_.GetValue();
				return lastIntensity_;
			}

			set
			{
				intensity_.SetValue(value);
				lastIntensity_ = value;
			}
		}

		// not supported
		public float Speed
		{
			get { return 0; }
			set { }
		}

		public override string ToString()
		{
			return $"MacGruber: intensity={lastIntensity_:0.000} speed=n/a";
		}
	}


	class MacGruberOrgasmer : IOrgasmer
	{
		private Person person_ = null;
		private W.VamActionParameter action_;

		public MacGruberOrgasmer(Person p)
		{
			person_ = p;
			action_ = new W.VamActionParameter(p, "MacGruber.Breathing", "QueueOrgasm");
		}

		public void Orgasm()
		{
			action_.Fire();
		}
	}


	abstract class BasicGazer : IGazer
	{
		protected Person person_ = null;

		public abstract int LookAt { get; set; }
		public abstract Vector3 Target { get; set; }
		public abstract void Update(float s);

		protected BasicGazer(Person p)
		{
			person_ = p;
		}

		public abstract bool Blink { get; set; }

		public void LookInFront()
		{
			LookAt = GazeSettings.LookAtTarget;

			Target =
				person_.HeadPosition +
				Vector3.Rotate(new Vector3(0, 0, 1), person_.Bearing);
		}
	}


	class MacGruberGaze : BasicGazer
	{
		private int lookat_ = GazeSettings.LookAtDisabled;
		private W.VamBoolParameter toggle_;
		private W.VamBoolParameter lookatTarget_;
		private VamEyes eyes_ = null;

		public MacGruberGaze(Person p)
			: base(p)
		{
			eyes_ = new VamEyes(p);
			toggle_ = new W.VamBoolParameter(p, "MacGruber.Gaze", "enabled");
			lookatTarget_ = new W.VamBoolParameter(p, "MacGruber.Gaze", "LookAt EyeTarget");
		}

		public override bool Blink
		{
			get { return eyes_.Blink; }
			set { eyes_.Blink = value; }
		}

		public override int LookAt
		{
			get
			{
				return lookat_;
			}

			set
			{
				lookat_ = value;
				eyes_.LookAt = value;
				Set();
			}
		}

		public override Vector3 Target
		{
			get { return eyes_.Target; }
			set { eyes_.Target = value; }
		}

		public override void Update(float s)
		{
			eyes_.Update(s);
		}

		private void Set()
		{
			switch (lookat_)
			{
				case GazeSettings.LookAtDisabled:
				{
					toggle_.SetValue(false);
					break;
				}

				case GazeSettings.LookAtTarget:
				{
					toggle_.SetValue(true);
					lookatTarget_.SetValue(true);
					break;
				}

				case GazeSettings.LookAtPlayer:
				{
					toggle_.SetValue(true);
					lookatTarget_.SetValue(false);
					break;
				}
			}
		}

		public override string ToString()
		{
			return
				$"MacGruber: " +
				$"lookat={GazeSettings.ToString(lookat_)} " +
				$"target={Target}";
		}
	}
}
