namespace Cue
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

		private readonly Person person_;
		private readonly Expression e_;
		private int state_ = InactiveState;

		private float weight_ = 0;
		private float intensity_ = 0;
		private float speed_ = 0;
		private float min_ = 0;
		private float max_ = 1;
		private float add_ = 0;
		private MoodType mainMood_ = MoodType.None;

		private Duration holdTime_;


		public WeightedExpression(Person p, Expression e)
		{
			person_ = p;

			var ps = p.Personality;

			e_ = e;

			holdTime_ = new Duration(
				e.MinHoldTime < 0 ? ps.Get(PS.ExpressionMinHoldTime) : e.MinHoldTime,
				e.MaxHoldTime < 0 ? ps.Get(PS.ExpressionMaxHoldTime) : e.MaxHoldTime);

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

		public float Add
		{
			set { add_ = value; }
		}

		public void Set(
			float weight, float intensity, float speed,
			float min, float max, MoodType mainMood)
		{
			weight_ = weight;
			intensity_ = intensity;
			speed_ = speed;
			min_ = min;
			max_ = max;
			mainMood_ = mainMood;
		}

		public bool Active
		{
			get { return (state_ != InactiveState); }
		}

		public bool Finished
		{
			get { return (state_ == FinishedState); }
		}

		public bool Activate()
		{
			var bps = e_.BodyParts;

			if (bps.Length > 0)
			{
				bool found = false;

				for (int i = 0; i < bps.Length; ++i)
				{
					if (!person_.Body.Get(bps[i]).LockedFor(BodyPartLock.Morph))
					{
						found = true;
						break;
					}
				}

				if (!found)
					return false;
			}

			Activate(RandomTarget(), RandomTargetTime());
			return true;
		}

		public void Activate(float target, float time)
		{
			e_.SetAuto(0.1f, MinRandomTime(), MaxRandomTime());
			e_.SetTarget(target, time);
			state_ = RampUpState;
			add_ = 0;
		}

		public void Deactivate()
		{
			e_.SetAuto(0, 0, 0);
			e_.SetTargetAndStop(0, RandomResetTime());
			state_ = InactiveState;
		}

		public void FixedUpdate(float s)
		{
			e_.Add = add_;
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
					holdTime_.Update(s, 0);
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

		IRandom rng_ = new NormalRandom(0.3f, 0.9f, 1, 5);

		private float RandomTarget()
		{
			if (e_.MaxOnly)
			{
				return 1.0f;
			}
			else
			{
				if (mainMood_ == MoodType.Excited)
					return rng_.RandomFloat(min_, intensity_, intensity_);
				else
					return U.RandomFloat(min_, max_) * U.Clamp(intensity_, 0, 1);
			}
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
}
