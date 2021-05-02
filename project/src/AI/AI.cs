using System;
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


	interface IInteraction
	{
		void Update(float s);
	}

	class KissingInteraction : IInteraction
	{
		public const float StartDistance = 0.15f;
		public const float StopDistance = 0.1f;
		public const float PlayerStopDistance = 0.2f;
		public const float MinimumActiveTime = 3;
		public const float MinimumStoppedTime = 2;
		public const float Cooldown = 10;

		private Person person_;
		private float elapsed_ = 0;
		private float cooldownRemaining_ = 0;

		public KissingInteraction(Person p)
		{
			person_ = p;
		}

		public void Update(float s)
		{
			if (person_.Body.Lips == null)
				return;

			cooldownRemaining_ = Math.Max(cooldownRemaining_ - s, 0);

			if (person_.Kisser.Active)
			{
				if (person_.Kisser.Elapsed >= MinimumActiveTime)
					TryStop();
			}
			else if (cooldownRemaining_ == 0)
			{
				elapsed_ += s;
				if (elapsed_ > 1)
				{
					TryStart();
					elapsed_ = 0;
				}
			}
		}

		private bool TryStart()
		{
			var srcLips = person_.Body.Lips.Position;

			for (int i = 0; i < Cue.Instance.Persons.Count; ++i)
			{
				var target = Cue.Instance.Persons[i];
				if (target == person_)
					continue;

				if (target.Body.Lips == null || target.Kisser.Active)
					continue;

				// todo: check rotations
				var targetLips = target.Body.Lips.Position;

				if (Vector3.Distance(srcLips, targetLips) < StartDistance)
				{
					Cue.LogInfo($"starting kiss for {person_} and {target}");
					person_.Kisser.StartReciprocal(target);
					return true;
				}
			}

			return false;
		}

		private bool TryStop()
		{
			var target = person_.Kisser.Target;
			if (target == null)
				return false;

			if (target.Body.Lips == null)
				return false;

			var srcLips = person_.Body.Lips.Position;
			var targetLips = target.Body.Lips.Position;
			var d = Vector3.Distance(srcLips, targetLips);

			var hasPlayer = (
				person_ == Cue.Instance.Player ||
				target == Cue.Instance.Player);

			var sd = (hasPlayer ? PlayerStopDistance : StopDistance);

			if (d >= sd)
			{
				Cue.LogInfo($"{person_}: stopping kiss, {d}>={sd}");
				person_.Kisser.Stop();
				target.Kisser.Stop();
				cooldownRemaining_ = Cooldown;
				return true;
			}

			return false;
		}
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
					RunEvent(new SitEvent(person_, slot));
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
					RunEvent(new StandEvent(person_, slot));
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
