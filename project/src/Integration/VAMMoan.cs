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
		private const float DefaultBreathingMax = 0.2f;

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
			public Sys.Vam.ActionParameter[] intensities;
			public Sys.Vam.FloatParameter availableIntensities;
			public bool hasAvailableIntensities;
		}


		private Person person_ = null;
		private Logger log_;

		private string lastAction_ = "";
		private int intensitiesCount_ = -1;
		private Parameters p_;
		private float voiceCheckElapsed_ = 0;
		private float forceIntensityElapsed_ = 0;
		private string warning_ = "";
		private float oldVolume_ = 0;
		private float intensity_ = 0;
		private float breathingRange_ = DefaultBreathingMax;

		private string voice_ = "";


		private Voice()
		{
		}

		public Voice(JSONClass o)
		{
			breathingRange_ = U.Clamp(
				J.OptFloat(o, "breathingRange", DefaultBreathingMax),
				0, 1);
		}

		public IVoice Clone()
		{
			var b = new Voice();
			b.CopyFrom(this);
			return b;
		}

		private void CopyFrom(Voice v)
		{
			breathingRange_ = v.breathingRange_;
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

			CheckVoice();

			p_.enabled.Value = true;
			p_.autoJaw.Value = true;
			SetOptimalJaw();

			MacGruber.Voice.Disable(p);
		}

		private void SetOptimalJaw()
		{
			var atom = CueMain.Instance.MVRPluginManager?.containingAtom;
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
				UpdateIntensity(true);
			}
		}

		public void StartOrgasm()
		{
			Fire(p_.orgasm);
		}

		public void StopOrgasm()
		{
			// no-op
		}

		public void Debug(DebugLines debug)
		{
			debug.Add("provider", "vammoan");
			debug.Add("intensitiesCount", $"{intensitiesCount_}");
			debug.Add("breathingRange", $"{breathingRange_:0.00}");
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

				UpdateIntensity(true);
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

		public string Warning
		{
			get { return warning_; }
		}

		public void SetIntensity(float v)
		{
			intensity_ = v;
			UpdateIntensity();
		}

		private void UpdateIntensity(bool force = false)
		{
			if (intensitiesCount_ == 0)
				return;

			//var k = person_.AI.GetEvent<KissEvent>();
			//if (k != null && k.Active)
			//{
			//	Fire(p_.kissing, force);
			//	return;
			//}

			if (intensity_ <= 0)
			{
				Fire(p_.disabled, force);
			}
			else if (intensity_ <= breathingRange_)
			{
				Fire(p_.breathing, force);
			}
			else
			{
				float range = 1 - breathingRange_;
				float v = intensity_ - breathingRange_;
				float p = (v / range);

				int index = (int)(p * intensitiesCount_);
				index = U.Clamp(index, 0, intensitiesCount_ - 1);

				Fire(p_.intensities[index], force);
			}
		}

		private void Fire(Sys.Vam.ActionParameter a, bool force = false)
		{
			var n = a.ParameterName;

			if (lastAction_ != n || force)
			{
				lastAction_ = n;
				log_.Info($"setting to '{n}'");
				a.Fire();
			}
		}

		public override string ToString()
		{
			return $"VAMMoan v={voice_} i={lastAction_}";
		}
	}
}
