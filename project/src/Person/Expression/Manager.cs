using System;
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
			exps_ = new WeightedExpression[all.Length];

			for (int i = 0; i < all.Length; ++i)
			{
				all[i].Init(person_);
				exps_[i] = new WeightedExpression(all[i]);
			}

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
						exps_[i].Set(1, 1, 1, 0.9f, 1.0f);
						exps_[i].Activate(1.0f, 0.2f);
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
							exps_[i].Set(1, 1, 1, 0.9f, 1.0f);
							exps_[i].Activate(1.0f, 0.2f);
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
					{
						exps_[i].Activate();
						return true;
					}

					r -= exps_[i].Weight;
				}
			}

			return false;
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


				if (isOrgasming_)
				{
					if (e.IsMood(Moods.Orgasm))
					{
						weight += 1;
						intensity = 1;
						min = 0.8f;
						max = 1.0f;
					}
				}
				else
				{
					if (e.IsMood(Moods.Happy))
					{
						weight += m.Get(Moods.Happy);
						intensity = Math.Max(intensity, m.Get(Moods.Happy));
					}

					if (e.IsMood(Moods.Excited))
					{
						weight += m.Get(Moods.Excited) * 2;
						intensity = Math.Max(intensity, m.Get(Moods.Excited));
					}

					if (e.IsMood(Moods.Angry))
					{
						weight += m.Get(Moods.Angry);
						intensity = Math.Max(intensity, m.Get(Moods.Angry));
					}

					if (e.IsMood(Moods.Tired))
					{
						weight += expressionTiredness;
						intensity = Math.Max(intensity, expressionTiredness);
					}
				}


				if (!e.IsMood(Moods.Tired))
				{
					weight *= Math.Max(1 - expressionTiredness, 0.05f);
				}


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
				s[i++] = $"{exps_[j]}";

			s[i++] = "";
			s[i++] = (needsMore_ ? "needs more" : "");

			return s;
		}
	}
}
