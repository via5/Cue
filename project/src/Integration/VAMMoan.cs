using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue.VamMoan
{
	sealed class Voice : IVoice
	{
		private const string PluginName = "VAMMoanPlugin.VAMMoan";
		private const float DefaultBreathingMax = 0.2f;
		private const float DefaultBreathingIntensityCutoff = 0.6f;
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
			public Sys.Vam.ActionParameter[] intensities;
			public Sys.Vam.FloatParameter availableIntensities;
			public bool hasAvailableIntensities;
		}

		class RandomRange
		{
			public Pair<float, float> range = new Pair<float, float>(0, 0);
			public IRandom rng = new UniformRandom();

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

		class Pause
		{
			public float minExcitement = 0;
			public RandomRange timeRange = new RandomRange();
			public float chance = 0;
			public IRandom rng = new UniformRandom();

			public bool active = false;
			public float time = 0;
			public float elapsed = 0;
			public float lastRng = 0;

			public Pause Clone()
			{
				var p = new Pause();

				p.minExcitement = minExcitement;
				p.timeRange = timeRange.Clone();
				p.chance = chance;
				p.rng = rng.Clone();

				return p;
			}

			public string SettingsToString()
			{
				return
					$"minEx={minExcitement:0.00} timeRange={timeRange} " +
					$"chance={chance};rng={rng}";
			}

			public string LiveToString()
			{
				return
					$"active={active} time={time:0.00} elapsed={elapsed:0.00} " +
					$"lastrng={lastRng:0.00}";
			}
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
		private float breathingRange_ = DefaultBreathingMax;
		private float breathingIntensityCutoff_ = DefaultBreathingIntensityCutoff;
		private string orgasmAction_ = "Voice orgasm";

		private float intensity_ = 0;
		private float intensityTarget_ = 0;
		private float intensityWait_ = 0;
		private float intensityTime_ = 0;
		private float intensityElapsed_ = 0;
		private float lastIntensity_ = 0;

		private Pause pause_ = new Pause();

		private bool kissEnabled_ = false;
		private float kissVoiceChance_ = 0;

		private float oldVolume_ = 0;
		private bool muted_ = false;

		private RandomRange intensityWaitRange_ = new RandomRange();
		private RandomRange intensityTimeRange_ = new RandomRange();

		private IRandom intensityTargetRng_ = new UniformRandom();

		private Voice()
		{
		}

		public Voice(JSONClass o)
		{
			breathingRange_ = U.Clamp(
				J.OptFloat(o, "breathingRange", DefaultBreathingMax),
				0, 1);

			breathingIntensityCutoff_ = U.Clamp(
				J.OptFloat(o, "breathingIntensityCutoff", DefaultBreathingIntensityCutoff),
				0, 1);

			J.OptString(o, "orgasmAction", ref orgasmAction_);

			intensityWaitRange_ = ParseRange(o, "intensityWait");
			intensityTimeRange_ = ParseRange(o, "intensityTime");

			if (o.HasKey("intensityTarget"))
			{
				var ot = o["intensityTarget"].AsObject;
				if (!ot.HasKey("rng"))
					throw new LoadFailed("intensityTarget missing rng");

				intensityTargetRng_ = BasicRandom.FromJSON(ot["rng"].AsObject);
				if (intensityTargetRng_ == null)
					throw new LoadFailed("bad intensityTarget rng");
			}

			pause_ = ParsePause(o);

			if (o.HasKey("kiss"))
			{
				var ko = o["kiss"].AsObject;

				kissEnabled_ = J.ReqBool(ko, "enabled");
				kissVoiceChance_ = J.ReqFloat(ko, "voiceChance");
			}
		}

		private RandomRange ParseRange(JSONClass o, string name)
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

		private Pause ParsePause(JSONClass o)
		{
			var p = new Pause();

			if (o.HasKey("pause"))
			{
				var po = o["pause"].AsObject;

				p.minExcitement = J.ReqFloat(po, "minExcitement");
				p.timeRange = ParseRange(po, "time");

				var co = po["chance"].AsObject;

				p.chance = J.ReqFloat(co, "value");

				if (co.HasKey("rng"))
				{
					p.rng = BasicRandom.FromJSON(co["rng"]);
					if (p.rng == null)
						throw new LoadFailed("bad pause rng");
				}
			}

			return p;
		}

		public IVoice Clone()
		{
			var b = new Voice();
			b.CopyFrom(this);
			return b;
		}

		private void CopyFrom(Voice b)
		{
			breathingRange_ = b.breathingRange_;
			breathingIntensityCutoff_ = b.breathingIntensityCutoff_;
			orgasmAction_ = b.orgasmAction_;
			intensityWaitRange_ = b.intensityWaitRange_.Clone();
			intensityTimeRange_ = b.intensityTimeRange_.Clone();
			intensityTargetRng_ = b.intensityTargetRng_.Clone();
			pause_ = b.pause_.Clone();
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

				if (person_.IsPlayer && Cue.Instance.Options.MutePlayer)
				{
					if (!muted_)
					{
						log_.Info("person is player, muting");
						oldVolume_ = p_.volume.Value;
						p_.volume.Value = 0;
						muted_ = true;
					}
				}
				else
				{
					if (muted_)
					{
						log_.Info("person is not player, unmuting");
						p_.volume.Value = oldVolume_;
						muted_ = false;
					}
				}
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
			else if (pause_.active)
			{
				pause_.elapsed += s;

				if (pause_.elapsed >= pause_.time)
				{
					pause_.active = false;
					NextIntensity(1.0f);
				}
			}
			else if (Math.Abs(intensity_ - intensityTarget_) < 0.001f)
			{
				if (CheckPause())
					return;

				NextIntensity();
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
		}

		private void NextIntensity(float forceTargetNow = -1)
		{
			lastIntensity_ = intensity_;
			intensity_ = intensityTarget_;

			intensityWait_ = intensityWaitRange_.rng.RandomFloat(
				intensityWaitRange_.range.first,
				intensityWaitRange_.range.second,
				maxIntensity_);

			intensityElapsed_ = 0;

			if (forceTargetNow >= 0)
			{
				intensityTarget_ = forceTargetNow;
				intensity_ = forceTargetNow;
				intensityTime_ = 0;
				SetIntensity(true);
			}
			else
			{
				intensityTime_ = intensityTimeRange_.rng.RandomFloat(
					intensityTimeRange_.range.first,
					intensityTimeRange_.range.second,
					maxIntensity_);

				float min = 0;
				if (maxIntensity_ >= breathingIntensityCutoff_)
					min = breathingRange_ + 0.01f;

				intensityTarget_ = intensityTargetRng_.RandomFloat(
					min, maxIntensity_, maxIntensity_);
			}
		}

		private bool CheckPause()
		{
			if (maxIntensity_ < pause_.minExcitement)
				return false;

			pause_.lastRng = pause_.rng.RandomFloat(0, 1, maxIntensity_);
			if (pause_.lastRng >= pause_.chance)
				return false;

			pause_.active = true;
			pause_.elapsed = 0;
			pause_.time = pause_.timeRange.rng.RandomFloat(
				pause_.timeRange.range.first,
				pause_.timeRange.range.second,
				maxIntensity_);

			Fire(p_.disabled);

			return true;
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
			A("intensitiesCount", $"{intensitiesCount_}");
			A("breathingRange", $"{breathingRange_:0.00}");
			A("breathingCutoff", $"{breathingIntensityCutoff_:0.00}");
			A("orgasmAction", orgasmAction_);
			A("", "");
			A("maxIntensity", $"{maxIntensity_:0.00}");
			A("intensity", $"{intensity_:0.00}->{intensityTarget_:0.00} rng={intensityTargetRng_}");
			A("intensityWait", $"{intensityWait_:0.00}");
			A("intensityTime", $"{intensityElapsed_:0.00}/{intensityTime_:0.00}");
			A("pause settings", pause_.SettingsToString());
			A("pause", pause_.LiveToString());
			A("", "");
			A("intensityWait", $"{intensityWaitRange_}");
			A("intensityTime", $"{intensityTimeRange_}");
			A("lastIntensity", $"{lastIntensity_:0.00}");
			A("", "");
			A("lastAction", lastAction_);
			A("inOrgasm", $"{inOrgasm_}");


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

		public string Warning
		{
			get { return warning_; }
		}

		private void SetIntensity(bool force = false)
		{
			if (inOrgasm_ || pause_.active)
				return;

			if (intensitiesCount_ == 0)
				return;

			//var k = person_.AI.GetEvent<KissEvent>();
			//if (k != null && k.Active)
			//{
			//	Fire(p_.kissing, force);
			//	return;
			//}

			if (intensity_ >= 0 && intensity_ <= breathingRange_)
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
