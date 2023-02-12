using SimpleJSON;
using System.Collections.Generic;

namespace Cue
{
	public class NoVoice : IVoice
	{
		public NoVoice(JSONClass options)
		{
		}

		public string Name
		{
			get { return "none"; }
		}

		public bool Muted
		{
			set { }
		}

		public bool MouthEnabled
		{
			get { return false; }
			set { }
		}

		public bool ChestEnabled
		{
			get { return false; }
			set { }
		}

		public string Warning
		{
			get { return null; }
		}

		public IVoice Clone()
		{
			return new NoVoice(null);
		}

		public void Debug(DebugLines debug)
		{
			// no-op
		}

		public void Destroy()
		{
			// no-op
		}

		public void Init(Person p)
		{
			// no-op
		}

		public void Load(JSONClass o, bool inherited)
		{
			// no-op
		}

		public void SetBJ(float v)
		{
			// no-op
		}

		public void SetBreathing()
		{
			// no-op
		}

		public void SetKissing()
		{
			// no-op
		}

		public void SetMoaning(float v)
		{
			// no-op
		}

		public void SetOrgasm()
		{
			// no-op
		}

		public void SetSilent()
		{
			// no-op
		}

		public void Update(float s)
		{
			// no-op
		}
	}


	public class Voice
	{
		private const float NothingCheckInterval = 1;

		private Person person_ = null;
		private IVoice provider_ = null;
		private Logger log_;
		private List<IVoiceState> states_ = new List<IVoiceState>();
		private IVoiceState current_ = null;
		private int currentPrio_ = -1;

		private float maxIntensity_ = 0;
		private bool muted_ = false;
		private string mutedWhy_ = "";

		private bool nothingCanRun_ = false;
		private float nothingElapsed_ = 0;

		private DebugLines debug_ = new DebugLines();


		public Voice()
		{
			log_ = new Logger(Logger.Object, "voice");
		}

		public Voice(JSONClass o)
		{
			Load(o, false);
		}

		public void Load(JSONClass o, bool inherited)
		{
			if (o == null)
				throw new LoadFailed("no object");

			if (o.HasKey("provider"))
			{
				var po = o["provider"].AsObject;

				if (provider_ == null || provider_.Name != J.ReqString(po, "name"))
					provider_ = CreateProvider(po);
				else
					provider_.Load(J.OptObject(po, "options"), inherited);
			}
			else
			{
				if (!inherited)
					throw new LoadFailed("voice missing provider");
			}

			if (states_.Count == 0)
			{
				Cue.Assert(!inherited);
				states_.AddRange(BasicVoiceState.CreateAll(o));
			}
			else
			{
				Cue.Assert(inherited);

				for (int i = 0; i < states_.Count; ++i)
					states_[i].Load(o, true);
			}
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

			CheckMute(true);
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
			if (nothingCanRun_)
			{
				nothingElapsed_ += s;
				if (nothingElapsed_ < NothingCheckInterval)
					return;

				nothingElapsed_ = 0;
				nothingCanRun_ = false;
			}

			MaxIntensity = person_.Mood.MovementEnergy;

			for (int i = 0; i < states_.Count; ++i)
				states_[i].EarlyUpdate(s);

			if (current_ != null)
				CheckForHigherPrio();

			if (current_ != null)
				current_.Update(s);

			if (current_ == null || current_.Done)
			{
				Next();

				if (current_ == null)
				{
					nothingCanRun_ = true;
					nothingElapsed_ = 0;
				}
			}

			provider_.Update(s);
			CheckMute();
		}

		private void CheckForHigherPrio()
		{
			for (int i = 0; i < states_.Count; ++i)
			{
				if (states_[i] == current_)
					continue;

				int prio = states_[i].CanRun();

				if (prio > currentPrio_)
				{
					SetCurrent(states_[i], prio);
					break;
				}
			}
		}

		private void Next()
		{
			IVoiceState next = null;
			int nextPrio = -1;

			for (int i = 0; i < states_.Count; ++i)
			{
				int prio = states_[i].CanRun();
				if (prio != BasicVoiceState.CannotRun && prio > nextPrio)
				{
					next = states_[i];
					nextPrio = prio;
				}
			}

			if (next == null)
			{
				log_.Verbose("no state can run");
				SetCurrent(null, -1);
			}
			else
			{
				SetCurrent(next, nextPrio);
			}
		}

		private void SetCurrent(IVoiceState s, int prio)
		{
			current_ = s;
			currentPrio_ = prio;

			if (current_ != null)
				current_.Start();
		}

		private void CheckMute(bool force=false)
		{
			if (ShouldMute())
			{
				if (!muted_ || force)
				{
					if (!force)
						log_.Info($"muting: {mutedWhy_}");

					provider_.Muted = true;
					muted_ = true;
				}
			}
			else
			{
				if (muted_ || force)
				{
					if (!force)
						log_.Info("unmuting");

					provider_.Muted = false;
					muted_ = false;
				}
			}
		}

		private bool ShouldMute()
		{
			if (person_.IsPlayer && Cue.Instance.Options.MutePlayer)
			{
				mutedWhy_ = "person is player";
				return true;
			}

			return false;
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

		public bool ChestEnabled
		{
			get { return provider_.ChestEnabled; }
			set { provider_.ChestEnabled = value; }
		}

		public float MaxIntensity
		{
			get { return maxIntensity_; }
			private set { maxIntensity_ = value; }
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
			debug_.Add("muted", $"{(muted_ ? "yes: " + mutedWhy_ : "no")}");
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

		public override string ToString()
		{
			return provider_.ToString();
		}
	}
}
