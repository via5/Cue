using System;

namespace Cue
{
	using FloatIndex = BasicEnumValues.FloatIndex;

	public class MoodValue
	{
		private readonly Person person_;
		private readonly MoodType mood_;

		private readonly FloatIndex min_, max_;
		private readonly FloatIndex minExpr_, maxExpr_;
		private readonly FloatIndex minChoked_, maxChoked_;

		private DampedFloat v_ = new DampedFloat();
		private float temp_ = -1;
		private float tempTime_ = 0;
		private float tempElapsed_ = 0;

		private Sys.IFloatParameter param_;
		private Sys.IBoolParameter forceParam_;

		public MoodValue(
			Person p, MoodType type,
			FloatIndex min, FloatIndex max,
			FloatIndex minExpr, FloatIndex maxExpr,
			FloatIndex minChoked, FloatIndex maxChoked)
		{
			person_ = p;
			mood_ = type;

			min_ = min;
			max_ = max;
			minExpr_ = minExpr;
			maxExpr_ = maxExpr;
			minChoked_ = minChoked;
			maxChoked_ = maxChoked;

			if (p.IsInteresting)
			{
				param_ = Cue.Instance.Sys.RegisterFloatParameter(
					$"{p.ID}.Mood.{MoodType.ToString(type)}.Value",
					OnValue, 0, 0, 1);

				forceParam_ = Cue.Instance.Sys.RegisterBoolParameter(
					$"{p.ID}.Mood.{MoodType.ToString(type)}.ForceValue",
					b => OnForced(b),
					v_.IsForced);
			}

			Reset();
		}

		public void Reset()
		{
			Set(person_.Personality.Get(min_), true);
		}

		public DampedFloat Damped
		{
			get { return v_; }
		}

		public float Value
		{
			get
			{
				if (temp_ < 0)
					return v_.Value;
				else
					return temp_;
			}
		}

		public float Minimum
		{
			get
			{
				return Math.Min(
					person_.Personality.Get(min_),
					person_.Personality.Get(max_));
			}
		}

		public float Maximum
		{
			get
			{
				return Math.Max(
					person_.Personality.Get(min_),
					person_.Personality.Get(max_));
			}
		}

		public float MinimumExpression
		{
			get
			{
				return Math.Min(
					person_.Personality.Get(minExpr_),
					person_.Personality.Get(maxExpr_));
			}
		}

		public float MaximumExpression
		{
			get
			{
				return Math.Max(
					person_.Personality.Get(minExpr_),
					person_.Personality.Get(maxExpr_));
			}
		}

		public float MinimumWhenChoked
		{
			get
			{
				return Math.Min(
					person_.Personality.Get(minChoked_),
					person_.Personality.Get(maxChoked_));
			}
		}

		public float MaximumWhenChoked
		{
			get
			{
				return Math.Max(
					person_.Personality.Get(minChoked_),
					person_.Personality.Get(maxChoked_));
			}
		}

		public bool IsForced
		{
			get { return v_.IsForced; }
		}

		public void Set(float v, bool fast = false)
		{
			var ps = person_.Personality;
			v = U.Clamp(v, ps.Get(min_), ps.Get(max_));

			v_.Target = v;

			if (fast)
				v_.SetValue(v);
		}

		public void SetTemporary(float v, float time)
		{
			temp_ = v;
			tempTime_ = time;
			tempElapsed_ = 0;
		}

		public void Update(float s)
		{
			v_.Update(s);

			if (temp_ >= 0)
			{
				tempElapsed_ += s;
				if (tempElapsed_ >= tempTime_)
				{
					temp_ = -1;
					tempTime_ = 0;
					tempElapsed_ = 0;
				}
			}

			if (param_ != null)
				param_.Value = Value;

			if (forceParam_ != null)
				forceParam_.Value = v_.IsForced;
		}

		private void OnValue(float f)
		{
			if (v_.IsForced)
				v_.SetForced(f);
			else
				Set(f);
		}

		private void OnForced(bool b)
		{
			if (b)
				v_.SetForced(param_.Value);
			else
				v_.UnsetForced();
		}
	}


	public class Mood
	{
		public const float NoOrgasm = 10000;

		private const int NormalState = 1;
		private const int OrgasmHighState = 2;
		private const int OrgasmLowState = 3;
		private const int PostOrgasmState = 4;

