using SimpleJSON;
using System;

namespace Cue
{
	interface IDuration
	{
		bool Enabled { get; }
		bool Finished { get; }
		float Progress { get; }
		float Energy { set; }
		void Update(float s);
	}


	class SlidingDuration : IDuration
	{
		private Duration d_;
		private float min_, max_;
		private float nextMin_, nextMax_;
		private float windowSize_;
		private IEasing windowEasing_;
		private float f_ = 0;
		private bool finished_ = false;

		public SlidingDuration()
			: this(0, 0, 0, 0, 0, null)
		{
		}

		public SlidingDuration(
			float min, float max, float nextMin, float nextMax,
			float windowSize, IEasing windowEasing)
		{
			d_ = new Duration(min, max, nextMin, nextMax);
			min_ = min;
			max_ = max;
			nextMin_ = nextMin;
			nextMax_ = nextMax;
			windowSize_ = windowSize;
			windowEasing_ = windowEasing;

			if (windowSize_ == 0)
				windowSize_ = Math.Abs(max - min);

			Next();
		}

		public SlidingDuration(SlidingDuration d) : this(
			d.min_, d.max_, d.nextMin_, d.nextMax_,
			d.windowSize_, d.windowEasing_?.Clone())
		{
		}

		public static SlidingDuration FromJSON(
			JSONClass o, string key, bool mandatory = false)
		{
			if (!o.HasKey(key))
			{
				if (mandatory)
					throw new LoadFailed($"duration '{key}' is missing");
				else
					return new SlidingDuration();
			}

			var a = o[key].AsArray;

			if (a != null && a.Count == 2)
			{
				float min;
				if (!float.TryParse(a[0], out min))
					throw new LoadFailed($"duration '{key}' min is not a number");

				float max;
				if (!float.TryParse(a[1], out max))
					throw new LoadFailed($"duration '{key}' max is not a number");

				return new SlidingDuration(min, max, 0, 0, 0, null);
			}
			else
			{
				var oo = o[key].AsObject;

				if (oo != null)
				{
					float min;
					if (!float.TryParse(oo["min"], out min))
						throw new LoadFailed($"duration '{key}' min is not a number");

					float max;
					if (!float.TryParse(oo["max"], out max))
						throw new LoadFailed($"duration '{key}' max is not a number");

					float ws;
					if (!float.TryParse(oo["window"], out ws))
						throw new LoadFailed($"duration '{key}' window size is not a number");

					string en = oo["windowEasing"];
					IEasing e = EasingFactory.FromString(en);
					if (e == null)
						throw new LoadFailed($"duration '{key}' bad easing name");

					float nextMin;
					if (!float.TryParse(oo["nextMinTime"], out nextMin))
						throw new LoadFailed($"duration '{key}' nextMinTime is not a number");

					float nextMax;
					if (!float.TryParse(oo["nextMaxTime"], out nextMax))
						throw new LoadFailed($"duration '{key}' nextMaxTime is not a number");

					return new SlidingDuration(min, max, nextMin, nextMax, ws, e);
				}
				else
				{
					throw new LoadFailed($"duration '{key}' not a duration");
				}
			}
		}

		public float Minimum
		{
			get { return d_.Minimum; }
		}

		public float Maximum
		{
			get { return d_.Maximum; }
		}

		public bool Finished
		{
			get { return finished_; }
		}

		public bool Enabled
		{
			get { return d_.Enabled; }
		}

		public float Progress
		{
			get { return d_.Progress; }
		}

		public float Energy
		{
			set { WindowMagnitude = value; }
		}

		public float Current
		{
			get { return d_.Current; }
		}

		public float WindowMagnitude
		{
			set { f_ = value; }
		}

		public void Reset()
		{
			d_.Reset();
			finished_ = false;
		}

		public void Restart()
		{
			d_.Restart();
			finished_ = false;
		}

		public void Update(float s)
		{
			if (finished_)
			{
				finished_ = false;
				Next();
			}

			d_.Update(s);

			if (d_.Finished)
				finished_ = true;
		}

