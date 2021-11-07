using SimpleJSON;
using System;

namespace Cue
{
	class Duration
	{
		struct Range
		{
			public float min, max;

			public Range(float min, float max)
			{
				this.min = min;
				this.max = max;
			}
		}

		private Range fullRange_;
		private Range currentRange_;
		private Range nextTimeRange_;
		private float windowSize_;
		private IEasing windowEasing_;
		private float magnitude_ = 0;

		private float current_ = 0;
		private float elapsed_ = 0;

		private float nextTime_ = 0;
		private float nextElapsed_ = 0;

		private bool finished_ = false;

		public Duration()
			: this(0, 0, 0, 0, 0, null)
		{
		}

		public Duration(float min, float max)
			: this(min, max, 0, 0, 0, null)
		{
		}

		public Duration(float min, float max, float windowSize, IEasing windowEasing=null)
			: this(min, max, 0, 0, windowSize, windowEasing)
		{
		}

		public Duration(
			float min, float max, float nextMin, float nextMax,
			float windowSize, IEasing windowEasing)
		{
			fullRange_ = new Range(min, max);
			currentRange_ = fullRange_;
			nextTimeRange_ = new Range(nextMin, nextMax);
			windowSize_ = windowSize;
			windowEasing_ = windowEasing ?? new LinearEasing();

			if (windowSize_ == 0)
				windowSize_ = Math.Abs(max - min);

			Reset();
		}

		public Duration Clone()
		{
			return new Duration(
				fullRange_.min, fullRange_.max,
				nextTimeRange_.min, nextTimeRange_.max,
				windowSize_, windowEasing_.Clone());
		}

		public void CopyParametersFrom(Duration d)
		{
			fullRange_ = d.fullRange_;
			nextTimeRange_ = d.nextTimeRange_;
			windowSize_ = d.windowSize_;

			if (windowEasing_.GetIndex() != d.windowEasing_.GetIndex())
				windowEasing_ = d.windowEasing_.Clone();
		}

		public static Duration FromJSON(
			JSONClass o, string key, bool mandatory = false)
		{
			if (!o.HasKey(key))
			{
				if (mandatory)
					throw new LoadFailed($"duration '{key}' is missing");
				else
					return new Duration();
			}

			var a = o[key].AsArray;
			var oo = o[key].AsObject;

			if (a != null && a.Count == 2)
			{
				float min;
				if (!float.TryParse(a[0], out min))
					throw new LoadFailed($"duration '{key}' min is not a number");

				float max;
				if (!float.TryParse(a[1], out max))
					throw new LoadFailed($"duration '{key}' max is not a number");

				return new Duration(min, max, 0, 0, 0, null);
			}
			else if (oo != null)
			{
				float min;
				if (!float.TryParse(oo["min"].Value, out min))
					throw new LoadFailed($"duration '{key}' min is not a number");

				float max;
				if (!float.TryParse(oo["max"].Value, out max))
					throw new LoadFailed($"duration '{key}' max is not a number");

				float ws = 0;
				if (oo.HasKey("window"))
				{
					if (!float.TryParse(oo["window"].Value, out ws))
						throw new LoadFailed($"duration '{key}' window size is not a number");
				}

				IEasing e = null;

				if (oo.HasKey("windowEasing"))
				{
					e = EasingFactory.FromString(oo["windowEasing"].Value);
					if (e == null)
						throw new LoadFailed($"duration '{key}' bad easing name");
				}

				float nextMin = 0;
				if (oo.HasKey("nextMinTime"))
				{
					if (!float.TryParse(oo["nextMinTime"].Value, out nextMin))
						throw new LoadFailed($"duration '{key}' nextMinTime is not a number");
				}

				float nextMax = 0;
				if (oo.HasKey("nextMaxTime"))
				{
					if (!float.TryParse(oo["nextMaxTime"].Value, out nextMax))
						throw new LoadFailed($"duration '{key}' nextMaxTime is not a number");
				}

				return new Duration(min, max, nextMin, nextMax, ws, e);
			}
			else if (o[key].Value != "")
			{
				float v;
				if (!float.TryParse(o[key].Value, out v))
					throw new LoadFailed($"duration '{key}' is not a duration");

				return new Duration(v, v);
			}
			else
			{
				throw new LoadFailed($"duration '{key}' not a duration");
			}
		}

		public float Minimum
		{
			get { return currentRange_.min; }
		}

		public float Maximum
		{
			get { return currentRange_.max; }
		}

		public float NextMin
		{
			get { return nextTimeRange_.min; }
		}

		public float NextMax
		{
			get { return nextTimeRange_.max; }
		}

		public bool Finished
		{
			get { return finished_; }
		}

		public bool Enabled
		{
			get { return (fullRange_.min != fullRange_.max); }
		}

		public float Progress
		{
			get
			{
				if (current_ <= 0)
					return 1;

				return U.Clamp(elapsed_ / current_, 0, 1);
			}
		}

		public float Magnitude
		{
			set { magnitude_ = value; }
		}

		public float Current
		{
			get { return current_; }
		}

		public float Remaining
		{
			get { return Math.Max(current_ - elapsed_, 0); }
		}

		public void Reset(bool forceFast = false)
		{
			NextRange();

			elapsed_ = 0;
			nextElapsed_ = 0;
			finished_ = false;
			NextValue(forceFast);
		}

		public void Restart()
		{
			elapsed_ = 0;
			finished_ = false;
		}

		public void Update(float s)
		{
			if (finished_)
			{
				finished_ = false;
				NextRange();

				if (nextElapsed_ >= nextTime_)
				{
					nextElapsed_ = 0;
					NextValue();
				}

				Restart();
			}

			elapsed_ += s;
			nextElapsed_ += s;

			if (elapsed_ > current_)
				finished_ = true;
		}

		private void NextRange(bool forceFast = false)
		{
			if (windowEasing_ == null)
				return;

			var range = fullRange_.max - fullRange_.min;

			if (fullRange_.min < fullRange_.max)
				range -= windowSize_;
			else
				range += windowSize_;

			var wmin = fullRange_.min + range * windowEasing_.Magnitude(magnitude_);

			float wmax;

			if (fullRange_.min < fullRange_.max)
				wmax = wmin + windowSize_;
			else
				wmax = wmin - windowSize_;

			SetRange(wmin, wmax);
		}

		public void SetRange(float min, float max)
		{
			if (currentRange_.min != min || currentRange_.max != max)
			{
				currentRange_.min = min;
				currentRange_.max = max;
				nextElapsed_ = nextTime_ + 1;
			}
		}

		private void NextValue(bool forceFast = false)
		{
			if (forceFast)
				current_ = currentRange_.min;
			else
				current_ = U.RandomFloat(currentRange_.min, currentRange_.max);

			nextTime_ = U.RandomFloat(nextTimeRange_.min, nextTimeRange_.max);
		}

		public string ToLiveString()
		{
			return
				$"{elapsed_:0.##}/{current_:0.##}" +
				$"({currentRange_.min:0.##},{currentRange_.max:0.##},{finished_},{nextTime_})" +
				$"/ws={windowSize_:0.00},f={magnitude_:0.00}," +
				$"e={windowEasing_?.GetShortName()}";
		}

		public override string ToString()
		{
			return $"[{currentRange_.min:0.##},{currentRange_.max:0.##}]/ws={windowSize_:0.00}";
		}
	}
}
