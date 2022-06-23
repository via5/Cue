using SimpleJSON;
using System.Collections.Generic;

namespace Cue
{
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
