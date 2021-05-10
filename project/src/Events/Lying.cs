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
			: base(p, "LieDown")
		{
			o_ = o;
		}

		public override bool Update(float s)
		{
			switch (state_)
			{
				case NoState:
				{
					log_.Info("going to sleep");
					person_.PushAction(new MoveAction(person_, o_.Position, BasicObject.NoBearing));
					state_ = Moving;
					elapsed_ = 0;
					break;
				}

				case Moving:
				{
					if (person_.Idle)
					{
						log_.Info("reached bed, sleeping");
						state_ = Sleeping;
					}

					break;
				}

				case Sleeping:
				{
					elapsed_ += s;
					if (elapsed_ > 0)
					{
						log_.Info("finished sleeping");
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
