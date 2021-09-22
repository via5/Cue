using System;

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

		private ForceableFloat flatExcitement_ = new ForceableFloat();
		private IEasing excitementEasing_ = new SineOutEasing();

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

		public string RateString
		{
			get
			{
				var tr = person_.Excitement.TotalRate;

				var r = GetRate();
				if (r == 0)
					return "0";

				var p = (r / tr) * 100;

				return $"{(int)Math.Round(p)}%";
			}
		}

		public float TimeSinceLastOrgasm
		{
			get { return timeSinceLastOrgasm_; }
		}

		public float Excitement
		{
			get { return excitementEasing_.Magnitude(flatExcitement_.Value); }
		}

		public ForceableFloat FlatExcitementValue
		{
			get { return flatExcitement_; }
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

		public bool OrgasmJustStarted
		{
			get { return state_ == OrgasmState && elapsed_ == 0; }
		}

		public float GazeEnergy
		{
			get
			{
				var tf = person_.Personality.Get(PSE.GazeEnergyTirednessFactor);
				return U.Clamp(Excitement - (Tiredness * tf), 0, 1);
			}
		}

		public float GazeTiredness
		{
			get
			{
				var tf = person_.Personality.Get(PSE.GazeTirednessFactor);
				return U.Clamp(Tiredness * tf, 0, 1);
			}
		}

		public float MovementEnergy
		{
			get
			{
				var tf = person_.Personality.Get(PSE.MovementEnergyTirednessFactor);
				return U.Clamp(Excitement - (Tiredness * tf), 0, 1);
			}
		}

		public float MovementTiredness
		{
			get
			{
				var tf = person_.Personality.Get(PSE.MovementTirednessFactor);
				return U.Clamp(Tiredness * tf, 0, 1);
			}
		}

		public float ExpressionExcitement
		{
			get
			{
				var tf = person_.Personality.Get(PSE.ExpressionExcitementFactor);
				return U.Clamp(Excitement * tf, 0, 1);
			}
		}

		public float ExpressionTiredness
		{
			get
			{
				var tf = person_.Personality.Get(PSE.ExpressionTirednessFactor);
				return U.Clamp(Tiredness * tf, 0, 1);
			}
		}

		public bool IsIdle
		{
			get
			{
				return (Excitement < person_.Personality.Get(PSE.IdleMaxExcitement));
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

					if (!flatExcitement_.IsForced && flatExcitement_.Value >= 1)
						DoOrgasm();

					break;
				}

				case OrgasmState:
				{
					var pp = person_.Physiology;

					if (elapsed_ >= pp.Get(PE.OrgasmTime))
					{
						person_.Animator.StopType(Animations.Orgasm);

						tiredness_.UpRate = pp.Get(PE.TirednessRateDuringPostOrgasm);
						tiredness_.Target = 1;

						baseTiredness_ += pp.Get(PE.OrgasmBaseTirednessIncrease);
						baseTiredness_ = U.Clamp(baseTiredness_, 0, 1);

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
						flatExcitement_.Value = pp.Get(PE.ExcitementPostOrgasm);
					}

					break;
				}
			}

			UpdateTiredness(s);
			UpdateExcitement(s);
			UpdateExpressions();
		}

		private void UpdateTiredness(float s)
		{
			var pp = person_.Physiology;

			if (state_ == NormalState)
			{
				if (timeSinceLastOrgasm_ > pp.Get(PE.DelayAfterOrgasmUntilTirednessDecay))
				{
					if (Excitement < pp.Get(PE.TirednessMaxExcitementForBaseDecay))
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

		private void UpdateExcitement(float s)
		{
			var pp = person_.Physiology;
			var ps = person_.Personality;
			var ex = person_.Excitement;

			if (flatExcitement_.Value > ex.Max)
			{
				flatExcitement_.Value = Math.Max(
					flatExcitement_.Value + pp.Get(PE.ExcitementDecayRate) * s,
					ex.Max);
			}
			else
			{
				var rate = ex.TotalRate;
				var tirednessFactor =
					Tiredness * ps.Get(PSE.TirednessExcitementRateFactor);

				rate = rate - (rate * tirednessFactor);

				rate *= pp.Get(PE.RateAdjustment);

				flatExcitement_.Value = U.Clamp(
					flatExcitement_.Value + rate * s,
					0, ex.Max);
			}
		}

		private float GetRate()
		{
			var ex = person_.Excitement;
			var ps = person_.Personality;

			var rate = ex.TotalRate;

			var tirednessFactor =
				Tiredness * ps.Get(PSE.TirednessExcitementRateFactor);

			return rate - (rate * tirednessFactor);
		}

		private void UpdateExpressions()
		{
			for (int i = 0; i < Expressions.Count; ++i)
			{
				if (i == Expressions.Pleasure)
					person_.Expression.SetIntensity(i, ExpressionExcitement);

				if (i == Expressions.Tired)
					person_.Expression.SetIntensity(i, ExpressionTiredness);
				else
					person_.Expression.SetDampen(i, ExpressionTiredness);
			}
		}

		private void DoOrgasm()
		{
			person_.Log.Info("orgasm");
			person_.Orgasmer.Orgasm();

			person_.Animator.StopType(Animations.Sex);
			person_.Animator.PlayType(Animations.Orgasm);

			flatExcitement_.Value = 1;
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
