﻿using System;
using System.Collections.Generic;

namespace Cue
{
	public class ExpressionManager
	{
		private const int MaxActive = 4;
		private const int MaxActivePerMood = 2;
		private const int MaxEmergency = 1;
		private const float MoreCheckInterval = 1;

		private Person person_;
		private WeightedExpression[] exps_ = new WeightedExpression[0];
		private bool needsMore_ = false;
		private float moreElapsed_ = 0;
		private Personality lastPersonality_ = null;
		private bool enabled_ = true;
		private bool isOrgasming_ = false;
		private bool isZapped_ = false;

		private float slapTime_ = 0;
		private float slapElapsed_ = 0;
		private float slapAmount_ = 0;
		private bool slapUp_ = false;
		private IEasing slapEasing_ = new QuadInOutEasing();
		private Duration slapUpdateInterval_ = new Duration(5);
		private bool slapUpdate_ = true;
		private bool slapping_ = false;

		public ExpressionManager(Person p)
		{
			person_ = p;
		}

		public WeightedExpression[] GetAllExpressions()
		{
			return exps_;
		}

		public Expression[] GetExpressionsForMood(MoodType mood)
		{
			var list = new List<Expression>();

			for (int i = 0; i < exps_.Length; ++i)
			{
				if (exps_[i].Expression.IsMood(mood))
					list.Add(exps_[i].Expression);
			}

			return list.ToArray();
		}

		public void Enable()
		{
			for (int i = 0; i < exps_.Length; ++i)
				exps_[i].Deactivate();

			enabled_ = true;

			for (int i = 0; i < MaxActive; ++i)
				NextActive();
		}

		public void Disable()
		{
			for (int i = 0; i < exps_.Length; ++i)
				exps_[i].Deactivate();

			enabled_ = false;
		}

		private void Init()
		{
			var all = person_.Personality.GetExpressions();
			var exps = new List<WeightedExpression>();

			for (int i = 0; i < all.Length; ++i)
			{
				if (all[i].Init(person_))
					exps.Add(new WeightedExpression(person_, all[i]));
			}

			exps_ = exps.ToArray();

			for (int i = 0; i < MaxActive; ++i)
				NextActive();

			lastPersonality_ = person_.Personality;
			slapUpdateInterval_ = person_.Personality.GetDuration(PS.SlapUpdateInterval);
		}

		public void Slapped(float speed)
		{
			if (slapping_)
				return;

			slapElapsed_ = 0;
			slapUp_ = true;
			slapping_ = true;

			if (slapUpdate_)
			{
				slapUpdate_ = false;

				float minTime = person_.Personality.Get(PS.SlapMinTime);
				float maxTime = person_.Personality.Get(PS.SlapMaxTime);

				if (speed >= minTime && speed <= maxTime)
					maxTime = speed;

				slapTime_ = U.RandomNormal(minTime, maxTime);

				slapAmount_ = U.RandomFloat(
					person_.Personality.Get(PS.SlapMinExpressionChange),
					person_.Personality.Get(PS.SlapMaxExpressionChange));
			}
		}

		private float GetSlap()
		{
			float slap = 0;

			if (slapTime_ > 0)
			{
				if (slapUp_)
				{
					slap = slapElapsed_ / slapTime_;
					if (slap >= 1)
					{
						slap = 1;
						slapElapsed_ = 0;
						slapUp_ = false;
					}
				}
				else
				{
					slap = slapElapsed_ / slapTime_;
					if (slap >= 1)
					{
						slap = 1;
						slapping_ = false;
					}

					slap = 1 - slap;
				}
			}

			return slapEasing_.Magnitude(slap);
		}

		private void EmergencyExpression(MoodType mood, float intensity, float min, float max, float time)
		{
			bool foundActive = false;

			for (int i = 0; i < exps_.Length; ++i)
			{
				if (!foundActive && exps_[i].Active && exps_[i].Expression.IsMood(mood))
				{
					foundActive = true;
					ActivateForEmergency(exps_[i], intensity, min, max, time);
				}
				else if (exps_[i].Active)
				{
					exps_[i].Deactivate();
				}
			}

			if (!foundActive)
			{
				for (int i = 0; i < exps_.Length; ++i)
				{
					if (exps_[i].Expression.IsMood(mood))
					{
						ActivateForEmergency(exps_[i], intensity, min, max, time);
						break;
					}
				}
			}
		}

