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
		private CallAction call_ = null;
		private Action post_ = null;

		public CallEvent(Person p, Person caller, Action post=null)
			: base(p)
		{
			caller_ = caller;
			post_ = post;
		}

		public override bool Update(float s)
		{
			if (call_ == null)
			{
				call_ = new CallAction(caller_);
				person_.PushAction(call_);
			}
			else
			{
				if (!person_.Idle)
				{
					post_?.Invoke();
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
