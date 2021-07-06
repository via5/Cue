namespace Cue
{
	class Mood
	{
		public const float NoOrgasm = 10000;

		public const int NormalState = 1;
		public const int OrgasmState = 2;
		public const int PostOrgasmState = 3;

		private readonly Person person_;
		private int state_ = NormalState;
		private float elapsed_ = 0;
		private float timeSinceLastOrgasm_ = NoOrgasm;

		private ForceableValue excitement_ = new ForceableValue();
		private ForceableValue tiredness_ = new ForceableValue();

		public Mood(Person p)
		{
			person_ = p;
		}

		public int State
		{
			get { return state_; }
		}

		public string StateString
		{
			get
			{
				switch (state_)
				{
					case NormalState:
						return "normal";

					case OrgasmState:
						return "orgasm";

					case PostOrgasmState:
						return "post orgasm";

					default:
						return $"?{state_}";
				}
			}
		}

		public float TimeSinceLastOrgasm
		{
			get { return timeSinceLastOrgasm_; }
		}

		public float Excitement
		{
			get { return excitement_.Value; }
		}

		public ForceableValue ExcitementValue
		{
			get { return excitement_; }
		}

		public float Tiredness
		{
			get { return tiredness_.Value; }
		}

		public ForceableValue TirednessValue
		{
			get { return tiredness_; }
		}

		public void ForceOrgasm()
		{
			DoOrgasm();
		}

		public void Update(float s)
		{
			elapsed_ += s;

			excitement_.Value = person_.Excitement.Value;

			person_.Breathing.Intensity = Excitement;
			person_.Expression.Set(Expressions.Pleasure, Excitement);

			if (excitement_.UnforcedValue >= 1)
				DoOrgasm();

			switch (state_)
			{
				case NormalState:
				{
					timeSinceLastOrgasm_ += s;
					break;
				}

				case OrgasmState:
				{
					var ss = person_.Physiology.Sensitivity;

					if (elapsed_ >= ss.OrgasmTime)
					{
						person_.Animator.StopType(Animation.OrgasmType);
						SetState(PostOrgasmState);
					}

					break;
				}

				case PostOrgasmState:
				{
					var ss = person_.Physiology.Sensitivity;

					tiredness_.Value += s;

					if (elapsed_ > ss.PostOrgasmTime)
					{
						SetState(NormalState);
						person_.Excitement.FlatValue = ss.ExcitementPostOrgasm;
					}

					break;
				}
			}
		}

		private void DoOrgasm()
		{
			var ss = person_.Physiology.Sensitivity;

			person_.Log.Info("orgasm");
			person_.Orgasmer.Orgasm();
			person_.Animator.PlayType(Animation.OrgasmType);
			person_.Excitement.FlatValue = 1;
			SetState(OrgasmState);
			timeSinceLastOrgasm_ = 0;
		}

		private void SetState(int s)
		{
			state_ = s;
			elapsed_ = 0;
		}
	}
}