		public void FixedUpdate(float s)
		{
			slapUpdateInterval_.Update(s, person_.Mood.MovementEnergy);
			if (slapUpdateInterval_.Finished)
				slapUpdate_ = true;

			if (slapping_)
				slapElapsed_ += s;

			if (lastPersonality_ != person_.Personality)
				Init();

			var ps = person_.Personality;

			if (!isOrgasming_ && person_.Mood.State == Mood.OrgasmState)
			{
				isOrgasming_ = true;

				EmergencyExpression(
					MoodType.Orgasm, 1.0f,
					ps.Get(PS.OrgasmExpressionRangeMin),
					ps.Get(PS.OrgasmExpressionRangeMax),
					ps.Get(PS.OrgasmFirstExpressionTime));
			}
			else if (isOrgasming_ && person_.Mood.State != Mood.OrgasmState)
			{
				isOrgasming_ = false;
			}


			if (!isZapped_ && person_.Body.Zap.Active)
			{
				isZapped_ = true;

				EmergencyExpression(
					MoodType.Excited,
					person_.Body.Zap.Intensity,
					person_.Body.Zap.Intensity, person_.Body.Zap.Intensity,
					0.5f);
			}
			else if (isZapped_ && !person_.Body.Zap.Active)
			{
				isZapped_ = false;
			}


			int finished = 0;
			int activeCount = 0;
			float slap = GetSlap();

			for (int i = 0; i < exps_.Length; ++i)
			{
				exps_[i].Add = 0;

				if (exps_[i].Active)
					exps_[i].Add = slap * slapAmount_;
				else
					exps_[i].Add = 0;

				exps_[i].FixedUpdate(s);

				if (exps_[i].Active)
				{
					if (exps_[i].Finished)
					{
						exps_[i].Deactivate();
						++finished;
					}
					else
					{
						++activeCount;
					}
				}
			}

			if (!enabled_)
				return;

			for (int i = 0; i < finished; ++i)
			{
				if (NextActive())
					++activeCount;
			}

			if (activeCount < MaxActive)
			{
				if (needsMore_)
				{
					moreElapsed_ += s;

					if (moreElapsed_ > MoreCheckInterval || person_.Body.Zap.Intensity > 0)
					{
						moreElapsed_ = 0;
						var tries = MaxActive - activeCount;

						for (int i = 0; i < tries; ++i)
						{
							if (NextActive())
								++activeCount;
						}

						if (activeCount >= MaxActive)
							needsMore_ = false;
					}
				}
				else
				{
					needsMore_ = true;
					moreElapsed_ = 0;
				}
			}
		}

		private bool NextActive()
		{
			UpdateExpressions();

			float totalWeight = 0;
			for (int i = 0; i < exps_.Length; ++i)
			{
				if (exps_[i].Active)
					continue;

				totalWeight += exps_[i].Weight;
			}

			if (totalWeight > 0)
			{
				var r = U.RandomFloat(0, totalWeight);

				for (int i = 0; i < exps_.Length; ++i)
				{
					if (exps_[i].Active)
						continue;

					if (r < exps_[i].Weight)
						return Activate(exps_[i]);

					r -= exps_[i].Weight;
				}
			}

			return false;
		}

		private bool Activate(WeightedExpression e)
		{
			if (e.Expression.Exclusive)
			{
				if (!e.Activate())
					return false;

				for (int i = 0; i < exps_.Length; ++i)
				{
					if (exps_[i] == e)
						continue;

					if (exps_[i].Active)
					{
						if (exps_[i].Expression.AffectsAnyBodyPart(e.Expression.BodyParts))
						{
							exps_[i].Deactivate();
						}
					}
				}
			}
			else
			{
				for (int i = 0; i < exps_.Length; ++i)
				{
					if (exps_[i] == e)
						continue;

					if (exps_[i].Active && exps_[i].Expression.Exclusive)
					{
						if (exps_[i].Expression.AffectsAnyBodyPart(e.Expression.BodyParts))
							return false;
					}
				}

				if (!e.Activate())
					return false;
			}

			return true;
		}

		private void ActivateForEmergency(
			WeightedExpression e, float intensity,
			float min, float max, float time)
		{
			var ps = person_.Personality;

			e.Set(1, intensity, 1, min, max);
			e.Activate(intensity, time);
		}

