namespace Cue
{
	class ClockwiseSilverHandjob : IHandjob
	{
		private Person person_;
		private Logger log_;
		private Sys.Vam.BoolParameter enabled_ = null;
		private Sys.Vam.BoolParameter active_ = null;
		private Sys.Vam.BoolParameterRO running_ = null;
		private Sys.Vam.StringChooserParameter male_ = null;
		private Sys.Vam.StringChooserParameter hand_ = null;
		private Sys.Vam.FloatParameter handX_ = null;
		private Sys.Vam.FloatParameter handY_ = null;
		private Sys.Vam.FloatParameter handZ_ = null;
		private Sys.Vam.FloatParameter closeMax_ = null;
		private Sys.Vam.FloatParameter speed_ = null;
		private Sys.Vam.FloatParameter zStrokeMax_ = null;
		private Sys.Vam.FloatParameter hand2Side_ = null;
		private Sys.Vam.FloatParameter hand2UpDown_ = null;
		private Sys.Vam.FloatParameter topOnlyChance_ = null;

		private float elapsed_ = 0;
		private bool closedHand_ = false;
		private bool wasActive_ = false;
		private Person leftTarget_ = null;
		private Person rightTarget_ = null;

		public ClockwiseSilverHandjob(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Integration, p, "ClockwiseHJ");
			enabled_ = new Sys.Vam.BoolParameter(p, "ClockwiseSilver.HJ", "enabled");
			active_ = new Sys.Vam.BoolParameter(p, "ClockwiseSilver.HJ", "isActive");
			running_ = new Sys.Vam.BoolParameterRO(p, "ClockwiseSilver.HJ", "isHJRoutine");
			male_ = new Sys.Vam.StringChooserParameter(p, "ClockwiseSilver.HJ", "Atom");
			hand_ = new Sys.Vam.StringChooserParameter(p, "ClockwiseSilver.HJ", "handedness");
			handX_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Hand Side/Side");
			handY_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Hand Fwd/Bkwd");
			handZ_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Hand Shift Up/Down");
			closeMax_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Hand Close Max");
			speed_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Overall Speed");
			zStrokeMax_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Z Stroke Max");
			hand2Side_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Hand2 Side/Side");
			hand2UpDown_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Hand2 Shift Up/Down");
			topOnlyChance_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Top Only Chance");

			active_.Value = false;
		}

		public bool Active
		{
			get { return running_.Value; }
		}

		public bool LeftUsed
		{
			get { return (wasActive_ && (leftTarget_ != null)); }
		}

		public bool RightUsed
		{
			get { return (wasActive_ && (rightTarget_ != null)); }
		}

		public Person[] Targets
		{
			get
			{
				if (wasActive_)
				{
					if (leftTarget_ != null && rightTarget_ != null)
						return new Person[] { leftTarget_, rightTarget_ };
					else if (leftTarget_ != null)
						return new Person[] { leftTarget_ };
					else if (rightTarget_ != null)
						return new Person[] { rightTarget_ };
				}

				return null;
			}
		}

		public bool StartBoth(Person p)
		{
			if (!StartCommon(p, "Both"))
				return false;

			leftTarget_ = p;
			rightTarget_ = p;

			zStrokeMax_.Value = 10;
			hand2Side_.Value = -0.05f;
			hand2UpDown_.Value = 0.15f;
			topOnlyChance_.Value = 0;

			return true;
		}

		public bool StartLeft(Person p)
		{
			if (!StartCommon(p, "Left"))
				return false;

			StartSingleHandCommon(p);

			leftTarget_ = p;
			rightTarget_ = null;

			return true;
		}

		public bool StartRight(Person p)
		{
			if (!StartCommon(p, "Right"))
				return false;

			StartSingleHandCommon(p);

			leftTarget_ = null;
			rightTarget_ = p;

			return true;
		}

		private bool StartCommon(Person target, string hand)
		{
			if (target.Atom.IsMale)
			{
				male_.Value = target.ID;
			}
			else if (target.Body.HasPenis)
			{
				var s = target.Body.Get(BP.Penis).Sys as Sys.Vam.StraponBodyPart;
				male_.Value = s.Dildo.ID;
			}
			else
			{
				log_.Error("can't start, target is not male and has no dildo");
				return false;
			}

			hand_.Value = hand;
			enabled_.Value = true;

			handX_.Value = 0;
			handY_.Value = 0;
			handZ_.Value = 0;
			closeMax_.Value = 0.5f;

			elapsed_ = 0;
			closedHand_ = false;

			active_.Value = true;

			return true;
		}

		private void StartSingleHandCommon(Person p)
		{
			zStrokeMax_.Value = zStrokeMax_.DefaultValue;
			topOnlyChance_.Value = 0.1f;
		}

		public void Stop()
		{
			active_.Value = false;
			leftTarget_ = null;
			rightTarget_ = null;
		}

		public void StopLeft()
		{
			// todo
			Stop();
		}

