using System;

namespace Cue.Proc
{
	abstract class ThrustProcAnimation : BasicProcAnimation
	{
		protected struct Config
		{
			public float hipForceMin;
			public float hipForceMax;
			public Vector3 hipTorqueMin;
			public Vector3 hipTorqueMax;
			public Vector3 hipTorqueWin;
		}

		private const float DirectionChangeMaxDistance = 0.05f;
		private const float ForceFarDistance = 0.07f;
		private const float ForceCloseDistance = 0.04f;
		private const float MinimumForce = 1;//0.4f;
		private const float ForceChangeMaxAmount = 0.02f;

		private float chestTorqueMin_ = -10;
		private float chestTorqueMax_ = -50;
		private float headTorqueMin_ = 0;
		private float headTorqueMax_ = -10;
		private float durationMin_ = 1;
		private float durationMax_ = 0.1f;
		private float durationWin_ = 0.15f;
		private float durationInterval_ = 10;
		private Force hipForce_ = null;
		private Force hipTorque_ = null;

		private float lastForceFactor_ = 0;
		private Vector3 lastDir_ = Vector3.Zero;
		private Person receiver_ = null;
		private Config config_;

		protected ThrustProcAnimation(string name)
			: base(name)
		{
			RootGroup.Sync = new DurationSync(
				new Duration(
					durationMin_, durationMax_,
					durationInterval_, durationInterval_,
					durationWin_, new CubicOutEasing()),
				new Duration(
					durationMin_, durationMax_,
					durationInterval_, durationInterval_,
					durationWin_, new CubicOutEasing()),
				new Duration(0, 0), new Duration(0, 0),
				DurationSync.Loop | DurationSync.ResetBetween);


			RootGroup.AddTarget(new Force(
				"hipForce", Force.AbsoluteForce, BP.Hips,
				new SlidingMovement(
					Vector3.Zero, Vector3.Zero,
					durationInterval_, durationInterval_,
					new Vector3(0, 0, 0), new LinearEasing()),
				new ParentTargetSync()));

			RootGroup.AddTarget(new Force(
				"hipTorque", Force.RelativeTorque, BP.Hips,
				new SlidingMovement(
					Vector3.Zero, Vector3.Zero,
					durationInterval_, durationInterval_,
					new Vector3(0, 0, 0), new LinearEasing()),
				new ParentTargetSync()));

			RootGroup.AddTarget(new Force(
				Force.RelativeTorque, BP.Chest,
				new SlidingMovement(
					new Vector3(chestTorqueMin_, 0, 0),
					new Vector3(chestTorqueMax_, 0, 0),
					durationInterval_, durationInterval_,
					new Vector3(0, 0, 0), new LinearEasing()),
				new ParentTargetSync()));

			RootGroup.AddTarget(new Force(
				Force.RelativeTorque, BP.Head,
				new SlidingMovement(
					new Vector3(headTorqueMin_, 0, 0),
					new Vector3(headTorqueMax_, 0, 0),
					durationInterval_, durationInterval_,
					new Vector3(0, 0, 0), new LinearEasing()),
				new ParentTargetSync()));


			RootGroup.AddTarget(new Force(
				Force.RelativeForce, BP.Chest,
				new SlidingMovement(
					new Vector3(0, 0, -300),
					new Vector3(0, 0, 300)),
				new DurationSync(
					new Duration(0.5f, 3), null,
					new Duration(0, 3), null,
					DurationSync.Loop)));

			RootGroup.AddTarget(new Force(
				Force.RelativeForce, BP.Head,
				new SlidingMovement(
					new Vector3(0, 0, -100),
					new Vector3(0, 0, 100)),
				new DurationSync(
					new Duration(0.5f, 3), null,
					new Duration(0, 3), null,
					DurationSync.Loop)));
		}

		protected Person Receiver
		{
			get { return receiver_; }
		}

		public override bool Start(Person p, AnimationContext cx)
		{
			if (!base.Start(p, cx))
				return false;

			receiver_ = cx.ps as Person;

			SetEnergySource(receiver_);
			SetAsMainSync(receiver_);

			hipForce_ = FindTarget("hipForce") as Force;
			if (hipForce_ == null)
			{
				Log.Error("hipForce not found");
			}
			else
			{
				hipForce_.BeforeNextAction = () => UpdateForce(hipForce_);
			}

			if (!DoStart())
				return false;

			config_ = GetConfig();

			hipTorque_ = FindTarget("hipTorque") as Force;
			if (hipTorque_ == null)
			{
				Log.Error("hipTorque not found");
			}
			else
			{
				hipTorque_.Movement.SetRange(
					config_.hipTorqueMin,
					config_.hipTorqueMax,
					config_.hipTorqueWin);
			}

			UpdateForces(true);
			Reset();

			return true;
		}

		protected abstract bool DoStart();
		protected abstract Config GetConfig();

		private void UpdateForces(bool alwaysUpdate = false)
		{
			if (hipForce_ != null)
			{
				if (hipForce_.Done || alwaysUpdate)
					UpdateForce(hipForce_);
			}
		}

