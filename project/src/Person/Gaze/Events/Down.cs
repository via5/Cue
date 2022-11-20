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
				targets_.SetDownWeight(w, "down");

			return Continue;
		}

		public override string ToString()
		{
			return "look down";
		}
	}
}
