using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue.VamMoan
{
	sealed class Voice : IVoice
	{
		private const string PluginName = "VAMMoanPlugin.VAMMoan";
		private const float VoiceCheckInterval = 2;
		private const float ForceIntensityInterval = 1;

		struct Parameters
		{
			public Sys.Vam.BoolParameter enabled;
			public Sys.Vam.BoolParameter autoJaw;
			public Sys.Vam.StringChooserParameter voice;
			public Sys.Vam.FloatParameter volume;
			public Sys.Vam.ActionParameter disabled;
			public Sys.Vam.ActionParameter breathing;
			public Sys.Vam.ActionParameter orgasm;
			public Sys.Vam.ActionParameter kissing;
			public Sys.Vam.BoolParameter bjEnabled;         // >= v20
			public Sys.Vam.BoolParameter breathingEnabled;
			public Sys.Vam.ActionParameter[] bjIntensities; // <  v20
			public Sys.Vam.ActionParameter[] intensities;
			public Sys.Vam.FloatParameter availableIntensities;
			public bool hasAvailableIntensities;
		}


		private Person person_ = null;
		private Logger log_;

		private string lastAction_ = "";
		private int intensitiesCount_ = 0;
		private Parameters p_;
		private float voiceCheckElapsed_ = 0;
		private float forceIntensityElapsed_ = 0;
		private string warning_ = "";
		private float oldVolume_ = 0;
		private Sys.Vam.ActionParameter currentAction_ = null;

		private string voice_ = "";


		private Voice()
		{
		}

		public Voice(JSONClass o)
		{
			Load(o, false);
		}

		public void Load(JSONClass o, bool inherited)
		{
		}

		public IVoice Clone()
		{
			var b = new Voice();
			b.CopyFrom(this);
			return b;
		}

		private void CopyFrom(Voice v)
		{
		}

		public void Init(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Integration, p, "vammoan");

			p_.enabled = BP("enabled");
			p_.autoJaw = BP("Enable auto-jaw animation");
			p_.voice = SCP("voice");
			p_.volume = FP("Voice volume");
			p_.disabled = AP("Voice disabled");
			p_.breathing = AP("Voice breathing");
			p_.kissing = AP("Voice kissing");
			p_.intensities = GetIntensities();
			p_.availableIntensities = FP("VAMM IntensitiesCount");
			p_.orgasm = AP("Voice orgasm");
			p_.bjEnabled = BP("Enable blowjob sounds");
			p_.breathingEnabled = BP("Enable breathing animation");

			p_.bjIntensities = new Sys.Vam.ActionParameter[]
			{
				AP("Voice blowjob"),
				AP("Voice blowjob intense")
			};

			oldVolume_ = p_.volume.Value;

			CheckVoice();

			p_.enabled.Value = true;
			p_.autoJaw.Value = true;
			SetOptimalJaw();

			MacGruber.Voice.Disable(p);
		}

		private void SetOptimalJaw()
		{
			var atom = (Cue.Instance.Sys.DefaultAtom as Sys.Vam.VamAtom)?.Atom;
			var atomUI = atom?.UITransform?.GetComponentInChildren<AtomUI>();
			var bs = atomUI?.GetComponentsInChildren<UIDynamicButton>(true);

			if (bs == null)
				return;

			for (int i=0; i<bs.Length; ++i)
			{
				if (bs[i]?.buttonText?.text == "Set optimal auto-jaw animation parameters")
				{
					bs[i]?.button?.onClick?.Invoke();
					break;
				}
			}
		}

		public static void Disable(Person p)
		{
			var e = Sys.Vam.Parameters.GetBool(p, PluginName, "enabled");

			if (e != null)
				e.val = false;
		}

		public string Name
		{
			get { return "vammoan"; }
		}

		public bool Muted
		{
			set
			{
				if (value)
				{
					oldVolume_ = p_.volume.Value;
					p_.volume.Value = 0;
				}
				else
				{
					p_.volume.Value = oldVolume_;
				}
			}
		}

		public void Update(float s)
		{
			voiceCheckElapsed_ += s;
			if (voiceCheckElapsed_ >= VoiceCheckInterval)
			{
				voiceCheckElapsed_ = 0;
				CheckVoice();
			}

			// the orgasm action is special because it will prevent another
			// intensity from being set while running, and so the time after
			// which the intensity can be changed depends on the audio length
			//
			// so just fire the action once in a while to make sure it's the
			// active one

			forceIntensityElapsed_ += s;
			if (forceIntensityElapsed_ >= ForceIntensityInterval)
			{
				forceIntensityElapsed_ = 0;
				Fire(true);
			}
		}

		public void Debug(DebugLines debug)
		{
			debug.Add("provider", "vammoan");
			debug.Add("intensitiesCount", $"{intensitiesCount_}");
			debug.Add("lastAction", lastAction_);
		}

		private void CheckVoice(bool force = false)
		{
			CheckVersion();

			var v = p_.voice.Value;

			if (force || v != voice_)
			{
				if (p_.hasAvailableIntensities)
				{
					float c = p_.availableIntensities.Value;
					intensitiesCount_ = U.Clamp((int)c, 0, p_.intensities.Length);
				}
				else
				{
					intensitiesCount_ = p_.intensities.Length;
				}

				Fire(true);
				SetOptimalJaw();
				voice_ = v;
			}
		}

		private void CheckVersion()
		{
			warning_ = "";

			if (p_.voice.Check())
			{
				if (p_.availableIntensities.Check())
				{
					p_.hasAvailableIntensities = true;
				}
				else
				{
					warning_ = $"VAMMoan 11 or above required";
					p_.hasAvailableIntensities = false;
				}
			}
			else
			{
				warning_ = $"VAMMoan missing";
			}
		}

		private Sys.Vam.ActionParameter[] GetIntensities()
		{
			var actions = new List<Sys.Vam.ActionParameter>();

			for (int i = 0; i < 5; ++i)
				actions.Add(AP($"Voice intensity {i}"));

			return actions.ToArray();
		}

		private Sys.Vam.BoolParameter BP(string name)
		{
			return new Sys.Vam.BoolParameter(person_, PluginName, name);
		}

		private Sys.Vam.StringChooserParameter SCP(string name)
		{
			return new Sys.Vam.StringChooserParameter(person_, PluginName, name);
		}

		private Sys.Vam.ActionParameter AP(string name)
		{
			return new Sys.Vam.ActionParameter(person_, PluginName, name);
		}

		private Sys.Vam.FloatParameter FP(string name)
		{
			return new Sys.Vam.FloatParameter(person_, PluginName, name);
		}

		public void Destroy()
		{
			// no-op
		}

		public bool MouthEnabled
		{
			get { return p_.autoJaw.Value; }
			set { p_.autoJaw.Value = value; }
		}

		public bool ChestEnabled
		{
			get { return p_.breathingEnabled.Value; }
			set { p_.breathingEnabled.Value = value; }
		}

		public string Warning
		{
			get { return warning_; }
		}

		public void SetMoaning(float v)
		{
			if (p_.intensities.Length == 0)
				return;

			int index = (int)(v * p_.intensities.Length);
			index = U.Clamp(index, 0, p_.intensities.Length - 1);

			SetAction(p_.intensities[index], false);
			Fire();
		}

		public void SetBreathing()
		{
			SetAction(p_.breathing, false);
			Fire();
		}

		public void SetSilent()
		{
			SetAction(p_.disabled, false);
			Fire();
		}

		public void SetOrgasm()
		{
			SetAction(p_.orgasm, false);
			Fire();
		}

		public void SetKissing()
		{
			SetAction(p_.kissing, false);
			Fire();
		}

		public void SetBJ(float v)
		{
			if (p_.bjEnabled.Check())
			{
				SetAction(p_.breathing, true);
			}
			else
			{
				if (p_.bjIntensities.Length == 0)
					return;

				int index = (int)(v * p_.bjIntensities.Length);
				index = U.Clamp(index, 0, p_.bjIntensities.Length - 1);

				SetAction(p_.bjIntensities[index], false);
			}

			Fire();
		}

		private void SetAction(Sys.Vam.ActionParameter a, bool bj)
		{
			currentAction_ = a;
			p_.bjEnabled.Value = bj;
		}

		private void Fire(bool force = false)
		{
			if (currentAction_ == null)
				return;

			var n = currentAction_.ParameterName;

			if (lastAction_ != n || force)
			{
				lastAction_ = n;
				log_.Info($"setting to '{n}'");
				currentAction_.Fire();
			}
		}

		public override string ToString()
		{
			return $"VAMMoan v={voice_} i={lastAction_}";
		}
	}
}
