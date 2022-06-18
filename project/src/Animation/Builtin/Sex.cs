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
			public float durationMin;
			public float durationMax;
			public float durationWin;
			public float durationInterval;
		}

		private const float DirectionChangeMaxDistance = 0.05f;
		private const float ForceFarDistance = 0.07f;
		private const float ForceCloseDistance = 0.04f;
		private const float MinimumForce = 1;//0.4f;
		private const float ForceChangeMaxAmount = 0.02f;

		private float chestTorqueMin_ = -10;
		private float chestTorqueMax_ = -100;
		private float chestTorqueWin_ = 40;
		private float headTorqueMin_ = 0;
		private float headTorqueMax_ = -20;
		private float headTorqueWin_ = 10;
		private Force hipForce_ = null;
		private Force hipTorque_ = null;

		private float lastForceFactor_ = 0;
		private Vector3 lastDir_ = Vector3.Zero;
		private Person receiver_ = null;
		private Config config_;

		protected ThrustProcAnimation(string name, Config c)
			: base(name)
		{
			config_ = c;

			RootGroup.Sync = new DurationSync(
				new Duration(
					config_.durationMin, config_.durationMax,
					config_.durationInterval, config_.durationInterval,
					config_.durationWin, new CubicOutEasing()),
				null, null, null,
				DurationSync.Loop | DurationSync.ResetBetween);


			RootGroup.AddTarget(new Force(
				"hipForce", Force.AbsoluteForce, BP.Hips,
				Vector3.Zero, Vector3.Zero, config_.durationInterval, Vector3.Zero,
				new ParentTargetSync()));

			RootGroup.AddTarget(new Force(
				"hipTorque", Force.RelativeTorque, BP.Hips,
				Vector3.Zero, Vector3.Zero, config_.durationInterval, Vector3.Zero,
				new ParentTargetSync()));

			RootGroup.AddTarget(new Force(
				"", Force.RelativeTorque, BP.Chest,
				new Vector3(chestTorqueMin_, 0, 0),
				new Vector3(chestTorqueMax_, 0, 0),
				config_.durationInterval, new Vector3(chestTorqueWin_, 0, 0),
				new ParentTargetSync()));

			RootGroup.AddTarget(new Force(
				"", Force.RelativeTorque, BP.Head,
				new Vector3(headTorqueMin_, 0, 0),
				new Vector3(headTorqueMax_, 0, 0),
				config_.durationInterval, new Vector3(headTorqueWin_, 0, 0),
				new ParentTargetSync()));


			RootGroup.AddTarget(new Force(
				"", Force.RelativeForce, BP.Chest,
				new Vector3(0, 0, -300), new Vector3(0, 0, 300), null, Vector3.Zero,
				new DurationSync(
					new Duration(0.5f, 3), null,
					new Duration(0, 3), null,
					DurationSync.Loop)));

			RootGroup.AddTarget(new Force(
				"", Force.RelativeForce, BP.Head,
				new Vector3(0, 0, -100), new Vector3(0, 0, 100), null, Vector3.Zero,
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
			receiver_ = cx.ps as Person;

			SetEnergySource(receiver_);

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

			hipTorque_ = FindTarget("hipTorque") as Force;
			if (hipTorque_ == null)
			{
				Log.Error("hipTorque not found");
			}
			else
			{
				hipTorque_.SetRange(
					config_.hipTorqueMin,
					config_.hipTorqueMax,
					config_.hipTorqueWin);
			}

			if (!base.Start(p, cx))
				return false;

			SetAsMainSync(receiver_);
			Reset();
			UpdateForces(true);

			return true;
		}

		protected abstract bool DoStart();

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

			f.SetRangeWithDirection(fmin, fmax, 0, dir);
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
		public SexProcAnimation()
			: base("cueSex", GetConfig())
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

		private static Config GetConfig()
		{
			var c = new Config();

			c.hipForceMin = 300;
			c.hipForceMax = 1500;

			c.hipTorqueMin = new Vector3(0, 0, 0);
			c.hipTorqueMax = new Vector3(-30, 0, 0);
			c.hipTorqueWin = new Vector3(0, 0, 0);

			c.durationMin = 1.0f;
			c.durationMax = 0.15f;
			c.durationWin = 0.1f;
			c.durationInterval = 10;

			return c;
		}
	}


	class FrottageProcAnimation : ThrustProcAnimation
	{
		public FrottageProcAnimation()
			: base("cueFrottage", GetConfig())
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

		private static Config GetConfig()
		{
			var c = new Config();

			c.hipForceMin = 300;
			c.hipForceMax = 800;

			c.hipTorqueMin = new Vector3(-20, 0, 0);
			c.hipTorqueMax = new Vector3(-150, 0, 0);
			c.hipTorqueWin = new Vector3(20, 0, 0);

			c.durationMin = 0.5f;
			c.durationMax = 0.08f;
			c.durationWin = 0.08f;
			c.durationInterval = 10;

			return c;
		}
	}
}
