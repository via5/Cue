namespace Cue
{
	interface IEvent
	{
		void Stop();
		bool Update(float s);
	}

	abstract class BasicEvent : IEvent
	{
		protected Person person_;
		//private Slot lockedSlot_ = null;

		protected BasicEvent(Person p)
		{
			person_ = p;
		}

		public virtual void Stop()
		{
			Unlock();
		}

		protected void Unlock()
		{
			// todo, this event didn't necessarily lock that slot
			if (person_.LockedSlot != null)
				person_.LockedSlot.Unlock(person_);
		}

		public abstract bool Update(float s);
	}


	class CallEvent : BasicEvent
	{
		private const int NoState = 0;
		private const int MovingState = 1;
		private const int IdlingState = 2;

		private Person caller_;
		private int state_ = NoState;

		public CallEvent(Person p, Person caller)
			: base(p)
		{
			caller_ = caller;
		}

		public override bool Update(float s)
		{
			switch (state_)
			{
				case NoState:
				{
					var target =
						caller_.Position +
						Vector3.Rotate(new Vector3(0, 0, 0.5f), caller_.Bearing);

					person_.MoveTo(target, caller_.Bearing + 180);
					person_.Gaze.LookAt = GazeSettings.LookAtTarget;
					person_.Gaze.Target = caller_.Atom.HeadPosition;
					state_ = MovingState;

					break;
				}

				case MovingState:
				{
					if (!person_.HasTarget)
					{
						person_.PushAction(new RandomAnimationAction(
							Resources.Animations.GetAll(
								Resources.Animations.StandIdle, person_.Sex)));

						state_ = IdlingState;
					}

					break;
				}

				case IdlingState:
				{
					return false;
				}
			}

			return true;
		}

		public override string ToString()
		{
			return "CallEvent";
		}
	}
}
