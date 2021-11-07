using SimpleJSON;
using System;
using UnityEngine;

namespace Cue.Proc
{
	class MorphTarget : BasicTarget
	{
		public const int NoFlags = 0x00;
		public const int StartHigh = 0x01;

		public const int NoForceTarget = 0;
		public const int ForceToZero = 1;
		public const int ForceToRangePercent = 2;
		public const int ForceIgnore = 3;

		private int bodyPartType_;
		private BodyPart bp_ = null;
		private string morphId_;
		private float min_, max_, mid_;
		private Morph morph_ = null;
		private bool autoSet_ = true;

		private float r_ = 0;
		private float mag_ = 0;
		private IEasing easing_;
		private bool finished_ = false;
		private float last_;
		private bool closeToMid_ = false;
		private bool awayFromMid_ = false;
		private float timeActive_ = 0;
		private float intensity_ = 1;
		private bool limitHit_ = false;
		private float limitedValue_ = 0;
		private int flags_;

		public MorphTarget(
			int bodyPart, string morphId, float min, float max,
			ISync sync, IEasing easing = null, int flags = NoFlags)
				: base("", sync)
		{
			bodyPartType_ = bodyPart;
			morphId_ = morphId;
			min_ = min;
			max_ = max;
			easing_ = easing ?? new SinusoidalEasing();
			flags_ = flags;
		}

		public static MorphTarget Create(JSONClass o)
		{
			string id = o["morph"];

			try
			{
				var bodyPart = BP.FromString(o["bodyPart"]);
				if (bodyPart == BP.None)
					throw new LoadFailed($"bad body part '{o["bodyPart"]}'");

				float min;
				if (!float.TryParse(o["min"], out min))
					throw new LoadFailed("min is not a number");

				float max;
				if (!float.TryParse(o["max"], out max))
					throw new LoadFailed("max is not a number");

				var duration = Duration.FromJSON(o, "duration");
				var delay = Duration.FromJSON(o, "delay");

				var sync = new SlidingDurationSync(
					duration.Clone(), duration.Clone(),
					delay.Clone(), delay.Clone(),
					SlidingDurationSync.Loop);

				return new MorphTarget(bodyPart, id, min, max, sync);
			}
			catch (LoadFailed e)
			{
				throw new LoadFailed($"morph '{id}'/{e.Message}");
			}
		}

		public override bool Done
		{
			get { return finished_; }
		}

		public override ITarget Clone()
		{
			return new MorphTarget(
				bodyPartType_, morphId_, min_, max_,
				Sync.Clone(), easing_.Clone(), flags_);
		}

		public IEasing Easing
		{
			get { return easing_; }
			set { easing_ = value; }
		}

		public bool CloseToMid
		{
			get { return closeToMid_; }
		}

		public bool LimitHit
		{
			get { return limitHit_; }
		}

		public float Intensity
		{
			get { return intensity_; }
			set { intensity_ = value; }
		}

		public bool AutoSet
		{
			get { return autoSet_; }
			set { autoSet_ = value; }
		}

		protected override void DoStart(Person p, AnimationContext cx)
		{
			morph_ = new Morph(person_, morphId_, bodyPartType_);

			if (bodyPartType_ != -1)
				bp_ = person_.Body.Get(bodyPartType_);

			mid_ = Mid();
			last_ = mid_;
			Next(false, Bits.IsSet(flags_, StartHigh));
		}

		public override void RequestStop()
		{
			base.RequestStop();
			last_ = morph_.Value;
			r_ = mid_;
		}

		public override void Reset()
		{
			base.Reset();

			if (morph_ != null)
			{
				if (bp_ != null && !bp_.LockedFor(BodyPartLock.Morph, LockKey))
					morph_.Reset();
			}
		}

		public override void FixedUpdate(float s)
		{
			if (morph_ == null)
			{
				finished_ = true;
				return;
			}

			finished_ = false;
			timeActive_ += s;

			int r = Sync.FixedUpdate(s);

			switch (r)
			{
				case BasicSync.Working:
				{
					mag_ = Sync.Magnitude;
					break;
				}

				case BasicSync.DurationFinished:
				{
					mag_ = 0;
					Next(true);
					break;
				}

				case BasicSync.Looping:
				{
					mag_ = 0;
					Next(false);
					break;
				}

				case BasicSync.Delaying:
				case BasicSync.DelayFinished:
				{
					morph_.LimiterEnabled = true;

					break;
				}

				case BasicSync.SyncFinished:
				{
					finished_ = true;
					break;
				}
			}

			if (autoSet_)
				Set(s, null);
		}

		private float Mid()
		{
			if (morph_ == null)
				return 0;

			return morph_.DefaultValue;
		}

		public void Set(float s, float[] remaining)
		{
			closeToMid_ = false;
			if (morph_ == null)
				return;

			float v = Mathf.Lerp(last_, r_, easing_.Magnitude(mag_));
			SetValue(s, v, remaining);
		}

		private void SetValue(float s, float rawV, float[] remaining)
		{
			float v = rawV;

			limitHit_ = false;
			if (remaining != null && bodyPartType_ >= 0)
			{
				if (Math.Abs(rawV - mid_) > remaining[bodyPartType_])
				{
					limitedValue_ = Math.Sign(rawV) * remaining[bodyPartType_];
					v = mid_ + limitedValue_;
					limitHit_ = true;
				}
			}

			var d = v - mid_;

			if (bp_ != null && !bp_.LockedFor(BodyPartLock.Morph, LockKey))
				morph_.Value = v;


			d = Math.Abs(d);

			if (d < 0.01f)
				timeActive_ = 0;

			if (awayFromMid_ && d < 0.01f)
			{
				closeToMid_ = true;
				awayFromMid_ = false;
			}
			else if (d > 0.01f)
			{
				awayFromMid_ = true;
			}

			if (remaining != null && bodyPartType_ >= 0)
				remaining[bodyPartType_] -= d;
		}

		private void Next(bool resetBetween, bool forceHigh = false)
		{
			if (resetBetween)
				last_ = mid_;
			else
				last_ = morph_.Value;

			if (limitHit_ && timeActive_ >= 10)
			{
				//Cue.LogVerbose(
				//	$"{person_.ID} {morphId_}: active for {timeActive_:0.00}, " +
				//	$"too long, forcing to 0");

				// force a reset to allow other morphs to take over the prio
				r_ = mid_;
				timeActive_ = 0;
			}
			else
			{
				float range = Math.Abs(max_ - min_) * intensity_;
				float r;

				if (forceHigh)
					r = range;
				else
					r = U.RandomFloat(0, range);

				if (min_ < max_)
					r_ = min_ + r;
				else
					r_ = min_ - r;
			}
		}

		public override string ToString()
		{
			return $"morph {morphId_} ({BP.ToString(bodyPartType_)})";
		}

		public override string ToDetailedString()
		{
			return
				$"morph {morphId_} ({BP.ToString(bodyPartType_)})\n" +
				$"min={min_} max={max_} mid={mid_} r={r_} mag={mag_}\n" +
				$"finished={finished_} last={last_} timeactive={timeActive_}\n" +
				$"intensity={intensity_} limitHit={limitHit_} autoset={autoSet_} lv={limitedValue_}\n" +
				$"morph={morph_}";
		}
	}
}
