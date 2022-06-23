using SimpleJSON;
using System.Collections.Generic;

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

		public static List<IVoiceState> CreateAll(JSONClass o)
		{
			return new List<IVoiceState>()
			{
				new VoiceStateNormal(o),
				new VoiceStatePause(o),
				new VoiceStateOrgasm(o),
				new VoiceStateKiss(o),
				new VoiceStateBJ(o)
			};
		}

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


	public abstract class VoiceStateWithMoaning : VoiceState
	{
		private bool enabled_ = false;
		private float voiceChance_ = 0;
		private float voiceTime_ = 0;
		private float elapsed_ = 0;

		private bool moaning_ = false;
		private float lastRng_ = 0;

		protected void LoadWithMoaning(JSONClass o, bool inherited)
		{
			if (o.HasKey("enabled"))
				enabled_ = J.ReqBool(o, "enabled");
			else if (!inherited)
				throw new LoadFailed("missing enabled");

			if (o.HasKey("voiceChance"))
				voiceChance_ = J.ReqFloat(o, "voiceChance");
			else if (!inherited)
				throw new LoadFailed("missing voiceChance");

			if (o.HasKey("voiceTime"))
				voiceTime_ = J.ReqFloat(o, "voiceTime");
			else if (!inherited)
				throw new LoadFailed("missing voiceTime");
		}

		protected void CopyFrom(VoiceStateWithMoaning o)
		{
			enabled_ = o.enabled_;
			voiceChance_ = o.voiceChance_;
			voiceTime_ = o.voiceTime_;
		}

		public override int CanRun()
		{
			if (HasEmergency())
			{
				SetLastState("ok");
				return Emergency;
			}

			SetLastState("not kissing");
			return CannotRun;
		}

		public override bool HasEmergency()
		{
			if (!enabled_)
				return false;

			if (DoCanRun())
			{
				SetLastState("ok");
				return true;
			}

			return false;
		}

		protected override void DoUpdate(float s)
		{
			if (!DoCanRun())
			{
				SetDone();
				return;
			}

			elapsed_ += s;
			if (elapsed_ >= voiceTime_)
			{
				elapsed_ = 0;

				lastRng_ = U.RandomFloat(0, 1);
				if (lastRng_ <= voiceChance_)
				{
					moaning_ = true;
					v_.Provider.SetMoaning(v_.MaxIntensity);
				}
				else
				{
					moaning_ = false;
					DoSetSound();
				}
			}
		}

		protected override void DoDebug(DebugLines debug)
		{
			debug.Add("elapsed", $"{elapsed_:0.00}/{voiceTime_:0.00}");
			debug.Add("moaning", $"{moaning_:0.00}");
			debug.Add("lastRng", $"{lastRng_:0.00}/{voiceChance_:0.00}");
		}

		protected abstract bool DoCanRun();
		protected abstract void DoSetSound();
	}
}
