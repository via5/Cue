namespace Cue
{
	interface IEvent
	{
		string Name { get; }
		void OnPluginState(bool b);
		void FixedUpdate(float s);
		void Update(float s);
		string[] Debug();
	}


	abstract class BasicEvent : IEvent
	{
		private string name_;
		protected Person person_;
		private Logger log_;

		protected BasicEvent(string name, Person p)
		{
			name_ = name;
			person_ = p;
			log_ = new Logger(Logger.Event, p, "event." + name);
		}

		public Logger Log
		{
			get { return log_; }
		}

		public string Name
		{
			get { return name_; }
		}

		public static IEvent[] All(Person p)
		{
			return new IEvent[]
			{
				new MouthEvent(p),
				new KissEvent(p),
				new SmokeEvent(p),
				new ThrustEvent(p),
				new HandLinker(p),
				new HandEvent(p),
				new PenetratedEvent(p),
				new SuckFingerEvent(p),
				new GrabEvent(p)
			};
		}

		public virtual void OnPluginState(bool b)
		{
			// no-op
		}

		public virtual void FixedUpdate(float s)
		{
			// no-op
		}

		public virtual void Update(float s)
		{
			// no-op
		}

		public override string ToString()
		{
			return name_;
		}

		public virtual string[] Debug()
		{
			return null;
		}
	}
}
