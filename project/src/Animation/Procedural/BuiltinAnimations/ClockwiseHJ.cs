namespace Cue.Proc
{
	abstract class ClockwiseHJ : BasicProcAnimation
	{
		private Logger log_;
		private Sys.Vam.BoolParameter enabled_ = null;
		private Sys.Vam.BoolParameter active_ = null;
		private Sys.Vam.BoolParameterRO running_ = null;
		private Sys.Vam.StringChooserParameter male_ = null;
		private Sys.Vam.StringChooserParameter hand_ = null;
		protected Sys.Vam.FloatParameter handX_ = null;
		protected Sys.Vam.FloatParameter handY_ = null;
		protected Sys.Vam.FloatParameter handZ_ = null;
		protected Sys.Vam.FloatParameter closeMax_ = null;
		protected Sys.Vam.FloatParameter speed_ = null;
		protected Sys.Vam.FloatParameter zStrokeMax_ = null;
		protected Sys.Vam.FloatParameter hand2Side_ = null;
		protected Sys.Vam.FloatParameter hand2UpDown_ = null;
		protected Sys.Vam.FloatParameter topOnlyChance_ = null;

		private float elapsed_ = 0;
		private bool closedHand_ = false;
		private bool wasActive_ = false;
		protected Person leftTarget_ = null;
		protected Person rightTarget_ = null;

		protected ClockwiseHJ(string name)
			: base(name, false)
		{
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

		private void Init(Person p)
		{
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

			handX_.Value = 0;
			handY_.Value = 0;
			handZ_.Value = 0;
			closeMax_.Value = 0.5f;

			elapsed_ = 0;
			closedHand_ = false;

			active_.Value = true;

			return true;
		}

		protected void StartSingleHandCommon()
		{
			zStrokeMax_.Value = zStrokeMax_.DefaultValue;
			topOnlyChance_.Value = 0.1f;
		}
	}


	class ClockwiseHJBoth : ClockwiseHJ
	{
		public ClockwiseHJBoth()
			: base("cwHJboth")
		{
		}

		public override BasicProcAnimation Clone()
		{
			var a = new ClockwiseHJBoth();
			a.CopyFrom(this);
			return a;
		}

		protected override bool DoStart(Person target)
		{
			if (!StartCommon(target, "Both"))
				return false;

			leftTarget_ = target;
			rightTarget_ = target;

			zStrokeMax_.Value = 10;
			topOnlyChance_.Value = 0;

			// this is broken, depends on orientation
			//hand2Side_.Value = -0.05f;
			//hand2UpDown_.Value = 0.15f;

			return true;
		}
	}


	class ClockwiseHJLeft : ClockwiseHJ
	{
		public ClockwiseHJLeft()
			: base("cwHJleft")
		{
		}

		public override BasicProcAnimation Clone()
		{
			var a = new ClockwiseHJLeft();
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


	class ClockwiseHJRight : ClockwiseHJ
	{
		public ClockwiseHJRight()
			: base("cwHJright")
		{
		}

		public override BasicProcAnimation Clone()
		{
			var a = new ClockwiseHJRight();
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
