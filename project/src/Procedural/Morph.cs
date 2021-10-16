using SimpleJSON;
using System;
using UnityEngine;

namespace Cue.Proc
{
	class MorphTarget : BasicTarget
	{
		public const int NoForceTarget = 0;
		public const int ForceToZero = 1;
		public const int ForceToRangePercent = 2;
		public const int ForceIgnore = 3;

		private int bodyPart_;
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

		private int forceType_ = NoForceTarget;
		private float forceRangePercent_ = 0;
		private float forceTarget_ = 0;
		private ISync oldSync_ = null;


		public MorphTarget(
			Person p, int bodyPart, string id, float start, float end,
			float minTime, float maxTime, float delayOff, float delayOn,
			bool resetBetween = false)
				: this(bodyPart, id, start, end, new SlidingDurationSync(
					new SlidingDuration(minTime, maxTime, 0, 0, 0, new LinearEasing()),
					new SlidingDuration(minTime, maxTime, 0, 0, 0, new LinearEasing()),
					new Duration(0, delayOn),
					new Duration(0, delayOff),
					(resetBetween ?
						SlidingDurationSync.ResetBetween :
						SlidingDurationSync.Loop)))
		{
			Start(p);
		}

		public MorphTarget(
			int bodyPart, string morphId, float min, float max,
			ISync sync, IEasing easing = null)
				: base("", sync)
		{
			bodyPart_ = bodyPart;
			morphId_ = morphId;
			min_ = min;
			max_ = max;

			easing_ = easing ?? new SinusoidalEasing();
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
					new SlidingDuration(
						duration.Minimum, duration.Maximum,
						duration.NextMin, duration.NextMax,
						0, new LinearEasing()),
					new SlidingDuration(
						duration.Minimum, duration.Maximum,
						duration.NextMin, duration.NextMax,
						0, new LinearEasing()),
					new Duration(delay),
					new Duration(delay),
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
				bodyPart_, morphId_, min_, max_, Sync.Clone());
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

		protected override void DoStart(Person p)
		{
			morph_ = new Morph(person_.Atom.GetMorph(morphId_));

			mid_ = Mid();
			last_ = mid_;
			r_ = mid_;
		}

		public override void Reset()
		{
			base.Reset();

			if (morph_ != null)
			{
				if (bodyPart_ == BP.None ||
					!person_.Body.Get(bodyPart_).LockedFor(BodyPartLock.Morph))
				{
					morph_.Reset();
				}
			}
		}

		public void ForceChange()
		{
			Sync.Reset();
			finished_ = false;
			timeActive_ = 0;
			Next(false);
		}

		public void Force(int type, float rangePercent, ISync newSync = null)
		{
			bool wasForced = (forceType_ != NoForceTarget);

			forceType_ = type;
			forceRangePercent_ = rangePercent;

			if (forceType_ == ForceToZero)
			{
				forceTarget_ = morph_.DefaultValue;
			}
			else if (forceType_ == ForceToRangePercent)
			{
				var range = Math.Abs(max_ - min_);

				if (min_ < max_)
					forceTarget_ = min_ + range * forceRangePercent_;
				else
					forceTarget_ = min_ - range * forceRangePercent_;
			}

			if (forceType_ != NoForceTarget)
			{
				last_ = morph_.Value;
				r_ = forceTarget_;
				morph_.LimiterEnabled = false;

				if (newSync != null)
				{
					oldSync_ = Sync;
					Sync = newSync;
					Sync.Reset();
				}
			}
			else if (wasForced)
			{
				StopForced();
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
					if (forceType_ != NoForceTarget)
						forceType_ = ForceIgnore;

					finished_ = true;
					break;
				}
			}

			if (autoSet_)
				Set(s, null);
		}

		private void StopForced()
		{
			if (oldSync_ != null)
			{
				Sync = oldSync_;
				oldSync_ = null;
			}

			morph_.LimiterEnabled = true;
			mag_ = 0;
			Next(false);
			Sync.Reset();
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
			if (remaining != null && bodyPart_ >= 0)
			{
				if (Math.Abs(rawV - mid_) > remaining[bodyPart_])
				{
					limitedValue_ = Math.Sign(rawV) * remaining[bodyPart_];
					v = mid_ + limitedValue_;
					limitHit_ = true;
				}
			}

			var d = v - mid_;

			if (bodyPart_ == BP.None ||
				!person_.Body.Get(bodyPart_).LockedFor(BodyPartLock.Morph))
			{
				if (forceType_ != ForceIgnore)
					morph_.Value = v;
			}


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

			if (forceType_ != ForceIgnore)
			{
				if (remaining != null && bodyPart_ >= 0)
					remaining[bodyPart_] -= d;
			}
		}

		private void Next(bool resetBetween)
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
				var r = U.RandomFloat(0, range);

				if (min_ < max_)
					r_ = min_ + r;
				else
					r_ = min_ - r;
			}
		}

		public override string ToString()
		{
			return $"morph {morphId_} ({BP.ToString(bodyPart_)}) {forceTarget_}";
		}

		public override string ToDetailedString()
		{
			return
				$"morph {morphId_} ({BP.ToString(bodyPart_)})\n" +
				$"min={min_} max={max_} mid={mid_} r={r_} mag={mag_}\n" +
				$"finished={finished_} last={last_} timeactive={timeActive_}\n" +
				$"intensity={intensity_} limitHit={limitHit_} autoset={autoSet_} lv={limitedValue_}\n" +
				$"morph={morph_}\n" +
				$"force={forceType_} rp={forceRangePercent_} t={forceTarget_}";
		}
	}
}
