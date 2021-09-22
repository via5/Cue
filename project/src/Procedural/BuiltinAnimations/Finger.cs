﻿using System.Collections.Generic;

namespace Cue.Proc
{
	abstract class FingerProcAnimation : BasicProcAnimation
	{
		private float durationMin_ = 1;
		private float durationMax_ = 0.08f;
		private float durationWin_ = 0.1f;
		private float durationInterval_ = 10;

		private float torqueMin_ = 10;
		private float torqueMax_ = 30;
		private float torqueWin_ = 5;

		private float forceMin_ = 100;
		private float forceMax_ = 600;
		private float forceWin_ = 100;

		public FingerProcAnimation(
			string name, string handName, Vector3 torqueDir, Vector3 forceDir)
				: base(name, false)
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
				Force.RelativeTorque, BP.None, handName,
				new SlidingMovement(
					torqueMin_ * torqueDir, torqueMax_ * torqueDir,
					0, 0, torqueWin_ * torqueDir, new LinearEasing()),
				new LinearEasing(), new ParentTargetSync(),
				new LinearEasing(), new LinearEasing()));

			g.AddTarget(new Force(
				Force.RelativeForce, BP.None, handName,
				new SlidingMovement(
					forceMin_ * forceDir, forceMax_ * forceDir,
					0, 0, forceWin_ * forceDir, new LinearEasing()),
				new LinearEasing(), new ParentTargetSync(),
				new LinearEasing(), new LinearEasing()));

			AddTarget(g);
		}

		public override bool Start(Person p, object ps)
		{
			if (!base.Start(p, ps))
				return false;

			if (ps is Person)
				SetEnergySource(ps as Person);

			return true;
		}
	}


	class LeftFingerProcAnimation : FingerProcAnimation
	{
		public LeftFingerProcAnimation()
			: base(
				  "procLeftFinger", "lHand",
				  new Vector3(0, 0, 1), new Vector3(-1, 0, 0))
		{
		}

		public override BasicProcAnimation Clone()
		{
			var a = new LeftFingerProcAnimation();
			a.CopyFrom(this);
			return a;
		}
	}


	class RightFingerProcAnimation : FingerProcAnimation
	{
		public RightFingerProcAnimation()
			: base(
				  "procRightFinger", "rHand",
				  new Vector3(0, 0, -1), new Vector3(1, 0, 0))
		{
		}

		public override BasicProcAnimation Clone()
		{
			var a = new RightFingerProcAnimation();
			a.CopyFrom(this);
			return a;
		}
	}
}