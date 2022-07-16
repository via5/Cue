namespace Cue
{
	class GazeAbove : BasicGazeEvent
	{
		public GazeAbove(Person p)
			: base(p, I.GazeAbove)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;

			if (person_.Mood.State == Mood.OrgasmState)
			{
				targets_.SetAboveWeight(
					person_.Mood.GazeEnergy * ps.Get(PS.LookAboveMaxWeightOrgasm),
					"orgasm state");
			}
			else
			{
				if (person_.Mood.Get(MoodType.Excited) >= ps.Get(PS.LookAboveMinExcitement))
				{
					if (person_.Excitement.PhysicalRate >= ps.Get(PS.LookAboveMinPhysicalRate))
					{
						targets_.SetAboveWeight(
							person_.Mood.GazeEnergy * ps.Get(PS.LookAboveMaxWeight),
							"normal state");
					}
				}
			}

			return Continue;
		}

		public override string ToString()
		{
			return "look above";
		}
	}
}
