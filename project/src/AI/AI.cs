using System.Collections.Generic;

namespace Cue
{
	interface IAI
	{
		T GetEvent<T>() where T : class, IEvent;
		List<IEvent> Events { get; }

		void Init();
		void FixedUpdate(float s);
		void Update(float s);
		bool EventsEnabled { get; set; }
		void OnPluginState(bool b);
	}


	class PersonAI : IAI
	{
		private Person person_ = null;
		private Logger log_;
		private bool eventsEnabled_ = true;
		private readonly List<IEvent> events_ = new List<IEvent>();

		public PersonAI(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.AI, person_, "AI");

			events_.AddRange(BasicEvent.All(p));
		}

		public void Init()
		{
			person_.Animator.PlayType(Animations.Idle);
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

		public List<IEvent> Events
		{
			get { return events_; }
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
	}
}
