using System;

namespace Cue
{
	public class Mood
	{
		public const float NoOrgasm = 10000;

		public const int NormalState = 1;
		public const int OrgasmState = 2;
		public const int PostOrgasmState = 3;

		private readonly Person person_;
		private int state_ = NormalState;
		private float elapsed_ = 0;
		private float timeSinceLastOrgasm_ = NoOrgasm;
		private IEasing energyRampUpEasing_ = new LinearEasing();

		private DampedFloat tiredness_ = new DampedFloat();
		private float baseTiredness_ = 0;
		private float baseExcitement_ = 0;

		private ForceableFloat[] moods_ = new ForceableFloat[MoodType.Count];


		public Mood(Person p)
		{
			person_ = p;

			for (int i = 0; i < moods_.Length; ++i)
				moods_[i] = new ForceableFloat();

			OnPersonalityChanged();
			p.PersonalityChanged += OnPersonalityChanged;
		}

		private void OnPersonalityChanged()
		{
			var es = person_.Personality.GetString(
					PS.MovementEnergyRampUpAfterOrgasmEasing);

			var e = EasingFactory.FromString(es);

			if (e == null && es != "")
			{
				person_.Log.Error(
					$"bad easing name '{es}' for " +
					$"MovementEnergyRampUpAfterOrgasmEasing");

				return;
			}

			energyRampUpEasing_ = e;
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
			// if interacting with an exicted character, or an excited
			// character to have low energy if interacting with a tired
			// character

			Person mostExcited = null;
			Person mostTired = null;

			if (a != null)
			{
				mostExcited = HighestValue(mostExcited, a, MoodType.Excited);
				mostTired = HighestValue(mostTired, a, MoodType.Tired);
			}

			if (b != null && b != a)
			{
				mostExcited = HighestValue(mostExcited, b, MoodType.Excited);
				mostTired = HighestValue(mostTired, b, MoodType.Tired);
			}

			if (c != null && c != b)
			{
				mostExcited = HighestValue(mostExcited, c, MoodType.Excited);
				mostTired = HighestValue(mostTired, c, MoodType.Tired);
			}

			if (mostExcited == null || mostTired == null)
				return 1;

			return mostTired.Mood.MovementEnergyForExcitement(
					mostExcited.Mood.Get(MoodType.Excited));
		}

