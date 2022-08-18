using System.Collections.Generic;

namespace Cue
{
	class CWVersionChecker
	{
		private string pluginName_;
		private string expectedVersion_;

		private Dictionary<string, Sys.Vam.StringParameter> versions_ =
			new Dictionary<string, Sys.Vam.StringParameter>();

		public CWVersionChecker(string pluginName, string expectedVersion)
		{
			pluginName_ = pluginName;
			expectedVersion_ = expectedVersion;
		}

		public string GetWarning(Person p)
		{
			Sys.Vam.StringParameter v;

			if (!versions_.TryGetValue(p.ID, out v))
			{
				Logger.Global.Info("!");
				v = new Sys.Vam.StringParameter(p.VamAtom, pluginName_, "version");
				versions_.Add(p.ID, v);
			}

			if (v.Check())
			{
				if (v.Value == expectedVersion_)
					return "";
			}

			return $"bad {pluginName_} plugin, use the one from Cue/third-party";
		}
	};


	abstract class ClockwiseHJAnimation : BuiltinAnimation
	{
		private const float WaitForDoneTime = 5;
		private const string PluginName = "ClockwiseSilver.HJ";
		private const string PluginVersion = "2";

		private Logger log_;
		private Sys.Vam.BoolParameter enabled_ = null;
		private Sys.Vam.BoolParameter active_ = null;
		private Sys.Vam.BoolParameter doReturn_ = null;
		private Sys.Vam.BoolParameter pause_ = null;
		private Sys.Vam.BoolParameterRO running_ = null;
		private Sys.Vam.StringChooserParameter male_ = null;
		private Sys.Vam.StringChooserParameter hand_ = null;
		protected Sys.Vam.FloatParameter volume_ = null;
		protected Sys.Vam.FloatParameter speed_ = null;

		private bool wasActive_ = false;
		protected Person leftTarget_ = null;
		protected Person rightTarget_ = null;

		private bool wasSilent_ = false;
		private bool waitForDone_ = false;
		private float waitForDoneElapsed_ = 0;

		private static CWVersionChecker versionChecker_ =
			new CWVersionChecker(PluginName, PluginVersion);

		protected ClockwiseHJAnimation(string name)
			: base(name)
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

			pause_.Value = false;

			return DoStart(target);
		}

		protected abstract bool DoStart(Person target);

		public override void RequestStop(int stopFlags = Animation.NoStopFlags)
		{
			base.RequestStop(stopFlags);

			doReturn_.Value = !Bits.IsSet(stopFlags, Animation.StopNoReturn);
			active_.Value = false;
			leftTarget_ = null;
			rightTarget_ = null;
		}

		public override bool Pause()
		{
			pause_.Value = true;
			return true;
		}

		public override bool Resume()
		{
			pause_.Value = false;
			return true;
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

			UpdateSpeed(s);

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

		private void UpdateSpeed(float s)
		{
			speed_.Value = speed_.DefaultValue + SpeedRange * MovementEnergy;
		}

		private float SpeedRange
		{
			get { return (speed_.Maximum - speed_.DefaultValue); }
		}

		private float MovementEnergy
		{
			get { return Mood.MultiMovementEnergy(Person, leftTarget_, rightTarget_); }
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
			log_ = new Logger(Logger.Integration, p, "cwhj");

			enabled_  = BOP(p, "enabled");
			active_   = BOP(p, "isActive");
			doReturn_ = BOP(p, "doReturn");
			pause_    = BOP(p, "pause");
			running_  = BOPRO(p, "isHJRoutine");
			male_     = SCP(p, "Atom");
			hand_     = SCP(p, "handedness");
			speed_    = FP(p, "Speed");
			volume_   = FP(p, "Audio Volume");

			active_.Value = false;

			if (!Cue.Instance.Options.HJAudio)
			{
				wasSilent_ = true;
				volume_.Value = 0;
			}
		}

		private Sys.Vam.BoolParameter BOP(Person p, string param)
		{
			return new Sys.Vam.BoolParameter(p, PluginName, param);
		}

		private Sys.Vam.BoolParameterRO BOPRO(Person p, string param)
		{
			return new Sys.Vam.BoolParameterRO(p, PluginName, param);
		}

		private Sys.Vam.FloatParameter FP(Person p, string param)
		{
			return new Sys.Vam.FloatParameter(p, PluginName, param);
		}

		private Sys.Vam.StringChooserParameter SCP(Person p, string param)
		{
			return new Sys.Vam.StringChooserParameter(p, PluginName, param);
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
