namespace Cue
{
	public class Expression
	{
		struct TargetInfo
		{
			public float value;
			public float time;
			public float start;
			public float elapsed;
			public bool valid;
			public bool stopAfter;
			public bool auto;

			public override string ToString()
			{
				if (!valid)
					return "-";

				return $"{value:0.00}";
			}
		}

		struct AutoInfo
		{
			public float range;
			public float minTime;
			public float maxTime;

			public AutoInfo(float range, float minTime, float maxTime)
			{
				this.range = range;
				this.minTime = minTime;
				this.maxTime = maxTime;
			}
		}

		public struct Config
		{
			public bool exclusive;
			public float minExcitement;
			public bool maxOnly;
			public float minHoldTime;
			public float maxHoldTime;
		}


		private string name_;
		private bool[] moods_ = new bool[Moods.Count];
		private MorphGroup g_;
		private Config config_;
		private TargetInfo target_;
		private IEasing easing_ = new SinusoidalEasing();
		private float value_ = 0;
		private AutoInfo auto_ = new AutoInfo(0.1f, 0.5f, 2.0f);

		public Expression(string name, int mood, Config c, MorphGroup g)
				: this(name, new int[] { mood }, c, g)
		{
		}

		public Expression(string name, int[] moods, Config c, MorphGroup g)
				: this(name)
		{
			g_ = g;
			config_ = c;

			for (int i = 0; i < moods.Length; ++i)
				moods_[moods[i]] = true;
		}

		private Expression(string name)
		{
			name_ = name;
			target_.valid = false;
		}

		public Expression Clone()
		{
			var e = new Expression(name_);
			e.CopyFrom(this);
			return e;
		}

		private void CopyFrom(Expression e)
		{
			moods_ = new bool[e.moods_.Length];
			e.moods_.CopyTo(moods_, 0);
			g_ = e.g_.Clone();
			config_ = e.config_;
			easing_ = e.easing_.Clone();
		}

		public bool Init(Person p)
		{
			return g_.Init(p);
		}

		public override string ToString()
		{
			return
				$"{name_} {MoodString()} " +
				$"{g_.Value:0.00}=>{target_:0.00} tv={target_.valid} " +
				$"{target_.elapsed:0.00}/{target_.time:0.00}";
		}

		public string Name
		{
			get { return name_; }
		}

		public float Target
		{
			get { return target_.value; }
		}

		public bool Exclusive
		{
			get { return config_.exclusive; }
		}

		public int[] BodyParts
		{
			get { return g_.BodyParts; }
		}

		public float MinExcitement
		{
			get { return config_.minExcitement; }
		}

		public float MinHoldTime
		{
			get { return config_.minHoldTime; }
		}

		public float MaxHoldTime
		{
			get { return config_.maxHoldTime; }
		}

		public bool MaxOnly
		{
			get { return config_.maxOnly; }
		}

		public void SetAuto(float range, float minTime, float maxTime)
		{
			auto_.range = range;
			auto_.minTime = minTime;
			auto_.maxTime = maxTime;
		}

		public bool Finished
		{
			get { return (!target_.valid || target_.auto || target_.elapsed >= target_.time); }
		}

		public string MoodString()
		{
			string s = "";

			for (int i = 0; i < moods_.Length; ++i)
			{
				if (moods_[i])
				{
					if (s != "")
						s += "|";

					s += Moods.ToString(i);
				}
			}

			return s;
		}

		public bool IsMood(int t)
		{
			return moods_[t];
		}

		public bool AffectsAnyBodyPart(int[] bodyParts)
		{
			return g_.AffectsAnyBodyPart(bodyParts);
		}

		public void SetTarget(float t, float time)
		{
			target_.start = g_.Value;
			target_.value = t;
			target_.time = time;
			target_.elapsed = 0;
			target_.valid = true;
			target_.auto = false;
			target_.stopAfter = false;
		}

		public void SetTargetAndStop(float t, float time)
		{
			SetTarget(t, time);
			target_.stopAfter = true;
		}

		public void Reset()
		{
			g_.Reset();
		}

		public void FixedUpdate(float s)
		{
			if (target_.valid)
			{
				target_.elapsed += s;

				float p = U.Clamp(target_.elapsed / target_.time, 0, 1);
				float t = easing_.Magnitude(p);
				float v = U.Lerp(target_.start, target_.value, t);

				g_.Value = v;

				if (!target_.auto)
					value_ = v;

				if (p >= 1)
				{
					if (target_.stopAfter)
						target_.valid = false;
					else
						NextAuto();
				}
			}
			else
			{
				NextAuto();
			}
		}

		private void NextAuto()
		{
			if (auto_.range <= 0)
				return;

			float v = U.RandomFloat(value_ - auto_.range, value_ + auto_.range);
			v = U.Clamp(v, 0, 1);

			float t = U.RandomFloat(auto_.minTime, auto_.maxTime);

			SetTarget(v, t);
			target_.auto = true;
		}
	}
}
