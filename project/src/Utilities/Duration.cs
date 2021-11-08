using SimpleJSON;
using System;

namespace Cue
{
	class Duration
	{
		class Range
		{
			public float min, max;
			public float window;
			public IEasing windowEasing;
			public IRandom rng;

			public Range(float min, float max, float window, IEasing windowEasing, IRandom rng)
			{
				this.min = min;
				this.max = max;
				this.window = window;
				this.windowEasing = windowEasing ?? new LinearEasing();
				this.rng = rng ?? new UniformRandom();

				if (this.window == 0)
					this.window = Math.Abs(this.max - this.min);
			}

			public string ToDetailedString()
			{
				return
					$"min={min:0.00} max={max:0.00} win={window} " +
					$"easing={windowEasing} rng={rng}";
			}

			public static Range FromJSON(string key, string rangeName, JSONClass o)
			{
				float min;
				if (!float.TryParse(o["min"].Value, out min))
					throw new LoadFailed($"duration '{key}': range '{rangeName}' min is not a number");

				float max;
				if (!float.TryParse(o["max"].Value, out max))
					throw new LoadFailed($"duration '{key}': range '{rangeName}' max is not a number");

				float window = 0;
				if (o.HasKey("window"))
				{
					if (!float.TryParse(o["window"].Value, out window))
						throw new LoadFailed($"duration '{key}': range '{rangeName}' window size is not a number");
				}

				IEasing windowEasing = null;

				if (o.HasKey("windowEasing"))
				{
					windowEasing = EasingFactory.FromString(o["windowEasing"].Value);
					if (windowEasing == null)
						throw new LoadFailed($"duration '{key}': range '{rangeName}' bad easing name");
				}

				IRandom rnd;

				try
				{
					if (o.HasKey("rng"))
						rnd = BasicRandom.FromJSON(o["rng"].AsObject);
					else
						rnd = new UniformRandom();
				}
				catch (LoadFailed e)
				{
					throw new LoadFailed($"duration '{key}': range '{rangeName}': {e.Message}");
				}

				return new Range(min, max, window, windowEasing, rnd);
			}

			public Range Clone()
			{
				return new Range(
					min, max, window, windowEasing.Clone(), rng.Clone());
			}
		}

		private Range fullRange_;
		private Range nextTimeRange_;
		private float magnitude_ = 0;

		private float min_ = 0;
		private float max_ = 0;
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
				: this(
					  new Range(min, max, windowSize, windowEasing, null),
					  new Range(nextMin, nextMax, 0, null, null))
		{
		}

		private Duration(Range fullRange, Range nextTimeRange)
		{
			fullRange_ = fullRange;
			nextTimeRange_ = nextTimeRange;

			Reset();
		}

		public Duration Clone()
		{
			return new Duration(fullRange_.Clone(), nextTimeRange_.Clone());
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
				return FromJSONArray(key, a);
			else if (oo != null)
				return FromJSONObject(key, oo);
			else if (o[key].Value != "")
				return FromJSONValue(key, o);
			else
				throw new LoadFailed($"duration '{key}' not a duration");
		}

		private static Duration FromJSONArray(string key, JSONArray a)
		{
			float min;
			if (!float.TryParse(a[0], out min))
				throw new LoadFailed($"duration '{key}' array, min is not a number");

			float max;
			if (!float.TryParse(a[1], out max))
				throw new LoadFailed($"duration '{key}' array, max is not a number");

			return new Duration(min, max, 0, 0, 0, null);
		}

		private static Duration FromJSONObject(string key, JSONClass oo)
		{
			Range fullRange;
			if (oo.HasKey("range"))
				fullRange = Range.FromJSON(key, "range", oo["range"].AsObject);
			else
				throw new LoadFailed($"duration '{key}' missing range");

			Range nextTimeRange;
			if (oo.HasKey("nextTimeRange"))
				nextTimeRange = Range.FromJSON(key, "nextTime", oo["nextTime"].AsObject);
			else
				nextTimeRange = new Range(0, 0, 0, null, null);

			return new Duration(fullRange, nextTimeRange);
		}

		private static Duration FromJSONValue(string key, JSONClass o)
		{
			float v;
			if (!float.TryParse(o[key].Value, out v))
				throw new LoadFailed($"duration '{key}' is not a duration");

			return new Duration(v, v);
		}

		public float Minimum
		{
			get { return min_; }
		}

		public float Maximum
		{
			get { return max_; }
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

		public void Update(float s, float magnitude)
		{
			magnitude_ = magnitude;

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
			var range = fullRange_.max - fullRange_.min;

			if (fullRange_.min < fullRange_.max)
				range -= fullRange_.window;
			else
				range += fullRange_.window;

			var wmin = fullRange_.min + range * fullRange_.windowEasing.Magnitude(magnitude_);

			float wmax;

			if (fullRange_.min < fullRange_.max)
				wmax = wmin + fullRange_.window;
			else
				wmax = wmin - fullRange_.window;

			SetRange(wmin, wmax);
		}

		public void SetRange(float min, float max)
		{
			if (min_ != min || max_ != max)
			{
				min_ = min;
				max_ = max;
				nextElapsed_ = nextTime_ + 1;
			}
		}

		private void NextValue(bool forceFast = false)
		{
			if (forceFast)
				current_ = min_;
			else
				current_ = fullRange_.rng.RandomFloat(min_, max_, magnitude_);

			nextTime_ = nextTimeRange_.rng.RandomFloat(
				nextTimeRange_.min, nextTimeRange_.max, magnitude_);
		}

		public string ToLiveString()
		{
			return
				$"{elapsed_:0.##}/{current_:0.##}" +
				$"({min_:0.##},{max_:0.##},{finished_},{nextTime_})" +
				$"/ws={fullRange_.window:0.00},f={magnitude_:0.00}";
		}

		public override string ToString()
		{
			return $"[{min_:0.##},{max_:0.##}]/ws={fullRange_.window:0.00}";
		}

		public string ToDetailedString()
		{
			return
				$"fullRange: {fullRange_.ToDetailedString()}\n" +
				$"nextTimeRange: {nextTimeRange_.ToDetailedString()}\n" +
				$"mag={magnitude_:0.00} min={min_:0.00} max={max_:0.00} finished={finished_}\n" +
				$"elapsed={elapsed_:0.00}/{current_:0.00} next={nextTime_:0.00}/{nextElapsed_:0.00}";
		}
	}
}