		private void UpdateExpressions()
		{
			var m = person_.Mood;
			var ps = person_.Personality;

			float expressionTiredness = U.Clamp(
				m.Get(MoodType.Tired) * ps.Get(PS.ExpressionTirednessFactor),
				0, 1);

			// todo: this needs a rewrite, it was made when expressions had
			// multiple moods, but can only have one mood now, and they're
			// duplicated instead

			int[] countPerMood = new int[MoodType.Count];
			for (int i = 0; i < countPerMood.Length; ++i)
				countPerMood[i] = 0;

			for (int i = 0; i < exps_.Length; ++i)
			{
				if (exps_[i].Active)
					++countPerMood[exps_[i].Expression.Mood.Int];
			}


			for (int i = 0; i < exps_.Length; ++i)
			{
				var we = exps_[i];
				var e = we.Expression;

				float weight = 0;
				float intensity = 0;
				float min = 0, max = 1;

				MoodType highestMood = MoodType.None;
				float highestMoodValue = 0;

				if (isOrgasming_)
				{
					if (e.IsMood(MoodType.Orgasm))
					{
						if (m.Get(MoodType.Orgasm) > highestMoodValue)
						{
							highestMood = MoodType.Orgasm;
							highestMoodValue = m.Get(MoodType.Orgasm);
						}

						weight += e.DefaultWeight;
						intensity = 1;
						min = ps.Get(PS.OrgasmExpressionRangeMin);
						max = ps.Get(PS.OrgasmExpressionRangeMax);
					}
				}
				else
				{
					if (e.IsMood(MoodType.Happy))
					{
						if (m.Get(MoodType.Happy) > highestMoodValue)
						{
							highestMood = MoodType.Happy;
							highestMoodValue = m.Get(MoodType.Happy);
						}

						weight += e.DefaultWeight * m.Get(MoodType.Happy);
						intensity = Math.Max(intensity, m.Get(MoodType.Happy));
						intensity = Math.Min(intensity, ps.Get(PS.MaxHappyExpression));
					}

					if (e.IsMood(MoodType.Excited))
					{
						if (m.Get(MoodType.Excited) > highestMoodValue)
						{
							highestMood = MoodType.Excited;
							highestMoodValue = m.Get(MoodType.Excited);
						}

						weight += e.DefaultWeight * m.Get(MoodType.Excited) * ps.Get(PS.ExcitedExpressionWeightModifier);
						intensity = Math.Max(intensity, m.Get(MoodType.Excited));
						intensity = Math.Min(intensity, ps.Get(PS.MaxExcitedExpression));
					}

					if (e.IsMood(MoodType.Playful))
					{
						if (m.Get(MoodType.Playful) > highestMoodValue)
						{
							highestMood = MoodType.Playful;
							highestMoodValue = m.Get(MoodType.Playful);
						}

						weight += e.DefaultWeight * m.Get(MoodType.Playful);
						intensity = Math.Max(intensity, m.Get(MoodType.Playful));
						intensity = Math.Min(intensity, ps.Get(PS.MaxPlayfulExpression));
					}

					if (e.IsMood(MoodType.Angry))
					{
						if (m.Get(MoodType.Angry) > highestMoodValue)
						{
							highestMood = MoodType.Angry;
							highestMoodValue = m.Get(MoodType.Angry);
						}

						weight += e.DefaultWeight * m.Get(MoodType.Angry);
						intensity = Math.Max(intensity, m.Get(MoodType.Angry));
						intensity = Math.Min(intensity, ps.Get(PS.MaxAngryExpression));
					}

					if (e.IsMood(MoodType.Surprised))
					{
						if (m.Get(MoodType.Surprised) > highestMoodValue)
						{
							highestMood = MoodType.Surprised;
							highestMoodValue = m.Get(MoodType.Surprised);
						}

						weight += e.DefaultWeight * m.Get(MoodType.Surprised);
						intensity = Math.Max(intensity, m.Get(MoodType.Surprised));
						intensity = Math.Min(intensity, ps.Get(PS.MaxSurprisedExpression));
					}

					if (e.IsMood(MoodType.Tired))
					{
						if (m.Get(MoodType.Tired) > highestMoodValue)
						{
							highestMood = MoodType.Tired;
							highestMoodValue = m.Get(MoodType.Tired);
						}

						weight += e.DefaultWeight * expressionTiredness;
						intensity = Math.Max(intensity, expressionTiredness);
						intensity = Math.Min(intensity, ps.Get(PS.MaxTiredExpression));
					}

					if (m.Get(MoodType.Excited) < e.MinExcitement)
						weight = 0;

					if (e.Exclusive)
						weight *= ps.Get(PS.ExclusiveExpressionWeightModifier);
				}


				if (!e.IsMood(MoodType.Tired))
				{
					weight *= Math.Max(1 - expressionTiredness, 0.05f);
				}


				if (countPerMood[e.Mood.Int] >= MaxActivePerMood)
					weight = 0;

				float speed = 1 - expressionTiredness;

				we.Set(weight, intensity, speed, min, max);
			}
		}

		public void OnPluginState(bool b)
		{
			if (!b)
			{
				for (int i = 0; i < exps_.Length; ++i)
					exps_[i].Reset();
			}
		}

		public void Debug(DebugLines debug)
		{
			debug.Add("slap:");
			debug.Add($"  time={slapTime_:0.00} elapsed={slapElapsed_:0.00} amount={slapAmount_:0.00} up={slapUp_}");
			debug.Add($"  interval={slapUpdateInterval_.ToLiveString()}");
			debug.Add($"  update={slapUpdate_} slap={slapping_},{GetSlap():0.00}");

			for (int j = 0; j < exps_.Length; ++j)
			{
				if (exps_[j].Active)
					debug.Add(exps_[j].ToDetailedString());
			}

			while (debug.Count < MaxActive)
				debug.Add("none");

			while (debug.Count < (MaxEmergency + MaxActive))
				debug.Add("none (emergency)");

			debug.Add("");

			for (int j = 0; j < exps_.Length; ++j)
				debug.Add(exps_[j].ToDetailedString());

			debug.Add("");
			debug.Add((needsMore_ ? "needs more" : ""));
		}
	}
}
