namespace Cue
{
	class GazeMouth : BasicGazeEvent
	{
		public GazeMouth(Person p)
			: base(p, I.GazeMouth)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;
			var e = person_.AI.GetEvent<MouthEvent>();

			if (e.Active)
			{
				var t = e.Target;

				if (t != null)
				{
					if (g_.ShouldAvoidInsidePersonalSpace(t))
					{
						targets_.SetShouldAvoid(
							t, true, g_.AvoidWeight(t),
							$"mouthevent, but avoid in ps");

						return Continue | NoGazer | Busy;
					}
					else
					{
						targets_.SetWeight(
							t, BP.Eyes,
							ps.Get(PS.BlowjobEyesWeight), "mouthevent");

						targets_.SetWeight(
							t, t.Body.GenitalsBodyPart,
							ps.Get(PS.BlowjobGenitalsWeight), "mouthevent");

						return Continue | NoGazer | Busy | NoRandom;
					}
				}
			}

			return Continue;
		}

		public override string ToString()
		{
			return "mouthevent";
		}
	}
}