		public void Next()
		{
			if (windowEasing_ != null)
			{
				var range = max_ - min_;

				if (min_ < max_)
					range -= windowSize_;
				else
					range += windowSize_;

				var wmin = min_ + range * windowEasing_.Magnitude(f_);

				float wmax;

				if (min_ < max_)
					wmax = wmin + windowSize_;
				else
					wmax = wmin - windowSize_;

				d_.SetRange(wmin, wmax);
			}
		}

		public string ToLiveString()
		{
			return
				d_.ToLiveString() +
				$"/ws={windowSize_:0.00},f={f_:0.00}," +
				$"e={windowEasing_?.GetShortName()}";
		}

		public override string ToString()
		{
			return d_.ToString() + $"/ws={windowSize_:0.00}";
		}
	}


	class Duration
	{
		private float min_, max_;
		private float nextMin_, nextMax_;

		private float next_ = 0;
		private float nextElapsed_ = 0;
		private float current_ = 0;
		private float elapsed_ = 0;
		private bool finished_ = false;

		public Duration(Pair<float, float> p)
			: this(p.first, p.second, 0, 0)
		{
		}

		public Duration(Duration d)
			: this(d.min_, d.max_, d.nextMin_, d.nextMax_)
		{
		}

		public Duration()
			: this(0, 0, 0, 0)
		{
		}

		public Duration(float min, float max)
			: this(min, max, 0, 0)
		{
		}

		public Duration(float min, float max, float nextMin, float nextMax)
		{
			min_ = min;
			max_ = max;
			nextMin_ = nextMin;
			nextMax_ = nextMax;

			Next();
		}

		public static Duration FromJSON(JSONClass o, string key, bool mandatory = false)
		{
			if (!o.HasKey(key))
			{
				if (mandatory)
					throw new LoadFailed($"duration '{key}' is missing");
				else
					return new Duration();
			}

			var a = o[key].AsArray;
			if (a == null)
				throw new LoadFailed($"duration '{key}' node is not an array");

			if (a.Count != 2)
				throw new LoadFailed($"duration '{key}' array must have 2 elements");

			float min;
			if (!float.TryParse(a[0], out min))
				throw new LoadFailed($"duration '{key}' min is not a number");

			float max;
			if (!float.TryParse(a[1], out max))
				throw new LoadFailed($"duration '{key}' max is not a number");

			return new Duration(min, max);
		}

		public float Minimum
		{
			get { return min_; }
		}

		public float Maximum
		{
			get { return max_; }
		}

		public bool Finished
		{
			get { return finished_; }
		}

		public float NextMin
		{
			get{ return nextMin_; }
		}

		public float NextMax
		{
			get { return nextMax_; }
		}

		public bool Enabled
		{
			get { return (min_ != 0 || max_ != 0); }
		}

		public float Progress
		{
			get { return U.Clamp(elapsed_ / current_, 0, 1); }
		}

		public float Current
		{
			get { return current_; }
		}

		public float Remaining
		{
			get { return current_ - elapsed_; }
		}

		public void Reset()
		{
			elapsed_ = 0;
			nextElapsed_ = 0;
			finished_ = false;
			Next();
		}

		public void Restart()
		{
			finished_ = false;
			elapsed_ = 0;
		}

		public void SetRange(Pair<float, float> p)
		{
			SetRange(p.first, p.second);
		}

		public void SetRange(float min, float max)
		{
			if (min_ != min || max_ != max)
			{
				min_ = min;
				max_ = max;
				nextElapsed_ = next_ + 1;
			}
		}

		public void Update(float s)
		{
			if (finished_)
			{
				if (nextElapsed_ >= next_)
				{
					nextElapsed_ = 0;
					Next();
				}

				Restart();
			}

			elapsed_ += s;
			nextElapsed_ += s;

			if (elapsed_ > current_)
				finished_ = true;
		}

		public string ToLiveString()
		{
			return
				$"{elapsed_:0.##}/{current_:0.##}" +
				$"({min_:0.##},{max_:0.##},{finished_},{next_})";
		}

		public override string ToString()
		{
			return $"[{min_:0.##},{max_:0.##}]";
		}

		private void Next()
		{
			current_ = U.RandomFloat(min_, max_);
			next_ = U.RandomFloat(nextMin_, nextMax_);
		}
	}
}
