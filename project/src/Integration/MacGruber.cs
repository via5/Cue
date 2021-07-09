namespace Cue
{
	class MacGruberBreather : IBreather
	{
		struct Pair
		{
			public Sys.Vam.FloatParameter min, max;

			public Pair(Sys.Vam.FloatParameter min, Sys.Vam.FloatParameter max)
			{
				this.min = min;
				this.max = max;
			}
		}

		struct Parameters
		{
			public Sys.Vam.StringChooserParameter breathDataset_;
			public Sys.Vam.StringChooserParameter orgasmDataset_;
			public Sys.Vam.FloatParameter intensity;
			public Sys.Vam.FloatParameter desktopVolume;
			public Sys.Vam.FloatParameter vrVolume;
			public Sys.Vam.FloatParameter pitch;

			public Pair chestMorph, chestJointDrive, stomach, mouthMorph;
			public Sys.Vam.FloatParameter chestJointDriveSpring;
			public Sys.Vam.FloatParameter mouthOpenTime, mouthCloseTime;
			public Sys.Vam.FloatParameter lipsMorphMax;
			public Sys.Vam.FloatParameter noseInMorphMax, noseOutMorphMax;
		}

		private Person person_;
		private Parameters p_ = new Parameters();

		public MacGruberBreather(Person p)
		{
			person_ = p;

			p_.breathDataset_ = BSC("Breath Dataset");
			p_.orgasmDataset_ = BSC("Orgasm Dataset");
			p_.intensity = BF("Intensity");
			p_.desktopVolume = AAF("Volume Desktop");
			p_.vrVolume = AAF("Volume VR");
			p_.pitch = BF("Pitch");
			p_.chestMorph = DBPF("ChestMorph Min", "ChestMorph Max");
			p_.chestJointDrive = DBPF("ChestJointDrive Min", "ChestJointDrive Max");
			p_.stomach = DBPF("StomachMin", "StomachMax");
			p_.mouthMorph = DBPF("MouthMorph Min", "MouthMorph Max");
			p_.chestJointDriveSpring = DBF("ChestJointDrive Spring");
			p_.mouthOpenTime = DBF("Mouth Open Time");
			p_.mouthCloseTime = DBF("Mouth Close Time");
			p_.lipsMorphMax = DBF("LipsMorph Max");
			p_.noseInMorphMax = DBF("NoseInMorph Max");
			p_.noseOutMorphMax = DBF("NoseOutMorph Max");

			p_.breathDataset_.Value = p.Physiology.Voice;
			p_.orgasmDataset_.Value = "Candy Orgasm Soft";
			p_.vrVolume.Value = p_.desktopVolume.Value;
			p_.pitch.Value = 0.8f + person_.Physiology.VoicePitch * 0.4f;
		}

		private Sys.Vam.FloatParameter DBF(string name)
		{
			return new Sys.Vam.FloatParameter(
				person_, "MacGruber.DriverBreathing", name);
		}

		private Pair DBPF(string min, string max)
		{
			return new Pair(
				new Sys.Vam.FloatParameter(
					person_, "MacGruber.DriverBreathing", min),
				new Sys.Vam.FloatParameter(
					person_, "MacGruber.DriverBreathing", max));
		}

		private Sys.Vam.StringChooserParameter BSC(string name)
		{
			return new Sys.Vam.StringChooserParameter(
				person_, "MacGruber.Breathing", name);
		}

		private Sys.Vam.FloatParameter BF(string name)
		{
			return new Sys.Vam.FloatParameter(
				person_, "MacGruber.Breathing", name);
		}

		private Sys.Vam.FloatParameter AAF(string name)
		{
			return new Sys.Vam.FloatParameter(
				person_, "MacGruber.AudioAttenuation", name);
		}

		public float Intensity
		{
			get
			{
				return p_.intensity.Value;
			}

			set
			{
				p_.intensity.Value = value;
				Apply();
			}
		}

		// not supported, tied to pitch
		//
		public float Speed
		{
			get { return 0; }
			set { }
		}

		private void Apply()
		{
			var f = Intensity;

			var p = p_.mouthMorph.max.Parameter;
			if (p != null)
				p.val = p.defaultVal * f;

			p = p_.lipsMorphMax.Parameter;
			if (p != null)
				p.val = p.defaultVal * f;

			p = p_.chestMorph.max.Parameter;
			if (p != null)
				p.val = p.defaultVal * f;

			p = p_.stomach.max.Parameter;
			if (p != null)
				p.val = p.defaultVal * f;
		}

		public override string ToString()
		{
			return $"MacGruber: v={p_.intensity} pitch={p_.pitch.Value}";
		}
	}


	class MacGruberOrgasmer : IOrgasmer
	{
		private Person person_ = null;
		private Sys.Vam.ActionParameter action_;

		public MacGruberOrgasmer(Person p)
		{
			person_ = p;
			action_ = new Sys.Vam.ActionParameter(
				p, "MacGruber.Breathing", "QueueOrgasm");
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
		private Sys.Vam.BoolParameter enabled_;
		private Sys.Vam.FloatParameter gazeDuration_;
		private Sys.Vam.FloatParameter maxAngleHor_;
		private Sys.Vam.FloatParameter maxAngleVer_;

		// 12
		private Sys.Vam.BoolParameter lookatTarget_;

		// 13+
		private Sys.Vam.StringChooserParameter lookatAtom_;
		private Sys.Vam.StringChooserParameter lookatControl_;


		public MacGruberGaze(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Integration, p, "MacGruberGaze");

			enabled_ = new Sys.Vam.BoolParameter(p, "MacGruber.Gaze", "enabled");
			gazeDuration_ = new Sys.Vam.FloatParameter(p, "MacGruber.Gaze", "Gaze Duration");
			maxAngleHor_ = new Sys.Vam.FloatParameter(p, "MacGruber.Gaze", "Max Angle Horizontal");
			maxAngleVer_ = new Sys.Vam.FloatParameter(p, "MacGruber.Gaze", "Max Angle Vertical");

			lookatTarget_ = new Sys.Vam.BoolParameter(p, "MacGruber.Gaze", "LookAt EyeTarget");
			lookatAtom_ = new Sys.Vam.StringChooserParameter(p, "MacGruber.Gaze", "LookAt Atom");
			lookatControl_ = new Sys.Vam.StringChooserParameter(p, "MacGruber.Gaze", "LookAt Control");

			enabled_.Value = false;
			maxAngleHor_.Value = 90;
			maxAngleVer_.Value = 60;
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
						log_.Verbose("enabling");
					else
						log_.Verbose("disabling");

					enabled_.Value = value;
				}
			}
		}

		public float Duration
		{
			get
			{
				return gazeDuration_.Value;
			}

			set
			{
				gazeDuration_.Value = value;
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
			return $"MacGruber: enabled={enabled_} d={gazeDuration_}";
		}
	}
}
