using System.Collections.Generic;

namespace Cue
{
	interface IAI
	{
		T GetEvent<T>() where T : class, IEvent;
		bool InteractWith(IObject o);
		void RunCommand(ICommand e);
		void FixedUpdate(float s);
		void Update(float s);
		void MakeIdle();
		bool CommandsEnabled { get; set; }
		bool EventsEnabled { get; set; }
		ICommand ForcedCommand { get; }
		ICommand Command { get; }
		void OnPluginState(bool b);
	}


	class PersonAI : IAI
	{
		private Person person_ = null;
		private Logger log_;
		private int i_ = -1;
		private readonly List<ICommand> commands_ = new List<ICommand>();
		private bool commandsEnabled_ = true;
		private bool eventsEnabled_ = true;
		private ICommand forced_ = null;
		private readonly List<IEvent> events_ = new List<IEvent>();

		public PersonAI(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.AI, person_, "AI");

			//foreach (var o in Cue.Instance.Objects)
			//{
			//	if (o.Slots.Has(Slot.Sit))
			//		commands_.Add(new SitCommand(person_, o));
			//	if (o.Slots.Has(Slot.Lie))
			//		commands_.Add(new LieDownCommand(person_, o));
			//	if (o.Slots.Has(Slot.Stand))
			//		commands_.Add(new StandCommand(person_, o));
			//}

			commands_.Add(new StandCommand(p));
			events_.AddRange(BasicEvent.All(p));
		}

		public bool CommandsEnabled
		{
			get
			{
				return commandsEnabled_;
			}

			set
			{
				commandsEnabled_ = value;
				if (!commandsEnabled_)
					Stop();
			}
		}

		public bool EventsEnabled
		{
			get { return eventsEnabled_; }
			set { eventsEnabled_ = value; }
		}

		public T GetEvent<T>() where T : class, IEvent
		{
			for (int i = 0; i < events_.Count; ++i)
			{
				if (events_[i] is T)
					return events_[i] as T;
			}

			return null;
		}

		public ICommand ForcedCommand
		{
			get
			{
				return forced_;
			}
		}

		public ICommand Command
		{
			get
			{
				if (i_ >= 0 && i_ < commands_.Count && commandsEnabled_)
					return commands_[i_];
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
					RunCommand(new SitCommand(person_, slot));
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
					RunCommand(new StandCommand(person_, slot));
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
			RunCommand(null);
		}

		public void RunCommand(ICommand e)
		{
			if (forced_ != null)
			{
				log_.Info($"stopping current forced command {forced_}");
				forced_.Stop();
			}

			forced_ = e;

			if (forced_ != null)
			{
				log_.Info($"stop to run forced command {forced_}");
				Stop();
			}
		}

		public void FixedUpdate(float s)
		{
			if (eventsEnabled_)
			{
				for (int i = 0; i < events_.Count; ++i)
					events_[i].FixedUpdate(s);
			}
		}

		public void Update(float s)
		{
			if (forced_ != null)
			{
				if (!forced_.Update(s))
				{
					log_.Info("forced command finished, stopping");
					forced_.Stop();
					forced_ = null;
				}
			}
			else if (commandsEnabled_)
			{
				if (commands_.Count > 0)
				{
					if (i_ == -1)
					{
						i_ = 0;
					}
					else
					{
						if (!commands_[i_].Update(s))
						{
							commands_[i_].Stop();

							++i_;
							if (i_ >= commands_.Count)
								i_ = 0;
						}
					}
				}
			}

			if (eventsEnabled_)
			{
				for (int i = 0; i < events_.Count; ++i)
					events_[i].Update(s);
			}
		}

		public void OnPluginState(bool b)
		{
			for (int i = 0; i < events_.Count; ++i)
				events_[i].OnPluginState(b);
		}

		private void Stop()
		{
			if (i_ >= 0 && i_ < commands_.Count)
			{
				commands_[i_].Stop();
				i_ = -1;
			}

			person_.Actions.Clear();
		}
	}
}
