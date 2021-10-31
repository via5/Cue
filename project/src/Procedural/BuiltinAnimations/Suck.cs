namespace Cue.Proc
{
	class SuckProcAnimation : BasicProcAnimation
	{
		private float durationMin_ = 0.8f;
		private float durationMax_ = 1.5f;
		private float durationWin_ = 0;
		private float durationInterval_ = 5;

		public SuckProcAnimation()
			: base("procSuck", false)
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
					SlidingDurationSync.Loop | SlidingDurationSync.StartFast));

			g.AddTarget(new MorphTarget(
				BP.Lips, "Lips Pucker",
				0.3f, 1, new ParentTargetSync(), null, MorphTarget.StartHigh));

			g.AddTarget(new MorphTarget(
				BP.Mouth, "Mouth Open",
				0, 1, new ParentTargetSync(), null));

			AddTarget(g);
		}

		public override BasicProcAnimation Clone()
		{
			var a = new SuckProcAnimation();
			a.CopyFrom(this);
			return a;
		}
	}
}