		private readonly Person person_;
		private bool wasPlayer_ = false;
		private int state_ = NormalState;
		private float elapsedThisState_ = 0;
		private float timeSinceLastOrgasm_ = NoOrgasm;
		private IEasing energyRampUpEasing_ = new LinearEasing();

		private float baseTiredness_ = 0;
		private ForceableFloat baseExcitement_ = new ForceableFloat();
		private MoodValue[] moods_ = new MoodValue[MoodType.Count];


		public Mood(Person p)
		{
			person_ = p;

			var ps = person_.Personality;

			moods_[MoodType.Happy.Int] = new MoodValue(
				person_, MoodType.Happy,
				PS.MinHappy, PS.MaxHappy,
				PS.MinHappyExpression, PS.MaxHappyExpression,
				PS.MinHappyExpressionChoked, PS.MaxHappyExpressionChoked);

			moods_[MoodType.Playful.Int] = new MoodValue(
				person_, MoodType.Playful,
				PS.MinPlayful, PS.MaxPlayful,
				PS.MinPlayfulExpression, PS.MaxPlayfulExpression,
				PS.MinPlayfulExpressionChoked, PS.MaxPlayfulExpressionChoked);

			moods_[MoodType.Excited.Int] = new MoodValue(
				person_, MoodType.Excited,
				PS.MinExcited, PS.MaxExcited,
				PS.MinExcitedExpression, PS.MaxExcitedExpression,
				PS.MinExcitedExpressionChoked, PS.MaxExcitedExpressionChoked);

			moods_[MoodType.Angry.Int] = new MoodValue(
				person_, MoodType.Angry,
				PS.MinAngry, PS.MaxAngry,
				PS.MinAngryExpression, PS.MaxAngryExpression,
				PS.MinAngryExpressionChoked, PS.MaxAngryExpressionChoked);

			moods_[MoodType.Surprised.Int] = new MoodValue(
				person_, MoodType.Surprised,
				PS.MinSurprised, PS.MaxSurprised,
				PS.MinSurprisedExpression, PS.MaxSurprisedExpression,
				PS.MinSurprisedExpressionChoked, PS.MaxSurprisedExpressionChoked);

			moods_[MoodType.Tired.Int] = new MoodValue(
				person_, MoodType.Tired,
				PS.MinTired, PS.MaxTired,
				PS.MinTiredExpression, PS.MaxTiredExpression,
				PS.MinTiredExpressionChoked, PS.MaxTiredExpressionChoked);

			moods_[MoodType.Orgasm.Int] = new MoodValue(
				person_, MoodType.Orgasm,
				PS.MinOrgasm, PS.MaxOrgasm,
				PS.MinOrgasmExpression, PS.MaxOrgasmExpression,
				PS.MinOrgasmExpressionChoked, PS.MaxOrgasmExpressionChoked);

			OnPersonalityChanged();
			p.PersonalityChanged += OnPersonalityChanged;
		}

		private void OnPersonalityChanged()
		{
			var ps = person_.Personality;

			var es = ps.GetString(PS.MovementEnergyRampUpAfterOrgasmEasing);

			var e = EasingFactory.FromString(es);

			if (e == null && es != "")
			{
				person_.Log.Error(
					$"bad easing name '{es}' for " +
					$"MovementEnergyRampUpAfterOrgasmEasing");

				return;
			}

			energyRampUpEasing_ = e;

			foreach (var m in moods_)
				m.Reset();

			SetBaseTiredness(ps.Get(PS.MinTired));
		}

		public static bool ShouldStopSexAnimation(
			Person main, Person other = null)
		{
			return
				!HasEnergyForAnimation(main) ||
				!HasEnergyForAnimation(other);
		}

		public static bool CanStartSexAnimation(
			Person main, Person other = null)
		{
			return
				HasEnergyForAnimation(main) &&
				HasEnergyForAnimation(other);
		}

		private float BaseExcitement
		{
			get { return baseExcitement_.Value; }
		}

		private static bool HasEnergyForAnimation(Person p)
		{
			if (p != null)
			{
				if (p.Mood.MovementEnergy == 0 && p.Mood.Get(MoodType.Excited) > 0)
					return false;
			}

			return true;
		}

