using System.Collections.Generic;

namespace Cue.Proc
{/*
	class Mood
	{
		private readonly int type_;
		private float max_ = 0;
		private float intensity_ = 0;
		private float dampen_ = 0;
		private readonly List<Expression> exps_ = new List<Expression>();
		private SlidingDuration wait_;

		public Mood(Person p, int type)
		{
			type_ = type;
			wait_ = new SlidingDuration(0.5f, 4);
		}

		public int Type
		{
			get { return type_; }
		}

		public float Maximum
		{
			get { return max_; }
			set { max_ = value; }
		}

		public float Intensity
		{
			get
			{
				return intensity_;
			}

			set
			{
				if (intensity_ != value)
				{
					intensity_ = value;

					for (int i = 0; i < exps_.Count; ++i)
						exps_[i].AutoRange = intensity_ * 0.05f;
				}
			}
		}

		public float Dampen
		{
			get { return dampen_; }
			set { dampen_ = value; }
		}

		public void Add(Expression e)
		{
			e.AutoRange = 0;
			exps_.Add(e);
		}

		public Expression[] Expressions
		{
			get { return exps_.ToArray(); }
		}

		public void Reset()
		{
			for (int i = 0; i < exps_.Count; ++i)
				exps_[i].Reset();
		}

		public void FixedUpdate(float s)
		{
			wait_.Update(s);

			if (wait_.Finished)
			{
				//float intensity = max_ * intensity_ * (1 - dampen_);
				float target = U.RandomFloat(0, 1) * intensity_;
				for (int i = 0; i < exps_.Count; ++i)
					exps_[i].SetTarget(target, U.RandomFloat(0.4f, 2));
			}

			for (int i = 0; i < exps_.Count; ++i)
				exps_[i].FixedUpdate(s);
		}

		public override string ToString()
		{
			return "";
				//$"{Moods.ToString(type_)} " +
				//$"max={max_} int ={intensity_} damp={dampen_}";
		}
	}*/


	class WeightedExpression
	{
		private const int InactiveState = 0;
		private const int RampUpState = 1;
		private const int HoldState = 2;
		private const int FinishedState = 3;

		private readonly Expression e_;
		private float weight_ = 0;
		private int state_ = InactiveState;

		private Duration holdTime_ = new Duration(0, 2);

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
			set { weight_ = value; }
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

		public override string ToString()
		{
			return $"{e_} w={weight_:0.00} state={state_}";
		}

		private float RandomTarget()
		{
			return U.RandomFloat(0, 1) * U.Clamp(weight_, 0, 1);
		}

		private float RandomTargetTime()
		{
			return U.RandomFloat(0.4f, 2);
		}

		private float RandomResetTime()
		{
			return U.RandomFloat(0.4f, 2);
		}
	}


	class MoodProcAnimation : BasicProcAnimation
	{
		private const int MaxActive = 2;
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
			var s = new string[exps_.Length + 2];

			int i = 0;

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
			UpdateWeights();

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

		private void UpdateWeights()
		{
			for (int i = 0; i < exps_.Length; ++i)
			{
				var we = exps_[i];
				var e = we.Expression;

				float w = 0;

				if (e.IsType(Expressions.Happy))
					w += 1;

				if (e.IsType(Expressions.Excited))
					w += person_.Mood.ExpressionExcitement;

				we.Weight = w;
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
