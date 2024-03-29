﻿using SimpleJSON;
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
		void Debug(DebugLines debug);
	}


	public abstract class BasicVoiceState : IVoiceState
	{
		public const int CannotRun = 0;
		public const int LowPriority = 1;
		public const int HighPriority = 2;
		public const int Emergency = 3;

		protected Voice v_ = null;
		private bool enabled_ = false;
		private bool done_ = false;
		private string lastState_ = "";

		public abstract IVoiceState Clone();

		public void Load(JSONClass vo, bool inherited)
		{
			if (vo.HasKey(Name))
			{
				var o = J.ReqObject(vo, Name);

				if (o.HasKey("enabled"))
					enabled_ = J.ReqBool(o, "enabled");
				else if (!inherited)
					throw new LoadFailed("missing enabled");

				if (enabled_)
					DoLoad(o, inherited);
			}
			else if (!inherited)
			{
				throw new LoadFailed($"missing state {Name}");
			}
		}

		protected virtual void DoLoad(JSONClass o, bool inherited)
		{
		}

		protected void CopyFrom(BasicVoiceState o)
		{
			enabled_ = o.enabled_;
		}

		public static List<IVoiceState> CreateAll(JSONClass o)
		{
			return new List<IVoiceState>()
			{
				new VoiceStateNormal(o),
				new VoiceStateOrgasm(o),
				new VoiceStateKiss(o),
				new VoiceStateBJ(o),
				new VoiceStateChoked(o)
			};
		}

		public Person Person
		{
			get { return v_.Person; }
		}

		public void Init(Voice v)
		{
			v_ = v;
		}

		public void Start()
		{
			v_.MouthEnabled = true;
			v_.ChestEnabled = true;

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

		public int CanRun()
		{
			if (!enabled_)
				return CannotRun;

			return DoCanRun();
		}

		public void Debug(DebugLines debug)
		{
			debug.Add("enabled", $"{enabled_}");

			if (enabled_)
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

		protected abstract int DoCanRun();

		protected abstract void DoDebug(DebugLines debug);
	}


	public abstract class VoiceStateWithMoaning : BasicVoiceState
	{
		private float voiceChance_ = 0;
		private float voiceChanceMinExcitement_ = 0.2f;
		private float voiceTime_ = 0;
		private float elapsed_ = 0;

		private bool moaning_ = false;
		private float lastRng_ = 0;

		protected override void DoLoad(JSONClass o, bool inherited)
		{
			if (o.HasKey("voiceChance"))
				voiceChance_ = J.ReqFloat(o, "voiceChance");
			else if (!inherited)
				throw new LoadFailed("missing voiceChance");

			if (o.HasKey("voiceChanceMinExcitement"))
				voiceChanceMinExcitement_ = J.ReqFloat(o, "voiceChanceMinExcitement");
			else if (!inherited)
				throw new LoadFailed("missing voiceChanceMinExcitement");

			if (o.HasKey("voiceTime"))
				voiceTime_ = J.ReqFloat(o, "voiceTime");
			else if (!inherited)
				throw new LoadFailed("missing voiceTime");
		}

		protected void CopyFrom(VoiceStateWithMoaning o)
		{
			base.CopyFrom(o);
			voiceChance_ = o.voiceChance_;
			voiceTime_ = o.voiceTime_;
		}

		protected override void DoUpdate(float s)
		{
			if (CanRun() == CannotRun)
			{
				SetDone();
				return;
			}

			elapsed_ += s;
			if (elapsed_ >= voiceTime_)
			{
				elapsed_ = 0;

				lastRng_ = U.RandomFloat(0, 1);
				if (ShouldMoan())
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

		private bool ShouldMoan()
		{
			if (Person.Mood.Get(MoodType.Excited) >= voiceChanceMinExcitement_)
			{
				if (lastRng_ <= voiceChance_)
					return true;
			}

			return false;
		}

		protected override void DoDebug(DebugLines debug)
		{
			debug.Add("elapsed", $"{elapsed_:0.00}/{voiceTime_:0.00}");
			debug.Add("moaning", $"{moaning_} minEx={voiceChanceMinExcitement_} ex={Person.Mood.Get(MoodType.Excited):0.00}");
			debug.Add("lastRng", $"{lastRng_:0.00}/{voiceChance_:0.00}");
		}

		protected abstract void DoSetSound();
	}
}