		public static float MultiMovementEnergy(
			Person a, Person b = null, Person c = null)
		{
			// this takes into account the excitement and tiredness of both
			// characters involved
			//
			// this allows for an unexcited character to have high energy
			// if interacting with an excited character, or an excited
			// character to have low energy if interacting with a tired
			// character

			Person mostExcited = null;
			Person mostTired = null;

			if (a != null)
			{
				mostExcited = HighestExcitement(mostExcited, a);
				mostTired = HighestTiredness(mostTired, a);
			}

			if (b != null && b != a)
			{
				mostExcited = HighestExcitement(mostExcited, b);
				mostTired = HighestTiredness(mostTired, b);
			}

			if (c != null && c != b)
			{
				mostExcited = HighestExcitement(mostExcited, c);
				mostTired = HighestTiredness(mostTired, c);
			}

			if (mostExcited == null || mostTired == null)
				return 1;

			return mostTired.Mood.MovementEnergyForExcitement(
					mostExcited.Mood.Get(MoodType.Excited));
		}

		private static Person HighestExcitement(Person current, Person check)
		{
			if (current == null)
				return check;
			else if (check.Mood.Get(MoodType.Excited) > current.Mood.Get(MoodType.Excited))
				return check;
			else
				return current;
		}

		private static Person HighestTiredness(Person current, Person check)
		{
			if (current == null)
				return check;
			else if (check.Mood.Get(MoodType.Tired) > current.Mood.Get(MoodType.Tired))
				return check;
			else
				return current;
		}

		private int State
		{
			get { return state_; }
		}

		public bool IsOrgasmingHigh()
		{
			return (state_ == OrgasmHighState);
		}

		// either high or low
		public bool IsOrgasming()
		{
			switch (state_)
			{
				case OrgasmHighState:  // fall-through
				case OrgasmLowState:
					return true;

				case PostOrgasmState: // fall-through
				case NormalState:
				default:
					return false;
			}
		}

