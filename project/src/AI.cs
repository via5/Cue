using System.Collections.Generic;

namespace Cue
{
	interface IAI
	{
		bool InteractWith(IObject o);
		void RunEvent(IEvent e);
		void Update(float s);
		bool Enabled { get; set; }
	}

	class PersonAI : IAI
	{
		private Person person_ = null;
		private int i_ = -1;
		private readonly List<IEvent> events_ = new List<IEvent>();
		private bool enabled_ = false;
		private IEvent forced_ = null;

		public PersonAI(Person person)
		{
			person_ = person;

			foreach (var o in Cue.Instance.Objects)
			{
				if (o.Slots.Has(Slot.Sit))
					events_.Add(new SitAndThinkEvent(person_, o));
				if (o.Slots.Has(Slot.Lie))
					events_.Add(new LieDownEvent(person_, o));
				if (o.Slots.Has(Slot.Stand))
					events_.Add(new StandAndThinkEvent(person_, o));
			}
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

		public bool InteractWith(IObject o)
		{
			if (!person_.TryLockSlot(o))
			{
				// can't lock the given object
				return false;
			}

			person_.MakeIdle();

			var slot = person_.LockedSlot;

			if (slot.Type == Slot.Sit)
			{
				if (person_ == Cue.Instance.Player)
				{
					person_.PushAction(new SitAction(slot));
					person_.PushAction(new MoveAction(slot.Position, BasicObject.NoBearing));
				}
				else
				{
					RunEvent(new SitAndThinkEvent(person_, slot));
				}
			}
			else if (slot.Type == Slot.Stand)
			{
				if (person_ == Cue.Instance.Player)
				{
					person_.PushAction(new MakeIdleAction());
					person_.PushAction(new MoveAction(slot.Position, slot.Bearing));
				}
				else
				{
					RunEvent(new StandAndThinkEvent(person_, slot));
				}
			}

			return true;
		}

		public void RunEvent(IEvent e)
		{
			if (forced_ != null)
				forced_.Stop();

			forced_ = e;

			if (forced_ != null)
			{
				Stop();
				person_.MakeIdle();
			}
		}

		public void Update(float s)
		{
			if (forced_ != null)
			{
				if (!forced_.Update(s))
					forced_ = null;

				return;
			}

			if (enabled_)
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
							++i_;
							if (i_ >= events_.Count)
								i_ = 0;
						}
					}
				}
			}

			if (i_ == -1)
			{
				if (person_ != Cue.Instance.Player)
				{
					person_.Gaze.Target = Cue.Instance.Player.HeadPosition;
					person_.Gaze.LookAt = GazeSettings.LookAtTarget;
				}
			}
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
