﻿namespace Cue
{
	class ClockwiseKissAnimation : BuiltinAnimation
	{
		private Logger log_;
		private Sys.Vam.BoolParameter enabled_ = null;
		private Sys.Vam.BoolParameter active_ = null;
		private Sys.Vam.BoolParameterRO running_ = null;
		private Sys.Vam.StringChooserParameter atom_ = null;
		private Sys.Vam.StringChooserParameter target_ = null;
		private Sys.Vam.BoolParameter trackPos_ = null;
		private Sys.Vam.BoolParameter trackRot_ = null;
		private Sys.Vam.FloatParameter headAngleX_ = null;
		private Sys.Vam.FloatParameter headAngleY_ = null;
		private Sys.Vam.FloatParameter headAngleZ_ = null;
		private Sys.Vam.FloatParameter lipDepth_ = null;
		private Sys.Vam.FloatParameter morphDuration_ = null;
		private Sys.Vam.FloatParameter morphSpeed_ = null;
		private Sys.Vam.FloatParameter tongueLength_ = null;
		private Sys.Vam.FloatParameter trackingSpeed_ = null;
		private Sys.Vam.BoolParameter closeEyes_ = null;
		private bool wasKissing_ = false;
		private bool randomMovements_ = false;
		private bool randomSpeeds_ = false;
		private float elapsed_ = 0;


		private const float MinDistance = 0.001f;
		private const float MaxDistance = 0.008f;
		private const float MaxLipDepth = 0.14f;

		private const float HeadAngleXMin = -10;
		private const float HeadAngleXMax = 10;
		private const float HeadAngleYMin = -10;
		private const float HeadAngleYMax = 10;
		private const float HeadAngleZMin = -10;
		private const float HeadAngleZMax = 10;

		private const float StartHeadAngleXWithPlayer = -45;
		private const float StartHeadAngleYWithPlayer = 0;
		private const float StartHeadAngleZWithPlayer = 0;
		private const float StartHeadAngleXLeader = -10;
		private const float StartHeadAngleYLeader = 0;
		private const float StartHeadAngleZLeader = -20;
		private const float StartHeadAngleX = -30;
		private const float StartHeadAngleY = 0;
		private const float StartHeadAngleZ = -40;
		private const float StartLipDepthLeader = 0.02f;
		private const float StartLipDepth = 0;

		private const float StartTrackingSpeed = 0.1f;
		private const float StopTrackingSpeed = 0.1f;
		private const float TrackingSpeedTime = 3;


		private float startAngleX_ = 0;
		private InterpolatedRandomRange randomHeadAngleX_ =
			new InterpolatedRandomRange(
				new Pair<float, float>(HeadAngleXMin, HeadAngleXMax),
				new Pair<float, float>(0, 5),
				new Pair<float, float>(1, 3));

		private float startAngleY_ = 0;
		private InterpolatedRandomRange randomHeadAngleY_ =
			new InterpolatedRandomRange(
				new Pair<float, float>(HeadAngleYMin, HeadAngleYMax),
				new Pair<float, float>(0, 5),
				new Pair<float, float>(1, 3));

		private float startAngleZ_ = 0;
		private InterpolatedRandomRange randomHeadAngleZ_ =
			new InterpolatedRandomRange(
				new Pair<float, float>(HeadAngleZMin, HeadAngleZMax),
				new Pair<float, float>(0, 5),
				new Pair<float, float>(1, 3));

		private float startLipDepth_ = 0;
		private InterpolatedRandomRange randomLipDepth_ =
			new InterpolatedRandomRange(
				new Pair<float, float>(0, 0.02f),
				new Pair<float, float>(0, 5),
				new Pair<float, float>(1, 3));

		private string[] targetStorableCache_ =
			Sys.Vam.Parameters.MakeStorableNamesCache("ClockwiseSilver.Kiss");


		public ClockwiseKissAnimation()
			: base("cwKiss")
		{
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
				log_.Error("can't start, plugin not found");
				return false;
			}

			if (running_.Value)
			{
				log_.Error("can't start, already active");
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

			if (person_.IsPlayer)
			{
				// player never leads
				leader = false;
			}
			else if (person_.Body.Get(BP.Head).Grabbed && !target.IsPlayer)
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
				target.VamAtom.Atom, "ClockwiseSilver.Kiss", "isActive",
				targetStorableCache_);

			if (active != null && active.val)
			{
				var trackPos = Sys.Vam.Parameters.GetBool(
					target.VamAtom.Atom, "ClockwiseSilver.Kiss", "trackPosition",
					targetStorableCache_);

				if (trackPos != null)
					return trackPos.val;
			}

			return false;
		}

		private void Init(Person p)
		{
			log_ = new Logger(Logger.Integration, p, "ClockwiseKiss");

			enabled_ = new Sys.Vam.BoolParameter(
				p, "ClockwiseSilver.Kiss", "enabled");

			active_ = new Sys.Vam.BoolParameter(
				p, "ClockwiseSilver.Kiss", "isActive");

			running_ = new Sys.Vam.BoolParameterRO(
				p, "ClockwiseSilver.Kiss", "Is Kissing");

			atom_ = new Sys.Vam.StringChooserParameter(
				p, "ClockwiseSilver.Kiss", "atom");

			target_ = new Sys.Vam.StringChooserParameter(
				p, "ClockwiseSilver.Kiss", "kissTargetJSON");

			trackPos_ = new Sys.Vam.BoolParameter(
				p, "ClockwiseSilver.Kiss", "trackPosition");

			trackRot_ = new Sys.Vam.BoolParameter(
				p, "ClockwiseSilver.Kiss", "trackRotation");

			headAngleX_ = new Sys.Vam.FloatParameter(
				p, "ClockwiseSilver.Kiss", "Head Angle X");

			headAngleY_ = new Sys.Vam.FloatParameter(
				p, "ClockwiseSilver.Kiss", "Head Angle Y");

			headAngleZ_ = new Sys.Vam.FloatParameter(
				p, "ClockwiseSilver.Kiss", "Head Angle Z");

			lipDepth_ = new Sys.Vam.FloatParameter(
				p, "ClockwiseSilver.Kiss", "Lip Depth");

			morphDuration_ = new Sys.Vam.FloatParameter(
				p, "ClockwiseSilver.Kiss", "Morph Duration");

			morphSpeed_ = new Sys.Vam.FloatParameter(
				p, "ClockwiseSilver.Kiss", "Morph Speed");

			tongueLength_ = new Sys.Vam.FloatParameter(
				p, "ClockwiseSilver.Kiss", "Tongue Length");

			trackingSpeed_ = new Sys.Vam.FloatParameter(
				p, "ClockwiseSilver.Kiss", "Tracking Speed");

			closeEyes_ = new Sys.Vam.BoolParameter(
				p, "ClockwiseSilver.Kiss", "closeEyes");

			active_.Value = false;
		}

		private void DoKiss(Person target, bool leader)
		{
			enabled_.Value = true;

			// force reset
			atom_.Value = "";
			target_.Value = "";
			atom_.Value = target.ID;
			target_.Value = "LipTrigger";

			if (target.IsPlayer)
			{
				headAngleX_.Value = StartHeadAngleXWithPlayer;
				headAngleY_.Value = StartHeadAngleYWithPlayer;
				headAngleZ_.Value = StartHeadAngleZWithPlayer;
				lipDepth_.Value = 0;

				closeEyes_.Value = !person_.Personality.GetBool(PSE.AvoidGazePlayer);

				randomMovements_ = false;
				randomSpeeds_ = true;
			}
			else
			{
				if (leader)
				{
					startLipDepth_ = StartLipDepthLeader;
					startAngleX_ = StartHeadAngleXLeader;
					startAngleY_ = StartHeadAngleYLeader;
					startAngleZ_ = StartHeadAngleZLeader;
				}
				else
				{
					startLipDepth_ = StartLipDepth;
					startAngleX_ = StartHeadAngleX;
					startAngleY_ = StartHeadAngleY;
					startAngleZ_ = StartHeadAngleZ;
				}


				headAngleX_.Value = startAngleX_;
				headAngleY_.Value = startAngleY_;
				headAngleZ_.Value = startAngleZ_;
				lipDepth_.Value = startLipDepth_;

				closeEyes_.Value = true;

				randomHeadAngleX_.Reset();
				randomHeadAngleY_.Reset();
				randomHeadAngleZ_.Reset();
				randomLipDepth_.Reset();

				randomMovements_ = leader;
				randomSpeeds_ = true;
			}

			trackingSpeed_.Value = StartTrackingSpeed;
			trackPos_.Value = leader;
			trackRot_.Value = true;
			active_.Value = true;
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

		public override void RequestStop()
		{
			base.RequestStop();

			if (active_.Value)
			{
				log_.Info("stopping");

				trackingSpeed_.Value = StopTrackingSpeed;
				active_.Value = false;
				elapsed_ = 0;
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

			if (k && randomSpeeds_ && active_.Value)
			{
				var ps = person_.Personality;

				// don't go too low
				var range =
					(morphDuration_.DefaultValue - morphDuration_.Minimum) * 0.6f;

				morphDuration_.Value =
					morphDuration_.DefaultValue -
					range * person_.Mood.MovementEnergy;

				range = morphSpeed_.Maximum - morphSpeed_.DefaultValue;
				morphSpeed_.Value =
					morphSpeed_.DefaultValue +
					range * person_.Mood.MovementEnergy;
			}

			if (k)
			{
				trackingSpeed_.Value = U.Lerp(
					StartTrackingSpeed, trackingSpeed_.DefaultValue,
					(elapsed_ / TrackingSpeedTime));
			}
		}
	}
}