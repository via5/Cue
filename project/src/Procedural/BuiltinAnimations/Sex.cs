using System;
using System.Collections.Generic;

namespace Cue.Proc
{
	class SexProcAnimation : BasicProcAnimation
	{
		private const float DirectionChangeMaxDistance = 0.05f;
		private const float ForceFarDistance = 0.07f;
		private const float ForceCloseDistance = 0.04f;
		private const float MinimumForce = 1;//0.4f;
		private const float ForceChangeMaxAmount = 0.02f;

		private float hipForceMin_ = 300;
		private float hipForceMax_ = 1200;
		private float hipTorqueMin_ = 0;
		private float hipTorqueMax_ = -20;
		private float chestTorqueMin_ = -10;
		private float chestTorqueMax_ = -50;
		private float headTorqueMin_ = 0;
		private float headTorqueMax_ = -10;
		private float durationMin_ = 1;
		private float durationMax_ = 0.1f;
		private float durationWin_ = 0.15f;
		private float durationInterval_ = 10;
		private Force[] forces_ = null;

		private float lastForceFactor_ = 0;
		private Vector3 lastDir_ = Vector3.Zero;

		public SexProcAnimation()
			: base("procSex", false)
		{
			var g = new ConcurrentTargetGroup(
				"g", new Duration(), new Duration(), true,
				new SlidingDurationSync(
					new SlidingDuration(
						durationMin_, durationMax_,
						durationInterval_, durationInterval_,
						durationWin_, new CubicOutEasing()),
					new SlidingDuration(
						durationMin_, durationMax_,
						durationInterval_, durationInterval_,
						durationWin_, new CubicOutEasing()),
					new Duration(0, 0), new Duration(0, 0),
					SlidingDurationSync.Loop | SlidingDurationSync.ResetBetween));


			g.AddTarget(new Force(
				Force.AbsoluteForce, BP.Hips, "hip",
				new SlidingMovement(
					Vector3.Zero, Vector3.Zero,
					0, 0, new Vector3(0, 0, 0), new LinearEasing()),
				new LinearEasing(), new ParentTargetSync(),
				new LinearEasing(), new LinearEasing()));

			g.AddTarget(new Force(
				Force.RelativeTorque, BP.Hips, "hip",
				new SlidingMovement(
					new Vector3(hipTorqueMin_, 0, 0),
					new Vector3(hipTorqueMax_, 0, 0),
					0, 0, new Vector3(0, 0, 0), new LinearEasing()),
				new LinearEasing(), new ParentTargetSync(),
				new LinearEasing(), new LinearEasing()));

			g.AddTarget(new Force(
				Force.RelativeTorque, BP.Chest, "chest",
				new SlidingMovement(
					new Vector3(chestTorqueMin_, 0, 0),
					new Vector3(chestTorqueMax_, 0, 0),
					0, 0, new Vector3(0, 0, 0), new LinearEasing()),
				new LinearEasing(), new ParentTargetSync(),
				new LinearEasing(), new LinearEasing()));

			g.AddTarget(new Force(
				Force.RelativeTorque, BP.Head, "head",
				new SlidingMovement(
					new Vector3(headTorqueMin_, 0, 0),
					new Vector3(headTorqueMax_, 0, 0),
					0, 0, new Vector3(0, 0, 0), new LinearEasing()),
				new LinearEasing(), new ParentTargetSync(),
				new LinearEasing(), new LinearEasing()));

			AddTarget(g);
		}

		public override BasicProcAnimation Clone()
		{
			var a = new SexProcAnimation();
			a.CopyFrom(this);
			return a;
		}

		public override void Start(Person p)
		{
			base.Start(p);

			if (forces_ == null)
			{
				var list = new List<Force>();
				GatherForces(list, Targets);
				forces_ = list.ToArray();
			}

			UpdateForces(true);
		}

		private void UpdateForces(bool alwaysUpdate = false)
		{
			for (int i = 0; i < forces_.Length; ++i)
			{
				var f = forces_[i];

				if (f.Done || alwaysUpdate)
					UpdateForce(f);
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
			var fmin = hipForceMin_ * p;
			var fmax = hipForceMax_ * p;

			var dir = GetDirection();

			f.Movement.SetRange(dir * fmin, dir * fmax);
		}

		private void GatherForces(List<Force> list, List<ITarget> targets)
		{
			for (int i = 0; i < targets.Count; ++i)
			{
				var t = targets[i];

				if (t is ITargetGroup)
				{
					GatherForces(list, ((ITargetGroup)t).Targets);
				}
				else if (t is Force)
				{
					var f = (Force)t;

					if (f.Type == Force.AbsoluteForce)
					{
						f.BeforeNextAction = () => UpdateForce(f);
						list.Add(f);
					}
				}
			}
		}

		public override string ToDetailedString()
		{
			return
				base.ToDetailedString() + "\n" +
				$"ff={lastForceFactor_} dir={lastDir_}";
		}
	}
}
