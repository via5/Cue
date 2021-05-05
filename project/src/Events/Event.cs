using System;

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

		private Person caller_;
		private Action post_ = null;
		private int state_ = NoState;

		public CallEvent(Person p, Person caller, Action post=null)
			: base(p)
		{
			caller_ = caller;
			post_ = post;
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

					Cue.LogInfo($"{person_}: moving to {caller_}");
					person_.MoveTo(target, caller_.Bearing + 180);
					person_.Gaze.LookAt(caller_);
					state_ = MovingState;

					break;
				}

				case MovingState:
				{
					if (!person_.HasTarget)
					{
						Cue.LogInfo($"{person_}: call event finished");
						post_?.Invoke();
						return false;
					}

					break;
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
