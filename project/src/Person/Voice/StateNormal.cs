using System;
using SimpleJSON;

namespace Cue
{
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
}
