﻿namespace Cue
{
	public class Expression
	{
		struct TargetInfo
		{
			public float value;
			public float time;
			public float start;
			public float elapsed;
			public bool reset;
			public bool valid;
			public bool stopAfter;
			public bool auto;

			public override string ToString()
			{
				if (!valid)
					return "R";

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
			public float weight;
			public bool exclusive;
			public float minExcitement;
			public bool maxOnly;
			public float minHoldTime;
			public float maxHoldTime;
			public bool forMale;
			public bool forFemale;
			public float permanent;
		}


		private string name_;
		private readonly Logger log_;
		private MoodType mood_ = MoodType.None;
		private MorphGroup g_;
		private Config config_;
		private TargetInfo target_;
		private IEasing easing_ = new QuadInOutEasing();
		private float value_ = 0;
		private AutoInfo auto_ = new AutoInfo(0.1f, 0.5f, 2.0f);
		private float add_ = 0;

		public Expression(string name, MoodType mood, Config c, MorphGroup g)
			: this(name, mood)
		{
			g_ = g;
			config_ = c;
		}

		private Expression(string name, MoodType mood)
		{
			name_ = name;
			log_ = new Logger(Logger.AI, "expression " + name);
			mood_ = mood;
			target_.valid = false;
		}

		public Expression Clone()
		{
			var e = new Expression(name_, mood_);
			e.CopyFrom(this);
			return e;
		}

		public Logger Log
		{
			get { return log_; }
		}

		private void CopyFrom(Expression e)
		{
			g_ = e.g_.Clone();
			config_ = e.config_;
			easing_ = e.easing_.Clone();
		}

		public bool Init(Person p)
		{
			if (!config_.forFemale && !p.Atom.IsMale)
			{
				Log.Verbose($"init failed: not for female and atom {p} is not male");
				return false;
			}

			if (!config_.forMale && p.Atom.IsMale)
			{
				Log.Verbose($"init failed: not for male and atom {p} is male");
				return false;
			}

			return g_.Init(p);
		}

		public override string ToString()
		{
			return
				$"{name_} {MoodString()} " +
				$"{g_.DebugValueString()}=>{target_:0.00} " +
				$"{target_.elapsed:0.0}/{target_.time:0.0}";
		}

		public MorphGroup MorphGroup
		{
			get { return g_; }
		}

		public string Name
		{
			get { return name_; }
			set { name_ = value; }
		}

		public float Target
		{
			get { return target_.value; }
		}

		public bool Permanent
		{
			get { return (config_.permanent >= 0); }
		}

		public float PermanentValue
		{
			get { return config_.permanent; }
		}

		public bool Exclusive
		{
			get { return config_.exclusive; }
		}

		public float DefaultWeight
		{
			get { return config_.weight; }
		}

		public BodyPartType[] BodyParts
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

		public float Add
		{
			set { add_ = value; }
		}

		public MoodType Mood
		{
			get { return mood_; }
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
			return MoodType.ToString(mood_);
		}

		public bool AffectsAnyBodyPart(BodyPartType[] bodyParts)
		{
			return g_.AffectsAnyBodyPart(bodyParts);
		}

		public void SetTarget(float t, float time)
		{
			DoSetTarget(t, time, false, false);
		}

		public void Deactivate(float time)
		{
			DoSetTarget(0, time, true, true);
		}

		private void DoSetTarget(float t, float time, bool reset, bool stopAfter)
		{
			target_.start = g_.Value;
			target_.value = t;
			target_.time = time;
			target_.reset = reset;
			target_.elapsed = 0;
			target_.valid = true;
			target_.auto = false;
			target_.stopAfter = stopAfter;
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

				bool finished = false;

				if (target_.reset)
				{
					if (g_.MoveTowardsReset())
						finished = true;
				}
				else
				{
					float targetValue = target_.value;
					if (targetValue > 0)
						targetValue += add_;

					float v = U.Lerp(target_.start, targetValue, t);
					g_.MoveTowards(target_.start, targetValue, t);

					if (!target_.auto)
						value_ = v;

					if (p >= 1)
						finished = true;
				}

				if (finished)
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
