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

		private ForceableFloat excitement_ = new ForceableFloat();
		private DampedFloat tiredness_ = new DampedFloat();

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

		public ForceableFloat ExcitementValue
		{
			get { return excitement_; }
		}

		public float Tiredness
		{
			get { return tiredness_.Value; }
		}

		public DampedFloat TirednessValue
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
					var pp = person_.Physiology;
					var ss = pp.Sensitivity;


					if (elapsed_ > ss.PostOrgasmTime)
					{
						SetState(NormalState);
						person_.Excitement.FlatValue = ss.ExcitementPostOrgasm;
					}

					break;
				}
			}

			UpdateTiredness(s);
		}

		private void UpdateTiredness(float s)
		{
			var pp = person_.Physiology;

			tiredness_.DownRate = pp.TirednessDecayRate;

			if (state_ == PostOrgasmState)
			{
				tiredness_.UpRate = pp.TirednessRateDuringPostOrgasm;
				tiredness_.Target = 1;
			}
			else
			{
				if (Excitement >= pp.TirednessMaxExcitementDecay)
				{
					tiredness_.UpRate = Excitement * pp.TirednessExcitementRate;
					tiredness_.Target = 1;
				}
				else if (timeSinceLastOrgasm_ >= pp.DelayAfterOrgasmUntilTirednessDecay)
				{
					tiredness_.Target = 0;
				}
			}

			tiredness_.Update(s);
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