		public string StateString
		{
			get
			{
				switch (state_)
				{
					case NormalState:
						return "normal";

					case OrgasmHighState:
						return "orgasmHigh";

					case OrgasmLowState:
						return "orgasmLow";

					case PostOrgasmState:
						return "postorgasm";

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

		public float Get(MoodType i)
		{
			return moods_[i.Int].Value;
		}

		public MoodValue GetMoodValue(MoodType i)
		{
			return moods_[i.Int];
		}

		public bool IsExcitementForced
		{
			get { return moods_[MoodType.Excited.Int].IsForced; }
		}

		public void SetTemporary(MoodType i, float value, float time)
		{
			moods_[i.Int].SetTemporary(value, time);
		}

		private void Set(MoodType i, float value, bool fast=false)
		{
			var m = moods_[i.Int];

			if (!person_.Body.Breathing)
			{
				value = U.Clamp(value, m.MinimumWhenChoked, m.MaximumWhenChoked);
				fast = true;
			}

			m.Set(value, fast);
		}

		private void SetDefault(MoodType i, bool fast=false)
		{
			Set(i, moods_[i.Int].Minimum, fast);
		}

		public DampedFloat GetDamped(MoodType i)
		{
			return moods_[i.Int].Damped;
		}

		public void SetBaseExcitement(float f)
		{
			baseExcitement_.Value = f;
		}

		public ForceableFloat GetBaseExcitement()
		{
			return baseExcitement_;
		}

		public float BaseTiredness
		{
			get { return baseTiredness_; }
		}

		private void SetBaseTiredness(float f)
		{
			baseTiredness_ = U.Clamp(f,
				person_.Personality.Get(PS.MinTired),
				person_.Personality.Get(PS.MaxTired));
		}

		public float GazeEnergy
		{
			get
			{
				var tf = person_.Personality.Get(PS.GazeEnergyTirednessFactor);
				return U.Clamp(Get(MoodType.Excited) - (Get(MoodType.Tired) * tf), 0, 1);
			}
		}

		public float GazeTiredness
		{
			get
			{
				var tf = person_.Personality.Get(PS.GazeTirednessFactor);
				return U.Clamp(Get(MoodType.Tired) * tf, 0, 1);
			}
		}

		public float MovementEnergy
		{
			get
			{
				return MovementEnergyForExcitement(Get(MoodType.Excited));
			}
		}

		private float MovementEnergyForExcitement(float e)
		{
			var ps = person_.Personality;
			float max = float.MaxValue;

			if (state_ != OrgasmHighState)
			{
				var rampUpTime = ps.Get(PS.MovementEnergyRampUpAfterOrgasm);
				if (rampUpTime > 0)
				{
					var f = U.Clamp(timeSinceLastOrgasm_ / rampUpTime, 0, 1);
					max = energyRampUpEasing_.Magnitude(f);
				}
			}

			var tf = ps.Get(PS.MovementEnergyTirednessFactor);

			// note: max can be 0, so Clamp() can still return 0 even if min is
			// higher
			return U.Clamp(e - (Get(MoodType.Tired) * tf), 0.01f, max);
		}

		public bool IsIdle
		{
			get
			{
				return (Get(MoodType.Excited) <= person_.Personality.Get(PS.IdleMaxExcitement));
			}
		}

		public void ForceOrgasm()
		{
			if (State != OrgasmHighState && State != OrgasmLowState)
				DoOrgasm(false);
		}

		public void Update(float s)
		{
			elapsedThisState_ += s;

			for (int i = 0; i < moods_.Length; ++i)
				moods_[i].Update(s);

			if (person_.IsPlayer)
			{
				if (!wasPlayer_)
				{
					SetBaseTiredness(0);
					baseExcitement_.Value = 0;

					for (int i = 0; i < moods_.Length; ++i)
						moods_[i].Set(0);

					wasPlayer_ = true;
				}

				return;
			}
			else
			{
				wasPlayer_ = false;
			}


			switch (state_)
			{
				case NormalState:
				{
					timeSinceLastOrgasm_ += s;

					if (Get(MoodType.Excited) >= 1)
					{
						if (!IsExcitementForced)
							DoOrgasm();
					}

					break;
				}

				case OrgasmHighState:
				{
					var ps = person_.Personality;

					if (elapsedThisState_ >= ps.Get(PS.OrgasmHighTime))
					{
						SetState(OrgasmLowState);
					}

					break;
				}

				case OrgasmLowState:
				{
					var ps = person_.Personality;

					if (elapsedThisState_ >= ps.Get(PS.OrgasmLowTime))
					{
						person_.Animator.StopType(AnimationType.Orgasm);

						GetMoodValue(MoodType.Tired).Damped.UpRate = ps.Get(PS.TirednessRateDuringPostOrgasm);
						GetMoodValue(MoodType.Tired).Damped.Target = 1;
						SetBaseTiredness(baseTiredness_ + ps.Get(PS.OrgasmBaseTirednessIncrease));

						Set(MoodType.Orgasm, 0, true);
						SetState(PostOrgasmState);

						person_.Options.GetAnimationOption(AnimationType.Orgasm).Trigger(false);
					}

					break;
				}

				case PostOrgasmState:
				{
					var ps = person_.Personality;

					if (elapsedThisState_ > ps.Get(PS.PostOrgasmTime))
					{
						SetState(NormalState);
						baseExcitement_.Value = ps.Get(PS.ExcitementPostOrgasm);
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
			var anger = ps.Get(PS.AngerWhenPlayerInteracts);

			if (anger > 0 && person_.Status.InteractingWith(Cue.Instance.Player))
			{
				var ex = Get(MoodType.Excited);
				var happyMaxEx = ps.Get(PS.AngerMaxExcitementForHappiness);
				var angerMaxEx = ps.Get(PS.AngerMaxExcitementForAnger);

				if (ex < angerMaxEx)
				{
					Set(MoodType.Angry, anger);
				}
				else
				{
					var exInRange = (ex - angerMaxEx) / angerMaxEx;
					var v = anger - (exInRange * ps.Get(PS.AngerExcitementFactorForAnger));

					Set(MoodType.Angry, v);
				}

				if (ex < happyMaxEx)
				{
					Set(MoodType.Happy, 0);
					Set(MoodType.Playful, 0);
				}
				else
				{
					var exInRange = (ex - happyMaxEx) / happyMaxEx;
					var v = (exInRange * ps.Get(PS.AngerExcitementFactorForHappiness));

					Set(MoodType.Happy, v);
					Set(MoodType.Playful, v);
				}
			}
			else
			{
				if (person_.Gaze.Picker.CurrentTarget?.Reluctant ?? false)
				{
					Set(MoodType.Happy, 0);
					Set(MoodType.Playful, 0);
					Set(MoodType.Angry, ps.Get(PS.AvoidGazeAnger));
				}
				else
				{
					SetDefault(MoodType.Happy);
					SetDefault(MoodType.Playful);
					SetDefault(MoodType.Angry);
				}
			}

			SetDefault(MoodType.Surprised);
		}

		private void UpdateTiredness(float s)
		{
			var ps = person_.Personality;

			if (state_ == NormalState)
			{
				if (timeSinceLastOrgasm_ > ps.Get(PS.DelayAfterOrgasmUntilTirednessDecay))
				{
					if (Get(MoodType.Excited) < ps.Get(PS.TirednessMaxExcitementForBaseDecay))
						SetBaseTiredness(baseTiredness_ - s * ps.Get(PS.TirednessBaseDecayRate));
				}

				GetMoodValue(MoodType.Tired).Damped.DownRate = ps.Get(PS.TirednessBackToBaseRate);
				GetMoodValue(MoodType.Tired).Damped.Target = baseTiredness_;
			}
			else if (IsOrgasming())
			{
				GetMoodValue(MoodType.Tired).Damped.DownRate = 0;
			}

			GetMoodValue(MoodType.Tired).Damped.Update(s);
		}

		private void UpdateExcitement(float s)
		{
			var ps = person_.Personality;
			var ex = person_.Excitement;

			if (baseExcitement_.Value > ex.Max)
			{
				baseExcitement_.Value = Math.Max(
					baseExcitement_.Value + ps.Get(PS.ExcitementDecayRate) * s,
					ex.Max);
			}
			else
			{
				var rate = ex.TotalRate;
				var tirednessFactor =
					Get(MoodType.Tired) * ps.Get(PS.TirednessExcitementRateFactor);

				rate = rate - (rate * tirednessFactor);
				rate *= Cue.Instance.Options.Excitement;

				baseExcitement_.Value = U.Clamp(
					baseExcitement_.Value + rate * s,
					0, ex.Max);
			}

			baseExcitement_.Value = Math.Min(
				baseExcitement_.Value, person_.Options.MaxExcitement);

			if (person_.Body.Zap.Active)
			{
				float zapped = person_.Body.Zap.Intensity * 0.9f;
				float e = Math.Max(BaseExcitement, zapped);
				e = Math.Min(e, person_.Options.MaxExcitement);
				Set(MoodType.Excited, e, true);
			}
			else
			{
				Set(MoodType.Excited, BaseExcitement);
			}
		}

		private float GetRate()
		{
			var ex = person_.Excitement;
			var ps = person_.Personality;

			var rate = ex.TotalRate;

			var tirednessFactor =
				Get(MoodType.Tired) * ps.Get(PS.TirednessExcitementRateFactor);

			return rate - (rate * tirednessFactor);
		}

		public void DoOrgasmFromSync()
		{
			if (state_ == NormalState)
				DoOrgasm(false);
		}

		private void DoOrgasm(bool syncOthers = true)
		{
			person_.Log.Verbose("orgasm");

			if (person_.Options.GetAnimationOption(AnimationType.Orgasm).Play)
				person_.Animator.PlayType(AnimationType.Orgasm);

			person_.Options.GetAnimationOption(AnimationType.Orgasm).Trigger(true);
			person_.Body.DoOrgasm();

			baseExcitement_.Value = 1;
			Set(MoodType.Excited, baseExcitement_.Value);
			Set(MoodType.Orgasm, 1, true);

			SetState(OrgasmHighState);
			timeSinceLastOrgasm_ = 0;

			SyncOrgasms();
		}

		private void SyncOrgasms()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				float min = p.Personality.Get(PS.OrgasmSyncMinExcitement);

				if (min > 0)
				{
					float e = p.Mood.Get(MoodType.Excited);
					if (e >= min)
					{
						if (p.Mood.TimeSinceLastOrgasm >= 5)
						{
							person_.Log.Verbose($"{p} is at {e}, min is {min}, syncing orgasm");
							p.Mood.DoOrgasmFromSync();
						}
					}
				}
			}
		}

		private void SetState(int s)
		{
			state_ = s;
			elapsedThisState_ = 0;
		}
	}
}
