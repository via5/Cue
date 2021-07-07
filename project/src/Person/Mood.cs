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
		private float baseTiredness_ = 0;

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

		public float BaseTiredness
		{
			get { return baseTiredness_; }
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

			switch (state_)
			{
				case NormalState:
				{
					timeSinceLastOrgasm_ += s;

					if (excitement_.UnforcedValue >= 1)
						DoOrgasm();

					break;
				}

				case OrgasmState:
				{
					var pp = person_.Physiology;
					var ss = pp.Sensitivity;

					if (elapsed_ >= ss.OrgasmTime)
					{
						person_.Animator.StopType(Animation.OrgasmType);
						tiredness_.UpRate = pp.TirednessRateDuringPostOrgasm;
						tiredness_.Target = 1;
						baseTiredness_ += pp.OrgasmBaseTirednessIncrease;
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

			if (state_ == NormalState)
			{
				if (timeSinceLastOrgasm_ > pp.DelayAfterOrgasmUntilTirednessDecay)
				{
					if (Excitement < pp.TirednessMaxExcitementForBaseDecay)
					{
						baseTiredness_ = U.Clamp(
							baseTiredness_ - s * pp.TirednessBaseDecayRate,
							0, 1);
					}
				}

				tiredness_.DownRate = pp.TirednessBackToBaseRate;
				tiredness_.Target = baseTiredness_;
			}
			else if (state_ == OrgasmState)
			{
				tiredness_.DownRate = 0;
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
