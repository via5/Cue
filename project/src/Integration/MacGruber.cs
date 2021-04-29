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


	class MacGruberGaze : IGazer
	{
		protected Person person_ = null;
		private W.VamBoolParameter toggle_;
		private W.VamBoolParameter lookatTarget_;

		public MacGruberGaze(Person p)
		{
			person_ = p;
			toggle_ = new W.VamBoolParameter(p, "MacGruber.Gaze", "enabled");
			lookatTarget_ = new W.VamBoolParameter(p, "MacGruber.Gaze", "LookAt EyeTarget");
			toggle_.SetValue(false);
		}

		public bool Enabled
		{
			get { return toggle_.GetValue(); }
			set { toggle_.SetValue(value); }
		}

		public void Update(float s)
		{
		}

		public override string ToString()
		{
			return $"MacGruber: enabled={Enabled}";
		}
	}
}
