namespace Cue
{
	abstract class ClockwiseHJAnimation : BuiltinAnimation
	{
		private const float WaitForDoneTime = 5;

		private Logger log_;
		private Sys.Vam.BoolParameter enabled_ = null;
		private Sys.Vam.BoolParameter active_ = null;
		private Sys.Vam.BoolParameterRO running_ = null;
		private Sys.Vam.StringChooserParameter male_ = null;
		private Sys.Vam.StringChooserParameter hand_ = null;
		protected Sys.Vam.FloatParameter volume_ = null;
		protected Sys.Vam.FloatParameter speedMin_ = null;
		protected Sys.Vam.FloatParameter speedMax_ = null;

		private bool wasActive_ = false;
		protected Person leftTarget_ = null;
		protected Person rightTarget_ = null;

		private bool wasSilent_ = false;
		private bool waitForDone_ = false;
		private float waitForDoneElapsed_ = 0;

		protected ClockwiseHJAnimation(string name)
			: base(name)
		{
		}

		public override void Reset(Person p)
		{
			var active = new Sys.Vam.BoolParameter(p, "ClockwiseSilver.HJ", "isActive");
			active.Value = false;
		}

		public override bool Done
		{
			get { return !active_.Value && !running_.Value; }
		}

		private bool Active
		{
			get { return running_.Value; }
		}

		public override bool Start(Person p, AnimationContext cx)
		{
			if (!base.Start(p, cx))
				return false;

			Init(p);

			if (!active_.Check())
			{
				log_.Error("can't start, plugin not found");
				return false;
			}

			if (cx == null)
			{
				log_.Error("can't start, context is null");
				return false;
			}

			var target = cx.ps as Person;
			if (target == null)
			{
				log_.Error("can't start, target is null");
				return false;
			}

			return DoStart(target);
		}

		protected abstract bool DoStart(Person target);

		public override void RequestStop()
		{
			base.RequestStop();

			active_.Value = false;
			leftTarget_ = null;
			rightTarget_ = null;
		}

		public override void Update(float s)
		{
			base.Update(s);

			if (waitForDone_)
			{
				if (leftTarget_ == null && rightTarget_ == null)
				{
					// stopped
					log_.Info("animation was stopped before cw could start");
					waitForDone_ = false;
					return;
				}

				waitForDoneElapsed_ += s;

				if (!running_.Value)
				{
					log_.Info("finally stopped, starting");
					active_.Value = true;
					waitForDone_ = false;
				}
				else if (waitForDoneElapsed_ >= WaitForDoneTime)
				{
					log_.Info("still running, giving up");
					RequestStop();
					waitForDone_ = false;
					return;
				}
			}

			wasActive_ = Active;
			if (!wasActive_)
				return;

			// speed
			{
				var minRangeMin = 1.0f;
				var minRangeMax = 4.0f;

				var maxRangeMin = 4.0f;
				var maxRangeMax = 8.0f;

				var minRange = minRangeMax - minRangeMin;
				var maxRange = maxRangeMax - maxRangeMin;

				var e = Mood.MultiMovementEnergy(Person, leftTarget_, rightTarget_);
				var minSpeed = minRangeMin + minRange * e;
				var maxSpeed = maxRangeMin + maxRange * e;

				speedMin_.Value = minSpeed;
				speedMax_.Value = maxSpeed;
			}

			if (wasSilent_ && Cue.Instance.Options.HJAudio)
			{
				wasSilent_ = false;
				volume_.Value = volume_.DefaultValue;
			}
			else if (!wasSilent_ && !Cue.Instance.Options.HJAudio)
			{
				wasSilent_ = true;
				volume_.Value = 0;
			}
		}

		private void Init(Person p)
		{
			log_ = new Logger(Logger.Integration, p, "cwhj");
			enabled_ = new Sys.Vam.BoolParameter(p, "ClockwiseSilver.HJ", "enabled");
			active_ = new Sys.Vam.BoolParameter(p, "ClockwiseSilver.HJ", "isActive");
			running_ = new Sys.Vam.BoolParameterRO(p, "ClockwiseSilver.HJ", "isHJRoutine");
			male_ = new Sys.Vam.StringChooserParameter(p, "ClockwiseSilver.HJ", "Atom");
			hand_ = new Sys.Vam.StringChooserParameter(p, "ClockwiseSilver.HJ", "handedness");
			speedMin_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Speed Min");
			speedMax_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Speed Max");
			volume_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Audio Volume");

			active_.Value = false;

			if (!Cue.Instance.Options.HJAudio)
			{
				wasSilent_ = true;
				volume_.Value = 0;
			}
		}

		protected bool StartCommon(Person target, string hand)
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
				log_.Error(
					$"can't start with {target}, "+
					$"target is not male and has no dildo");

				return false;
			}

			hand_.Value = hand;
			enabled_.Value = true;

			if (!active_.Value && running_.Value)
			{
				waitForDone_ = true;
				waitForDoneElapsed_ = 0;
				log_.Info("still running, waiting for done");
			}
			else
			{
				waitForDone_ = false;
				active_.Value = true;
			}

			return true;
		}

		protected void StartSingleHandCommon()
		{
			// no-op
		}
	}


	class ClockwiseHJBothAnimation : ClockwiseHJAnimation
	{
		public ClockwiseHJBothAnimation()
			: base("cwHJboth")
		{
		}

		public override BuiltinAnimation Clone()
		{
			var a = new ClockwiseHJBothAnimation();
			a.CopyFrom(this);
			return a;
		}

		protected override bool DoStart(Person target)
		{
			if (!StartCommon(target, "Both"))
				return false;

			leftTarget_ = target;
			rightTarget_ = target;

			return true;
		}
	}


	class ClockwiseHJLeftAnimation : ClockwiseHJAnimation
	{
		public ClockwiseHJLeftAnimation()
			: base("cwHJleft")
		{
		}

		public override BuiltinAnimation Clone()
		{
			var a = new ClockwiseHJLeftAnimation();
			a.CopyFrom(this);
			return a;
		}

		protected override bool DoStart(Person target)
		{
			if (!StartCommon(target, "Left"))
				return false;

			StartSingleHandCommon();

			leftTarget_ = target;
			rightTarget_ = null;

			return true;
		}
	}


	class ClockwiseHJRightAnimation : ClockwiseHJAnimation
	{
		public ClockwiseHJRightAnimation()
			: base("cwHJright")
		{
		}

		public override BuiltinAnimation Clone()
		{
			var a = new ClockwiseHJRightAnimation();
			a.CopyFrom(this);
			return a;
		}

		protected override bool DoStart(Person target)
		{
			if (!StartCommon(target, "Right"))
				return false;

			StartSingleHandCommon();

			leftTarget_ = null;
			rightTarget_ = target;

			return true;
		}
	}
}
