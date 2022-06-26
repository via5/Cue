using System.Collections.Generic;

namespace Cue
{
	public interface IAI
	{
		T GetEvent<T>() where T : class, IEvent;
		IEvent[] Events { get; }

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
		private IEvent[] events_ = null;
		private bool hasIdlePose_ = false;

		public PersonAI(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.AI, person_, "ai");
			events_ = BasicEvent.All();
		}

		public void Init()
		{
			for (int i = 0; i < events_.Length; ++i)
				events_[i].Init(person_);
		}

		public bool EventsEnabled
		{
			get { return eventsEnabled_; }
			set { eventsEnabled_ = value; }
		}

		public T GetEvent<T>() where T : class, IEvent
		{
			for (int i = 0; i < events_.Length; ++i)
			{
				if (events_[i] is T)
					return events_[i] as T;
			}

			return null;
		}

		public IEvent[] Events
		{
			get { return events_; }
		}

		public void FixedUpdate(float s)
		{
			if (eventsEnabled_)
			{
				for (int i = 0; i < events_.Length; ++i)
					events_[i].FixedUpdate(s);
			}
		}

		public void Update(float s)
		{
			if (Cue.Instance.Options.IdlePose && !hasIdlePose_)
			{
				hasIdlePose_ = true;
				person_.Animator.PlayType(AnimationTypes.Idle);
			}
			else if (!Cue.Instance.Options.IdlePose && hasIdlePose_)
			{
				hasIdlePose_ = false;
				person_.Animator.StopType(AnimationTypes.Idle);
			}

			if (eventsEnabled_)
			{
				for (int i = 0; i < events_.Length; ++i)
					events_[i].Update(s);
			}
		}

		public void OnPluginState(bool b)
		{
			for (int i = 0; i < events_.Length; ++i)
				events_[i].OnPluginState(b);
		}
	}
}
