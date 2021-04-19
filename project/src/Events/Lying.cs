namespace Cue
{
	class LieDownEvent : BasicEvent
	{
		const int NoState = 0;
		const int Moving = 1;
		const int Sleeping = 2;

		private IObject o_;
		private int state_ = NoState;
		private float elapsed_ = 0;

		public LieDownEvent(Person p, IObject o)
			: base(p)
		{
			o_ = o;
		}

		public override bool Update(float s)
		{
			switch (state_)
			{
				case NoState:
				{
					Cue.LogInfo("going to sleep");
					person_.PushAction(new MoveAction(o_.Position, BasicObject.NoBearing));
					state_ = Moving;
					elapsed_ = 0;
					break;
				}

				case Moving:
				{
					if (person_.Idle)
					{
						Cue.LogInfo("reached bed, sleeping");
						state_ = Sleeping;
					}

					break;
				}

				case Sleeping:
				{
					elapsed_ += s;
					if (elapsed_ > 0)
					{
						Cue.LogInfo("finished sleeping");
						state_ = NoState;
						return false;
					}

					break;
				}
			}

			return true;
		}
	}
}