		// gets the direction between the genitals so the forces go that way
		//
		// changes in direction need to be dampened because if the hips are
		// to the side of the target, they'll come down at an angle and
		// bounce back in the opposite direction
		//
		// this would reverse the direction for the next thrust, which would
		// just compound the direction changes infinitely
		//
		// dampening the direction changes eventually centers the movement
		//
		private Vector3 GetDirection()
		{
			if (receiver_ == null)
			{
				var rot = person_.Body.Get(BP.Hips).Rotation;
				return rot.Rotate(new Vector3(0, 0, 1)).Normalized;
			}

			var thisBP = person_.Body.Get(person_.Body.GenitalsBodyPart);
			var targetBP = receiver_.Body.Get(receiver_.Body.GenitalsBodyPart);

			// direction between genitals
			var currentDir = (targetBP.Position - thisBP.Position).Normalized;

			Vector3 dir;

			if (lastDir_ == Vector3.Zero)
			{
				dir = currentDir;
			}
			else
			{
				dir = Vector3.MoveTowards(
					lastDir_, currentDir, DirectionChangeMaxDistance);
			}

			lastDir_ = dir;

			return dir;
		}

		// gets a [0, 1] factor that multiplies the maximum force applied, based
		// on the distance between genitals
		//
		// this avoids forces that are too large when the genitals are close
		//
		// changes in forces need to be dampened because the hips don't always
		// have time to fully move back up before the distance is checked, which
		// would constantly alternate between different force factors
		//
		private float GetForceFactor()
		{
			if (receiver_ == null)
				return 0.7f;

			var thisBP = person_.Body.Get(person_.Body.GenitalsBodyPart);
			var targetBP = receiver_.Body.Get(receiver_.Body.GenitalsBodyPart);

			var range = ForceFarDistance - ForceCloseDistance;

			var dist = Vector3.Distance(thisBP.Position, targetBP.Position);
			var cdist = U.Clamp(dist, ForceCloseDistance, ForceFarDistance);
			var currentP = Math.Max((cdist - ForceCloseDistance) / range, MinimumForce);

			float p;

			if (lastForceFactor_ == 0)
			{
				p = currentP;
			}
			else
			{
				if (currentP > lastForceFactor_)
					p = Math.Min(lastForceFactor_ + ForceChangeMaxAmount, currentP);
				else
					p = Math.Max(lastForceFactor_ - ForceChangeMaxAmount, currentP);
			}

			lastForceFactor_ = p;

			return p;
		}

		private void UpdateForce(Force f)
		{
			var p = GetForceFactor();
			var dir = GetDirection();

			float fmin = config_.hipForceMin * p;
			float fmax = config_.hipForceMax * p;

			f.Movement.SetRange(dir * fmin, dir * fmax);
		}

		public override string ToDetailedString()
		{
			return
				base.ToDetailedString() + "\n" +
				$"ff={lastForceFactor_} dir={lastDir_}";
		}
	}


	class SexProcAnimation : ThrustProcAnimation
	{
		private float hipForceMin_ = 300;
		private float hipForceMax_ = 1400;
		private float hipTorqueMin_ = 0;
		private float hipTorqueMax_ = -20;
		private float hipTorqueWindow_ = 0;

		public SexProcAnimation()
			: base("cueSex")
		{
		}

		public override BuiltinAnimation Clone()
		{
			var a = new SexProcAnimation();
			a.CopyFrom(this);
			return a;
		}

		protected override bool DoStart()
		{
			if (Receiver == null)
			{
				Log.Error("no receiver");
				return false;
			}

			return true;
		}

		protected override Config GetConfig()
		{
			var c = new Config();

			c.hipForceMin = hipForceMin_;
			c.hipForceMax = hipForceMax_;

			c.hipTorqueMin = new Vector3(hipTorqueMin_, 0, 0);
			c.hipTorqueMax = new Vector3(hipTorqueMax_, 0, 0);
			c.hipTorqueWin = new Vector3(hipTorqueWindow_, 0, 0);

			return c;
		}
	}


	class FrottageProcAnimation : ThrustProcAnimation
	{
		private float hipAloneForceMin_ = 300;
		private float hipAloneForceMax_ = 800;
		private float hipAloneTorqueMin_ = -60;
		private float hipAloneTorqueMax_ = -120;
		private float hipAloneTorqueWindow_ = 20;

		public FrottageProcAnimation()
			: base("cueFrottage")
		{
		}

		public override BuiltinAnimation Clone()
		{
			var a = new FrottageProcAnimation();
			a.CopyFrom(this);
			return a;
		}

		protected override bool DoStart()
		{
			return true;
		}

		protected override Config GetConfig()
		{
			var c = new Config();

			c.hipForceMin = hipAloneForceMin_;
			c.hipForceMax = hipAloneForceMax_;

			c.hipTorqueMin = new Vector3(hipAloneTorqueMin_, 0, 0);
			c.hipTorqueMax = new Vector3(hipAloneTorqueMax_, 0, 0);
			c.hipTorqueWin = new Vector3(hipAloneTorqueWindow_, 0, 0);

			return c;
		}
	}
}
