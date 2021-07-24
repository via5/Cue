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

		public float RawExcitement
		{
			get { return excitement_.Value; }
		}

		public ForceableFloat ExcitementValue
		{
			get { return excitement_; }
		}

		public float RawTiredness
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

		public bool OrgasmJustStarted
		{
			get { return state_ == OrgasmState && elapsed_ == 0; }
		}

		public float Energy
		{
			get
			{
				var tf = person_.Personality.Get(PSE.EnergyTirednessFactor);
				return U.Clamp(RawExcitement - (RawTiredness * tf), 0, 1);
			}
		}

		public void ForceOrgasm()
		{
			DoOrgasm();
		}

		public void Update(float s)
		{
			elapsed_ += s;


			switch (state_)
			{
				case NormalState:
				{
					timeSinceLastOrgasm_ += s;

					if (!excitement_.IsForced && excitement_.Value >= 1)
						DoOrgasm();

					break;
				}

				case OrgasmState:
				{
					var pp = person_.Physiology;

					if (elapsed_ >= pp.Get(PE.OrgasmTime))
					{
						person_.Animator.StopType(Animation.OrgasmType);
						tiredness_.UpRate = pp.Get(PE.TirednessRateDuringPostOrgasm);
						tiredness_.Target = 1;
						baseTiredness_ += pp.Get(PE.OrgasmBaseTirednessIncrease);
						SetState(PostOrgasmState);
					}

					break;
				}

				case PostOrgasmState:
				{
					var pp = person_.Physiology;

					if (elapsed_ > pp.Get(PE.PostOrgasmTime))
					{
						SetState(NormalState);
						person_.Excitement.FlatValue = pp.Get(PE.ExcitementPostOrgasm);
					}

					break;
				}
			}

			excitement_.Value = person_.Excitement.Value;

			UpdateTiredness(s);
			UpdateExpressions();
		}

		private void UpdateExpressions()
		{
			for (int i = 0; i < Expressions.Count; ++i)
			{
				if (i == Expressions.Pleasure)
					person_.Expression.SetIntensity(i, RawExcitement);

				if (i == Expressions.Tired)
					person_.Expression.SetIntensity(i, RawTiredness);
				else
					person_.Expression.SetDampen(i, RawTiredness);
			}
		}

		private void UpdateTiredness(float s)
		{
			var pp = person_.Physiology;

			if (state_ == NormalState)
			{
				if (timeSinceLastOrgasm_ > pp.Get(PE.DelayAfterOrgasmUntilTirednessDecay))
				{
					if (RawExcitement < pp.Get(PE.TirednessMaxExcitementForBaseDecay))
					{
						baseTiredness_ = U.Clamp(
							baseTiredness_ - s * pp.Get(PE.TirednessBaseDecayRate),
							0, 1);
					}
				}

				tiredness_.DownRate = pp.Get(PE.TirednessBackToBaseRate);
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
			person_.Log.Info("orgasm");
			person_.Orgasmer.Orgasm();
			person_.Animator.PlayType(Animation.OrgasmType);
			person_.Excitement.FlatValue = 1;
			person_.Expression.ForceChange();
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
