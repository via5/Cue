namespace Cue
{
	class GazeRandom : BasicGazeEvent
	{
		public GazeRandom(Person p)
			: base(p, I.GazeRandom)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;

			if (Bits.IsSet(flags, NoRandom))
			{
				targets_.SetRandomWeightIfZero(0, "random, but busy");
				SetLastResult("no, busy");
			}
			else
			{
				if (person_.Mood.GazeTiredness >= ps.Get(PS.MaxTirednessForRandomGaze))
				{
					targets_.SetRandomWeightIfZero(0, "random, but tired");
					SetLastResult("yes, but tired");
				}
				else
				{
					if (Cue.Instance.IsSceneIdle())
					{
						if (Cue.Instance.IsSceneEmpty())
						{
							targets_.SetRandomWeightIfZero(
								ps.Get(PS.IdleEmptyRandomWeight), "random, scene idle and empty");

							SetLastResult("yes, idle and empty");
						}
						else
						{
							targets_.SetRandomWeightIfZero(
								ps.Get(PS.IdleNaturalRandomWeight), "random, scene idle");

							SetLastResult("yes, idle");
						}
					}
					else
					{
						targets_.SetRandomWeightIfZero(
							ps.Get(PS.NaturalRandomWeight), "random");

						SetLastResult("yes");
					}
				}
			}

			return Continue;
		}

		public override string ToString()
		{
			return "random";
		}
	}
}
