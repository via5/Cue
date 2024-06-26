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
		private Logger log_;
		private WeightedExpression[] exps_ = new WeightedExpression[0];
		private bool needsMore_ = false;
		private float moreElapsed_ = 0;
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
			log_ = new Logger(Logger.AI, "expression manager " + p.ID);

			person_.PersonalityChanged += Init;
		}

		public Logger Log
		{
			get { return log_; }
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
				if (exps_[i].Expression.Mood == mood)
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

		public void Init()
		{
			var all = person_.Personality.GetExpressions();
			var exps = new List<WeightedExpression>();

			for (int i = 0; i < all.Length; ++i)
			{
				if (all[i].Init(person_))
					exps.Add(new WeightedExpression(person_, all[i]));
				else
					Log.Verbose($"not using {all[i]}, couldn't init");
			}

			exps_ = exps.ToArray();

			foreach (var e in exps_)
			{
				if (e.Expression.Permanent)
				{
					e.Set(0, 1, 0, 1, 1);
					e.Activate(e.Expression.PermanentValue, 1.0f);
					e.Expression.SetAuto(0, 0, 0);
				}
			}

			for (int i = 0; i < MaxActive; ++i)
				NextActive();

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
				if (exps_[i].Expression.Permanent)
					continue;

				if (!foundActive && exps_[i].Active && exps_[i].Expression.Mood == mood)
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
					if (exps_[i].Expression.Permanent)
						continue;

					if (exps_[i].Expression.Mood == mood)
					{
						ActivateForEmergency(exps_[i], intensity, min, max, time);
						break;
					}
				}
			}
		}

		public void DebugSet(WeightedExpression e, float v)
		{
			U.BringToTop(exps_, e);
			e.Expression.SetTarget(v, 0);
		}

		public void FixedUpdate(float s)
		{
			slapUpdateInterval_.Update(s, person_.Mood.MovementEnergy);
			if (slapUpdateInterval_.Finished)
				slapUpdate_ = true;

			if (slapping_)
				slapElapsed_ += s;

			var ps = person_.Personality;

			if (!isOrgasming_ && person_.Mood.IsOrgasming())
			{
				isOrgasming_ = true;

				EmergencyExpression(
					MoodType.Orgasm, 1.0f,
					ps.Get(PS.OrgasmExpressionRangeMin),
					ps.Get(PS.OrgasmExpressionRangeMax),
					ps.Get(PS.OrgasmFirstExpressionTime));
			}
			else if (isOrgasming_ && !person_.Mood.IsOrgasming())
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
				if (exps_[i].Expression.Permanent)
					continue;

				if (exps_[i].Active)
					continue;

				totalWeight += exps_[i].Weight;
			}

			if (totalWeight > 0)
			{
				var r = U.RandomFloat(0, totalWeight);

				for (int i = 0; i < exps_.Length; ++i)
				{
					if (exps_[i].Expression.Permanent)
						continue;

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

					if (exps_[i].Expression.Permanent)
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

					if (exps_[i].Active && (exps_[i].Expression.Exclusive || exps_[i].Expression.Permanent))
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

			int[] countPerMood = new int[MoodType.Count];
			for (int i = 0; i < countPerMood.Length; ++i)
				countPerMood[i] = 0;

			for (int i = 0; i < exps_.Length; ++i)
			{
				if (exps_[i].Active && !exps_[i].Expression.Permanent)
					++countPerMood[exps_[i].Expression.Mood.Int];
			}

			for (int i = 0; i < exps_.Length; ++i)
			{
				if (exps_[i].Expression.Permanent)
					continue;

				var we = exps_[i];
				var e = we.Expression;
				var mv = m.GetMoodValue(e.Mood);

				float weight = e.DefaultWeight * mv.Value;
				float intensity = U.Clamp(mv.Value, mv.MinimumExpression, mv.MaximumExpression);

				if (m.Get(MoodType.Excited) < e.MinExcitement)
					weight = 0;

				if (e.Exclusive)
					weight *= ps.Get(PS.ExclusiveExpressionWeightModifier);

				if (e.Mood != MoodType.Tired)
					weight *= Math.Max(1 - expressionTiredness, 0.05f);

				if (countPerMood[e.Mood.Int] >= MaxActivePerMood)
					weight = 0;

				float speed = 1 - expressionTiredness;

				we.Set(weight, intensity, speed, mv.MinimumExpression, mv.MaximumExpression);
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
