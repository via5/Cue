namespace Cue
{
	class Mood
	{
		public const int None = 0;
		public const int Idle = 1;
		public const int Happy = 2;

		private Person person_;
		private int state_ = None;
		private float excitement_ = 0;
		private float lastRate_ = 0;

		private float mouthRate_ = 0.001f;
		private float breastsRate_ = 0.01f;
		private float genitalsRate_ = 0.1f;
		private float decayRate_ = -0.01f;
		private float rateAdjust_ = 0.1f;

		public Mood(Person p)
		{
			person_ = p;
		}

		public int State
		{
			get
			{
				return state_;
			}

			set
			{
				state_ = value;
				person_.Personality.SetMood(state_);
			}
		}

		public float Excitement
		{
			get { return excitement_; }
		}

		public void Update(float s)
		{
			float rate = 0;

			rate += person_.Excitement.Genitals * genitalsRate_;
			rate += person_.Excitement.Mouth * mouthRate_;
			rate += person_.Excitement.Breasts * breastsRate_;

			if (rate == 0)
				rate = decayRate_;

			excitement_ += rate * s * rateAdjust_;

			if (excitement_ >= 1)
			{
				person_.Orgasmer.Orgasm();
				excitement_ = 0;
			}

			person_.Breathing.Intensity = excitement_;
			lastRate_ = rate;
		}

		public void OnPluginState(bool b)
		{
		}

		public override string ToString()
		{
			string s = "";

			//s += $"state={state_} ";
			s += $"ex={excitement_:0.00}";

			if (lastRate_ < 0)
				s += "-";
			else
				s += "+";

			s += $"({lastRate_:0.000})";

			return s;
		}
	}


	interface IPersonality
	{
		void SetMood(int state);
	}


	class NeutralPersonality : IPersonality
	{
		private readonly Person person_;

		public NeutralPersonality(Person p)
		{
			person_ = p;
		}

		public void SetMood(int state)
		{
			person_.Expression.MakeNeutral();

			switch (state)
			{
				case Mood.Idle:
				{
					person_.Expression.Set(new Pair<int, float>[]
					{
						new Pair<int, float>(Expressions.Happy, 0.5f),
						new Pair<int, float>(Expressions.Mischievous, 0.0f)
					});

					break;
				}

				case Mood.Happy:
				{
					person_.Expression.Set(new Pair<int, float>[]
					{
						new Pair<int, float>(Expressions.Happy, 1.0f),
						new Pair<int, float>(Expressions.Mischievous, 0.0f)
					});

					break;
				}
			}
		}
	}


	class QuirkyPersonality : IPersonality
	{
		private readonly Person person_;

		public QuirkyPersonality(Person p)
		{
			person_ = p;
		}

		public void SetMood(int state)
		{
			person_.Expression.MakeNeutral();

			switch (state)
			{
				case Mood.Idle:
				{
					person_.Expression.Set(new Pair<int, float>[]
					{
						new Pair<int, float>(Expressions.Happy, 0.2f),
						new Pair<int, float>(Expressions.Mischievous, 0.4f)
					});

					break;
				}

				case Mood.Happy:
				{
					person_.Expression.Set(new Pair<int, float>[]
					{
						new Pair<int, float>(Expressions.Happy, 0.4f),
						new Pair<int, float>(Expressions.Mischievous, 1.0f)
					});

					break;
				}
			}
		}
	}
}
