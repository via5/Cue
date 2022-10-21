namespace Cue
{
	class GazeKissing : BasicGazeEvent
	{
		public GazeKissing(Person p)
			: base(p, I.GazeKissing)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;
			var k = person_.AI.GetEvent<KissEvent>();

			if (k.Active)
			{
				var t = k.Target;

				if (t != null)
				{
					if (g_.ShouldAvoidInsidePersonalSpace(t))
					{
						targets_.SetRandomWeight(
							1, $"kissing {t.ID}, but avoid in ps");

						targets_.SetReluctant(
							t, true, g_.AvoidWeight(t),
							$"kissing, but avoid in ps");
					}
					else
					{
						targets_.SetWeight(
							t, BP.Eyes, GazeTargets.ExclusiveWeight, "kissing");
					}

					// don't use NoGazer:
					//  - although the gazer does have to be disabled while
					//    kissing, the head will become busy anyway, which is
					//    checked in Gaze.Update()
					//
					//  - returning NoGazer would disable the gazer until a new
					//    target is picked, which doesn't happen immediately
					//    after kissing stops because the timer just continues
					//    running
					//
					//    since it might take several seconds before a new
					//    target is picked, the gazer would stay disabled and
					//    the head would stay still for a while
				}
			}

			return Continue;
		}

		public override string ToString()
		{
			return "kissing";
		}
	}
}
