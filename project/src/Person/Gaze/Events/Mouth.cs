namespace Cue
{
	class GazeMouth : BasicGazeEvent
	{
		private MouthEvent event_ = null;

		public GazeMouth(Person p)
			: base(p, I.GazeMouth)
		{
		}

		protected override int DoCheck(int flags)
		{
			if (event_ == null)
				event_ = person_.AI.GetEvent<MouthEvent>();

			var ps = person_.Personality;
			var e = event_;

			if (e.Active)
			{
				var t = e.Target;

				if (t != null)
				{
					if (g_.ShouldAvoidInsidePersonalSpace(t))
					{
						targets_.SetReluctant(
							t, true, g_.AvoidWeight(t),
							$"mouthevent, but avoid in ps");

						SetLastResult("active, but avoid in ps");
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

						SetLastResult("active");
						return Continue | NoGazer | Busy | NoRandom;
					}
				}
				else
				{
					SetLastResult("active but no target");
				}
			}
			else
			{
				SetLastResult("not active");
			}

			return Continue;
		}

		public override string ToString()
		{
			return "mouthevent";
		}
	}
}
