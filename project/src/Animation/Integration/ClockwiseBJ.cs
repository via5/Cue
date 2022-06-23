namespace Cue
{
	class ClockwiseBJAnimation : BuiltinAnimation
	{
		private Logger log_;
		private Person target_ = null;

		private Sys.Vam.BoolParameter enabled_ = null;
		private Sys.Vam.BoolParameter active_ = null;
		private Sys.Vam.BoolParameterRO running_ = null;
		private Sys.Vam.StringChooserParameter male_ = null;
		private Sys.Vam.FloatParameter sfxVolume_ = null;
		private Sys.Vam.FloatParameter moanVolume_ = null;
		private Sys.Vam.FloatParameter overallSpeed_ = null;
		private Sys.Vam.FloatParameter speedMin_ = null;
		private Sys.Vam.FloatParameter speedMax_ = null;

		private bool wasActive_ = false;

		public ClockwiseBJAnimation()
			: base("cwBJ")
		{
		}

		public override void Reset(Person p)
		{
			var active = new Sys.Vam.BoolParameter(p, "ClockwiseSilver.BJ", "isActive");
			active.Value = false;
		}

		public override BuiltinAnimation Clone()
		{
			var a = new ClockwiseBJAnimation();
			a.CopyFrom(this);
			return a;
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

			if (cx == null)
			{
				log_.Error("can't start, context is null");
				return false;
			}

			var target = cx.ps as Person;
			if (target == null)
			{
				log_.Error("can't start, no target");
				return false;
			}

			if (!active_.Check())
			{
				log_.Error("can't start, plugin not found");
				return false;
			}

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

		public override void RequestStop()
		{
			active_.Value = false;
			target_ = null;
		}

		public override void Update(float s)
		{
			base.Update(s);

			wasActive_ = Active;
			if (!wasActive_)
				return;

			// speed
			{
				var minRange = speedMin_.Maximum - speedMin_.DefaultValue;
				var maxRange = speedMax_.Maximum - speedMax_.DefaultValue;
				var range = overallSpeed_.Maximum - overallSpeed_.DefaultValue;
				var p =  Mood.MultiMovementEnergy(Person, target_);

				speedMin_.Value = speedMin_.DefaultValue + minRange * p;
				speedMax_.Value = speedMax_.DefaultValue + maxRange * p;
				overallSpeed_.Value = overallSpeed_.DefaultValue + range * p;
			}
		}

		private void Init(Person p)
		{
			log_ = new Logger(Logger.Integration, p, "cwbj");
			enabled_ = new Sys.Vam.BoolParameter(p, "ClockwiseSilver.BJ", "enabled");
			active_ = new Sys.Vam.BoolParameter(p, "ClockwiseSilver.BJ", "isActive");
			running_ = new Sys.Vam.BoolParameterRO(p, "ClockwiseSilver.BJ", "isBJRoutine");
			male_ = new Sys.Vam.StringChooserParameter(p, "ClockwiseSilver.BJ", "Atom");
			moanVolume_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Moan Volume");
			sfxVolume_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "SFX Volume");

			overallSpeed_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Overall Speed");
			speedMin_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Speed Min");
			speedMax_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Speed Max");

			active_.Value = false;

			// handled by vammoan
			moanVolume_.Value = 0;
			sfxVolume_.Value = 0;
		}
	}
}
