namespace Cue
{
	interface IPersonality
	{
		Pair<float, float> LookAtRandomInterval { get; }
		Pair<float, float> LookAtRandomGazeDuration { get; }
		float GazeDuration { get; }

		bool AvoidGazeInsidePersonalSpace { get; }
		bool AvoidGazeDuringSex { get; }
		bool AvoidGazeDuringSexOthers { get; }

		float NaturalRandomWeight { get; }
		float BlowjobEyesWeight { get; }
		float BlowjobGenitalsWeight { get; }
		float HandjobEyesWeight { get; }
		float HandjobGenitalsWeight { get; }
		float PenetrationEyesWeight { get; }
		float PenetrationChestWeight { get; }
		float PenetrationGenitalsWeight { get; }
		float GropedEyesWeight { get; }
		float GropedChestWeight { get; }
		float GropedGenitalsWeight { get; }
		float OtherSexEyesWeight { get; }

		string Name { get; }
		string StateString{ get; }
		void Update(float s);
	}


	abstract class BasicPersonality : IPersonality
	{
		protected readonly Person person_;
		private readonly string name_;
		private string state_ = "idle";
		private bool wasClose_ = false;
		private bool inited_ = false;

		public BasicPersonality(Person p, string name)
		{
			person_ = p;
			name_ = name;
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

		public bool AvoidGazeInsidePersonalSpace { get { return false; } }
		public bool AvoidGazeDuringSex { get { return false; } }
		public bool AvoidGazeDuringSexOthers { get { return false; } }

		public float NaturalRandomWeight { get { return 0.05f; } }

		public float BlowjobEyesWeight { get { return 0.1f; } }
		public float BlowjobGenitalsWeight { get { return 1; } }

		public float HandjobEyesWeight { get { return 1; } }
		public float HandjobGenitalsWeight { get { return 0.2f; } }

		public float PenetrationEyesWeight { get { return 1; } }
		public float PenetrationChestWeight { get { return 0; } }
		public float PenetrationGenitalsWeight { get { return 0.2f; } }

		public float GropedEyesWeight { get { return 1; } }
		public float GropedChestWeight { get { return 0.2f; } }
		public float GropedGenitalsWeight { get { return 0.2f; } }

		public float OtherSexEyesWeight { get { return 0.2f; } }

		public virtual void Update(float s)
		{
			if (!inited_)
			{
				Init();
				inited_ = true;
			}

			bool close = person_.Body.InsidePersonalSpace(Cue.Instance.Player);
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
					return new Pair<float, float>(3, 8);
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
