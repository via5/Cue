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
			}
			else
			{
				if (person_.Mood.GazeTiredness >= ps.Get(PS.MaxTirednessForRandomGaze))
				{
					targets_.SetRandomWeightIfZero(0, "random, but tired");
				}
				else
				{
					if (IsSceneIdle())
					{
						if (IsSceneEmpty())
						{
							targets_.SetRandomWeightIfZero(
								ps.Get(PS.IdleEmptyRandomWeight), "random, scene idle and empty");
						}
						else
						{
							targets_.SetRandomWeightIfZero(
								ps.Get(PS.IdleNaturalRandomWeight), "random, scene idle");
						}
					}
					else
					{
						targets_.SetRandomWeightIfZero(
							ps.Get(PS.NaturalRandomWeight), "random");
					}
				}
			}

			return Continue;
		}

		private bool IsSceneIdle()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (!p.Mood.IsIdle)
					return false;
			}

			return true;
		}

		private bool IsSceneEmpty()
		{
			return (Cue.Instance.ActivePersons.Length == 2);
		}

		public override string ToString()
		{
			return "random";
		}
	}
}