		public void StopRight()
		{
			// todo
			Stop();
		}

		public void Update(float s)
		{
			wasActive_ = Active;
			if (!wasActive_)
				return;

			if (!closedHand_)
			{
				elapsed_ += s;
				if (elapsed_ >= 2)
				{
					closedHand_ = true;
					//closeMax_.Value = 0.75f;
				}
			}

			// speed
			{
				// not too fast
				var range = (speed_.Maximum - speed_.DefaultValue) * 0.9f;

				var v = speed_.DefaultValue + range * person_.Mood.MovementEnergy;
				speed_.Value = v;
			}
		}

		public void OnPluginState(bool b)
		{
		}

		public override string ToString()
		{
			return
				$"ClockwiseHJ: " +
				$"running={running_} " +
				$"active={active_}";
		}
	}


	class ClockwiseSilverBlowjob : IBlowjob
	{
		private Person person_;
		private Logger log_;
		private Person target_ = null;

		private Sys.Vam.BoolParameter enabled_ = null;
		private Sys.Vam.BoolParameter active_ = null;
		private Sys.Vam.BoolParameterRO running_ = null;
		private Sys.Vam.StringChooserParameter male_ = null;
		private Sys.Vam.FloatParameter sfxVolume_ = null;
		private Sys.Vam.FloatParameter moanVolume_ = null;
		private Sys.Vam.FloatParameter volumeScaling_ = null;
		private Sys.Vam.FloatParameter headX_ = null;
		private Sys.Vam.FloatParameter headY_ = null;
		private Sys.Vam.FloatParameter headZ_ = null;

		private Sys.Vam.FloatParameter overallSpeed_ = null;
		private Sys.Vam.FloatParameter speedMin_ = null;
		private Sys.Vam.FloatParameter speedMax_ = null;
		private Sys.Vam.FloatParameter topOnlyChance_ = null;
		private Sys.Vam.FloatParameter mouthOpenMax_ = null;

		private bool wasActive_ = false;

		public ClockwiseSilverBlowjob(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Integration, p, "ClockwiseBJ");
			enabled_ = new Sys.Vam.BoolParameter(p, "ClockwiseSilver.BJ", "enabled");
			active_ = new Sys.Vam.BoolParameter(p, "ClockwiseSilver.BJ", "isActive");
			running_ = new Sys.Vam.BoolParameterRO(p, "ClockwiseSilver.BJ", "isBJRoutine");
			male_ = new Sys.Vam.StringChooserParameter(p, "ClockwiseSilver.BJ", "Atom");
			sfxVolume_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "SFX Volume");
			moanVolume_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Moan Volume");
			volumeScaling_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Volume Scaling");
			headX_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Head Side/Side");
			headY_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Head Up/Down");
			headZ_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Head Fwd/Bkwd");

			overallSpeed_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Overall Speed");
			speedMin_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Speed Min");
			speedMax_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Speed Max");
			topOnlyChance_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Top Only Chance");
			mouthOpenMax_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Mouth Open Max");

			active_.Value = false;
			sfxVolume_.Value = 0;
			moanVolume_.Value = 0;
			volumeScaling_.Value = 0;
			headX_.Value = 0;
			headY_.Value = 0;
			headZ_.Value = 0;
			topOnlyChance_.Value = 0.1f;
		}

		public bool Active
		{
			get { return running_.Value; }
		}

		public Person Target
		{
			get
			{
				if (Active)
					return target_;

				return null;
			}
		}

		public bool Start(Person target)
		{
			if (target.Atom.IsMale)
			{
				male_.Value = target.ID;
			}
			else if (target.Body.HasPenis)
			{
				var s = target.Body.Get(BP.Penis).Sys as Sys.Vam.StraponBodyPart;
				male_.Value = s.Dildo.ID;
			}
			else
			{
				log_.Error("can't start, target is not male and has no dildo");
				return false;
			}

			enabled_.Value = true;
			active_.Value = true;
			target_ = target;

			return true;
		}

		public void Stop()
		{
			active_.Value = false;
			target_ = null;
		}

		public void Update(float s)
		{
			wasActive_ = Active;
			if (!wasActive_)
				return;

			// speed
			{
				var minRange = speedMin_.Maximum - speedMin_.DefaultValue;
				var maxRange = speedMax_.Maximum - speedMax_.DefaultValue;
				var range = overallSpeed_.Maximum - overallSpeed_.DefaultValue;
				var p = person_.Mood.MovementEnergy;

				speedMin_.Value = speedMin_.DefaultValue + minRange * p;
				speedMax_.Value = speedMax_.DefaultValue + maxRange * p;
				overallSpeed_.Value = overallSpeed_.DefaultValue + range * p;
			}
		}

		public void OnPluginState(bool b)
		{
		}

		public override string ToString()
		{
			return
				$"ClockwiseBJ: " +
				$"running={running_} " +
				$"active={active_}";
		}
	}
}
