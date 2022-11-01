using SimpleJSON;

namespace Cue
{
	public interface IEvent
	{
		string Name { get; }
		bool Active { get; set; }
		bool CanToggle { get; }
		bool CanDisable { get; }

		IEventData ParseEventData(JSONClass o);
		void Init(Person p);
		void OnPluginState(bool b);
		void FixedUpdate(float s);
		void Update(float s);
		void ForceStop();
		void Debug(DebugLines debug);
		DebugButtons DebugButtons();
	}


	abstract class BasicEvent : IEvent
	{
		private string name_;
		protected Person person_ = null;
		private Logger log_;
		private bool enabled_ = true;

		private Sys.IBoolParameter enabledParam_ = null;
		private Sys.IBoolParameter activeParam_ = null;
		private Sys.IActionParameter activeToggle_ = null;

		protected BasicEvent(string name)
		{
			name_ = name;
			log_ = new Logger(Logger.Event, "event." + name);
		}

		public Logger Log
		{
			get { return log_; }
		}

		public string Name
		{
			get { return name_; }
		}

		public abstract bool Active { get; set; }

		public bool Enabled
		{
			get { return enabled_; }
			set { enabled_ = value; }
		}

		public abstract bool CanToggle { get; }
		public abstract bool CanDisable { get; }

		public static IEvent[] All()
		{
			// todo: there's an ordering problem, where GrabEvent locks the
			// head when grabbed, but MouthEvent tries to lock when the grab
			// is released
			//
			// GrabEvent is at the top right now, which fixes this, but it's
			// not a fix

			return new IEvent[]
			{
				new GrabEvent(),
				new MouthEvent(),
				new KissEvent(),
				new SmokeEvent(),
				new ThrustEvent(),
				new TribEvent(),
				new HandEvent(),
				new ZappedEvent(),
				new SuckFingerEvent(),
				new HandLinker(),
				new HoldBreathEvent()
			};
		}

		public static IEvent Create(string type)
		{
			foreach (var e in All())
			{
				if (e.Name.ToLower() == type.ToLower())
					return e;
			}

			return null;
		}

		public IEventData ParseEventData(JSONClass o)
		{
			return DoParseEventData(o);
		}

		protected virtual IEventData DoParseEventData(JSONClass o)
		{
			return null;
		}

		public void ForceStop()
		{
			DoForceStop();
		}

		protected virtual void DoForceStop()
		{
			// no-op
		}

		public void Init(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Event, person_, "event." + Name);
			DoInit();

			if (p.IsInteresting)
			{
				if (CanDisable)
				{
					enabledParam_ = Cue.Instance.Sys.RegisterBoolParameter(
						$"{person_.ID}.{Name}.Enabled",
						b => Enabled = b,
						Enabled);
				}

				if (CanToggle)
				{
					activeParam_ = Cue.Instance.Sys.RegisterBoolParameter(
						$"{person_.ID}.{Name}.Active",
						b => Active = b,
						Active);

					activeToggle_ = Cue.Instance.Sys.RegisterActionParameter(
						$"{person_.ID}.{Name}.Toggle",
						() => Active = !Active);
				}
			}
		}

		protected virtual void DoInit()
		{
			// no-op
		}

		public void OnPluginState(bool b)
		{
			DoOnPluginState(b);
		}

		protected virtual void DoOnPluginState(bool b)
		{
			// no-op
		}

		public void FixedUpdate(float s)
		{
			DoFixedUpdate(s);
		}

		protected virtual void DoFixedUpdate(float s)
		{
			// no-op
		}

		public void Update(float s)
		{
			DoUpdate(s);

			if (enabledParam_ != null)
				enabledParam_.Value = Enabled;

			if (activeParam_ != null)
				activeParam_.Value = Active;
		}

		protected virtual void DoUpdate(float s)
		{
		}

		public override string ToString()
		{
			return name_;
		}

		public virtual void Debug(DebugLines debug)
		{
		}

		public virtual DebugButtons DebugButtons()
		{
			return null;
		}
	}
}
