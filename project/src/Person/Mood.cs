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

		private float flatExcitement_ = 0;
		private IEasing excitementEasing_ = new SineOutEasing();

		private DampedFloat tiredness_ = new DampedFloat();
		private float baseTiredness_ = 0;

		private ForceableFloat[] moods_ = new ForceableFloat[Moods.Count];


		public Mood(Person p)
		{
			person_ = p;

			for (int i = 0; i < moods_.Length; ++i)
				moods_[i] = new ForceableFloat();
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

		public float Get(int i)
		{
			return moods_[i].Value;
		}

		public ForceableFloat GetValue(int i)
		{
			return moods_[i];
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
				return U.Clamp(Get(Moods.Excited) - (Get(Moods.Tired) * tf), 0, 1);
			}
		}

		public float GazeTiredness
		{
			get
			{
				var tf = person_.Personality.Get(PSE.GazeTirednessFactor);
				return U.Clamp(Get(Moods.Tired) * tf, 0, 1);
			}
		}

		public float MovementEnergy
		{
			get
			{
				var tf = person_.Personality.Get(PSE.MovementEnergyTirednessFactor);
				return U.Clamp(Get(Moods.Excited) - (Get(Moods.Tired) * tf), 0, 1);
			}
		}

		public bool IsIdle
		{
			get
			{
				return (Get(Moods.Excited) <= person_.Personality.Get(PSE.IdleMaxExcitement));
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

					if (!moods_[Moods.Excited].IsForced && moods_[Moods.Excited].Value >= 1)
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
						flatExcitement_ = pp.Get(PE.ExcitementPostOrgasm);
					}

					break;
				}
			}

			UpdateTiredness(s);
			UpdateExcitement(s);

			// this needs to be after tiredness and excitement, it depends on it
			UpdateMoods();
		}

		private void UpdateMoods()
		{
			var ps = person_.Personality;
			var anger = ps.Get(PSE.AngerWhenPlayerInteracts);

			if (anger > 0 && person_.Body.InteractingWith(Cue.Instance.Player))
			{
				var ex = Get(Moods.Excited);
				var happyMaxEx = ps.Get(PSE.AngerMaxExcitementForHappiness);
				var angerMaxEx = ps.Get(PSE.AngerMaxExcitementForAnger);

				if (ex < angerMaxEx)
				{
					moods_[Moods.Angry].Value = anger;
				}
				else
				{
					var exInRange = (ex - angerMaxEx) / angerMaxEx;
					var v = anger - (exInRange * ps.Get(PSE.AngerExcitementFactorForAnger));

					moods_[Moods.Angry].Value = U.Clamp(v, 0, 1);
				}

				if (ex < happyMaxEx)
				{
					moods_[Moods.Happy].Value = 0;
				}
				else
				{
					var exInRange = (ex - happyMaxEx) / happyMaxEx;
					var v = (exInRange * ps.Get(PSE.AngerExcitementFactorForHappiness));

					moods_[Moods.Happy].Value = U.Clamp(v, 0, 1);
				}
			}
			else
			{
				moods_[Moods.Happy].Value = 1;
				moods_[Moods.Angry].Value = 0;
			}
		}

		private void UpdateTiredness(float s)
		{
			var pp = person_.Physiology;

			if (state_ == NormalState)
			{
				if (timeSinceLastOrgasm_ > pp.Get(PE.DelayAfterOrgasmUntilTirednessDecay))
				{
					if (Get(Moods.Excited) < pp.Get(PE.TirednessMaxExcitementForBaseDecay))
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
			moods_[Moods.Tired].Value = tiredness_.Value;
		}

		private void UpdateExcitement(float s)
		{
			var pp = person_.Physiology;
			var ps = person_.Personality;
			var ex = person_.Excitement;

			if (flatExcitement_ > ex.Max)
			{
				flatExcitement_ = Math.Max(
					flatExcitement_ + pp.Get(PE.ExcitementDecayRate) * s,
					ex.Max);
			}
			else
			{
				var rate = ex.TotalRate;
				var tirednessFactor =
					Get(Moods.Tired) * ps.Get(PSE.TirednessExcitementRateFactor);

				rate = rate - (rate * tirednessFactor);

				rate *= pp.Get(PE.RateAdjustment);

				flatExcitement_ = U.Clamp(
					flatExcitement_ + rate * s,
					0, ex.Max);
			}

			moods_[Moods.Excited].Value = excitementEasing_.Magnitude(flatExcitement_);
		}

		private float GetRate()
		{
			var ex = person_.Excitement;
			var ps = person_.Personality;

			var rate = ex.TotalRate;

			var tirednessFactor =
				Get(Moods.Tired) * ps.Get(PSE.TirednessExcitementRateFactor);

			return rate - (rate * tirednessFactor);
		}

		private void DoOrgasm()
		{
			person_.Log.Info("orgasm");
			person_.Orgasmer.Orgasm();

			person_.Animator.StopType(Animations.Sex);
			person_.Animator.PlayType(Animations.Orgasm);

			flatExcitement_ = 1;
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
