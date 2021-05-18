using System.Security.Policy;

namespace Cue
{
	interface IPersonality
	{
		Pair<float, float> LookAtRandomInterval { get; }
		Pair<float, float> LookAtRandomGazeDuration { get; }
		float GazeDuration { get; }
		float GazeRandomTargetWeight(int targetType);
		IObject GazeAvoid();

		string Name { get; }
		string StateString{ get; }
		Sensitivity Sensitivity { get; }
		void Update(float s);
	}


	class Sensitivity
	{
		private Person person_;

		public Sensitivity(Person p)
		{
			person_ = p;
		}

		public float MouthRate { get { return 0.1f; } }
		public float MouthMax { get { return 0.05f; } }

		public float BreastsRate { get { return 0.01f; } }
		public float BreastsMax { get { return 0.1f; } }

		public float GenitalsRate { get { return 0.06f; } }
		public float GenitalsMax { get { return 0.3f; } }

		public float PenetrationRate { get { return 0.05f; } }
		public float PenetrationMax { get { return 1.0f; } }

		public float DecayPerSecond { get { return -0.1f; } }
		public float ExcitementPostOrgasm { get { return 0.0f; } }
		public float DelayPostOrgasm { get { return 10; } }
	}


	abstract class BasicPersonality : IPersonality
	{
		protected readonly Person person_;
		private readonly string name_;
		private Sensitivity sensitivity_;
		private string state_ = "idle";
		private bool wasClose_ = false;
		private bool inited_ = false;

		public BasicPersonality(Person p, string name)
		{
			person_ = p;
			name_ = name;
			sensitivity_ = new Sensitivity(p);
		}

		public string Name
		{
			get { return name_; }
		}

		public string StateString
		{
			get { return state_; }
			protected set { state_ = value; }
		}

		public Sensitivity Sensitivity
		{
			get { return sensitivity_; }
		}

		public virtual Pair<float, float> LookAtRandomInterval
		{
			get { return new Pair<float, float>(4, 10); }
		}

		public virtual Pair<float, float> LookAtRandomGazeDuration
		{
			get { return new Pair<float, float>(1, 3); }
		}

		public virtual float GazeDuration
		{
			get { return 1; }
		}

		public virtual IObject GazeAvoid()
		{
			return null;
		}

		public virtual float GazeRandomTargetWeight(int targetType)
		{
			switch (targetType)
			{
				case RandomTargetTypes.Sex: return 5;
				case RandomTargetTypes.Body: return 2;
				case RandomTargetTypes.Eyes: return 5;
				case RandomTargetTypes.Random: return 1;
				default: return 1;
			}
		}

		public virtual void Update(float s)
		{
			if (!inited_)
			{
				Init();
				inited_ = true;
			}

			bool close = person_.Body.PlayerIsClose;
			if (close != wasClose_)
			{
				person_.Log.Info("Personality: " + (close ? "now close" : "now far"));
				SetClose(close);
				wasClose_ = close;
			}
		}

		public override string ToString()
		{
			return $"{Name} {state_}";
		}

		protected abstract void Init();
		protected abstract void SetClose(bool b);
	}


	abstract class StandardPersonality : BasicPersonality
	{
		protected StandardPersonality(Person p, string name)
			: base(p, name)
		{
		}

		protected override void Init()
		{
			SetIdle();
		}

		protected override void SetClose(bool b)
		{
			if (b)
			{
				StateString = "happy";
				SetHappy();
			}
			else
			{
				StateString = "idle";
				SetIdle();
			}
		}

		protected abstract void SetIdle();
		protected abstract void SetHappy();
	}


	class NeutralPersonality : StandardPersonality
	{
		public NeutralPersonality(Person p)
			: base(p, "neutral")
		{
		}

		protected override void SetIdle()
		{
			person_.Expression.Set(new ExpressionIntensity[]
			{
				new ExpressionIntensity(Expressions.Happy, 0.5f),
				new ExpressionIntensity(Expressions.Mischievous, 0.0f)
			});
		}

		protected override void SetHappy()
		{
			person_.Expression.Set(new ExpressionIntensity[]
			{
				new ExpressionIntensity(Expressions.Happy, 0.5f),
				new ExpressionIntensity(Expressions.Mischievous, 0.0f)
			});
		}
	}


	class QuirkyPersonality : StandardPersonality
	{
		public QuirkyPersonality(Person p)
			: base(p, "quirky")
		{
		}

		protected override void SetIdle()
		{
			person_.Expression.Set(new ExpressionIntensity[]
			{
					new ExpressionIntensity(Expressions.Happy, 0.2f),
					new ExpressionIntensity(Expressions.Mischievous, 0.6f)
			});
		}

		protected override void SetHappy()
		{
			person_.Expression.Set(new ExpressionIntensity[]
			{
					new ExpressionIntensity(Expressions.Happy, 0.4f),
					new ExpressionIntensity(Expressions.Mischievous, 1.0f)
			});
		}
	}


	class TsunderePersonality : BasicPersonality
	{
		private bool angry_ = false;

		public TsunderePersonality(Person p)
			: base(p, "tsundere")
		{
		}

		protected override void Init()
		{
			SetIdle();
		}

		public override Pair<float, float> LookAtRandomInterval
		{
			get
			{
				if (angry_)
					return new Pair<float, float>(1, 5);
				else
					return base.LookAtRandomInterval;
			}
		}

		public override Pair<float, float> LookAtRandomGazeDuration
		{
			get
			{
				if (angry_)
					return new Pair<float, float>(0.3f, 1.5f);
				else
					return new Pair<float, float>(1, 3);
			}
		}

		public override float GazeRandomTargetWeight(int targetType)
		{
			switch (targetType)
			{
				case RandomTargetTypes.Sex: return 1;
				case RandomTargetTypes.Body: return 2;
				case RandomTargetTypes.Eyes: return 5;
				case RandomTargetTypes.Random: return 10;
				default: return 1;
			}
		}

		public override IObject GazeAvoid()
		{
			if (angry_)
				return Cue.Instance.Player;

			return null;
		}

		protected override void SetClose(bool b)
		{
			if (b)
			{
				StateString = "angry";
				SetAngry();
				angry_ = true;
			}
			else
			{
				StateString = "idle";
				SetIdle();
				angry_ = false;
			}
		}

		private void SetIdle()
		{
			person_.Expression.Set(new ExpressionIntensity[]
			{
				new ExpressionIntensity(Expressions.Happy, 0.0f),
				new ExpressionIntensity(Expressions.Mischievous, 0.2f),
				new ExpressionIntensity(Expressions.Angry, 0.3f)
			});
		}

		private void SetAngry()
		{
			person_.Expression.Set(new ExpressionIntensity[]
			{
				new ExpressionIntensity(Expressions.Happy, 0.0f),
				new ExpressionIntensity(Expressions.Mischievous, 0.0f),
				new ExpressionIntensity(Expressions.Angry, 1.0f)
			});
		}
	}
}
