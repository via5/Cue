using System;
using SimpleJSON;

namespace Cue
{
	public class VoiceStateNormal : VoiceState
	{
		private const float DefaultBreathingIntensityCutoff = 0.6f;
		private const float DefaultBreathingMax = 0.2f;

		private float breathingRange_ = DefaultBreathingMax;
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

		public VoiceStateNormal(JSONClass vo)
		{
			Load(vo, false);
		}

		public override void Load(JSONClass vo, bool inherited)
		{
			if (vo.HasKey("normalState"))
			{
				var o = J.ReqObject(vo, "normalState");

				if (o.HasKey("breathingRange"))
				{
					breathingRange_ = U.Clamp(
						J.OptFloat(o, "breathingRange", DefaultBreathingMax),
						0, 1);
				}

				if (o.HasKey("breathingIntensityCutoff"))
				{
					breathingIntensityCutoff_ = U.Clamp(
						J.OptFloat(o, "breathingIntensityCutoff", DefaultBreathingIntensityCutoff),
						0, 1);
				}

				if (o.HasKey("intensityWait"))
					intensityWaitRange_ = RandomRange.Create(o, "intensityWait");
				else if (!inherited)
					throw new LoadFailed("missing intensityWait");

				if (o.HasKey("intensityTime"))
					intensityTimeRange_ = RandomRange.Create(o, "intensityTime");
				else if (!inherited)
					throw new LoadFailed("missing intensityTime");

				if (o.HasKey("intensityTarget"))
				{
					var ot = o["intensityTarget"].AsObject;
					if (!ot.HasKey("rng"))
						throw new LoadFailed("intensityTarget missing rng");

					intensityTargetRng_ = BasicRandom.FromJSON(ot["rng"].AsObject);
					if (intensityTargetRng_ == null)
						throw new LoadFailed("bad intensityTarget rng");
				}
				else if (!inherited)
				{
					throw new LoadFailed("missing intensityTarget");
				}
			}
			else if (!inherited)
			{
				throw new LoadFailed("missing normalState");
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
			breathingRange_ = o.breathingRange_;
			breathingIntensityCutoff_ = o.breathingIntensityCutoff_;
			intensityWaitRange_ = o.intensityWaitRange_.Clone();
			intensityTimeRange_ = o.intensityTimeRange_.Clone();
			intensityTargetRng_ = o.intensityTargetRng_.Clone();
		}

		protected override int DoCanRun()
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
			intensityWait_ = intensityWaitRange_.RandomFloat(v_.MaxIntensity);
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
				intensityTime_ = intensityTimeRange_.RandomFloat(
					v_.MaxIntensity);

				intensityTarget_ = intensityTargetRng_.RandomFloat(
					0, v_.MaxIntensity, v_.MaxIntensity);
			}
		}

		private void SetIntensity()
		{
			if (intensity_ <= breathingRange_)
			{
				v_.Provider.SetBreathing();
			}
			else
			{
				float range = 1 - breathingRange_;
				float v = intensity_ - breathingRange_;
				float p = (v / range);

				v_.Provider.SetMoaning(p);
			}
		}

		protected override void DoDebug(DebugLines debug)
		{
			debug.Add("intensity", $"{intensity_:0.00}->{intensityTarget_:0.00} rng={intensityTargetRng_}");
			debug.Add("intensityWait", $"{intensityWait_:0.00}");
			debug.Add("intensityTime", $"{intensityElapsed_:0.00}/{intensityTime_:0.00}");
			debug.Add("breathingRange", $"{breathingRange_:0.00}");
			debug.Add("breathingCutoff", $"{breathingIntensityCutoff_:0.00}");
			debug.Add("intensityWait", $"{intensityWaitRange_}");
			debug.Add("intensityTime", $"{intensityTimeRange_}");
			debug.Add("lastIntensity", $"{lastIntensity_:0.00}");
		}
	}
}
