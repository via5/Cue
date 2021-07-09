namespace Cue
{
	static class PSE
	{
		// sliding durations
		public const int GazeDuration = 0;
		public const int SlidingDurationCount = 1;

		// bools
		public const int AvoidGazePlayer = 0;
		public const int AvoidGazeInsidePersonalSpace = 1;
		public const int AvoidGazeDuringSex = 2;
		public const int AvoidGazeDuringSexOthers = 3;
		public const int BoolCount = 4;

		// floats
		public const int MaxExcitementForAvoid = 0;
		public const int AvoidDelayAfterOrgasm = 1;
		public const int MaxEnergyForRandomGaze = 2;
		public const int OtherSexExcitementRate = 3;
		public const int MaxOtherSexExcitement = 4;
		public const int NaturalRandomWeight = 5;
		public const int BlowjobEyesWeight = 6;
		public const int BlowjobGenitalsWeight = 7;
		public const int HandjobEyesWeight = 8;
		public const int HandjobGenitalsWeight = 9;
		public const int PenetrationEyesWeight = 10;
		public const int PenetrationChestWeight = 11;
		public const int PenetrationGenitalsWeight = 12;
		public const int GropedEyesWeight = 13;
		public const int GropedChestWeight = 14;
		public const int GropedGenitalsWeight = 15;
		public const int OtherSexEyesWeight = 16;
		public const int NaturalOtherEyesWeight = 17;
		public const int BusyOtherEyesWeight = 18;
		public const int LookAboveMaxWeight = 19;
		public const int LookAboveMaxWeightOrgasm = 20;
		public const int GazeRandomIntervalMinimum = 21;
		public const int GazeRandomIntervalMaximum = 22;
		public const int FloatCount = 23;

		// strings
		public const int StringCount = 0;

		// states
		public const int IdleState = 0;
		public const int CloseState = 1;
		public const int StateCount = 2;


		private static string[] slidingDurationNames_ = new string[]
		{
			"gazeDuration"
		};

		public static int SlidingDurationFromString(string s)
		{
			for (int i = 0; i < slidingDurationNames_.Length; ++i)
			{
				if (slidingDurationNames_[i] == s)
					return i;
			}

			return -1;
		}

		public static string SlidingDurationToString(int i)
		{
			return slidingDurationNames_[i];
		}


		private static string[] boolNames_ = new string[]
		{
			"avoidGazePlayer", "avoidGazeInsidePersonalSpace",
			"avoidGazeDuringSex", "avoidGazeDuringSexOthers"
		};

		public static int BoolFromString(string s)
		{
			for (int i = 0; i < boolNames_.Length; ++i)
			{
				if (boolNames_[i] == s)
					return i;
			}

			return -1;
		}

		public static string BoolToString(int i)
		{
			return boolNames_[i];
		}


		private static string[] floatNames_ = new string[]
		{
			"maxExcitementForAvoid", "avoidDelayAfterOrgasm",
			"maxEnergyForRandomGaze", "otherSexExcitementRate",
			"maxOtherSexExcitement", "naturalRandomWeight", "blowjobEyesWeight",
			"blowjobGenitalsWeight", "handjobEyesWeight",
			"handjobGenitalsWeight", "penetrationEyesWeight",
			"penetrationChestWeight", "penetrationGenitalsWeight",
			"gropedEyesWeight", "gropedChestWeight", "gropedGenitalsWeight",
			"otherSexEyesWeight", "naturalOtherEyesWeight",
			"busyOtherEyesWeight", "lookAboveMaxWeight",
			"lookAboveMaxWeightOrgasm", "gazeRandomIntervalMinimum",
			"gazeRandomIntervalMaximum"
		};

		public static int FloatFromString(string s)
		{
			for (int i = 0; i < floatNames_.Length; ++i)
			{
				if (floatNames_[i] == s)
					return i;
			}

			return -1;
		}

		public static string FloatToString(int i)
		{
			return floatNames_[i];
		}



		private static string[] stringNames_ = new string[]
		{
		};

		public static int StringFromString(string s)
		{
			for (int i = 0; i < stringNames_.Length; ++i)
			{
				if (stringNames_[i] == s)
					return i;
			}

			return -1;
		}

		public static string StringToString(int i)
		{
			return stringNames_[i];
		}


		private static string[] stateNames_ = new string[]
		{
			"idle", "close"
		};

		public static string[] StateNames
		{
			get { return stateNames_; }
		}

		public static int StateFromString(string s)
		{
			for (int i = 0; i < stateNames_.Length; ++i)
			{
				if (stateNames_[i] == s)
					return i;
			}

			return -1;
		}

		public static string StateToString(int i)
		{
			return stateNames_[i];
		}
	}


	class Personality
	{
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

		public class State
		{
			public readonly string name;

			public bool[] bools;
			public float[] floats;
			public string[] strings;
			public SlidingDuration[] slidingDurations;
			public ExpressionIntensity[] expressions;

			public State(int i)
			{
				name = PSE.StateToString(i);
				bools = new bool[PSE.BoolCount];
				floats = new float[PSE.FloatCount];
				strings = new string[PSE.StringCount];
				slidingDurations = new SlidingDuration[PSE.SlidingDurationCount];
				expressions = new ExpressionIntensity[0];
			}
		}


		private readonly string name_;
		private Person person_;

		private bool wasClose_ = false;
		private bool inited_ = false;
		private bool forcedClose_ = false;
		private bool isCloseForced_ = false;

		private State[] states_ = new State[PSE.StateCount];

		public Personality(string name, Person p = null)
		{
			name_ = name;
			person_ = p;

			for (int i = 0; i < PSE.StateCount; ++i)
				states_[i] = new State(i);
		}

		public Personality Clone(string newName, Person p)
		{
			var ps = new Personality(newName ?? name_, p);

			for (int si = 0; si < states_.Length; ++si)
			{
				for (int i = 0; i < states_[si].bools.Length; ++i)
					ps.states_[si].bools[i] = states_[si].bools[i];

				for (int i = 0; i < states_[si].floats.Length; ++i)
					ps.states_[si].floats[i] = states_[si].floats[i];

				for (int i = 0; i < states_[si].strings.Length; ++i)
					ps.states_[si].strings[i] = states_[si].strings[i];

				for (int i = 0; i < states_[si].slidingDurations.Length; ++i)
					ps.states_[si].slidingDurations[i] = new SlidingDuration(states_[si].slidingDurations[i]);

				ps.states_[si].expressions = new ExpressionIntensity[states_[si].expressions.Length];
				for (int i = 0; i < states_[si].expressions.Length; ++i)
					ps.states_[si].expressions[i] = new ExpressionIntensity(states_[si].expressions[i]);
			}

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
					return states_[PSE.CloseState];
				else
					return states_[PSE.IdleState];
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

		public SlidingDuration GetSlidingDuration(int i)
		{
			return CurrentState.slidingDurations[i];
		}

		public void Set(State[] ss)
		{
			states_ = ss;
		}

		public bool GetBool(int i)
		{
			return CurrentState.bools[i];
		}

		public float Get(int i)
		{
			return CurrentState.floats[i];
		}

		public string GetString(int i)
		{
			return CurrentState.strings[i];
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

			for (int i = 0; i < CurrentState.slidingDurations.Length; ++i)
			{
				GetSlidingDuration(i).WindowMagnitude = person_.Mood.Energy;
				GetSlidingDuration(i).Update(s);
			}
		}

		public override string ToString()
		{
			return $"{Name}";
		}

		private void Init()
		{
			SetClose(false);
		}

		private void SetClose(bool b)
		{
			State s;

			if (b)
				s = states_[PSE.CloseState];
			else
				s = states_[PSE.IdleState];

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