		private static Person HighestValue(Person current, Person check, MoodType what)
		{
			if (current == null)
				return check;
			else if (check.Mood.Get(what) > current.Mood.Get(what))
				return check;
			else
				return current;
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

		public float Get(MoodType i)
		{
			return moods_[i.Int].Value;
		}

		private void Set(MoodType i, float value)
		{
			moods_[i.Int].Value = U.Clamp(value, 0, 1);
		}

		public ForceableFloat GetValue(MoodType i)
		{
			return moods_[i.Int];
		}


		public DampedFloat TirednessValue
		{
			get { return tiredness_; }
		}

		public float BaseTiredness
		{
			get { return baseTiredness_; }
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

		public float MovementEnergyForExcitement(float e)
		{
			var ps = person_.Personality;
			float max = float.MaxValue;

			if (state_ != OrgasmState || elapsed_ >= ps.Get(PS.MovementEnergyRampUpDelayAfterOrgasm))
			{
				var rampUpTime = ps.Get(PS.MovementEnergyRampUpAfterOrgasm);
				if (rampUpTime > 0)
				{
					var f = U.Clamp(timeSinceLastOrgasm_ / rampUpTime, 0, 1);
					max = energyRampUpEasing_.Magnitude(f);
				}
			}

			var tf = ps.Get(PS.MovementEnergyTirednessFactor);
			return U.Clamp(e - (Get(MoodType.Tired) * tf), 0, max);
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
			DoOrgasm(false);
		}

		public void Update(float s)
		{
			elapsed_ += s;


			switch (state_)
			{
				case NormalState:
				{
					timeSinceLastOrgasm_ += s;

					if (!GetValue(MoodType.Excited).IsForced && Get(MoodType.Excited) >= 1)
						DoOrgasm();

					break;
				}

				case OrgasmState:
				{
					var ps = person_.Personality;

					if (elapsed_ >= ps.Get(PS.OrgasmTime))
					{
						person_.Animator.StopType(AnimationType.Orgasm);

						tiredness_.UpRate = ps.Get(PS.TirednessRateDuringPostOrgasm);
						tiredness_.Target = 1;

						baseTiredness_ += ps.Get(PS.OrgasmBaseTirednessIncrease);
						baseTiredness_ = U.Clamp(baseTiredness_, 0, 1);

						SetState(PostOrgasmState);
					}

					break;
				}

				case PostOrgasmState:
				{
					var ps = person_.Personality;

					if (elapsed_ > ps.Get(PS.PostOrgasmTime))
					{
						SetState(NormalState);
						baseExcitement_ = ps.Get(PS.ExcitementPostOrgasm);
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

					Set(MoodType.Happy, U.Clamp(v, 0, ps.Get(PS.DefaultHappiness)));
					Set(MoodType.Playful, U.Clamp(v, 0, ps.Get(PS.DefaultPlayfulness)));
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
					Set(MoodType.Happy, ps.Get(PS.DefaultHappiness));
					Set(MoodType.Playful, ps.Get(PS.DefaultPlayfulness));
					Set(MoodType.Angry, ps.Get(PS.DefaultAnger));
				}
			}
		}

		private void UpdateTiredness(float s)
		{
			var ps = person_.Personality;

			if (state_ == NormalState)
			{
				if (timeSinceLastOrgasm_ > ps.Get(PS.DelayAfterOrgasmUntilTirednessDecay))
				{
					if (Get(MoodType.Excited) < ps.Get(PS.TirednessMaxExcitementForBaseDecay))
					{
						baseTiredness_ = U.Clamp(
							baseTiredness_ - s * ps.Get(PS.TirednessBaseDecayRate),
							0, 1);
					}
				}

				tiredness_.DownRate = ps.Get(PS.TirednessBackToBaseRate);
				tiredness_.Target = baseTiredness_;
			}
			else if (state_ == OrgasmState)
			{
				tiredness_.DownRate = 0;
			}

			tiredness_.Update(s);
			Set(MoodType.Tired, tiredness_.Value);
		}

		private void UpdateExcitement(float s)
		{
			var ps = person_.Personality;
			var ex = person_.Excitement;

			if (baseExcitement_ > ex.Max)
			{
				baseExcitement_ = Math.Max(
					baseExcitement_ + ps.Get(PS.ExcitementDecayRate) * s,
					ex.Max);
			}
			else
			{
				var rate = ex.TotalRate;
				var tirednessFactor =
					Get(MoodType.Tired) * ps.Get(PS.TirednessExcitementRateFactor);

				rate = rate - (rate * tirednessFactor);
				rate *= Cue.Instance.Options.Excitement;

				baseExcitement_ = U.Clamp(
					baseExcitement_ + rate * s,
					0, ex.Max);
			}

			float zapped = person_.Body.Zap.Intensity * 0.9f;
			Set(MoodType.Excited, Math.Max(baseExcitement_, zapped));
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

		private void DoOrgasm(bool syncOthers = true)
		{
			person_.Log.Info("orgasm");

			person_.Animator.PlayType(AnimationType.Orgasm);

			baseExcitement_ = 1;
			Set(MoodType.Excited, baseExcitement_);

			SetState(OrgasmState);
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
							person_.Log.Info($"{p} is at {e}, min is {min}, syncing orgasm");
							p.Mood.ForceOrgasm();
						}
					}
				}
			}
		}

		private void SetState(int s)
		{
			state_ = s;
			elapsed_ = 0;
		}
	}
}
