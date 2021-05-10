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
				lastIntensity_ = intensity_.Value;
				return lastIntensity_;
			}

			set
			{
				intensity_.Value = value;
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
		private const int NotFound = 0;
		private const int Target = 1;
		private const int Control = 2;

		private Person person_;
		private Logger log_;
		private int version_ = NotFound;
		private W.VamBoolParameter enabled_;

		// 12
		private W.VamBoolParameter lookatTarget_;

		// 13+
		private W.VamStringChooserParameter lookatAtom_;
		private W.VamStringChooserParameter lookatControl_;


		public MacGruberGaze(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Integration, p, "MacGruberGaze");
			enabled_ = new W.VamBoolParameter(p, "MacGruber.Gaze", "enabled");
			lookatTarget_ = new W.VamBoolParameter(p, "MacGruber.Gaze", "LookAt EyeTarget");
			lookatAtom_ = new W.VamStringChooserParameter(p, "MacGruber.Gaze", "LookAt Atom");
			lookatControl_ = new W.VamStringChooserParameter(p, "MacGruber.Gaze", "LookAt Control");
			enabled_.Value = false;
		}

		public bool Enabled
		{
			get
			{
				return enabled_.Value;
			}

			set
			{
				if (value != enabled_.Value)
				{
					if (value)
						log_.Info("enabling");
					else
						log_.Info("disabling");

					enabled_.Value = value;
				}
			}
		}

		public void Update(float s)
		{
			if (ParameterChanged())
				SetParameter();
		}

		private void SetParameter()
		{
			switch (version_)
			{
				case Target:
				{
					lookatTarget_.Value = true;
					break;
				}

				case Control:
				{
					lookatAtom_.Value = person_.ID;
					lookatControl_.Value = "eyeControlTarget";
					break;
				}
			}
		}

		private bool ParameterChanged()
		{
			switch (version_)
			{
				case NotFound:
				{
					return FindParameter(false);
				}

				case Target:
				{
					if (!lookatTarget_.Check())
					{
						log_.Error("target param gone");
						return FindParameter(true);
					}

					break;
				}

				case Control:
				{
					if (!lookatControl_.Check())
					{
						log_.Error("control param gone");
						return FindParameter(true);
					}

					break;
				}
			}

			return false;
		}

		private bool FindParameter(bool force)
		{
			if (lookatTarget_.Check(force))
			{
				log_.Info("using target (<13)");
				version_ = Target;
				return true;
			}
			else if (lookatControl_.Check(force))
			{
				log_.Info("using control (>12)");
				version_ = Control;
				return true;
			}
			else
			{
				version_ = NotFound;
			}

			return false;
		}

		public override string ToString()
		{
			return $"MacGruber: enabled={Enabled}";
		}
	}
}
