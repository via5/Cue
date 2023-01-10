namespace Cue
{
	class ClockwiseKissAnimation : BuiltinAnimation
	{
		private const string PluginName = "ClockwiseSilver.Kiss";
		private const string PluginVersion = "2";

		private Logger log_;
		private Sys.Vam.BoolParameter enabled_ = null;
		private Sys.Vam.BoolParameter active_ = null;
		private Sys.Vam.BoolParameterRO running_ = null;
		private Sys.Vam.StringChooserParameter atom_ = null;
		private Sys.Vam.StringChooserParameter targetParam_ = null;
		private Sys.Vam.BoolParameter trackPos_ = null;
		private Sys.Vam.BoolParameter trackRot_ = null;
		private Sys.Vam.FloatParameter headAngleX_ = null;
		private Sys.Vam.FloatParameter headAngleY_ = null;
		private Sys.Vam.FloatParameter headAngleZ_ = null;
		private Sys.Vam.FloatParameter morphDuration_ = null;
		private Sys.Vam.FloatParameter morphSpeed_ = null;
		private Sys.Vam.FloatParameter lipMorph_ = null;
		private Sys.Vam.FloatParameter upDownSpeed_ = null;
		private Sys.Vam.FloatParameter frontBackSpeed_ = null;
		private Sys.Vam.FloatParameter trackingSpeed_ = null;
		private Sys.Vam.BoolParameter closeEyes_ = null;
		private bool wasKissing_ = false;
		private float elapsed_ = 0;

		private const float StartHeadAngleXWithPlayer = -45;
		private const float StartHeadAngleYWithPlayer = 0;
		private const float StartHeadAngleZWithPlayer = 0;
		private const float StartHeadAngleXLeader = -10;
		private const float StartHeadAngleYLeader = 0;
		private const float StartHeadAngleZLeader = -20;
		private const float StartHeadAngleX = -30;
		private const float StartHeadAngleY = 0;
		private const float StartHeadAngleZ = -40;

		private const float StartTrackingSpeed = 0.1f;
		private const float StopTrackingSpeed = 0.1f;
		private const float DefaultTrackingSpeed = 0.1f;
		private const float TrackingSpeedTime = 3;

		private const float MaxMorphSpeed = 5.0f;
		private const float MaxLipMorph = 1.0f;
		private const float MaxUpDownSpeed = 1.0f;
		private const float MaxFrontBackSpeed = 1.0f;

		private string[] targetStorableCache_ =
			Sys.Vam.Parameters.MakeStorableNamesCache(PluginName);

		private static CWVersionChecker versionChecker_ =
			new CWVersionChecker(PluginName, PluginVersion);

		private Person target_ = null;


		public ClockwiseKissAnimation()
			: base("cwKiss")
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
			var a = new ClockwiseKissAnimation();
			a.CopyFrom(this);
			return a;
		}

		public override bool Done
		{
			get { return !running_.Value; }
		}

		private bool CanStart()
		{
			if (!active_.Check())
			{
				log_.Verbose("can't start, plugin not found");
				return false;
			}

			if (running_.Value)
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

			if (leader && TargetIsLeading(target))
			{
				// target is already started and leads
				leader = false;
			}

			DoKiss(target, leader);

			return true;
		}

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

			enabled_ = new Sys.Vam.BoolParameter(
				p, PluginName, "enabled");

			active_ = new Sys.Vam.BoolParameter(
				p, PluginName, "isActive");

			running_ = new Sys.Vam.BoolParameterRO(
				p, PluginName, "Is Kissing");

			atom_ = new Sys.Vam.StringChooserParameter(
				p, PluginName, "atom");

			targetParam_ = new Sys.Vam.StringChooserParameter(
				p, PluginName, "kissTargetJSON");

			trackPos_ = new Sys.Vam.BoolParameter(
				p, PluginName, "trackPosition");

			trackRot_ = new Sys.Vam.BoolParameter(
				p, PluginName, "trackRotation");

			headAngleX_ = new Sys.Vam.FloatParameter(
				p, PluginName, "Head Angle X");

			headAngleY_ = new Sys.Vam.FloatParameter(
				p, PluginName, "Head Angle Y");

			headAngleZ_ = new Sys.Vam.FloatParameter(
				p, PluginName, "Head Angle Z");

			morphDuration_ = new Sys.Vam.FloatParameter(
				p, PluginName, "Morph Duration");

			morphSpeed_ = new Sys.Vam.FloatParameter(
				p, PluginName, "Morph Speed");

			lipMorph_ = new Sys.Vam.FloatParameter(
				p, PluginName, "Lip Morph Max");

			upDownSpeed_ = new Sys.Vam.FloatParameter(
				p, PluginName, "Up Down Speed");

			frontBackSpeed_ = new Sys.Vam.FloatParameter(
				p, PluginName, "Front Back Speed");

			trackingSpeed_ = new Sys.Vam.FloatParameter(
				p, PluginName, "Tracking Speed");

			closeEyes_ = new Sys.Vam.BoolParameter(
				p, PluginName, "closeEyes");

			active_.Value = false;
		}

		private void DoKiss(Person target, bool leader)
		{
			enabled_.Value = true;

			// force reset
			atom_.Value = "";
			targetParam_.Value = "";
			atom_.Value = target.ID;
			targetParam_.Value = "LipTrigger";

			if (target.IsPlayer)
			{
				headAngleX_.Value = StartHeadAngleXWithPlayer;
				headAngleY_.Value = StartHeadAngleYWithPlayer;
				headAngleZ_.Value = StartHeadAngleZWithPlayer;
			}
			else
			{
				if (leader)
				{
					headAngleX_.Value = StartHeadAngleXLeader;
					headAngleY_.Value = StartHeadAngleYLeader;
					headAngleZ_.Value = StartHeadAngleZLeader;
				}
				else
				{
					headAngleX_.Value = StartHeadAngleX;
					headAngleY_.Value = StartHeadAngleY;
					headAngleZ_.Value = StartHeadAngleZ;
				}
			}

			closeEyes_.Value = !Person.Gaze.ShouldAvoid(target);

			trackingSpeed_.Value = StartTrackingSpeed;
			trackPos_.Value = leader && Person.Body.Get(BP.Head).CanApplyForce();
			trackRot_.Value = Person.Body.Get(BP.Head).CanApplyForce();
			active_.Value = true;
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
			var atom = atom_.Value;
			if (atom != "")
				return Cue.Instance.FindPerson(atom);

			return null;
		}

		public override void RequestStop(int stopFlags)
		{
			base.RequestStop(stopFlags);

			if (active_.Value)
			{
				Stop();
			}
		}

		private void Stop()
		{
			log_.Info("stopping");
			trackingSpeed_.Value = StopTrackingSpeed;
			active_.Value = false;
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

			var k = running_.Value;
			if (wasKissing_ != k)
				SetActive(k);

			if (k)
				elapsed_ += s;

			if (k && active_.Value)
			{
				float energy = MovementEnergy;

				// don't go too low
				var range = (morphDuration_.DefaultValue - morphDuration_.Minimum) * 0.6f;
				morphDuration_.Value = morphDuration_.DefaultValue - range * energy;

				range = MaxMorphSpeed - morphSpeed_.DefaultValue;
				morphSpeed_.Value = morphSpeed_.DefaultValue + range * energy;

				range = MaxLipMorph - lipMorph_.DefaultValue;
				lipMorph_.Value = lipMorph_.DefaultValue + range * energy;

				range = MaxUpDownSpeed - upDownSpeed_.DefaultValue;
				upDownSpeed_.Value = upDownSpeed_.DefaultValue + range * energy;

				range = MaxFrontBackSpeed - frontBackSpeed_.DefaultValue;
				frontBackSpeed_.Value = frontBackSpeed_.DefaultValue + range * energy;
			}

			if (k)
			{
				trackingSpeed_.Value = U.Lerp(
					StartTrackingSpeed, DefaultTrackingSpeed,
					(elapsed_ / TrackingSpeedTime));
			}
		}
	}
}
