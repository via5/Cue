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
		private const float ForceIntensityInterval = 1;

		struct Parameters
		{
			public Sys.Vam.BoolParameter enabled;
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
		private List<string[]> debug_ = null;
		private List<string> debugLines_ = null;

		private float maxIntensity_ = 0;
		private string lastAction_ = "";
		private int intensitiesCount_ = -1;
		private Parameters p_;
		private float voiceCheckElapsed_ = 0;
		private float forceIntensityElapsed_ = 0;
		private bool inOrgasm_ = false;
		private string warning_ = "";

		private string voice_ = "";
		private float pitch_ = -1;
		private float breathingMax_ = DefaultBreathingMax;
		private string orgasmAction_ = "Voice orgasm";

		private float intensity_ = 0;
		private float intensityTarget_ = 0;
		private float intensityWait_ = 0;

		private Pair<float, float> intensityWaitRange_ = new Pair<float, float>(0, 0);
		private IRandom intensityWaitRng_ = new UniformRandom();
		private IRandom intensityTargetRng_ = new UniformRandom();

		private Voice()
		{
		}

		public Voice(JSONClass o)
		{
			breathingMax_ = U.Clamp(
				J.OptFloat(o, "breathingMax", DefaultBreathingMax),
				0, 1);

			J.OptString(o, "orgasmAction", ref orgasmAction_);

			if (o.HasKey("intensityWait"))
			{
				var wt = o["intensityWait"].AsObject;

				if (!wt.HasKey("range"))
					throw new LoadFailed("intensityWait missing range");

				var a = wt["range"].AsArray;
				if (a.Count != 2)
					throw new LoadFailed("bad intensityWait range");

				intensityWaitRange_.first = a[0].AsFloat;
				intensityWaitRange_.second = a[1].AsFloat;

				if (wt.HasKey("rng"))
				{
					intensityWaitRng_ = BasicRandom.FromJSON(wt["rng"].AsObject);
					if (intensityWaitRng_ == null)
						throw new LoadFailed("bad intensityWait rng");
				}
			}

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
			orgasmAction_ = b.orgasmAction_;
			intensityWaitRange_ = b.intensityWaitRange_;
			intensityWaitRng_ = b.intensityWaitRng_.Clone();
			intensityTargetRng_ = b.intensityTargetRng_.Clone();
		}

		public void Init(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Integration, p, "vammoan");

			p_.enabled = BP("enabled");
			p_.autoJaw = BP("Enable auto-jaw animation");
			p_.voice = SCP("voice");
			p_.pitch = FP("Voice pitch");
			p_.breathing = AP("Voice breathing");
			p_.intensities = GetIntensities();
			p_.availableIntensities = FP("VAMM IntensitiesCount");

			if (orgasmAction_ != "")
				p_.orgasm = AP(orgasmAction_);

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
					Cue.LogError("!");
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

		public void Update(float s)
		{
			voiceCheckElapsed_ += s;
			if (voiceCheckElapsed_ >= VoiceCheckInterval)
			{
				voiceCheckElapsed_ = 0;
				CheckVoice();
			}

			UpdateIntensity(s);

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
				SetIntensity(true);
			}
		}

		private void UpdateIntensity(float s)
		{
			if (intensityWait_ > 0)
			{
				intensityWait_ -= s;
				if (intensityWait_ < 0)
					intensityWait_ = 0;
			}
			else if (Math.Abs(intensity_ - intensityTarget_) < 0.001f)
			{
				intensity_ = intensityTarget_;

				intensityWait_ = intensityWaitRng_.RandomFloat(
					intensityWaitRange_.first, intensityWaitRange_.second,
					maxIntensity_);

				intensityTarget_ = intensityTargetRng_.RandomFloat(
					0, maxIntensity_, maxIntensity_);
			}
			else if (intensity_ > intensityTarget_)
			{
				intensity_ -= 0.1f * s;
				if (intensity_ < intensityTarget_)
					intensity_ = intensityTarget_;
			}
			else
			{
				intensity_ += 0.1f * s;
				if (intensity_ > intensityTarget_)
					intensity_ = intensityTarget_;
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

		public string[] Debug()
		{
			if (debug_ == null)
				debug_ = new List<string[]>();

			if (debugLines_ == null)
				debugLines_ = new List<string>();

			int i = 0;

			Action<string, string> A = (a, b) =>
			{
				if (i >= debug_.Count)
					debug_.Add(new string[2]);

				debug_[i][0] = a;
				debug_[i][1] = b;

				++i;
			};

			A("provider", "vammoan");
			A("voice", voice_);
			A("intensitiesCount", $"{intensitiesCount_}");
			A("pitch", $"{pitch_:0.00}");
			A("breathingMax", $"{breathingMax_:0.00}");
			A("orgasmAction", orgasmAction_);
			A("", "");
			A("maxIntensity", $"{maxIntensity_:0.00}");
			A("intensity", $"{intensity_:0.00}");
			A("intensityTarget", $"{intensityTarget_:0.00}");
			A("intensityWait", $"{intensityWait_:0.00}");
			A("lastAction", lastAction_);
			A("inOrgasm", $"{inOrgasm_}");
			A("", "");
			A("intensityWaitrange", $"{intensityWaitRange_.first:0.00},{intensityWaitRange_.second:0.00}");
			A("intensityWaitRng", $"{intensityWaitRng_}");
			A("intensityTargetRng", $"{intensityTargetRng_}");


			MakeDebugLines();
			return debugLines_.ToArray();
		}

		private void MakeDebugLines()
		{
			int longest = 0;
			for (int i = 0; i < debug_.Count; ++i)
				longest = Math.Max(longest, debug_[i][0].Length);

			for (int i = 0; i < debug_.Count; ++i)
			{
				string s = debug_[i][0].PadRight(longest, ' ') + "  " + debug_[i][1];
				if (i >= debugLines_.Count)
					debugLines_.Add(s);
				else
					debugLines_[i] = s;
			}
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

				SetIntensity(true);
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
					warning_ = $"Cue requires VAMMoan 11 or above";
					p_.hasAvailableIntensities = false;
				}
			}
			else
			{
				warning_ = $"VAMMoan not found";
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
					SetPitch();
				}
			}
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
				return maxIntensity_;
			}

			set
			{
				maxIntensity_ = value;
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
				if (value != voice_ && value != "")
				{
					voice_ = value;

					if (p_.voice != null)
					{
						p_.voice.Value = value;
						CheckVoice(true);
					}
				}
			}
		}

		public string Warning
		{
			get { return warning_; }
		}

		private void SetPitch()
		{
			if (p_.pitch == null)
				return;

			float min = 0.8f;
			float max = 1.2f;
			float range = max - min;

			p_.pitch.Value = min + range * pitch_;
		}

		private void SetIntensity(bool force = false)
		{
			if (inOrgasm_)
				return;

			if (intensitiesCount_ == 0)
				return;

			if (intensity_ >= 0 && intensity_ <= breathingMax_)
			{
				Fire(p_.breathing, force);
			}
			else
			{
				float range = 1 - breathingMax_;
				float v = intensity_ - breathingMax_;
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
			return
				$"VAMMoan v={voice_} " +
				$"i={lastAction_} " +
				$"bm={breathingMax_}";
		}
	}
}
