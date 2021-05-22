namespace Cue
{
	class MacGruberBreather : IBreather
	{
		struct Pair
		{
			public W.VamFloatParameter min, max;

			public Pair(W.VamFloatParameter min, W.VamFloatParameter max)
			{
				this.min = min;
				this.max = max;
			}
		}

		struct Parameters
		{
			public W.VamFloatParameter intensity;
			public W.VamFloatParameter desktopVolume;
			public W.VamFloatParameter vrVolume;
			public W.VamFloatParameter pitch;

			public Pair chestMorph, chestJointDrive, stomach, mouthMorph;
			public W.VamFloatParameter chestJointDriveSpring;
			public W.VamFloatParameter mouthOpenTime, mouthCloseTime;
			public W.VamFloatParameter lipsMorphMax;
			public W.VamFloatParameter noseInMorphMax, noseOutMorphMax;
		}

		private Person person_;
		private Parameters p_ = new Parameters();

		public MacGruberBreather(Person p)
		{
			person_ = p;

			p_.intensity = BP("Intensity");
			p_.desktopVolume = AAP("Volume Desktop");
			p_.vrVolume = AAP("Volume VR");
			p_.pitch = BP("Pitch");
			p_.chestMorph = DBPP("ChestMorph Min", "ChestMorph Max");
			p_.chestJointDrive = DBPP("ChestJointDrive Min", "ChestJointDrive Max");
			p_.stomach = DBPP("StomachMin", "StomachMax");
			p_.mouthMorph = DBPP("MouthMorph Min", "MouthMorph Max");
			p_.chestJointDriveSpring = DBP("ChestJointDrive Spring");
			p_.mouthOpenTime = DBP("Mouth Open Time");
			p_.mouthCloseTime = DBP("Mouth Close Time");
			p_.lipsMorphMax = DBP("LipsMorph Max");
			p_.noseInMorphMax = DBP("NoseInMorph Max");
			p_.noseOutMorphMax = DBP("NoseOutMorph Max");

			p_.vrVolume.Value = p_.desktopVolume.Value;
			p_.pitch.Value = 0.9f + person_.Physiology.VoicePitch * 0.2f;
		}

		private W.VamFloatParameter DBP(string name)
		{
			return new W.VamFloatParameter(
				person_, "MacGruber.DriverBreathing", name);
		}

		private Pair DBPP(string min, string max)
		{
			return new Pair(
				new W.VamFloatParameter(
					person_, "MacGruber.DriverBreathing", min),
				new W.VamFloatParameter(
					person_, "MacGruber.DriverBreathing", max));
		}

		private W.VamFloatParameter BP(string name)
		{
			return new W.VamFloatParameter(
				person_, "MacGruber.Breathing", name);
		}

		private W.VamFloatParameter AAP(string name)
		{
			return new W.VamFloatParameter(
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
			return $"MacGruber: v={p_.intensity}";
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
		private W.VamFloatParameter gazeDuration_;
		private W.VamFloatParameter maxAngleHor_;
		private W.VamFloatParameter maxAngleVer_;

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
			gazeDuration_ = new W.VamFloatParameter(p, "MacGruber.Gaze", "Gaze Duration");
			maxAngleHor_ = new W.VamFloatParameter(p, "MacGruber.Gaze", "Max Angle Horizontal");
			maxAngleVer_ = new W.VamFloatParameter(p, "MacGruber.Gaze", "Max Angle Vertical");

			lookatTarget_ = new W.VamBoolParameter(p, "MacGruber.Gaze", "LookAt EyeTarget");
			lookatAtom_ = new W.VamStringChooserParameter(p, "MacGruber.Gaze", "LookAt Atom");
			lookatControl_ = new W.VamStringChooserParameter(p, "MacGruber.Gaze", "LookAt Control");

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
						log_.Info("enabling");
					else
						log_.Info("disabling");

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
			return $"MacGruber: enabled={enabled_}";
		}
	}
}
