using SimpleJSON;
using System;

namespace Cue
{
	public interface IEventData
	{
		BasicEventData Clone();
	}

	public abstract class BasicEventData : IEventData
	{
		public bool enabled = true;

		public abstract BasicEventData Clone();

		protected void CopyFrom(BasicEventData d)
		{
			enabled = d.enabled;
		}
	}

	public class EmptyEventData : BasicEventData
	{
		public override BasicEventData Clone()
		{
			var d = new EmptyEventData();
			d.CopyFrom(this);
			return d;
		}
	}


	public interface IEvent
	{
		string Name { get; }
		bool Active { get; set; }
		bool CanToggle { get; }
		bool CanDisable { get; }

		BasicEventData ParseEventData(JSONClass o);
		void Init(Person p);
		void OnPluginState(bool b);
		void FixedUpdate(float s);
		void Update(float s);
		void UpdatePaused(float s);
		void ForceStop();
		void Debug(DebugLines debug);
		DebugButtons DebugButtons();
	}


	abstract class BasicEvent<DataType> : IEvent where DataType : BasicEventData, new()
	{
		private string name_;
		protected Person person_ = null;
		private Logger log_;
		private DataType d_ = null;

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

		protected DataType Data
		{
			get { return d_; }
		}

		public string Name
		{
			get { return name_; }
		}

		public abstract bool Active { get; set; }

		public bool Enabled
		{
			get
			{
				Cue.Assert(d_ != null);
				return d_.enabled;
			}
			set { d_.enabled = value; }
		}

		public abstract bool CanToggle { get; }
		public abstract bool CanDisable { get; }

		public BasicEventData ParseEventData(JSONClass o)
		{
			var d = new DataType();

			d.enabled = J.OptBool(o, "enabled", true);

			if (d.enabled)
				DoParseEventData(o, d);

			return d;
		}

		protected virtual void DoParseEventData(JSONClass o, DataType d)
		{
			// no-op
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
			try
			{
				person_ = p;
				log_ = new Logger(Logger.Event, person_, "event." + Name);

				OnPersonalityChanged();
				person_.PersonalityChanged += OnPersonalityChanged;

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
			catch (Exception e)
			{
				Log.Error($"failed to init event {Name}");
				Log.Error(e.ToString());
			}
		}

		private void OnPersonalityChanged()
		{
			d_ = person_.Personality.CloneEventData(Name) as DataType;
			if (d_ == null)
			{
				Cue.Assert(
					typeof(DataType) == typeof(EmptyEventData),
					$"no event data for event '{Name}'");

				d_ = new EmptyEventData() as DataType;
			}

			Cue.Assert(d_ != null);
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
			// no-op
		}

		public void UpdatePaused(float s)
		{
			DoUpdatePaused(s);
		}

		protected virtual void DoUpdatePaused(float s)
		{
			// no-op
		}

		public override string ToString()
		{
			return name_;
		}

		public void Debug(DebugLines debug)
		{
			debug.Add("enabled", $"{d_.enabled}");

			if (d_.enabled)
				DoDebug(debug);
		}

		protected virtual void DoDebug(DebugLines debug)
		{
			// no-op
		}

		public virtual DebugButtons DebugButtons()
		{
			return null;
		}
	}
}
