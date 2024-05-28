namespace Cue
{
	class ClockwiseBJAnimation : BuiltinAnimation
	{
		private const string PluginName = "ClockwiseSilver.BJ";
		private const string PluginVersion = "2";

		private Logger log_;
		private Person target_ = null;

		private Sys.Vam.BoolParameter enabled_ = null;
		private Sys.Vam.BoolParameter active_ = null;
		private Sys.Vam.BoolParameter doReturn_ = null;
		private Sys.Vam.BoolParameterRO running_ = null;
		private Sys.Vam.StringChooserParameter male_ = null;
		private Sys.Vam.FloatParameter sfxVolume_ = null;
		private Sys.Vam.FloatParameter moanVolume_ = null;
		private Sys.Vam.FloatParameter speed_ = null;

		private bool wasActive_ = false;

		private static CWVersionChecker versionChecker_ =
			new CWVersionChecker(PluginName, PluginVersion);

		public ClockwiseBJAnimation()
			: base("cwBJ")
		{
		}

		public static string GetWarning(Person p)
		{
			return versionChecker_.GetWarning(p);
		}

		public override void Reset(Person p)
		{
			var active = new Sys.Vam.BoolParameter(p, PluginName, "isActive");
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

			if (target.Body.Strapon)
			{
				var s = target.Body.Get(BP.Penis).Sys as Sys.Vam.StraponBodyPart;
				male_.Value = s.Dildo.ID;
			}
			else  if (target.Body.HasPenis)
			{
				male_.Value = target.ID;
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

		public override void RequestStop(int stopFlags = Animation.NoStopFlags)
		{
			doReturn_.Value = !Bits.IsSet(stopFlags, Animation.StopNoReturn);
			active_.Value = false;
			target_ = null;
		}

		public override void Update(float s)
		{
			base.Update(s);

			wasActive_ = Active;
			if (!wasActive_)
				return;

			UpdateSpeed(s);
		}

		private void UpdateSpeed(float s)
		{
			speed_.Value = speed_.DefaultValue + SpeedRange * MovementEnergy;
		}

		private float SpeedRange
		{
			get { return speed_.Maximum - speed_.DefaultValue; }
		}

		private float MovementEnergy
		{
			get { return Mood.MultiMovementEnergy(Person, target_); }
		}

		public override void Debug(DebugLines debug)
		{
			debug.Add("active", $"{Active}");
			debug.Add("");
			debug.Add("SpeedRange", $"{SpeedRange}");
			debug.Add("MovementEnergy", $"{MovementEnergy}");
			debug.Add("");
			debug.Add("Speed", $"{speed_.Value}");
		}

		private void Init(Person p)
		{
			log_ = new Logger(Logger.Integration, p, "cwbj");
			enabled_ = new Sys.Vam.BoolParameter(p, PluginName, "enabled");
			active_ = new Sys.Vam.BoolParameter(p, PluginName, "isActive");
			doReturn_ = new Sys.Vam.BoolParameter(p, PluginName, "doReturn");
			running_ = new Sys.Vam.BoolParameterRO(p, PluginName, "isBJRoutine");
			male_ = new Sys.Vam.StringChooserParameter(p, PluginName, "Atom");
			moanVolume_ = new Sys.Vam.FloatParameter(p, PluginName, "Moan Volume");
			sfxVolume_ = new Sys.Vam.FloatParameter(p, PluginName, "SFX Volume");
			speed_ = new Sys.Vam.FloatParameter(p, PluginName, "Overall Speed");

			active_.Value = false;

			// handled by vammoan
			moanVolume_.Value = 0;
			sfxVolume_.Value = 0;
		}
	}
}
