namespace Cue
{
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

				default:
				{
					person_.Expression.Set(new Pair<int, float>[] { }, true);
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

				default:
				{
					person_.Expression.Set(new Pair<int, float>[] { }, true);
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
