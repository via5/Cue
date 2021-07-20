namespace Cue
{
	class Personality
	{
		public const int IdleState = 0;
		public const int CloseState = 1;
		public const int StateCount = 2;


		public class ExpressionIntensity
		{
			public int type = -1;
			public float intensity = 0;

			public ExpressionIntensity()
			{
			}

			public ExpressionIntensity(ExpressionIntensity o)
			{
				type = o.type;
				intensity = o.intensity;
			}
		}

		public class State : EnumValueManager
		{
			public readonly string name;

			public ExpressionIntensity[] expressions;

			public State(int i)
				: base(new PSE())
			{
				name = StateToString(i);
				expressions = new ExpressionIntensity[0];
			}

			public void CopyFrom(State s)
			{
				base.CopyFrom(s);

				expressions = new ExpressionIntensity[s.expressions.Length];
				for (int i = 0; i < s.expressions.Length; ++i)
					expressions[i] = new ExpressionIntensity(s.expressions[i]);
			}
		}


		private readonly string name_;
		private Person person_;

		private bool wasClose_ = false;
		private bool inited_ = false;
		private bool forcedClose_ = false;
		private bool isCloseForced_ = false;

		private State[] states_ = new State[StateCount];

		public Personality(string name, Person p = null)
		{
			name_ = name;
			person_ = p;

			for (int i = 0; i < StateCount; ++i)
				states_[i] = new State(i);
		}

		public Personality Clone(string newName, Person p)
		{
			var ps = new Personality(newName ?? name_, p);

			for (int si = 0; si < states_.Length; ++si)
				ps.states_[si].CopyFrom(states_[si]);

			return ps;
		}

		public string Name
		{
			get { return name_; }
		}

		public State[] States
		{
			get { return states_; }
		}

		public State GetState(int i)
		{
			return states_[i];
		}

		private State CurrentState
		{
			get
			{
				if (wasClose_)
					return states_[CloseState];
				else
					return states_[IdleState];
			}
		}

		public Pair<float, float> LookAtRandomInterval
		{
			get
			{
				return new Pair<float, float>(
					Get(PSE.GazeRandomIntervalMinimum),
					Get(PSE.GazeRandomIntervalMaximum));
			}
		}

		public float GazeDuration
		{
			get { return GetSlidingDuration(PSE.GazeDuration).Current; }
		}

		public void Set(State[] ss)
		{
			states_ = ss;
		}

		public SlidingDuration GetSlidingDuration(int i)
		{
			return CurrentState.GetSlidingDuration(i);
		}

		public bool GetBool(int i)
		{
			return CurrentState.GetBool(i);
		}

		public float Get(int i)
		{
			return CurrentState.Get(i);
		}

		public string GetString(int i)
		{
			return CurrentState.GetString(i);
		}

		public virtual void Update(float s)
		{
			if (!inited_)
			{
				Init();
				inited_ = true;
			}

			bool close = false;

			if (Cue.Instance.Player != null)
			{
				close =
					person_.Body.InsidePersonalSpace(Cue.Instance.Player) ||
					person_.Kisser.Target == Cue.Instance.Player;
			}

			if (close != wasClose_)
			{
				person_.Log.Info("Personality: " + (close ? "now close" : "now far"));
				DoSetClose(close);
				wasClose_ = close;
			}

			for (int i = 0; i < PSE.SlidingDurationCount; ++i)
			{
				GetSlidingDuration(i).WindowMagnitude = person_.Mood.Energy;
				GetSlidingDuration(i).Update(s);
			}
		}

		public override string ToString()
		{
			return $"{Name}";
		}

		public static string StateToString(int s)
		{
			if (s < 0 || s >= StateCount)
				return $"?{s}";

			return StateNames[s];
		}

		public static string[] StateNames
		{
			get { return new string[] { "idleState", "closeState" }; }
		}


		private void Init()
		{
			SetClose(false);
		}

		private void SetClose(bool b)
		{
			State s;

			if (b)
				s = states_[CloseState];
			else
				s = states_[IdleState];

			for (int i = 0; i < s.expressions.Length; ++i)
			{
				person_.Expression.SetIntensity(
					s.expressions[i].type,
					s.expressions[i].intensity);
			}
		}

		public void ForceSetClose(bool enabled, bool close)
		{
			isCloseForced_ = enabled;
			forcedClose_ = close;
			DoSetClose(close);
		}

		private void DoSetClose(bool b)
		{
			if (isCloseForced_)
				SetClose(forcedClose_);
			else
				SetClose(b);
		}
	}
}
