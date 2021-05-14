namespace Cue
{
	interface IPersonality
	{
		Pair<float, float> LookAtRandomInterval { get; }
		Pair<float, float> LookAtRandomGazeDuration { get; }
		float GazeDuration { get; }

		string StateString{ get; }
		Sensitivity Sensitivity { get; }
		void Update(float s);
	}


	class Sensitivity
	{
		private Person person_;

		private float change_ = 0;

		private float mouthRate_ = 0.001f;
		private float breastsRate_ = 0.01f;
		private float genitalsRate_ = 0.1f;
		private float decayRate_ = -0.1f;
		private float rateAdjust_ = 0.1f;


		public Sensitivity(Person p)
		{
			person_ = p;
		}

		public float Change { get { return change_; } }
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

			change_ = rate * s * rateAdjust_;
		}

		public override string ToString()
		{
			string s = "";

			s += "change=";

			if (change_ < 0)
				s += "-";
			else
				s += "+";

			s += change_.ToString("0.000");

			return s;
		}
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

		public virtual void Update(float s)
		{
			if (!inited_)
			{
				Init();
				inited_ = true;
			}

			sensitivity_.Update(s);
			person_.Excitement.Value += sensitivity_.Change;
			person_.Breathing.Intensity = person_.Excitement.Value;
			person_.Expression.Set(Expressions.Pleasure, person_.Excitement.Value);

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
				if (person_.CanMoveHead)
					person_.Gaze.LookAt(Cue.Instance.Player);

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

		protected override void SetClose(bool b)
		{
			if (b)
			{
				StateString = "angry";

				if (person_.CanMoveHead)
					person_.Gaze.Avoid(Cue.Instance.Player);

				SetAngry();
				angry_ = true;
			}
			else
			{
				StateString = "idle";

				if (person_.CanMoveHead)
					person_.Gaze.LookAtRandom();

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
