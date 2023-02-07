namespace Cue
{
	public abstract class BasicClockwiseKissAnimation : BuiltinAnimation
	{
		private const string PluginName = "ClockwiseSilver.Kiss";
		private const string PluginVersion = "3";

		private Logger log_;

		private class Params
		{
			public Sys.Vam.BoolParameter enabled = null;
			public Sys.Vam.BoolParameter active = null;
			public Sys.Vam.BoolParameterRO running = null;
			public Sys.Vam.StringChooserParameter atom = null;
			public Sys.Vam.StringChooserParameter targetParam = null;
			public Sys.Vam.BoolParameter trackPos = null;
			public Sys.Vam.BoolParameter trackRot = null;
			public Sys.Vam.FloatParameter headAngleX = null;
			public Sys.Vam.FloatParameter headAngleY = null;
			public Sys.Vam.FloatParameter headAngleZ = null;
			public Sys.Vam.FloatParameter morphDuration = null;
			public Sys.Vam.FloatParameter morphSpeed = null;
			public Sys.Vam.FloatParameter lipMorph = null;
			public Sys.Vam.FloatParameter tongueMorph = null;
			public Sys.Vam.FloatParameter tongueLength = null;
			public Sys.Vam.FloatParameter mouthOpenMin = null;
			public Sys.Vam.FloatParameter mouthOpenMax = null;
			public Sys.Vam.FloatParameter upDownSpeed = null;
			public Sys.Vam.FloatParameter frontBackSpeed = null;
			public Sys.Vam.FloatParameter trackingSpeed = null;
			public Sys.Vam.BoolParameter closeEyes = null;
		}

		protected class Config
		{
			public readonly float StartHeadAngleXWithPlayer = -45;
			public readonly float StartHeadAngleYWithPlayer = 0;
			public readonly float StartHeadAngleZWithPlayer = 0;
			public readonly float StartHeadAngleXLeader = -10;
			public readonly float StartHeadAngleYLeader = 0;
			public readonly float StartHeadAngleZLeader = -20;
			public readonly float StartHeadAngleX = -30;
			public readonly float StartHeadAngleY = 0;
			public readonly float StartHeadAngleZ = -40;

			public readonly float StartTrackingSpeed = 0.1f;
			public readonly float StopTrackingSpeed = 0.1f;
			public readonly float DefaultTrackingSpeed = 0.1f;
			public readonly float TrackingSpeedTime = 3;

			public readonly float MaxMorphSpeed = 5.0f;
			public readonly float MaxLipMorph = 1.0f;
			public readonly float MaxTongueMorph = 0.8f;
			public readonly float MaxTongueLength = 0.2f;
			public readonly float MouthOpenMin = 0;
			public readonly float MouthOpenMax = 0.8f;
			public readonly float MaxUpDownSpeed = 1.0f;
			public readonly float MaxFrontBackSpeed = 1.0f;

			public Config()
			{
			}

			public Config(
				float maxLipMorph, float maxTongueMorph, float maxTongueLength,
				float mouthOpenMin, float mouthOpenMax)
			{
				MaxLipMorph = maxLipMorph;
				MaxTongueMorph = maxTongueMorph;
				MaxTongueLength = maxTongueLength;
				MouthOpenMin = mouthOpenMin;
				MouthOpenMax = mouthOpenMax;
			}

			public void Debug(DebugLines debug)
			{
				debug.Add("StartHeadAngleXWithPlayer", $"{StartHeadAngleXWithPlayer:0.00}");
				debug.Add("StartHeadAngleYWithPlayer", $"{StartHeadAngleYWithPlayer:0.00}");
				debug.Add("StartHeadAngleZWithPlayer", $"{StartHeadAngleZWithPlayer:0.00}");
				debug.Add("StartHeadAngleXLeader", $"{StartHeadAngleXLeader:0.00}");
				debug.Add("StartHeadAngleYLeader", $"{StartHeadAngleYLeader:0.00}");
				debug.Add("StartHeadAngleZLeader", $"{StartHeadAngleZLeader:0.00}");
				debug.Add("StartHeadAngleX", $"{StartHeadAngleX:0.00}");
				debug.Add("StartHeadAngleY", $"{StartHeadAngleY:0.00}");
				debug.Add("StartHeadAngleZ", $"{StartHeadAngleZ:0.00}");
				debug.Add("StartTrackingSpeed", $"{StartTrackingSpeed:0.00}");
				debug.Add("StopTrackingSpeed", $"{StopTrackingSpeed:0.00}");
				debug.Add("DefaultTrackingSpeed", $"{DefaultTrackingSpeed:0.00}");
				debug.Add("TrackingSpeedTime", $"{TrackingSpeedTime:0.00}");
				debug.Add("MaxMorphSpeed", $"{MaxMorphSpeed:0.00}");
				debug.Add("MaxLipMorph", $"{MaxLipMorph:0.00}");
				debug.Add("MaxTongueMorph", $"{MaxTongueMorph:0.00}");
				debug.Add("MaxTongueLength", $"{MaxTongueLength:0.00}");
				debug.Add("MaxUpDownSpeed", $"{MaxUpDownSpeed:0.00}");
				debug.Add("MaxFrontBackSpeed", $"{MaxFrontBackSpeed:0.00}");
			}
		}

		private string[] targetStorableCache_ =
			Sys.Vam.Parameters.MakeStorableNamesCache(PluginName);

		private static CWVersionChecker versionChecker_ =
			new CWVersionChecker(PluginName, PluginVersion);

		private Person target_ = null;
		private readonly Params p_ = new Params();
		private readonly Config c_;
		private bool wasKissing_ = false;
		private float elapsed_ = 0;

		protected BasicClockwiseKissAnimation(string name, Config c)
			: base(name)
		{
			c_ = c;
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
			get { return !p_.running.Value; }
		}

		private bool CanStart()
		{
			if (!p_.active.Check())
			{
				log_.Verbose("can't start, plugin not found");
				return false;
			}

			if (p_.running.Value)
			{
				log_.Verbose("can't start, already active");
				return false;
			}

			return true;
		}

		public override bool Start(Person p, AnimationContext cx)
		{
			if (!base.Start(p, cx))
				return false;

			if (cx == null)
			{
				log_.Error("can't start, context is null");
				return false;
			}

			Init(p);

			return StartReciprocal(p, cx.ps as Person);
		}

		private bool StartReciprocal(Person p, Person target)
		{
			if (target == null)
			{
				log_.Error($"can't start, target is null");
				return false;
			}

			log_.Info($"starting reciprocal with {target}");
			if (!CanStart())
				return false;

			// this person leads by default
			bool leader = true;

			if (Person.IsPlayer)
			{
				// player never leads
				leader = false;
			}
			else if (Person.Body.Get(BP.Head).Grabbed && !target.IsPlayer)
			{
				// this person's head is grabbed and being moved towards a
				// target that's not the player, make the target the leader
				// instead; the target might also be grabbed, but it doesn't
				// matter
				leader = false;
			}
			else if (!CanLead())
			{
				// this animation cannot lead
				leader = false;
			}

			if (leader && TargetIsLeading(target))
			{
				// target is already started and leads
				leader = false;
			}

			DoKiss(target, leader);

			return true;
		}

		protected abstract bool CanLead();

		private bool TargetIsLeading(Person target)
		{
			var active = Sys.Vam.Parameters.GetBool(
				target.VamAtom.Atom, PluginName, "isActive",
				targetStorableCache_);

			if (active != null && active.val)
			{
				var trackPos = Sys.Vam.Parameters.GetBool(
					target.VamAtom.Atom, PluginName, "trackPosition",
					targetStorableCache_);

				if (trackPos != null)
					return trackPos.val;
			}

			return false;
		}

		private void Init(Person p)
		{
			log_ = new Logger(Logger.Integration, p, "cwkiss");

			p_.enabled = new Sys.Vam.BoolParameter(
				p, PluginName, "enabled");

			p_.active = new Sys.Vam.BoolParameter(
				p, PluginName, "isActive");

			p_.running = new Sys.Vam.BoolParameterRO(
				p, PluginName, "Is Kissing");

			p_.atom = new Sys.Vam.StringChooserParameter(
				p, PluginName, "atom");

			p_.targetParam = new Sys.Vam.StringChooserParameter(
				p, PluginName, "kissTargetJSON");

			p_.trackPos = new Sys.Vam.BoolParameter(
				p, PluginName, "trackPosition");

			p_.trackRot = new Sys.Vam.BoolParameter(
				p, PluginName, "trackRotation");

			p_.headAngleX = new Sys.Vam.FloatParameter(
				p, PluginName, "Head Angle X");

			p_.headAngleY = new Sys.Vam.FloatParameter(
				p, PluginName, "Head Angle Y");

			p_.headAngleZ = new Sys.Vam.FloatParameter(
				p, PluginName, "Head Angle Z");

			p_.morphDuration = new Sys.Vam.FloatParameter(
				p, PluginName, "Morph Duration");

			p_.morphSpeed = new Sys.Vam.FloatParameter(
				p, PluginName, "Morph Speed");

			p_.lipMorph = new Sys.Vam.FloatParameter(
				p, PluginName, "Lip Morph Max");

			p_.tongueMorph = new Sys.Vam.FloatParameter(
				p, PluginName, "Tongue Morph Max");

			p_.tongueLength = new Sys.Vam.FloatParameter(
				p, PluginName, "Tongue Length");

			p_.mouthOpenMin = new Sys.Vam.FloatParameter(
				p, PluginName, "Mouth Open Min");

			p_.mouthOpenMax = new Sys.Vam.FloatParameter(
				p, PluginName, "Mouth Open Max");

			p_.upDownSpeed = new Sys.Vam.FloatParameter(
				p, PluginName, "Up Down Speed");

			p_.frontBackSpeed = new Sys.Vam.FloatParameter(
				p, PluginName, "Front Back Speed");

			p_.trackingSpeed = new Sys.Vam.FloatParameter(
				p, PluginName, "Tracking Speed");

			p_.closeEyes = new Sys.Vam.BoolParameter(
				p, PluginName, "closeEyes");

			p_.active.Value = false;
		}

		private void DoKiss(Person target, bool leader)
		{
			p_.enabled.Value = true;

			// force reset
			p_.atom.Value = "";
			p_.targetParam.Value = "";
			p_.atom.Value = target.ID;
			p_.targetParam.Value = "LipTrigger";

			if (target.IsPlayer)
			{
				p_.headAngleX.Value = c_.StartHeadAngleXWithPlayer;
				p_.headAngleY.Value = c_.StartHeadAngleYWithPlayer;
				p_.headAngleZ.Value = c_.StartHeadAngleZWithPlayer;
			}
			else
			{
				if (leader)
				{
					p_.headAngleX.Value = c_.StartHeadAngleXLeader;
					p_.headAngleY.Value = c_.StartHeadAngleYLeader;
					p_.headAngleZ.Value = c_.StartHeadAngleZLeader;
				}
				else
				{
					p_.headAngleX.Value = c_.StartHeadAngleX;
					p_.headAngleY.Value = c_.StartHeadAngleY;
					p_.headAngleZ.Value = c_.StartHeadAngleZ;
				}
			}

			p_.closeEyes.Value = !Person.Gaze.ShouldAvoid(target);
			p_.trackingSpeed.Value = c_.StartTrackingSpeed;
			p_.trackPos.Value = leader && Person.Body.Get(BP.Head).CanApplyForce();
			p_.trackRot.Value = Person.Body.Get(BP.Head).CanApplyForce();
			p_.active.Value = true;
			target_ = target;
			elapsed_ = 0;
		}

		private void SetActive(bool b)
		{
			if (b)
			{
				log_.Info("kiss got activated");

				var target = GetTarget();
				if (target != null)
					log_.Info($"now kissing {target}");
			}
			else
			{
				log_.Info($"kiss stopped");
				Stop();
			}

			wasKissing_ = b;
		}

		private Person GetTarget()
		{
			var atom = p_.atom.Value;
			if (atom != "")
				return Cue.Instance.FindPerson(atom);

			return null;
		}

		public override void RequestStop(int stopFlags)
		{
			base.RequestStop(stopFlags);

			if (p_.active.Value)
			{
				Stop();
			}
		}

		private void Stop()
		{
			log_.Info("stopping");
			p_.trackingSpeed.Value = c_.StopTrackingSpeed;
			p_.active.Value = false;
			target_ = null;
			elapsed_ = 0;
		}

		private float MovementEnergy
		{
			get
			{
				return Mood.MultiMovementEnergy(Person, target_);
			}
		}

		public override void Update(float s)
		{
			base.Update(s);

			var k = p_.running.Value;
			if (wasKissing_ != k)
				SetActive(k);

			if (k)
				elapsed_ += s;

			if (k && p_.active.Value)
			{
				float energy = MovementEnergy;

				// don't go too low
				var range = (p_.morphDuration.DefaultValue - p_.morphDuration.Minimum) * 0.6f;
				p_.morphDuration.Value = p_.morphDuration.DefaultValue - range * energy;

				range = c_.MaxMorphSpeed - p_.morphSpeed.DefaultValue;
				p_.morphSpeed.Value = p_.morphSpeed.DefaultValue + range * energy;

				if (c_.MaxLipMorph < p_.lipMorph.DefaultValue)
				{
					p_.lipMorph.Value = c_.MaxLipMorph;
				}
				else
				{
					range = c_.MaxLipMorph - p_.lipMorph.DefaultValue;
					p_.lipMorph.Value = p_.lipMorph.DefaultValue + range * energy;
				}

				if (Person.ID == "Person#2")
					Log.Info($"{p_.tongueMorph.Value} {c_.MaxTongueMorph}");

				p_.tongueMorph.Value = c_.MaxTongueMorph;
				p_.tongueLength.Value = c_.MaxTongueLength;
				p_.mouthOpenMin.Value = c_.MouthOpenMin;
				p_.mouthOpenMax.Value = c_.MouthOpenMax;

				range = c_.MaxUpDownSpeed - p_.upDownSpeed.DefaultValue;
				p_.upDownSpeed.Value = p_.upDownSpeed.DefaultValue + range * energy;

				range = c_.MaxFrontBackSpeed - p_.frontBackSpeed.DefaultValue;
				p_.frontBackSpeed.Value = p_.frontBackSpeed.DefaultValue + range * energy;
			}

			if (k)
			{
				p_.trackingSpeed.Value = U.Lerp(
					c_.StartTrackingSpeed, c_.DefaultTrackingSpeed,
					(elapsed_ / c_.TrackingSpeedTime));
			}
		}

		public override void Debug(DebugLines debug)
		{
			base.Debug(debug);

			debug.Add(Name);
			debug.Add("active", $"{wasKissing_}");
			debug.Add("target", $"{(target_ == null ? "(none)" : target_.ToString())}");
			debug.Add("elapsed", $"{elapsed_}");
			debug.Add("");

			c_.Debug(debug);
		}
	}

	class ClockwiseKissAnimation : BasicClockwiseKissAnimation
	{
		public ClockwiseKissAnimation()
			: base("cwKiss", MakeConfig())
		{
		}

		public override BuiltinAnimation Clone()
		{
			var a = new ClockwiseKissAnimation();
			a.CopyFrom(this);
			return a;
		}

		protected override bool CanLead()
		{
			return true;
		}

		private static Config MakeConfig()
		{
			return new Config();
		}
	}

	class ClockwiseKissAnimationSleeping : BasicClockwiseKissAnimation
	{
		public ClockwiseKissAnimationSleeping()
			: base("cwKissSleeping", MakeConfig())
		{
		}

		public override BuiltinAnimation Clone()
		{
			var a = new ClockwiseKissAnimationSleeping();
			a.CopyFrom(this);
			return a;
		}

		protected override bool CanLead()
		{
			return false;
		}

		private static Config MakeConfig()
		{
			return new Config(0, 0, 0, 0.5f, 0.5f);
		}
	}
}
