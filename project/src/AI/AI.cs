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
		void UpdatePaused(float s);
		bool EventsEnabled { get; set; }
		void StopAllEvents();
		void OnPluginState(bool b);
	}


	abstract class PersistentAnimation
	{
		private readonly Person person_;
		private readonly AnimationType type_;
		private bool running_ = false;
		private Animation anim_ = null;
		private bool stopping_ = false;

		public PersistentAnimation(Person p, AnimationType type)
		{
			person_ = p;
			type_ = type;
		}

		public Person Person
		{
			get { return person_; }
		}

		public void Update(float s)
		{
			if (ShouldRun() && !running_)
				Start();
			else if (!ShouldRun() && running_)
				Stop();
		}

		private bool ShouldRun()
		{
			if (!DoShouldRun())
				return false;

			var name = GetAnimationName();
			if (string.IsNullOrEmpty(name))
				return false;

			return true;
		}

		protected abstract bool DoShouldRun();
		protected abstract string GetAnimationName();

		private void Start()
		{
			if (stopping_)
			{
				var s = AnimationStatus.NotPlaying;

				if (anim_ == null)
					s = person_.Animator.PlayingStatus(type_);
				else
					s = person_.Animator.PlayingStatus(anim_);

				if (s == AnimationStatus.NotPlaying)
				{
					anim_ = null;
					stopping_ = false;
				}
				else
				{
					return;
				}
			}

			running_ = true;

			if (anim_ == null)
			{
				var name = GetAnimationName();
				anim_ = Resources.Animations.Find(name);

				if (anim_ == null)
					person_.Log.Error($"persistent animation {name} not found");
			}

			if (anim_ == null)
				person_.Animator.PlayType(type_);
			else
				person_.Animator.Play(anim_);
		}

		public void Stop()
		{
			running_ = false;
			stopping_ = true;

			if (anim_ == null)
			{
				person_.Animator.StopType(type_);
			}
			else
			{
				person_.Animator.Stop(anim_);
			}
		}
	}


	class IdlePersistentAnimation : PersistentAnimation
	{
		public IdlePersistentAnimation(Person p)
			: base(p, AnimationType.Idle)
		{
		}

		protected override bool DoShouldRun()
		{
			if (!Cue.Instance.Options.IdlePose)
				return false;

			if (!Person.Options.IdlePose)
				return false;

			return true;
		}

		protected override string GetAnimationName()
		{
			return Person.Personality.GetString(PS.IdleAnimation);
		}
	}


	class ExcitedPersistentAnimation : PersistentAnimation
	{
		public ExcitedPersistentAnimation(Person p)
			: base(p, AnimationType.Excited)
		{
		}

		protected override bool DoShouldRun()
		{
			if (!Cue.Instance.Options.ExcitedPose)
				return false;

			if (!Person.Options.ExcitedPose)
				return false;

			return true;
		}

		protected override string GetAnimationName()
		{
			return Person.Personality.GetString(PS.ExcitedAnimation);
		}
	}


	class PersonAI : IAI
	{
		private readonly Person person_ = null;
		private readonly Logger log_;
		private bool eventsEnabled_ = true;
		private IEvent[] events_ = null;
		private PersistentAnimation[] anims_;

		public PersonAI(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.AI, person_, "ai");
			events_ = BasicEvent.All();
			anims_ = new PersistentAnimation[]
			{
				new IdlePersistentAnimation(person_),
				new ExcitedPersistentAnimation(person_)
			};

			p.PersonalityChanged += OnPersonalityChanged;
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

		public void StopAllEvents()
		{
			foreach (var e in events_)
				e.Active = false;
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
			for (int i = 0; i < anims_.Length; ++i)
				anims_[i].Update(s);

			if (eventsEnabled_)
			{
				for (int i = 0; i < events_.Length; ++i)
					events_[i].Update(s);
			}
		}

		public void UpdatePaused(float s)
		{
			if (eventsEnabled_)
			{
				for (int i = 0; i < events_.Length; ++i)
					events_[i].UpdatePaused(s);
			}
		}

		public void OnPluginState(bool b)
		{
			for (int i = 0; i < events_.Length; ++i)
				events_[i].OnPluginState(b);
		}

		private void OnPersonalityChanged()
		{
			for (int i = 0; i < anims_.Length; ++i)
				anims_[i].Stop();
		}
	}
}
