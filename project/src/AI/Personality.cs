namespace Cue
{
	interface IPersonality
	{
		void SetMood(int state);
		void SetExcitement(float f);
	}


	abstract class BasicPersonality : IPersonality
	{
		public struct Expression
		{
			public int mood;
			public ExpressionIntensity[] intensities;

			public Expression(int mood, ExpressionIntensity[] intensities)
			{
				this.mood = mood;
				this.intensities = intensities;
			}
		}


		protected readonly Person person_;
		private Expression[] expressions_;

		public BasicPersonality(Person p, Expression[] e)
		{
			person_ = p;
			expressions_ = e;
		}

		public void SetExcitement(float f)
		{
			person_.Breathing.Intensity = f;
			person_.Expression.Set(Expressions.Pleasure, f);
		}

		public void SetMood(int mood)
		{
			//person_.Expression.MakeNeutral();

			for (int i = 0; i < expressions_.Length; ++i)
			{
				if (expressions_[i].mood == mood)
				{
					person_.Expression.Set(expressions_[i].intensities);
					return;
				}
			}
		}
	}


	class NeutralPersonality : BasicPersonality
	{
		public NeutralPersonality(Person p)
			: base(p, GetExpressions())
		{
		}

		private static Expression[] GetExpressions()
		{
			return new Expression[]
			{
				new Expression(Mood.Idle, new ExpressionIntensity[]
				{
					new ExpressionIntensity(Expressions.Happy, 0.5f),
					new ExpressionIntensity(Expressions.Mischievous, 0.0f)
				}),

				new Expression(Mood.Happy, new ExpressionIntensity[]
				{
					new ExpressionIntensity(Expressions.Happy, 0.7f),
					new ExpressionIntensity(Expressions.Mischievous, 0.0f)
				})
			};
		}

		public override string ToString()
		{
			return "neutral";
		}
	}


	class QuirkyPersonality : BasicPersonality
	{
		public QuirkyPersonality(Person p)
			: base(p, GetExpressions())
		{
		}

		private static Expression[] GetExpressions()
		{
			return new Expression[]
			{
				new Expression(Mood.Idle, new ExpressionIntensity[]
				{
					new ExpressionIntensity(Expressions.Happy, 0.2f),
					new ExpressionIntensity(Expressions.Mischievous, 0.4f)
				}),

				new Expression(Mood.Happy, new ExpressionIntensity[]
				{
					new ExpressionIntensity(Expressions.Happy, 0.4f),
					new ExpressionIntensity(Expressions.Mischievous, 1.0f)
				})
			};
		}

		public override string ToString()
		{
			return "quirky";
		}
	}
}
