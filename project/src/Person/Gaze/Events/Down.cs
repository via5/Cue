namespace Cue
{
	class GazeDown : BasicGazeEvent
	{
		private KissEvent kiss_ = null;

		public GazeDown(Person p)
			: base(p, I.GazeFront)
		{
		}

		protected override int DoCheck(int flags)
		{
			if (kiss_ == null)
				kiss_ = person_.AI.GetEvent<KissEvent>();

			var ps = person_.Personality;

			float w = ps.Get(PS.LookDownWeight);

			if (w > 0)
			{
				if (kiss_ != null && kiss_.Active)
				{
					SetLastResult("has weight but kissing active");
				}
				else
				{
					targets_.SetDownWeightIfNotSet(w, "down");
					SetLastResult("normal");
				}
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
