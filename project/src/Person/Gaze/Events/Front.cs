namespace Cue
{
	class GazeFront : BasicGazeEvent
	{
		public GazeFront(Person p)
			: base(p, I.GazeFront)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;
			targets_.SetFrontWeight(ps.Get(PS.LookFrontWeight), "front");
			return Continue;
		}

		public override string ToString()
		{
			return "look front";
		}
	}
}
