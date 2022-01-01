using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue.VamMoan
{
	sealed class Voice : IVoice
	{
		private const string PluginName = "VAMMoanPlugin.VAMMoan";
		private const float DefaultBreathingMax = 0.2f;
		private const float VoiceCheckInterval = 5;

		struct Parameters
		{
			public Sys.Vam.BoolParameter autoJaw;
			public Sys.Vam.StringChooserParameter voice;
			public Sys.Vam.FloatParameter pitch;
			public Sys.Vam.ActionParameter breathing;
			public Sys.Vam.ActionParameter orgasm;
			public Sys.Vam.ActionParameter[] intensities;
			public Sys.Vam.FloatParameter availableIntensities;
			public bool hasAvailableIntensities;
		}

		private Person person_ = null;
		private Logger log_;
		private float intensity_ = 0;
		private string lastAction_ = "";
		private int intensitiesCount_ = -1;
		private Parameters p_;
		private float voiceCheckElapsed_ = 0;
		private bool inOrgasm_ = false;

		private string voice_ = "";
		private float pitch_ = -1;
		private float breathingMax_ = DefaultBreathingMax;
		private IEasing intensitiesEasing_ = new LinearEasing();
		private string orgasmAction_ = "Voice orgasm";


		private Voice()
		{
		}

		public Voice(JSONClass o)
		{
			breathingMax_ = U.Clamp(
				J.OptFloat(o, "breathingMax", DefaultBreathingMax),
				0, 1);

			if (o.HasKey("intensitiesEasing"))
			{
				var en = o["intensitiesEasing"].Value;
				if (en != "")
				{
					var e = EasingFactory.FromString(en);
					if (e == null)
						log_.Error($"bad intensitiesEasing '{en}'");
					else
						intensitiesEasing_ = e;
				}
			}

			J.OptString(o, "orgasmAction", ref orgasmAction_);
		}

		public IVoice Clone()
		{
			var b = new Voice();
			b.CopyFrom(this);
			return b;
		}

		private void CopyFrom(Voice b)
		{
			voice_ = b.voice_;
			pitch_ = b.pitch_;
			breathingMax_ = b.breathingMax_;
			intensitiesEasing_ = b.intensitiesEasing_.Clone();
			orgasmAction_ = b.orgasmAction_;
		}

		public void Init(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Integration, p, "vammoan");

			Cue.Assert(person_ != null);
			p_.autoJaw = BP("Enable auto-jaw animation");
			p_.voice = SCP("voice");
			p_.pitch = FP("Voice pitch");
			p_.breathing = AP("Voice breathing");
			p_.intensities = GetIntensities();
			p_.availableIntensities = FP("VAMM IntensitiesCount");

			if (orgasmAction_ != "")
				p_.orgasm = AP(orgasmAction_);

			CheckVersion();
			CheckVoice();
		}

		public void Update(float s)
		{
			voiceCheckElapsed_ += s;

			if (voiceCheckElapsed_ >= VoiceCheckInterval)
			{
				voiceCheckElapsed_ = 0;
				CheckVoice();
			}
		}

		public void StartOrgasm()
		{
			if (p_.orgasm != null)
			{
				inOrgasm_ = true;
				Fire(p_.orgasm);
			}
		}

		public void StopOrgasm()
		{
			inOrgasm_ = false;
		}

		private void CheckVersion()
		{
			if (p_.voice.Check() && !p_.availableIntensities.Check())
			{
				log_.Error($"Cue requires VAMMoan 11 or above");
				p_.hasAvailableIntensities = false;
			}
			else
			{
				p_.hasAvailableIntensities = true;
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

		private void CheckVoice()
		{
			var v = p_.voice.Value;

			if (v != voice_)
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

				SetIntensity(true);
				voice_ = v;
			}
		}

		public void Destroy()
		{
			// no-op
		}

		public float Pitch
		{
			get
			{
				return pitch_;
			}

			set
			{
				if (pitch_ != value)
				{
					pitch_ = value;

					if (p_.pitch != null)
						p_.pitch.Value = value;
				}
			}
		}

		public Pair<float, float> PitchRange
		{
			get { return new Pair<float, float>(0.8f, 1.2f); }
		}

		public bool MouthEnabled
		{
			get { return p_.autoJaw.Value; }
			set { p_.autoJaw.Value = value; }
		}

		public float Intensity
		{
			get
			{
				return intensity_;
			}

			set
			{
				intensity_ = value;
				SetIntensity();
			}
		}

		public string[] AvailableVoices
		{
			get { return p_.voice.Choices; }
		}

		public string Name
		{
			get
			{
				return voice_;
			}

			set
			{
				if (value != voice_)
				{
					voice_ = value;

					if (p_.voice != null)
					{
						p_.voice.Value = value;
						CheckVoice();
					}
				}
			}
		}

		private void SetIntensity(bool force = false)
		{
			if (inOrgasm_)
				return;

			if (intensity_ >= 0 && intensity_ <= breathingMax_)
			{
				Fire(p_.breathing, force);
			}
			else
			{
				float range = 1 - breathingMax_;
				float v = intensity_ - breathingMax_;
				float p = intensitiesEasing_.Magnitude(v / range);

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
			return
				$"VAMMoan v={voice_} " +
				$"i={lastAction_} " +
				$"bm={breathingMax_}";
		}
	}
}
