namespace Cue
{
	class GazeOtherPersons : BasicGazeEvent
	{
		public GazeOtherPersons(Person p)
			: base(p, I.GazeOtherPersons)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_ || !p.IsInteresting)
					continue;

				if (g_.ShouldAvoid(p))
				{
					targets_.SetReluctant(p, true, g_.AvoidWeight(p), "avoid");
				}
				else if (person_.Mood.GazeTiredness >= ps.Get(PS.MaxTirednessForRandomGaze))
				{
					// doesn't do anything, just to get the why in the ui
					targets_.SetWeightIfZero(
						p, BP.Eyes, 0, "random person, but tired");
				}
				else if (p.Mood.State == Mood.OrgasmState)
				{
					person_.Gaze.Clear();

					targets_.SetWeightIfZero(
						p, BP.Eyes,
						ps.Get(PS.OtherEyesOrgasmWeight), "orgasming");
				}
				else
				{
					float w = 0;

					if (p.IsPlayer)
					{
						if (Bits.IsSet(flags, Busy))
							w = ps.Get(PS.BusyPlayerEyesWeight);
						else
							w = ps.Get(PS.NaturalPlayerEyesWeight);
					}
					else
					{
						if (Bits.IsSet(flags, Busy))
							w = ps.Get(PS.BusyOtherEyesWeight);
						else
							w = ps.Get(PS.NaturalOtherEyesWeight);
					}

					w += p.Mood.GazeEnergy * ps.Get(PS.OtherEyesExcitementWeight);

					targets_.SetWeightIfZero(
						p, BP.Eyes, w, "random person");
				}
			}

			return Continue;
		}

		protected override bool DoHasEmergency(float s)
		{
			var ps = person_.Personality;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_ || !p.IsInteresting)
					continue;

				if (!g_.ShouldAvoid(p) &&
					person_.Mood.GazeTiredness >= ps.Get(PS.MaxTirednessForRandomGaze) &&
					p.Mood.State == Mood.OrgasmState)
				{
					return true;
				}
			}

			return false;
		}

		public override string ToString()
		{
			return "others";
		}
	}
}
