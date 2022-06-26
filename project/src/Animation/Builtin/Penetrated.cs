namespace Cue
{
	class PenetratedAnimation : BuiltinAnimation
	{
		struct Config
		{
			public float lookUpChance;
			public float postReactionChance;
			public Pair<float, float> reactionTimeRange;
			public Pair<float, float> reactionHoldTimeRange;
			public Pair<float, float> postReactionTimeRange;
			public Pair<float, float> postReactionHoldTimeRange;
		}

		struct Settings
		{
			public bool lookUp;
			public bool doPostReaction;
			public float reactionTime;
			public float reactionHoldTime;
			public float postReactionTime;
			public float postReactionHoldTime;
			public Expression[] reaction;
			public float[] reactionTargets;
			public Expression[] postReaction;
			public float[] postReactionTargets;
		}

		private const int NoState = 0;
		private const int ReactionState = 1;
		private const int ReactionHoldState = 2;
		private const int PostReactionState = 3;
		private const int PostReactionHoldState = 4;
		private const int DoneState = 5;

		private float elapsed_ = 0;
		private int state_ = NoState;
		private Config config_;
		private Settings settings_;
		private Expression[] expressions_ = null;

		public PenetratedAnimation()
			: base("cuePenetrated")
		{
		}

		public override BuiltinAnimation Clone()
		{
			var a = new PenetratedAnimation();
			a.CopyFrom(this);
			return a;
		}

		public override bool Done
		{
			get { return (state_ == DoneState); }
		}

		public override bool Start(Person p, AnimationContext cx)
		{
			if (!base.Start(p, cx))
				return false;

			elapsed_ = 0;
			state_ = ReactionState;
			Person.Voice.MouthEnabled = false;

			expressions_ = Person.Expression.GetExpressionsForMood(MoodType.Excited);

			config_ = MakeConfig();
			settings_ = MakeSettings(expressions_, config_);

			Person.Expression.Disable();

			StartLookUp();

			for (int i = 0; i < settings_.reaction.Length; ++i)
				settings_.reaction[i].SetTarget(settings_.reactionTargets[i], settings_.reactionTime);

			return true;
		}

		private static Config MakeConfig()
		{
			Config c;

			c.lookUpChance = 0.75f;
			c.postReactionChance = 0.75f;
			c.reactionTimeRange = new Pair<float, float>(0.5f, 1.5f);
			c.reactionHoldTimeRange = new Pair<float, float>(0.5f, 3);
			c.postReactionTimeRange = new Pair<float, float>(0.5f, 2);
			c.postReactionHoldTimeRange = new Pair<float, float>(0.5f, 2);

			return c;
		}

		private static Settings MakeSettings(Expression[] expressions, Config c)
		{
			Settings s;

			s.lookUp = U.RandomBool(c.lookUpChance);
			s.doPostReaction = U.RandomBool(c.postReactionChance);
			s.reactionTime = U.RandomFloat(c.reactionTimeRange);
			s.reactionHoldTime = U.RandomFloat(c.reactionHoldTimeRange);
			s.postReactionTime = U.RandomFloat(c.postReactionTimeRange);
			s.postReactionHoldTime = U.RandomFloat(c.postReactionHoldTimeRange);

			U.Shuffle(expressions);

			{
				int av = s.doPostReaction ?
					expressions.Length - 1 :
					expressions.Length;

				int count = U.RandomInt(1, av);

				s.reaction = new Expression[count];
				s.reactionTargets = new float[count];

				for (int i = 0; i < s.reaction.Length; ++i)
					s.reaction[i] = expressions[i];

				float remaining = 1;
				for (int i = 0; i < s.reactionTargets.Length; ++i)
				{
					var f = U.RandomFloat(0, 1);
					s.reactionTargets[i] = f;
					remaining = U.Clamp(remaining - f, 0, 1);
				}

				s.reactionTargets[s.reactionTargets.Length - 1] += remaining;
			}

			if (s.doPostReaction)
			{
				int av = expressions.Length - s.reaction.Length;
				int count = U.RandomInt(1, av);

				s.postReaction = new Expression[count];
				s.postReactionTargets = new float[count];

				for (int i = 0; i < s.postReaction.Length; ++i)
					s.postReaction[i] = expressions[s.reaction.Length + i];

				float remaining = 1;
				for (int i = 0; i < s.postReactionTargets.Length; ++i)
				{
					var f = U.RandomFloat(0, 1);
					s.postReactionTargets[i] = f;
					remaining = U.Clamp(remaining - f, 0, 1);
				}

				s.postReactionTargets[s.postReactionTargets.Length - 1] += remaining;
			}
			else
			{
				s.postReaction = new Expression[0];
				s.postReactionTargets = new float[0];
				s.reactionHoldTime = c.reactionHoldTimeRange.second;
			}

			return s;
		}

		private void StartLookUp()
		{
			if (settings_.lookUp)
			{
				Person.Gaze.Picker.ForcedTarget = Person.Gaze.Targets.LookatAbove;
				Person.Gaze.Gazer.Duration = settings_.reactionTime;
			}
		}

		private void StopLookUp()
		{
			if (settings_.lookUp)
			{
				Person.Gaze.Picker.ForcedTarget = null;
				Person.Gaze.Gazer.Duration = settings_.postReactionTime;
			}
		}

		private void Restore()
		{
			Person.Voice.MouthEnabled = true;
			Person.Expression.Enable();
		}

		public override void Reset()
		{
			base.Reset();
			elapsed_ = 0;
			StopLookUp();
			Restore();
		}

		private void Finish()
		{
			elapsed_ = 0;
			state_ = DoneState;
			Restore();
		}

		private void StartPostReaction()
		{
			state_ = PostReactionState;

			for (int i = 0; i < expressions_.Length; ++i)
				expressions_[i].SetTarget(0, settings_.postReactionTime);

			for (int i = 0; i < settings_.postReaction.Length; ++i)
				settings_.postReaction[i].SetTarget(settings_.postReactionTargets[i], settings_.postReactionTime);
		}

		public override void FixedUpdate(float s)
		{
			elapsed_ += s;

			for (int i = 0; i < expressions_.Length; ++i)
				expressions_[i].FixedUpdate(s);

			switch (state_)
			{
				case ReactionState:
				{
					if (elapsed_ >= settings_.reactionTime)
					{
						elapsed_ = 0;
						state_ = ReactionHoldState;
					}

					break;
				}

				case ReactionHoldState:
				{
					if (elapsed_ >= settings_.reactionHoldTime)
					{
						elapsed_ = 0;

						if (settings_.doPostReaction)
							StartPostReaction();
						else
							Finish();

						StopLookUp();
					}

					break;
				}

				case PostReactionState:
				{
					if (elapsed_ >= settings_.postReactionTime)
					{
						elapsed_ = 0;
						state_ = PostReactionHoldState;
					}

					break;
				}

				case PostReactionHoldState:
				{
					if (elapsed_ >= settings_.postReactionHoldTime)
					{
						elapsed_ = 0;
						Finish();
					}

					break;
				}
			}
		}

		public override string[] Debug()
		{
			return new string[]
			{
				$"lookup            {DebugBool(settings_.lookUp, config_.lookUpChance)}",
				$"doPostReaction    {DebugBool(settings_.doPostReaction, config_.postReactionChance)}",
				$"reaction          {DebugTimes(settings_.reactionTime, config_.reactionTimeRange)}",
				$"reactionHold      {DebugTimes(settings_.reactionHoldTime, config_.reactionTimeRange)}",
				$"postReaction      {DebugTimes(settings_.postReactionTime, config_.postReactionTimeRange)}",
				$"postReactionHold  {DebugTimes(settings_.postReactionHoldTime, config_.postReactionHoldTimeRange)}",
				$"reactions         {DebugExpressions(settings_.reaction, settings_.reactionTargets)}",
				$"postReactions     {DebugExpressions(settings_.postReaction, settings_.postReactionTargets)}"
			};
		}

		private string DebugBool(bool b, float chance)
		{
			return $"{b} {(int)(chance * 100)}%";
		}

		private string DebugTimes(float time, Pair<float, float> range)
		{
			return $"{time:0.00}s ({range.first:0.00},{range.second:0.00})";
		}

		private string DebugExpressions(Expression[] es, float[] targets)
		{
			string s = "";

			if (es != null)
			{
				for (int i = 0; i < es.Length; ++i)
				{
					if (s != "")
						s += ",";

					s += $"{es[i].Name}({targets[i]:0.00})";
				}
			}

			return s;
		}
	}
}
