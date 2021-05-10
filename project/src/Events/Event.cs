﻿using System;

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
		protected Logger log_;
		private Slot lockedSlot_ = null;

		protected BasicEvent(Person p, string name)
		{
			person_ = p;
			log_ = new Logger(Logger.Event, p, name + "Event");
		}

		public Slot LockedSlot
		{
			get { return lockedSlot_; }
			set { lockedSlot_ = value; }
		}

		public virtual void Stop()
		{
			log_.Info($"{this}: stopping");
			Unlock();
		}

		protected void Unlock()
		{
			if (lockedSlot_ != null)
			{
				log_.Info($"{this}: {lockedSlot_} was locked by event, unlocking");
				lockedSlot_.Unlock(person_);
				lockedSlot_ = null;
			}
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
			: base(p, "Call")
		{
			caller_ = caller;
			post_ = post;
		}

		public override bool Update(float s)
		{
			if (call_ == null)
			{
				call_ = new CallAction(person_, caller_);
				person_.PushAction(call_);
			}
			else
			{
				if (person_.Idle)
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
