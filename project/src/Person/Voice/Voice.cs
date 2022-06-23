using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	class RandomRange
	{
		public Pair<float, float> range = new Pair<float, float>(0, 0);
		public IRandom rng = new UniformRandom();

		public static RandomRange Create(JSONClass o, string name)
		{
			RandomRange r = new RandomRange();

			if (o.HasKey(name))
			{
				var wt = o[name].AsObject;

				if (!wt.HasKey("range"))
					throw new LoadFailed($"{name} missing range");

				var a = wt["range"].AsArray;
				if (a.Count != 2)
					throw new LoadFailed($"bad {name} range");

				r.range.first = a[0].AsFloat;
				r.range.second = a[1].AsFloat;

				if (wt.HasKey("rng"))
				{
					r.rng = BasicRandom.FromJSON(wt["rng"].AsObject);
					if (r.rng == null)
						throw new LoadFailed($"bad {name} rng");
				}
			}

			return r;
		}

		public RandomRange Clone()
		{
			var r = new RandomRange();
			r.range = range;
			r.rng = rng.Clone();

			return r;
		}

		public override string ToString()
		{
			return $"{range.first:0.00},{range.second:0.00};rng={rng}";
		}
	}


	public interface IVoiceState
	{
		string Name { get; }
		string LastState { get; }
		bool Done { get; }

		IVoiceState Clone();
		void Init(Voice v);
		void Start();
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

		public void Init(Voice v)
		{
			v_ = v;
		}

		public void Start()
		{
			DoStart();
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


	public class VoiceStateNormal : VoiceState
	{
		private const float DefaultBreathingIntensityCutoff = 0.6f;

		private float breathingIntensityCutoff_ = DefaultBreathingIntensityCutoff;

		private float intensity_ = 0;
		private float intensityTarget_ = 0;
		private float intensityWait_ = 0;
		private float intensityTime_ = 0;
		private float intensityElapsed_ = 0;
		private float lastIntensity_ = 0;

		private RandomRange intensityWaitRange_ = new RandomRange();
		private RandomRange intensityTimeRange_ = new RandomRange();

		private IRandom intensityTargetRng_ = new UniformRandom();


		private VoiceStateNormal()
		{
		}

		public VoiceStateNormal(JSONClass o)
		{
			breathingIntensityCutoff_ = U.Clamp(
				J.OptFloat(o, "breathingIntensityCutoff", DefaultBreathingIntensityCutoff),
				0, 1);

			intensityWaitRange_ = RandomRange.Create(o, "intensityWait");
			intensityTimeRange_ = RandomRange.Create(o, "intensityTime");

			if (o.HasKey("intensityTarget"))
			{
				var ot = o["intensityTarget"].AsObject;
				if (!ot.HasKey("rng"))
					throw new LoadFailed("intensityTarget missing rng");

				intensityTargetRng_ = BasicRandom.FromJSON(ot["rng"].AsObject);
				if (intensityTargetRng_ == null)
					throw new LoadFailed("bad intensityTarget rng");
			}
		}

		public override string Name
		{
			get { return "normal"; }
		}

		public override IVoiceState Clone()
		{
			var s = new VoiceStateNormal();
			s.CopyFrom(this);
			return s;
		}

		private void CopyFrom(VoiceStateNormal o)
		{
			breathingIntensityCutoff_ = o.breathingIntensityCutoff_;
			intensityWaitRange_ = o.intensityWaitRange_.Clone();
			intensityTimeRange_ = o.intensityTimeRange_.Clone();
			intensityTargetRng_ = o.intensityTargetRng_.Clone();
		}

		public override int CanRun()
		{
			SetLastState("ok");
			return LowPriority;
		}

		protected override void DoUpdate(float s)
		{
			if (intensityWait_ > 0)
			{
				intensityWait_ -= s;
				if (intensityWait_ < 0)
					intensityWait_ = 0;
			}
			else if (Math.Abs(intensity_ - intensityTarget_) < 0.001f)
			{
				NextIntensity();
				SetDone();
			}
			else
			{
				intensityElapsed_ += s;

				if (intensityElapsed_ >= intensityTime_ || intensityTime_ == 0)
				{
					intensity_ = intensityTarget_;
				}
				else
				{
					intensity_ = U.Lerp(
						lastIntensity_, intensityTarget_,
						intensityElapsed_ / intensityTime_);
				}
			}

			SetIntensity();
		}

		private void NextIntensity(float forceTargetNow = -1)
		{
			lastIntensity_ = intensity_;
			intensity_ = intensityTarget_;

			intensityWait_ = intensityWaitRange_.rng.RandomFloat(
				intensityWaitRange_.range.first,
				intensityWaitRange_.range.second,
				v_.MaxIntensity);

			intensityElapsed_ = 0;

			if (forceTargetNow >= 0)
			{
				intensityTarget_ = forceTargetNow;
				intensity_ = forceTargetNow;
				intensityTime_ = 0;
				SetIntensity();
			}
			else
			{
				intensityTime_ = intensityTimeRange_.rng.RandomFloat(
					intensityTimeRange_.range.first,
					intensityTimeRange_.range.second,
					v_.MaxIntensity);

				intensityTarget_ = intensityTargetRng_.RandomFloat(
					0, v_.MaxIntensity, v_.MaxIntensity);

				// 0 is disabled
				if (intensityTarget_ <= 0)
					intensityTarget_ = 0.01f;
			}
		}

		private void SetIntensity()
		{
			v_.Provider.SetIntensity(intensity_);
		}

		protected override void DoDebug(DebugLines debug)
		{
			debug.Add("intensity", $"{intensity_:0.00}->{intensityTarget_:0.00} rng={intensityTargetRng_}");
			debug.Add("intensityWait", $"{intensityWait_:0.00}");
			debug.Add("intensityTime", $"{intensityElapsed_:0.00}/{intensityTime_:0.00}");
			debug.Add("breathingCutoff", $"{breathingIntensityCutoff_:0.00}");
			debug.Add("intensityWait", $"{intensityWaitRange_}");
			debug.Add("intensityTime", $"{intensityTimeRange_}");
			debug.Add("lastIntensity", $"{lastIntensity_:0.00}");
		}
	}


	public class VoiceStatePause : VoiceState
	{
		private float minExcitement_ = 0;
		private RandomRange timeRange_ = new RandomRange();
		private float chance_ = 0;
		private IRandom rng_ = new UniformRandom();

		private float time_ = 0;
		private float elapsed_ = 0;
		private float lastRng_ = 0;
		private bool inPause_ = false;


		private VoiceStatePause()
		{
		}

		public VoiceStatePause(JSONClass o)
		{
			if (o.HasKey("pause"))
			{
				var po = o["pause"].AsObject;

				minExcitement_ = J.ReqFloat(po, "minExcitement");
				timeRange_ = RandomRange.Create(po, "time");

				var co = po["chance"].AsObject;

				chance_ = J.ReqFloat(co, "value");

				if (co.HasKey("rng"))
				{
					rng_ = BasicRandom.FromJSON(co["rng"]);
					if (rng_ == null)
						throw new LoadFailed("bad pause rng");
				}
			}
		}

		public override string Name
		{
			get { return "pause"; }
		}

		public override IVoiceState Clone()
		{
			var s = new VoiceStatePause();
			s.CopyFrom(this);
			return s;
		}

		private void CopyFrom(VoiceStatePause o)
		{
			minExcitement_ = o.minExcitement_;
			timeRange_ = o.timeRange_.Clone();
			chance_ = o.chance_;
			rng_ = o.rng_.Clone();
		}

		protected override void DoStart()
		{
			elapsed_ = 0;

			time_ = timeRange_.rng.RandomFloat(
				timeRange_.range.first,
				timeRange_.range.second,
				v_.MaxIntensity);

			inPause_ = true;

			v_.Provider.SetIntensity(0);
		}

		protected override void DoUpdate(float s)
		{
			elapsed_ += s;

			if (inPause_)
			{
				if (elapsed_ >= time_)
				{
					v_.Provider.SetIntensity(1.0f);
					inPause_ = false;
				}
			}
			else
			{
				if (elapsed_ >= 3)
				{
					SetDone();
				}
			}
		}

		public override int CanRun()
		{
			if (v_.MaxIntensity < minExcitement_)
			{
				SetLastState("excitement too low");
				return CannotRun;
			}

			lastRng_ = rng_.RandomFloat(0, 1, v_.MaxIntensity);
			if (lastRng_ >= chance_)
			{
				SetLastState($"rng failed, {lastRng_} >= {chance_}");
				return CannotRun;
			}

			SetLastState("ok");
			return HighPriority;
		}

		public string SettingsToString()
		{
			return
				$"minEx={minExcitement_:0.00} timeRange={timeRange_} " +
				$"chance={chance_};rng={rng_}";
		}

		public string LiveToString()
		{
			return
				$"time={time_:0.00} elapsed={elapsed_:0.00} " +
				$"lastrng={lastRng_:0.00}";
		}

		protected override void DoDebug(DebugLines debug)
		{
			debug.Add("pause settings", SettingsToString());
			debug.Add("pause", LiveToString());
		}
	}


	public class VoiceStateOrgasm : VoiceState
	{
		private VoiceStateOrgasm()
		{
		}

		public VoiceStateOrgasm(JSONClass o)
		{
		}

		public override string Name
		{
			get { return "orgasm"; }
		}

		public override IVoiceState Clone()
		{
			var s = new VoiceStateOrgasm();
			s.CopyFrom(this);
			return s;
		}

		private void CopyFrom(VoiceStateOrgasm o)
		{
		}

		protected override void DoStart()
		{
			v_.Provider.StartOrgasm();
		}

		protected override void DoUpdate(float s)
		{
			if (v_.Person.Mood.State != Mood.OrgasmState)
			{
				v_.Provider.StopOrgasm();
				SetDone();
			}
		}

		public override int CanRun()
		{
			if (HasEmergency())
			{
				SetLastState("ok");
				return Emergency;
			}

			SetLastState("no orgasm");
			return CannotRun;
		}

		public override bool HasEmergency()
		{
			if (v_.Person.Mood.State == Mood.OrgasmState)
			{
				SetLastState("ok");
				return true;
			}

			return false;
		}

		protected override void DoDebug(DebugLines debug)
		{
		}
	}


	public class VoiceStateKiss : VoiceState
	{
		private bool kissEnabled_ = false;
		private float kissVoiceChance_ = 0;
		private KissEvent e_ = null;


		private VoiceStateKiss()
		{
		}

		public VoiceStateKiss(JSONClass o)
		{
			if (o.HasKey("kiss"))
			{
				var ko = o["kiss"].AsObject;

				kissEnabled_ = J.ReqBool(ko, "enabled");
				kissVoiceChance_ = J.ReqFloat(ko, "voiceChance");
			}
		}

		public override string Name
		{
			get { return "kiss"; }
		}

		public override IVoiceState Clone()
		{
			var s = new VoiceStateKiss();
			s.CopyFrom(this);
			return s;
		}

		private void CopyFrom(VoiceStateKiss o)
		{
		}

		protected override void DoUpdate(float s)
		{
			if (!IsKissing())
				SetDone();
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
			if (IsKissing())
			{
				SetLastState("ok");
				return true;
			}

			return false;
		}

		private bool IsKissing()
		{
			if (e_ == null)
				e_ = v_.Person.AI.GetEvent<KissEvent>();

			return e_.Active;
		}

		protected override void DoDebug(DebugLines debug)
		{
		}
	}


	public class Voice
	{
		private Person person_ = null;
		private IVoice provider_ = null;
		private Logger log_;
		private List<IVoiceState> states_ = new List<IVoiceState>();
		private IVoiceState current_ = null;

		private float maxIntensity_ = 0;
		private bool muted_ = false;
		private bool emergency_ = false;

		private DebugLines debug_ = new DebugLines();


		public Voice()
		{
			log_ = new Logger(Logger.Object, "voice");
		}

		public Voice(JSONClass o)
		{
			if (o == null)
				throw new LoadFailed("no object");

			if (!o.HasKey("provider"))
				throw new LoadFailed("voice missing provider");

			provider_ = CreateProvider(o["provider"].AsObject);

			states_.Add(new VoiceStateNormal(o));
			states_.Add(new VoiceStatePause(o));
			states_.Add(new VoiceStateOrgasm(o));
			states_.Add(new VoiceStateKiss(o));
		}

		private IVoice CreateProvider(JSONClass o)
		{
			if (o == null)
				throw new LoadFailed("voice provider not an object");

			var provider = J.ReqString(o, "name");
			var options = o["options"]?.AsObject;

			var p = Integration.CreateVoice(provider, options);
			if (p == null)
				throw new LoadFailed($"unknown voice provider '{provider}'");

			return p;
		}

		public void Init(Person p)
		{
			person_ = p;
			log_.Set(person_, "voice");
			provider_.Init(person_);

			for (int i = 0; i < states_.Count; ++i)
				states_[i].Init(this);
		}

		public void Destroy()
		{
			provider_?.Destroy();
		}

		public Voice Clone()
		{
			var v = new Voice();
			v.CopyFrom(this);
			return v;
		}

		private void CopyFrom(Voice v)
		{
			provider_ = v.provider_.Clone();

			foreach (var s in v.states_)
				states_.Add(s.Clone());
		}

		public IVoice Provider
		{
			get { return provider_; }
		}

		public void Update(float s)
		{
			CheckMute();

			if (!emergency_)
			{
				for (int i = 0; i < states_.Count; ++i)
				{
					if (states_[i].HasEmergency())
					{
						current_ = states_[i];
						current_.Start();
						emergency_ = true;
						return;
					}
				}
			}

			if (current_ != null)
				current_.Update(s);

			if (current_ == null || current_.Done)
			{
				emergency_ = false;

				IVoiceState next = null;
				int nextPrio = -1;

				for (int i = 0; i < states_.Count; ++i)
				{
					int prio = states_[i].CanRun();
					if (prio != VoiceState.CannotRun && prio > nextPrio)
					{
						next = states_[i];
						nextPrio = prio;
					}
				}

				if (next == null)
				{
					log_.Error("no state can run");
					current_ = null;
				}
				else
				{
					emergency_ = (nextPrio == VoiceState.Emergency);
					current_ = next;
					current_.Start();
				}
			}

			provider_.Update(s);
		}

		private void CheckMute()
		{
			if (person_.IsPlayer && Cue.Instance.Options.MutePlayer)
			{
				if (!muted_)
				{
					log_.Info("person is player, muting");
					provider_.Muted = true;
					muted_ = true;
				}
			}
			else
			{
				if (muted_)
				{
					log_.Info("person is not player, unmuting");
					provider_.Muted = true;
					muted_ = false;
				}
			}
		}

		public Person Person
		{
			get { return person_; }
		}

		public bool MouthEnabled
		{
			get { return provider_.MouthEnabled; }
			set { provider_.MouthEnabled = value; }
		}

		public float MaxIntensity
		{
			get { return maxIntensity_; }
			set { maxIntensity_ = value; }
		}

		public string Warning
		{
			get { return provider_.Warning; }
		}

		public string[] Debug()
		{
			debug_.Clear();

			provider_.Debug(debug_);
			debug_.Add("", "");

			debug_.Add("maxIntensity", $"{maxIntensity_:0.00}");
			debug_.Add("", "");

			if (current_ == null)
			{
				debug_.Add("state", "none");
			}
			else
			{
				debug_.Add("state", current_.Name);
				current_.Debug(debug_);
			}

			debug_.Add("", "");
			for (int i = 0; i < states_.Count; ++i)
				debug_.Add(states_[i].Name, states_[i].LastState);

			return debug_.MakeArray();
		}
	}
}
