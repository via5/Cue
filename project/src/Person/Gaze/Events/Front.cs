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

			float w = ps.Get(PS.LookFrontWeight);

			if (w > 0)
			{
				targets_.SetFrontWeight(w, "front");
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
			return "look front";
		}
	}
}
