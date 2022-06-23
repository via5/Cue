﻿using System;
using System.Collections.Generic;

namespace Cue
{
	public class ExpressionManager
	{
		private const int MaxActive = 4;
		private const int MaxEmergency = 1;
		private const float MoreCheckInterval = 1;

		private Person person_;
		private WeightedExpression[] exps_ = new WeightedExpression[0];
		private bool needsMore_ = false;
		private float moreElapsed_ = 0;
		private Personality lastPersonality_ = null;
		private bool enabled_ = true;
		private bool isOrgasming_ = false;

		public ExpressionManager(Person p)
		{
			person_ = p;
		}

		public Expression[] GetExpressionsForMood(int mood)
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
		}

		public void FixedUpdate(float s)
		{
			if (lastPersonality_ != person_.Personality)
				Init();

			if (!isOrgasming_ && person_.Mood.State == Mood.OrgasmState)
			{
				isOrgasming_ = true;
				bool foundActive = false;

				for (int i = 0; i < exps_.Length; ++i)
				{
					if (!foundActive && exps_[i].Active && exps_[i].Expression.IsMood(Moods.Orgasm))
					{
						foundActive = true;
						ActivateForOrgasm(exps_[i]);
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
						if (exps_[i].Expression.IsMood(Moods.Orgasm))
						{
							ActivateForOrgasm(exps_[i]);
							break;
						}
					}
				}
			}
			else if (isOrgasming_ && person_.Mood.State != Mood.OrgasmState)
			{
				isOrgasming_ = false;
			}


			int finished = 0;
			int activeCount = 0;

			for (int i = 0; i < exps_.Length; ++i)
			{
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

					if (moreElapsed_ > MoreCheckInterval)
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
							exps_[i].Deactivate();
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

		private void ActivateForOrgasm(WeightedExpression e)
		{
			var ps = person_.Personality;

			e.Set(
				1, 1, 1,
				ps.Get(PS.OrgasmExpressionRangeMin),
				ps.Get(PS.OrgasmExpressionRangeMax),
				Moods.Orgasm);

			e.Activate(1.0f, ps.Get(PS.OrgasmFirstExpressionTime));
		}

		private void UpdateExpressions()
		{
			var m = person_.Mood;
			var ps = person_.Personality;

			float expressionTiredness = U.Clamp(
				m.Get(Moods.Tired) * ps.Get(PS.ExpressionTirednessFactor),
				0, 1);


			for (int i = 0; i < exps_.Length; ++i)
			{
				var we = exps_[i];
				var e = we.Expression;

				float weight = 0;
				float intensity = 0;
				float min = 0, max = 1;

				int highestMood = Moods.None;
				float highestMoodValue = 0;

				if (isOrgasming_)
				{
					if (e.IsMood(Moods.Orgasm))
					{
						if (m.Get(Moods.Orgasm) > highestMoodValue)
						{
							highestMood = Moods.Orgasm;
							highestMoodValue = m.Get(Moods.Orgasm);
						}

						weight += 1;
						intensity = 1;
						min = ps.Get(PS.OrgasmExpressionRangeMin);
						max = ps.Get(PS.OrgasmExpressionRangeMax);
					}
				}
				else
				{
					if (e.IsMood(Moods.Happy))
					{
						if (m.Get(Moods.Happy) > highestMoodValue)
						{
							highestMood = Moods.Happy;
							highestMoodValue = m.Get(Moods.Happy);
						}

						weight += m.Get(Moods.Happy);
						intensity = Math.Max(intensity, m.Get(Moods.Happy));
					}

					if (e.IsMood(Moods.Excited))
					{
						if (m.Get(Moods.Excited) > highestMoodValue)
						{
							highestMood = Moods.Excited;
							highestMoodValue = m.Get(Moods.Excited);
						}

						weight += m.Get(Moods.Excited) * ps.Get(PS.ExcitedExpressionWeightModifier);
						intensity = Math.Max(intensity, m.Get(Moods.Excited));
						intensity = Math.Min(intensity, ps.Get(PS.MaxExcitedExpression));
					}

					if (e.IsMood(Moods.Playful))
					{
						if (m.Get(Moods.Playful) > highestMoodValue)
						{
							highestMood = Moods.Playful;
							highestMoodValue = m.Get(Moods.Playful);
						}

						weight += m.Get(Moods.Playful);
						intensity = Math.Max(intensity, m.Get(Moods.Playful));
					}

					if (e.IsMood(Moods.Angry))
					{
						if (m.Get(Moods.Angry) > highestMoodValue)
						{
							highestMood = Moods.Angry;
							highestMoodValue = m.Get(Moods.Angry);
						}

						weight += m.Get(Moods.Angry);
						intensity = Math.Max(intensity, m.Get(Moods.Angry));
					}

					if (e.IsMood(Moods.Tired))
					{
						if (m.Get(Moods.Tired) > highestMoodValue)
						{
							highestMood = Moods.Tired;
							highestMoodValue = m.Get(Moods.Tired);
						}

						weight += expressionTiredness;
						intensity = Math.Max(intensity, expressionTiredness);
					}

					if (m.Get(Moods.Excited) < e.MinExcitement)
						weight = 0;

					if (e.Exclusive)
						weight *= ps.Get(PS.ExclusiveExpressionWeightModifier);
				}


				if (!e.IsMood(Moods.Tired))
				{
					weight *= Math.Max(1 - expressionTiredness, 0.05f);
				}


				float speed = 1 - expressionTiredness;

				we.Set(weight, intensity, speed, min, max, highestMood);
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

		public string[] Debug()
		{
			var s = new string[MaxActive + MaxEmergency + 1 + exps_.Length + 2];

			int i = 0;

			for (int j = 0; j < exps_.Length; ++j)
			{
				if (exps_[j].Active)
					s[i++] = $"{exps_[j].ToDetailedString()}";
			}

			while (i < MaxActive)
				s[i++] = "none";

			while (i < (MaxEmergency + MaxActive))
				s[i++] = "none (emergency)";

			s[i++] = "";

			for (int j = 0; j < exps_.Length; ++j)
				s[i++] = $"{exps_[j].ToDetailedString()}";

			s[i++] = "";
			s[i++] = (needsMore_ ? "needs more" : "");

			return s;
		}
	}
}
