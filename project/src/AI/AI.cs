using System.Collections.Generic;

namespace Cue
{
	interface IAI
	{
		bool InteractWith(IObject o);
		void RunEvent(IEvent e);
		void Update(float s);
		bool Enabled { get; set; }
		IEvent Event { get; }
		void OnPluginState(bool b);
		Mood Mood { get; }
	}


	class PersonAI : IAI
	{
		private Person person_ = null;
		private int i_ = -1;
		private readonly List<IEvent> events_ = new List<IEvent>();
		private bool enabled_ = false;
		private IEvent forced_ = null;
		private readonly List<IInteraction> interactions_ = new List<IInteraction>();
		private Mood mood_;

		public PersonAI(Person p)
		{
			person_ = p;
			mood_ = new Mood(person_);

			foreach (var o in Cue.Instance.Objects)
			{
				if (o.Slots.Has(Slot.Sit))
					events_.Add(new SitEvent(person_, o));
				if (o.Slots.Has(Slot.Lie))
					events_.Add(new LieDownEvent(person_, o));
				if (o.Slots.Has(Slot.Stand))
					events_.Add(new StandEvent(person_, o));
			}

			interactions_.Add(new KissingInteraction(person_));
		}

		public bool Enabled
		{
			get
			{
				return enabled_;
			}

			set
			{
				enabled_ = value;
				if (!enabled_)
					Stop();
			}
		}

		public IEvent Event
		{
			get
			{
				if (forced_ != null)
					return forced_;
				else if (i_ >= 0 && i_ < events_.Count)
					return events_[i_];
				else
					return null;
			}
		}

		public Mood Mood
		{
			get { return mood_; }
		}

		public bool InteractWith(IObject o)
		{
			if (!person_.TryLockSlot(o))
			{
				// can't lock the given object
				return false;
			}


			var slot = person_.LockedSlot;

			if (slot.Type == Slot.Sit)
			{
				person_.MakeIdle();

				if (person_ == Cue.Instance.Player)
				{
					person_.PushAction(new SitAction(slot));
					person_.PushAction(new MoveAction(slot.Position, BasicObject.NoBearing));
				}
				else
				{
					RunEvent(new SitEvent(person_, slot));
				}

				return true;
			}
			else if (slot.Type == Slot.Stand)
			{
				person_.MakeIdle();

				if (person_ == Cue.Instance.Player)
				{
					person_.PushAction(new MakeIdleAction());
					person_.PushAction(new MoveAction(slot.Position, slot.Bearing));
				}
				else
				{
					RunEvent(new StandEvent(person_, slot));
				}

				return true;
			}

			slot.Unlock(person_);

			return false;
		}

		public void RunEvent(IEvent e)
		{
			if (forced_ != null)
				forced_.Stop();

			forced_ = e;

			if (forced_ != null)
			{
				Cue.LogInfo($"{person_.ID}: running event {e}");
				Stop();
				person_.MakeIdle();
			}
		}

		public void Update(float s)
		{
			if (forced_ != null)
			{
				if (!forced_.Update(s))
				{
					forced_.Stop();
					forced_ = null;
				}
			}
			else if (enabled_)
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

			mood_.Update(s);

			for (int i = 0; i < interactions_.Count; ++i)
				interactions_[i].Update(s);
		}

		public void OnPluginState(bool b)
		{
			mood_.OnPluginState(b);
		}

		private void Stop()
		{
			if (i_ >= 0 && i_ < events_.Count)
			{
				events_[i_].Stop();
				i_ = -1;
			}
		}
	}
}
