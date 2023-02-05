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
		private float forceMax_ = 400;
		private float forceWin_ = 100;

		public FingerProcAnimation(
			string name, BodyPartType bodyPart, Vector3 torqueDir, Vector3 forceDir)
				: base(name)
		{
			var g = new ConcurrentTargetGroup(
				"g", new Duration(), new Duration(), true,
				new DurationSync(
					new Duration(
						durationMin_, durationMax_,
						durationInterval_, durationInterval_,
						durationWin_, new CubicOutEasing()),
					new Duration(
						durationMin_, durationMax_,
						durationInterval_, durationInterval_,
						durationWin_, new CubicOutEasing()),
					new Duration(0, 0), new Duration(0, 0),
					DurationSync.Loop | DurationSync.ResetBetween));

			g.AddTarget(new Force(
				"", Force.RelativeTorque, bodyPart,
				torqueMin_ * torqueDir, torqueMax_ * torqueDir,
				null, torqueWin_ * torqueDir, new ParentTargetSync()));

			g.AddTarget(new Force(
				"", Force.RelativeForce, bodyPart,
				forceMin_ * forceDir, forceMax_ * forceDir,
				null, forceWin_ * forceDir, new ParentTargetSync()));

			AddTarget(g);
		}

		public override bool Start(Person p, AnimationContext cx)
		{
			if (cx.ps is Person)
				SetEnergySource(cx.ps as Person);

			if (!base.Start(p, cx))
				return false;

			return true;
		}
	}


	class LeftFingerProcAnimation : FingerProcAnimation
	{
		public LeftFingerProcAnimation()
			: base(
				  "cueLeftFinger", BP.LeftHand,
				  new Vector3(0, 0, 1), new Vector3(-1, 0, 0))
		{
		}

		public override BuiltinAnimation Clone()
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
				  "cueRightFinger", BP.RightHand,
				  new Vector3(0, 0, -1), new Vector3(1, 0, 0))
		{
		}

		public override BuiltinAnimation Clone()
		{
			var a = new RightFingerProcAnimation();
			a.CopyFrom(this);
			return a;
		}
	}


	public abstract class HandOnBreastProcAnimation : BasicProcAnimation
	{
		public HandOnBreastProcAnimation(string name, BodyPartType bp)
				: base(name)
		{
			var torqueMin = new Vector3(-7, -7, -7);
			var torqueMax = new Vector3(7, 7, 7);

			var forceMin = new Vector3(-25, -30, -30);
			var forceMax = new Vector3(25, 30, 30);


			AddForce(Force.RelativeTorque, bp,
				new Vector3(torqueMin.X, 0, 0),
				new Vector3(torqueMax.X, 0, 0));

			AddForce(Force.RelativeTorque, bp,
				new Vector3(0, torqueMin.Y, 0),
				new Vector3(0, torqueMax.Y, 0));

			AddForce(Force.RelativeTorque, bp,
				new Vector3(0, 0, torqueMin.Z),
				new Vector3(0, 0, torqueMax.Z));


			AddForce(Force.RelativeForce, bp,
				new Vector3(forceMin.X, 0, 0),
				new Vector3(forceMax.X, 0, 0));

			AddForce(Force.RelativeForce, bp,
				new Vector3(0, forceMin.Y, 0),
				new Vector3(0, forceMax.Y, 0));

			AddForce(Force.RelativeForce, bp,
				new Vector3(0, 0, forceMin.Z),
				new Vector3(0, 0, forceMax.Z));

			string morph = (bp == BP.LeftHand ? "Left" : "Right");

			AddMorph(bp, $"{morph} Fingers Grasp", -0.2f, 0.3f);
			AddMorph(bp, $"{morph} Fingers In-Out", -1, 1);
		}

		private void AddForce(int forceType, BodyPartType bp, Vector3 min, Vector3 max)
		{
			var g = new ConcurrentTargetGroup(
				"g", new Duration(), new Duration(), true,
				new DurationSync(
					new Duration(0.3f, 2),
					new Duration(0.3f, 2),
					new Duration(0, 2), new Duration(0, 2),
					DurationSync.Loop));

			g.AddTarget(new Force(
				"", forceType, bp, min, max,
				null, Vector3.Zero, new ParentTargetSync()));

			AddTarget(g);
		}

		private void AddMorph(BodyPartType bp, string name, float min, float max)
		{
			var g = new ConcurrentTargetGroup(
				"g", new Duration(), new Duration(), true,
				new DurationSync(
					new Duration(0.3f, 2),
					new Duration(0.3f, 2),
					new Duration(0, 3), new Duration(0, 3),
					DurationSync.Loop));

			g.AddTarget(new MorphTarget(
				bp, name, min, max, new ParentTargetSync()));

			AddTarget(g);
		}
	}


	public class LeftHandOnBreastProcAnimation : HandOnBreastProcAnimation
	{
		public LeftHandOnBreastProcAnimation()
				: base("cueLeftHandOnBreast", BP.LeftHand)
		{
		}

		public override BuiltinAnimation Clone()
		{
			var a = new LeftHandOnBreastProcAnimation();
			a.CopyFrom(this);
			return a;
		}
	}


	public class RightHandOnBreastProcAnimation : HandOnBreastProcAnimation
	{
		public RightHandOnBreastProcAnimation()
				: base("cueRightHandOnBreast", BP.RightHand)
		{
		}

		public override BuiltinAnimation Clone()
		{
			var a = new RightHandOnBreastProcAnimation();
			a.CopyFrom(this);
			return a;
		}
	}
}
