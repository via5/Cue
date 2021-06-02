using System.Collections.Generic;

namespace Cue
{
	interface IAI
	{
		bool InteractWith(IObject o);
		void RunEvent(IEvent e);
		void FixedUpdate(float s);
		void Update(float s);
		void MakeIdle();
		bool EventsEnabled { get; set; }
		bool InteractionsEnabled { get; set; }
		IEvent ForcedEvent { get; }
		IEvent Event { get; }
		void OnPluginState(bool b);
	}


	class PersonAI : IAI
	{
		private Person person_ = null;
		private Logger log_;
		private int i_ = -1;
		private readonly List<IEvent> events_ = new List<IEvent>();
		private bool eventsEnabled_ = true;
		private bool interactionsEnabled_ = true;
		private IEvent forced_ = null;
		private readonly List<IInteraction> interactions_ = new List<IInteraction>();

		public PersonAI(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.AI, person_, "AI");

			//foreach (var o in Cue.Instance.Objects)
			//{
			//	if (o.Slots.Has(Slot.Sit))
			//		events_.Add(new SitEvent(person_, o));
			//	if (o.Slots.Has(Slot.Lie))
			//		events_.Add(new LieDownEvent(person_, o));
			//	if (o.Slots.Has(Slot.Stand))
			//		events_.Add(new StandEvent(person_, o));
			//}

			events_.Add(new StandEvent(p));
			interactions_.AddRange(BasicInteraction.All(p));
		}

		public bool EventsEnabled
		{
			get
			{
				return eventsEnabled_;
			}

			set
			{
				eventsEnabled_ = value;
				if (!eventsEnabled_)
					Stop();
			}
		}

		public bool InteractionsEnabled
		{
			get { return interactionsEnabled_; }
			set { interactionsEnabled_ = value; }
		}

		public IEvent ForcedEvent
		{
			get
			{
				return forced_;
			}
		}

		public IEvent Event
		{
			get
			{
				if (i_ >= 0 && i_ < events_.Count && eventsEnabled_)
					return events_[i_];
				else
					return null;
			}
		}

		public bool InteractWith(IObject o)
		{
			//if (o is Person)
			//{
			//	log_.Info("target is person, calling");
			//	person_.UnlockSlot();
			//	person_.MakeIdle();
			//	person_.PushAction(new CallAction(person_, o as Person));
			//	return true;
			//}

			if (!person_.TryLockSlot(o))
			{
				// can't lock the given object
				log_.Info($"can't lock any slot in {person_}");
				return false;
			}


			var slot = person_.LockedSlot;
			log_.Info($"locked slot {slot}");

			if (slot.Type == Slot.Sit)
			{
				log_.Info($"this is a sit slot");
				person_.MakeIdle();

				if (person_ == Cue.Instance.Player)
				{
					person_.PushAction(new SitAction(person_, slot));
					person_.PushAction(new MoveAction(
						slot.ParentObject, person_, slot.Position, slot.Rotation.Bearing));
				}
				else
				{
					RunEvent(new SitEvent(person_, slot));
				}

				return true;
			}
			else if (slot.Type == Slot.Stand)
			{
				log_.Info($"this is a stand slot");
				person_.MakeIdle();

				if (person_ == Cue.Instance.Player)
				{
					person_.PushAction(new MakeIdleAction(person_));
					person_.PushAction(new MoveAction(
						slot.ParentObject, person_, slot.Position, slot.Rotation.Bearing));
				}
				else
				{
					RunEvent(new StandEvent(person_, slot));
				}

				return true;
			}
			else
			{
				log_.Info($"can't interact with {slot}, unlocking");
			}

			slot.Unlock(person_);

			return false;
		}

		public void MakeIdle()
		{
			Stop();
			RunEvent(null);
		}

		public void RunEvent(IEvent e)
		{
			if (forced_ != null)
			{
				log_.Info($"stopping current forced event {forced_}");
				forced_.Stop();
			}

			forced_ = e;

			if (forced_ != null)
			{
				log_.Info($"stop to run forced event {forced_}");
				Stop();
			}
		}

		public void FixedUpdate(float s)
		{
			if (interactionsEnabled_)
			{
				for (int i = 0; i < interactions_.Count; ++i)
					interactions_[i].FixedUpdate(s);
			}
		}

		public void Update(float s)
		{
			if (forced_ != null)
			{
				if (!forced_.Update(s))
				{
					log_.Info("forced event finished, stopping");
					forced_.Stop();
					forced_ = null;
				}
			}
			else if (eventsEnabled_)
			{
				if (events_.Count > 0)
				{
					if (i_ == -1)
					{
						i_ = 0;
					}
					else
					{
						if (!events_[i_].Update(s))
						{
							events_[i_].Stop();

							++i_;
							if (i_ >= events_.Count)
								i_ = 0;
						}
					}
				}
			}

			if (interactionsEnabled_)
			{
				for (int i = 0; i < interactions_.Count; ++i)
					interactions_[i].Update(s);
			}
		}

		public void OnPluginState(bool b)
		{
		}

		private void Stop()
		{
			if (i_ >= 0 && i_ < events_.Count)
			{
				events_[i_].Stop();
				i_ = -1;
			}

			person_.Actions.Clear();
		}
	}
}
