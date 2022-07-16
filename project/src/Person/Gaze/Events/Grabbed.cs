namespace Cue
{
	class GazeGrabbed : BasicGazeEvent
	{
		private BodyPart head_;
		private bool active_ = false;
		private float activeElapsed_ = 0;

		public GazeGrabbed(Person p)
			: base(p, I.GazeGrabbed)
		{
			head_ = person_.Body.Get(BP.Head);
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;

			if (active_)
			{
				targets_.SetWeight(
					Cue.Instance.Player, BP.Eyes,
					ps.Get(PS.LookAtPlayerOnGrabWeight), "head grabbed");

				// don't disable gazer, mg won't affect the head while it's
				// being grabbed, and it snaps the head back to its original
				// position when it's re-enabled
			}

			return Continue;
		}

		protected override bool DoHasEmergency(float s)
		{
			var ps = person_.Personality;

			if (ps.Get(PS.LookAtPlayerOnGrabWeight) != 0)
			{
				if (head_.GrabbedByPlayer && Cue.Instance.Player.IsInteresting)
				{
					active_ = true;
					activeElapsed_ = 0;
				}
				else if (active_)
				{
					activeElapsed_ += s;

					if (activeElapsed_ > ps.Get(PS.LookAtPlayerTimeAfterGrab))
						active_ = false;
				}
			}

			return active_;
		}

		public override string ToString()
		{
			return "head grabbed";
		}
	}
}
