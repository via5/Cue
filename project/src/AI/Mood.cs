﻿namespace Cue
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
		private bool updateExcitement_ = true;

		private float mouthRate_ = 0.001f;
		private float breastsRate_ = 0.01f;
		private float genitalsRate_ = 0.1f;
		private float decayRate_ = -0.1f;
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

		public string StateString
		{
			get
			{
				switch (state_)
				{
					case None:
						return "(none)";

					case Idle:
						return "idle";

					case Happy:
						return "happy";

					default:
						return $"?{state_}";
				}
			}
		}

		public float ForceExcitement
		{
			set
			{
				if (value < 0)
				{
					updateExcitement_ = true;
				}
				else
				{
					updateExcitement_ = false;
					excitement_ = value;
				}
			}
		}

		public float Excitement { get { return excitement_; } }
		public float LastRate { get { return lastRate_; } }
		public float MouthRate { get { return mouthRate_; } }
		public float BreastsRate { get { return breastsRate_; } }
		public float GenitalsRate { get { return genitalsRate_; } }
		public float DecayRate { get { return decayRate_; } }
		public float RateAdjust { get { return rateAdjust_; } }

		public void Update(float s)
		{
			float rate = 0;

			rate += person_.Excitement.Genitals * genitalsRate_;
			rate += person_.Excitement.Mouth * mouthRate_;
			rate += person_.Excitement.Breasts * breastsRate_;

			if (rate == 0)
				rate = decayRate_;

			if (updateExcitement_)
			{
				excitement_ = U.Clamp(excitement_ + rate * s * rateAdjust_, 0, 1);

				if (excitement_ >= 1)
				{
					person_.Orgasmer.Orgasm();
					excitement_ = 0;
				}
			}

			person_.Personality.SetExcitement(excitement_);
			lastRate_ = rate;
		}

		public void OnPluginState(bool b)
		{
		}

		public override string ToString()
		{
			string s = "";

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
		void SetExcitement(float f);
	}


	abstract class BasicPersonality : IPersonality
	{
		protected readonly Person person_;

		public BasicPersonality(Person p)
		{
			person_ = p;
		}

		public void SetExcitement(float f)
		{
			person_.Breathing.Intensity = f;
			person_.Expression.Set(new Pair<int, float>[]
			{
				new Pair<int, float>(Expressions.Pleasure, f)
			});
		}

		public abstract void SetMood(int state);
	}


	class NeutralPersonality : BasicPersonality
	{
		public NeutralPersonality(Person p)
			: base(p)
		{
		}

		public override void SetMood(int state)
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

		public override string ToString()
		{
			return "neutral";
		}
	}


	class QuirkyPersonality : BasicPersonality
	{
		public QuirkyPersonality(Person p)
			: base(p)
		{
		}

		public override void SetMood(int state)
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

		public override string ToString()
		{
			return "quirky";
		}
	}
}