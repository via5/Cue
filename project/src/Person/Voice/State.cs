using SimpleJSON;

namespace Cue
{
	public interface IVoiceState
	{
		string Name { get; }
		string LastState { get; }
		bool Done { get; }

		void Load(JSONClass vo, bool inherited);
		IVoiceState Clone();
		void Init(Voice v);
		void Start();
		void EarlyUpdate(float s);
		void Update(float s);
		int CanRun();
		bool HasEmergency();
		void Debug(DebugLines debug);
	}


	public abstract class VoiceState : IVoiceState
	{
		public const int CannotRun = 0;
		public const int LowPriority = 1;
		public const int HighPriority = 2;
		public const int Emergency = 3;

		protected Voice v_ = null;
		private bool done_ = false;
		private string lastState_ = "";

		public abstract IVoiceState Clone();

		public abstract void Load(JSONClass vo, bool inherited);

		public void Init(Voice v)
		{
			v_ = v;
		}

		public void Start()
		{
			DoStart();
		}

		public void EarlyUpdate(float s)
		{
			DoEarlyUpdate(s);
		}

		public void Update(float s)
		{
			done_ = false;
			DoUpdate(s);
		}

		public abstract int CanRun();

		public virtual bool HasEmergency()
		{
			return false;
		}

		public void Debug(DebugLines debug)
		{
			DoDebug(debug);
		}

		public abstract string Name { get; }

		public string LastState
		{
			get { return lastState_; }
		}

		public virtual bool Done
		{
			get { return done_; }
		}

		protected void SetDone()
		{
			done_ = true;
		}

		protected void SetLastState(string s)
		{
			lastState_ = s;
		}

		protected virtual void DoEarlyUpdate(float s)
		{
			// no-op
		}

		protected virtual void DoUpdate(float s)
		{
			// no-op
		}

		protected virtual void DoStart()
		{
			// no-op
		}

		protected abstract void DoDebug(DebugLines debug);
	}
}
