namespace Cue.Proc
{
	class PenetratedAnimation : BasicProcAnimation
	{
		private const float RampUpTime = 0.5f;
		private const float ZeroTime = 0.5f;
		private const float HoldTime = 3;
		private const float RampDownTime = 2;
		private const float Time = RampUpTime + HoldTime + RampDownTime;
		private float elapsed_ = 0;
		private bool resetting_ = false;

		public PenetratedAnimation()
			: base("procPenetrated", false)
		{
		}

		public override BasicProcAnimation Clone()
		{
			var a = new PenetratedAnimation();
			a.CopyFrom(this);
			return a;
		}

		public override bool Done
		{
			get { return (elapsed_ >= Time); }
		}

		public override bool Start(Person p, object ps)
		{
			base.Start(p, ps);

			elapsed_ = 0;
			resetting_ = false;

			person_.Breathing.MouthEnabled = false;

			foreach (var e in (person_.Expression as Proc.Expression).All)
			{
				foreach (var g in e.Groups)
				{
					if (e.Type == Expressions.Pleasure && g.Name == "pleasure")
					{
						foreach (var m in g.Morphs)
						{
							m.Force(
								MorphTarget.ForceToRangePercent, 1,
								new SlidingDurationSync(
									new SlidingDuration(RampUpTime, RampUpTime),
									new SlidingDuration(RampDownTime, RampDownTime),
									new Duration(HoldTime, HoldTime), null,
									SlidingDurationSync.ResetBetween));
						}
					}
					else
					{
						foreach (var m in g.Morphs)
						{
							m.Force(
								MorphTarget.ForceToZero, 0,
								new SlidingDurationSync(
									new SlidingDuration(ZeroTime, ZeroTime),
									null, null, null, SlidingDurationSync.NoFlags));
						}
					}
				}
			}

			return true;
		}

		public override void Reset()
		{
			base.Reset();
			elapsed_ = 0;
		}

		public override void FixedUpdate(float s)
		{
			elapsed_ += s;

			if (elapsed_ < Time)
			{
				if (elapsed_ >= (RampUpTime + HoldTime))
				{
					if (!resetting_)
					{
						resetting_ = true;

						foreach (var e in (person_.Expression as Proc.Expression).All)
						{
							foreach (var g in e.Groups)
							{
								if (e.Type != Expressions.Pleasure || g.Name != "pleasure")
								{
									g.Force(MorphTarget.NoForceTarget, 0);
								}
							}
						}
					}
				}

				var p = elapsed_ / Time;

				//person_.Body.Get(BP.RightHand).AddRelativeForce(
				//	new Vector3(0, 500, 0) * p);
			}
			else
			{
				person_.Gaze.Picker.ForcedTarget = null;
				person_.Breathing.MouthEnabled = true;

				foreach (var e in (person_.Expression as Proc.Expression).All)
				{
					foreach (var g in e.Groups)
					{
						g.Force(MorphTarget.NoForceTarget, 0);
					}
				}
			}
		}
	}
}
