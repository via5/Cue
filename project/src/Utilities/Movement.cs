using SimpleJSON;

namespace Cue
{
	class SlidingMovement
	{
		private Movement m_;
		private Vector3 min_, max_;
		private float nextMin_, nextMax_;
		private Vector3 windowSize_;
		private IEasing windowEasing_;
		private float f_ = 0;

		public SlidingMovement() : this(
			Vector3.Zero, Vector3.Zero, 0, 0, Vector3.Zero, null)
		{
		}

		public SlidingMovement(
			Vector3 min, Vector3 max, float nextMin, float nextMax,
			Vector3 windowSize, IEasing windowEasing)
		{
			m_ = new Movement(min, max, nextMin, nextMax);
			min_ = min;
			max_ = max;
			nextMin_ = nextMin;
			nextMax_ = nextMax;
			windowSize_ = windowSize;
			windowEasing_ = windowEasing;
		}

		public SlidingMovement(SlidingMovement m) : this(
			m.min_, m.max_, m.nextMin_, m.nextMax_,
			m.windowSize_, m.windowEasing_?.Clone())
		{
		}

		public static SlidingMovement FromJSON(
			JSONClass o, string key, bool mandatory = false)
		{
			if (!o.HasKey(key))
			{
				if (mandatory)
					throw new LoadFailed($"movement '{key}' is missing");
				else
					return new SlidingMovement();
			}

			var oo = o[key].AsObject;

			if (oo == null)
				throw new LoadFailed($"movement '{key}' not an object");

			var min = Vector3.FromJSON(oo, "min", true);
			var max = Vector3.FromJSON(oo, "max", true);
			var ws = Vector3.FromJSON(oo, "window", true);

			IEasing e = null;
			if (oo.HasKey("windowEasing") && oo["windowEasing"].Value != "")
			{
				string en = oo["windowEasing"];
				e = EasingFactory.FromString(en);
				if (e == null)
					throw new LoadFailed($"duration '{key}' bad easing name");
			}

			float nextMin;
			if (!float.TryParse(oo["nextMinTime"], out nextMin))
				throw new LoadFailed($"duration '{key}' nextMinTime is not a number");

			float nextMax;
			if (!float.TryParse(oo["nextMaxTime"], out nextMax))
				throw new LoadFailed($"duration '{key}' nextMaxTime is not a number");

			return new SlidingMovement(min, max, nextMin, nextMax, ws, e);
		}

		public void Reset()
		{
			m_.Reset();
		}

		public void Update(float s)
		{
			m_.Update(s);
		}

		public bool Next()
		{
			return m_.Next();
		}

		public void SetNext(Vector3 v)
		{
			m_.SetNext(v);
		}

		public Vector3 Lerped(float m)
		{
			return m_.Lerped(m);
		}

		public Vector3 Current
		{
			get { return m_.Current; }
		}

		public Vector3 Last
		{
			get { return m_.Last; }
		}

		public float WindowMagnitude
		{
			set
			{
				if (f_ != value)
				{
					f_ = value;
					SetWindow();
				}
			}
		}

		public override string ToString()
		{
			return
				$"{m_}\n" +
				$"/ws={windowSize_:0.00},f={f_:0.00}," +
				$"e={windowEasing_?.GetShortName()}";
		}

		private void SetWindow()
		{
			if (windowEasing_ != null)
			{
				Vector3 min, max;

				CalculateWindow(min_.X, max_.X, windowSize_.X, out min.X, out max.X);
				CalculateWindow(min_.Y, max_.Y, windowSize_.Y, out min.Y, out max.Y);
				CalculateWindow(min_.Z, max_.Z, windowSize_.Z, out min.Z, out max.Z);

				m_.SetRange(min, max);
			}
		}

		private void CalculateWindow(
			float min, float max, float size, out float wMin, out float wMax)
		{
			var range = max - min;

			if (min < max)
				range -= size;
			else
				range += size;

			wMin = min + range * windowEasing_.Magnitude(f_);

			if (min < max)
				wMax = wMin + size;
			else
				wMax = wMin - size;
		}
	}


	class Movement
	{
		private Vector3 min_, max_;
		private Vector3 last_ = Vector3.Zero;
		private Vector3 current_ = Vector3.Zero;

		private float nextMin_, nextMax_;
		private float next_ = 0;
		private float nextElapsed_ = 0;

		public Movement()
			: this(Vector3.Zero, Vector3.Zero, 0, 0)
		{
		}

		public Movement(Vector3 min, Vector3 max, float nextMin, float nextMax)
		{
			min_ = min;
			max_ = max;
			nextMin_ = nextMin;
			nextMax_ = nextMax;

			Next(true);
		}

		public Movement(Movement m)
			: this(m.min_, m.max_, m.nextMin_, m.nextMax_)
		{
		}

		public static Movement FromJSON(
			JSONClass o, string key, bool mandatory = false)
		{
			if (!o.HasKey(key))
			{
				if (mandatory)
					throw new LoadFailed($"movement '{key}' is missing");
				else
					return new Movement();
			}

			var oo = o[key].AsObject;

			if (oo == null)
				throw new LoadFailed($"movement '{key}' not an object");

			var min = Vector3.FromJSON(oo, "min", true);
			var max = Vector3.FromJSON(oo, "max", true);

			float nextMin;
			if (!float.TryParse(oo["nextMinTime"], out nextMin))
				throw new LoadFailed($"movement '{key}' nextMinTime is not a number");

			float nextMax;
			if (!float.TryParse(oo["nextMaxTime"], out nextMax))
				throw new LoadFailed($"movement '{key}' nextMaxTime is not a number");

			return new Movement(min, max, nextMin, nextMax);
		}

		public Vector3 Current
		{
			get { return current_; }
		}

		public Vector3 Last
		{
			get { return last_; }
		}

		public void Reset()
		{
			last_ = Vector3.Zero;
			current_ = Vector3.Zero;
			Next(true);
		}

		public void SetNext(Vector3 v)
		{
			last_ = current_;
			current_ = v;
		}

		public bool Next(bool force = false)
		{
			if (nextElapsed_ >= next_ || force)
			{
				nextElapsed_ = 0;
				next_ = U.RandomFloat(nextMin_, nextMax_);

				SetNext(new Vector3(
					U.RandomFloat(min_.X, max_.X),
					U.RandomFloat(min_.Y, max_.Y),
					U.RandomFloat(min_.Z, max_.Z)));

				return true;
			}

			return false;
		}

		public void SetRange(Vector3 min, Vector3 max)
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
			nextElapsed_ += s;
		}

		public Vector3 Lerped(float f)
		{
			return Vector3.Lerp(last_, current_, f);
		}

		public override string ToString()
		{
			return
				$"min={min_} max={max_}\n" +
				$"last={last_} current={current_} next={nextElapsed_:0.00}/{next_:0.00}";
		}
	}
}
