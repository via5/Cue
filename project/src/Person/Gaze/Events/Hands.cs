namespace Cue
{
	class GazeHands : BasicGazeEvent
	{
		private HandEvent event_ = null;

		public GazeHands(Person p)
			: base(p, I.GazeHands)
		{
		}

		protected override int DoCheck(int flags)
		{
			if (event_ == null)
				event_ = person_.AI.GetEvent<HandEvent>();

			var e = event_;

			int ret = Continue;

			if (e.Active)
			{
				if (e.LeftTarget != null && e.LeftTarget == e.RightTarget)
				{
					ret |= CheckTarget(e.LeftTarget);
				}
				else
				{
					if (e.LeftTarget != null)
						ret |= CheckTarget(e.LeftTarget);

					if (e.RightTarget != null)
						ret |= CheckTarget(e.RightTarget);
				}
			}


			foreach (var t in Cue.Instance.ActivePersons)
			{
				if (t == person_)
					continue;

				e = t.AI.GetEvent<HandEvent>();

				if (e.LeftTarget == person_ || e.RightTarget == person_)
					ret |= CheckTarget(t);
			}

			if (ret != Continue)
				SetLastResult("found something");
			else
				SetLastResult("nothing");

			return ret;
		}

		private int CheckTarget(Person t)
		{
			var ps = person_.Personality;

			if (g_.ShouldAvoidInsidePersonalSpace(t))
			{
				targets_.SetReluctant(
					t, true, g_.AvoidWeight(t),
					"handevent, but avoid in ps");

				return Continue | Busy;
			}
			else
			{
				if (t != person_)
				{
					targets_.SetWeight(
						t, BP.Eyes,
						ps.Get(PS.HandjobEyesWeight), "handevent");
				}

				targets_.SetWeight(
					t, t.Body.GenitalsBodyPart,
					ps.Get(PS.HandjobGenitalsWeight), "handevent");

				return Continue | Busy | NoRandom;
			}
		}

		public override string ToString()
		{
			return "handevent";
		}
	}
}
