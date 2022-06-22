using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	public class DebugLines
	{
		private List<string[]> debug_ = null;
		private List<string> debugLines_ = null;
		private int i_ = 0;

		public void Clear()
		{
			i_ = 0;
		}

		public void Add(string a, string b)
		{
			if (debug_ == null)
				debug_ = new List<string[]>();

			if (debugLines_ == null)
				debugLines_ = new List<string>();

			if (i_ >= debug_.Count)
				debug_.Add(new string[2]);

			debug_[i_][0] = a;
			debug_[i_][1] = b;

			++i_;
		}

		public string[] MakeArray()
		{
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
	}


	public class Voice
	{
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


		private const float DefaultBreathingIntensityCutoff = 0.6f;

		private Person person_ = null;
		private IVoice provider_ = null;
		private Logger log_;

		private float maxIntensity_ = 0;
		private bool inOrgasm_ = false;

		private float breathingIntensityCutoff_ = DefaultBreathingIntensityCutoff;

		private float intensity_ = 0;
		private float intensityTarget_ = 0;
		private float intensityWait_ = 0;
		private float intensityTime_ = 0;
		private float intensityElapsed_ = 0;
		private float lastIntensity_ = 0;

		private Pause pause_ = new Pause();

		private bool kissEnabled_ = false;
		private float kissVoiceChance_ = 0;

		private bool muted_ = false;

		private RandomRange intensityWaitRange_ = new RandomRange();
		private RandomRange intensityTimeRange_ = new RandomRange();

		private IRandom intensityTargetRng_ = new UniformRandom();

		private DebugLines debug_ = new DebugLines();


		public Voice()
		{
			log_ = new Logger(Logger.Object, "voice");
		}

		public Voice(JSONClass o)
		{
			provider_ = CreateProvider(o["provider"].AsObject);

			breathingIntensityCutoff_ = U.Clamp(
				J.OptFloat(o, "breathingIntensityCutoff", DefaultBreathingIntensityCutoff),
				0, 1);

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

		private IVoice CreateProvider(JSONClass o)
		{
			var provider = o["name"].Value;
			var options = o["options"].AsObject;

			var p = Integration.CreateVoice(o["name"].Value, o["options"].AsObject);
			if (p == null)
				throw new LoadFailed($"unknown voice provider '{provider}'");

			return p;
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

		public void Init(Person p)
		{
			person_ = p;
			log_.Set(person_, "voice");
			provider_.Init(person_);
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
			breathingIntensityCutoff_ = v.breathingIntensityCutoff_;
			intensityWaitRange_ = v.intensityWaitRange_.Clone();
			intensityTimeRange_ = v.intensityTimeRange_.Clone();
			intensityTargetRng_ = v.intensityTargetRng_.Clone();
			pause_ = v.pause_.Clone();
		}

		public IVoice Provider
		{
			get
			{
				return provider_;
			}
		}

		public void Update(float s)
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

			UpdateIntensity(s);
			SetIntensity();

			provider_.Update(s);
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
				SetIntensity();
			}
			else
			{
				intensityTime_ = intensityTimeRange_.rng.RandomFloat(
					intensityTimeRange_.range.first,
					intensityTimeRange_.range.second,
					maxIntensity_);

				intensityTarget_ = intensityTargetRng_.RandomFloat(
					0, maxIntensity_, maxIntensity_);

				// 0 is disabled
				if (intensityTarget_ <= 0)
					intensityTarget_ = 0.01f;
			}
		}

		private void SetIntensity()
		{
			if (inOrgasm_ || pause_.active)
				return;

			provider_.SetIntensity(intensity_);
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

			provider_.SetIntensity(0);

			return true;
		}


		public bool MouthEnabled
		{
			get { return provider_.MouthEnabled; }
			set { provider_.MouthEnabled = value; }
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
			}
		}

		public string Warning
		{
			get { return provider_.Warning; }
		}

		public void StartOrgasm()
		{
			inOrgasm_ = true;
			provider_.StartOrgasm();
		}

		public void StopOrgasm()
		{
			inOrgasm_ = false;
			provider_.StopOrgasm();
		}

		public string[] Debug()
		{
			debug_.Clear();

			provider_.Debug(debug_);
			debug_.Add("", "");
			debug_.Add("maxIntensity", $"{maxIntensity_:0.00}");
			debug_.Add("intensity", $"{intensity_:0.00}->{intensityTarget_:0.00} rng={intensityTargetRng_}");
			debug_.Add("intensityWait", $"{intensityWait_:0.00}");
			debug_.Add("intensityTime", $"{intensityElapsed_:0.00}/{intensityTime_:0.00}");
			debug_.Add("pause settings", pause_.SettingsToString());
			debug_.Add("pause", pause_.LiveToString());
			debug_.Add("breathingCutoff", $"{breathingIntensityCutoff_:0.00}");
			debug_.Add("", "");
			debug_.Add("intensityWait", $"{intensityWaitRange_}");
			debug_.Add("intensityTime", $"{intensityTimeRange_}");
			debug_.Add("lastIntensity", $"{lastIntensity_:0.00}");
			debug_.Add("inOrgasm", $"{inOrgasm_}");

			return debug_.MakeArray();
		}
	}
}
