namespace Cue
{
	class GazeDown : BasicGazeEvent
	{
		public GazeDown(Person p)
			: base(p, I.GazeFront)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;

			float w = ps.Get(PS.LookDownWeight);

			if (w > 0)
			{
				targets_.SetDownWeightIfNotSet(w, "down");
				SetLastResult("normal");
			}
			else
			{
				SetLastResult("weight is 0");
			}

			return Continue;
		}

		public override string ToString()
		{
			return "look down";
		}
	}
}
