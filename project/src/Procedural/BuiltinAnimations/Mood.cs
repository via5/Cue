using System;

namespace Cue.Proc
{
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
			return $"{e_.Name} ({Expressions.ToString(e_.Type)}) w={weight_:0.00}";
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


	class MoodProcAnimation : BasicProcAnimation
	{
		private const int MaxActive = 4;
		private const float MoreCheckInterval = 1;

		private WeightedExpression[] exps_ = new WeightedExpression[0];
		private bool needsMore_ = false;
		private float moreElapsed_ = 0;

		public MoodProcAnimation()
			: base("procMood", false)
		{
		}

		public override string[] Debug()
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

			for (int j=0; j< exps_.Length;++j)
				s[i++] = $"{exps_[j]}";

			s[i++] = "";
			s[i++] = (needsMore_ ? "needs more" : "");

			return s;
		}

		public override BasicProcAnimation Clone()
		{
			var a = new MoodProcAnimation();
			a.CopyFrom(this);
			return a;
		}

		public override bool Start(Person p, AnimationContext cx)
		{
			if (!base.Start(p, cx))
				return false;

			var all = BuiltinExpressions.All(p);
			exps_ = new WeightedExpression[all.Length];

			for (int i = 0; i < all.Length; ++i)
				exps_[i] = new WeightedExpression(all[i]);

			for (int i = 0; i < MaxActive; ++i)
				NextActive();

			return true;
		}

		public override void FixedUpdate(float s)
		{
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
			for (int i = 0; i < exps_.Length; ++i)
			{
				var we = exps_[i];
				var e = we.Expression;

				float weight = 0;
				float intensity = 0;


				if (e.IsType(Expressions.Happy))
				{
					weight += 1;
					intensity = Math.Max(intensity, 1);
				}

				if (e.IsType(Expressions.Excited))
				{
					weight += person_.Mood.ExpressionExcitement * 2;
					intensity = Math.Max(intensity, person_.Mood.ExpressionExcitement);
				}

				if (e.IsType(Expressions.Angry))
				{
					// todo
				}

				if (e.IsType(Expressions.Tired))
				{
					weight += person_.Mood.ExpressionTiredness;
					intensity = Math.Max(intensity, person_.Mood.ExpressionTiredness);
				}


				if (!e.IsType(Expressions.Tired))
				{
					weight *= Math.Max(1 - person_.Mood.ExpressionTiredness, 0.05f);
				}

				float speed = 1 - person_.Mood.ExpressionTiredness;

				we.Set(weight, intensity, speed);
			}
		}

		public override void Reset()
		{
			base.Reset();

			for (int i = 0; i < exps_.Length; ++i)
				exps_[i].Reset();
		}
	}
}
