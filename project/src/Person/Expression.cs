using System;
using System.Collections.Generic;

namespace Cue
{
	class Expression
	{
		struct TargetInfo
		{
			public float value;
			public float time;
			public float start;
			public float elapsed;
			public bool valid;
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


		private string name_;
		private bool[] moods_ = new bool[Moods.Count];
		private MorphGroup g_;
		private TargetInfo target_;
		private IEasing easing_ = new SinusoidalEasing();
		private float value_ = 0;
		private AutoInfo auto_ = new AutoInfo(0.1f, 0.5f, 2.0f);

		public Expression(string name, int mood, MorphGroup g)
			: this(name, new int[] { mood }, g)
		{
		}

		public Expression(string name, int[] moods, MorphGroup g)
			: this(name)
		{
			g_ = g;
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
			easing_ = e.easing_.Clone();
		}

		public void Init(Person p)
		{
			g_.Init(p);
		}

		public override string ToString()
		{
			return
				$"{name_} {MoodString()} " +
				$"{g_.Value:0.00}=>{target_:0.00} " +
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

		public void SetTarget(float t, float time)
		{
			target_.start = g_.Value;
			target_.value = t;
			target_.time = time;
			target_.elapsed = 0;
			target_.valid = true;
			target_.auto = false;
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
					NextAuto();
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


	class WeightedExpression
	{
		private float FastTimeMin = 0.4f;
		private float FastTimeMax = 2.0f;
		private float SlowTimeMin = 3.0f;
		private float SlowTimeMax = 5.0f;

		private const int InactiveState = 0;
		private const int RampUpState = 1;
		private const int HoldState = 2;
		private const int FinishedState = 3;

		private readonly Expression e_;
		private int state_ = InactiveState;

		private float weight_ = 0;
		private float intensity_ = 0;
		private float speed_ = 0;

		private Duration holdTime_ = new Duration(0, 3);


		public WeightedExpression(Expression e)
		{
			e_ = e;
			Deactivate();
		}

		public Expression Expression
		{
			get { return e_; }
		}

		public float Weight
		{
			get { return weight_; }
		}

		public float Intensity
		{
			get { return intensity_; }
		}

		public float Speed
		{
			get { return speed_; }
		}

		public void Set(float weight, float intensity, float speed)
		{
			weight_ = weight;
			intensity_ = intensity;
			speed_ = speed;
		}

		public bool Active
		{
			get { return (state_ != InactiveState); }
		}

		public bool Finished
		{
			get { return (state_ == FinishedState); }
		}

		public void Activate()
		{
			e_.SetAuto(0.1f, MinRandomTime(), MaxRandomTime());
			e_.SetTarget(RandomTarget(), RandomTargetTime());
			state_ = RampUpState;
		}

		public void Deactivate()
		{
			e_.SetAuto(0, 0, 0);
			e_.SetTarget(0, RandomResetTime());
			state_ = InactiveState;
		}

		public void FixedUpdate(float s)
		{
			e_.FixedUpdate(s);

			switch (state_)
			{
				case InactiveState:
				{
					break;
				}

				case RampUpState:
				{
					if (e_.Finished)
						state_ = HoldState;

					break;
				}

				case HoldState:
				{
					holdTime_.Update(s);
					if (holdTime_.Finished)
						state_ = FinishedState;

					break;
				}
			}
		}

		public void Reset()
		{
			e_.Reset();
		}

		public string ToDetailedString()
		{
			return
				$"{e_} w={weight_:0.00} i={intensity_:0.00} " +
				$"s={speed_:0.00} {StateToString(state_)}";
		}

		private static string StateToString(int state)
		{
			switch (state)
			{
				case InactiveState: return "inactive";
				case RampUpState: return "rampup";
				case HoldState: return "hold";
				case FinishedState: return "finished";
				default: return $"?{state}";
			}
		}

		public override string ToString()
		{
			return $"{e_.Name} ({e_.MoodString()}) w={weight_:0.00}";
		}

		private float RandomTarget()
		{
			return U.RandomFloat(0, 1) * U.Clamp(intensity_, 0, 1);
		}

		private float MinRandomTime()
		{
			return FastTimeMin + (SlowTimeMin - FastTimeMin) * (1 - speed_);
		}

		private float MaxRandomTime()
		{
			return FastTimeMax + (SlowTimeMax - FastTimeMax) * (1 - speed_);
		}

		private float RandomTargetTime()
		{
			return U.RandomFloat(MinRandomTime(), MaxRandomTime());
		}

		private float RandomResetTime()
		{
			return RandomTargetTime();
		}
	}


	class ExpressionManager
	{
		private const int MaxActive = 4;
		private const float MoreCheckInterval = 1;

		private Person person_;
		private WeightedExpression[] exps_ = new WeightedExpression[0];
		private bool needsMore_ = false;
		private float moreElapsed_ = 0;
		private Personality lastPersonality_ = null;
		private bool enabled_ = true;

		public ExpressionManager(Person p)
		{
			person_ = p;
		}

		public Expression[] GetExpressionsForMood(int mood)
		{
			var list = new List<Expression>();

			for (int i = 0; i < exps_.Length; ++i)
			{
				if (exps_[i].Expression.IsMood(mood))
					list.Add(exps_[i].Expression);
			}

			return list.ToArray();
		}

		public void Enable()
		{
			for (int i = 0; i < exps_.Length; ++i)
				exps_[i].Deactivate();

			enabled_ = true;

			for (int i = 0; i < MaxActive; ++i)
				NextActive();
		}

		public void Disable()
		{
			for (int i = 0; i < exps_.Length; ++i)
				exps_[i].Deactivate();

			enabled_ = false;
		}

		private void Init()
		{
			var all = person_.Personality.GetExpressions();
			exps_ = new WeightedExpression[all.Length];

			for (int i = 0; i < all.Length; ++i)
			{
				all[i].Init(person_);
				exps_[i] = new WeightedExpression(all[i]);
			}

			for (int i = 0; i < MaxActive; ++i)
				NextActive();

			lastPersonality_ = person_.Personality;
		}

		public void FixedUpdate(float s)
		{
			if (lastPersonality_ != person_.Personality)
				Init();

			int finished = 0;
			int activeCount = 0;

			for (int i = 0; i < exps_.Length; ++i)
			{
				exps_[i].FixedUpdate(s);

				if (exps_[i].Active)
				{
					if (exps_[i].Finished)
					{
						exps_[i].Deactivate();
						++finished;
					}
					else
					{
						++activeCount;
					}
				}
			}

			if (!enabled_)
				return;

			for (int i = 0; i < finished; ++i)
			{
				if (NextActive())
					++activeCount;
			}

			if (activeCount < MaxActive)
			{
				if (needsMore_)
				{
					moreElapsed_ += s;

					if (moreElapsed_ > MoreCheckInterval)
					{
						moreElapsed_ = 0;
						var tries = MaxActive - activeCount;

						for (int i = 0; i < tries; ++i)
						{
							if (NextActive())
								++activeCount;
						}

						if (activeCount >= MaxActive)
							needsMore_ = false;
					}
				}
				else
				{
					needsMore_ = true;
					moreElapsed_ = 0;
				}
			}
		}

		private bool NextActive()
		{
			UpdateExpressions();

			float totalWeight = 0;
			for (int i = 0; i < exps_.Length; ++i)
			{
				if (exps_[i].Active)
					continue;

				totalWeight += exps_[i].Weight;
			}

			if (totalWeight > 0)
			{
				var r = U.RandomFloat(0, totalWeight);

				for (int i = 0; i < exps_.Length; ++i)
				{
					if (exps_[i].Active)
						continue;

					if (r < exps_[i].Weight)
					{
						exps_[i].Activate();
						return true;
					}

					r -= exps_[i].Weight;
				}
			}

			return false;
		}

		private void UpdateExpressions()
		{
			var m = person_.Mood;
			var ps = person_.Personality;

			float expressionTiredness = U.Clamp(
				m.Get(Moods.Tired) * ps.Get(PSE.ExpressionTirednessFactor),
				0, 1);


			for (int i = 0; i < exps_.Length; ++i)
			{
				var we = exps_[i];
				var e = we.Expression;

				float weight = 0;
				float intensity = 0;


				if (e.IsMood(Moods.Happy))
				{
					weight += m.Get(Moods.Happy);
					intensity = Math.Max(intensity, m.Get(Moods.Happy));
				}

				if (e.IsMood(Moods.Excited))
				{
					weight += m.Get(Moods.Excited) * 2;
					intensity = Math.Max(intensity, m.Get(Moods.Excited));
				}

				if (e.IsMood(Moods.Angry))
				{
					weight += m.Get(Moods.Angry);
					intensity = Math.Max(intensity, m.Get(Moods.Angry));
				}

				if (e.IsMood(Moods.Tired))
				{
					weight += expressionTiredness;
					intensity = Math.Max(intensity, expressionTiredness);
				}


				if (!e.IsMood(Moods.Tired))
				{
					weight *= Math.Max(1 - expressionTiredness, 0.05f);
				}

				float speed = 1 - expressionTiredness;

				we.Set(weight, intensity, speed);
			}
		}

		public void OnPluginState(bool b)
		{
			if (!b)
			{
				for (int i = 0; i < exps_.Length; ++i)
					exps_[i].Reset();
			}
		}

		public string[] Debug()
		{
			var s = new string[MaxActive + 1 + exps_.Length + 2];

			int i = 0;

			for (int j = 0; j < exps_.Length; ++j)
			{
				if (exps_[j].Active)
					s[i++] = $"{exps_[j].ToDetailedString()}";
			}

			while (i < MaxActive)
				s[i++] = "none";

			s[i++] = "";

			for (int j = 0; j < exps_.Length; ++j)
				s[i++] = $"{exps_[j]}";

			s[i++] = "";
			s[i++] = (needsMore_ ? "needs more" : "");

			return s;
		}
	}
}
