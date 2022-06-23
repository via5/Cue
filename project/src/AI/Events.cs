﻿using SimpleJSON;

namespace Cue
{
	public interface IEvent
	{
		string Name { get; }

		IEventData ParseEventData(JSONClass o);
		void Init(Person p);
		void OnPluginState(bool b);
		void FixedUpdate(float s);
		void Update(float s);
		void ForceStop();
		string[] Debug();
	}


	abstract class BasicEvent : IEvent
	{
		private string name_;
		protected Person person_ = null;
		private Logger log_;

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

		public static IEvent[] All()
		{
			return new IEvent[]
			{
				new MouthEvent(),
				new KissEvent(),
				new SmokeEvent(),
				new ThrustEvent(),
				new HandLinker(),
				new HandEvent(),
				new PenetratedEvent(),
			//	new SuckFingerEvent(),
				new GrabEvent()
			};
		}

		public static IEvent Create(string type)
		{
			foreach (var e in All())
			{
				if (e.Name == type)
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

		public void Init(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Event, person_, "event." + Name);
			DoInit();
		}

		public virtual void ForceStop()
		{
			// no-op
		}

		protected virtual void DoInit()
		{
			// no-op
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
