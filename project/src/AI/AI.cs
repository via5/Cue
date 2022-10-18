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
		private Animation idle_ = null;

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
			if (ShouldIdle() && !hasIdlePose_)
			{
				hasIdlePose_ = true;

				if (idle_ == null)
				{
					var name = person_.Personality.GetString(PS.IdleAnimation);
					idle_ = Resources.Animations.Find(name);

					if (idle_ == null)
						person_.Log.Error($"idle animation {name} not found");
				}

				if (idle_ == null)
					person_.Animator.PlayType(AnimationType.Idle);
				else
					person_.Animator.Play(idle_);
			}
			else if (!ShouldIdle() && hasIdlePose_)
			{
				hasIdlePose_ = false;

				if (idle_ == null)
					person_.Animator.StopType(AnimationType.Idle);
				else
					person_.Animator.Stop(idle_);
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

		private bool ShouldIdle()
		{
			if (!Cue.Instance.Options.IdlePose)
				return false;

			var name = person_.Personality.GetString(PS.IdleAnimation);
			if (string.IsNullOrEmpty(name))
				return false;

			return true;
		}
	}
}
