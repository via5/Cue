using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue
{
	class ProceduralMorph
	{
		public const float NoDisableBlink = 10000;

		private const int NoState = 0;
		private const int ForwardState = 1;
		private const int DelayOnState = 2;
		private const int BackwardState = 3;
		private const int DelayOffState = 4;

		private Person person_;
		private string id_;
		private DAZMorph morph_ = null;
		private float start_, end_, mid_;
		private Duration forward_, backward_;
		private Duration delayOff_, delayOn_;
		private int state_ = NoState;
		private float r_ = 0;
		private float mag_ = 0;
		private IEasing easing_;
		private bool finished_ = false;
		private bool resetBetween_;
		private float last_;
		private bool closeToMid_ = false;
		private bool awayFromMid_ = false;
		private float timeActive_ = 0;

		private float disableBlinkAbove_ = NoDisableBlink;

		public ProceduralMorph(
			Person p, string id, float start, float end, float minTime, float maxTime,
			float delayOff, float delayOn, bool resetBetween=false)
		{
			person_ = p;
			id_ = id;

			morph_ = p.VamAtom.FindMorph(id);
			if (morph_ == null)
				Cue.LogError($"{p.ID}: morph '{id}' not found");

			start_ = start;
			end_ = end;
			mid_ = Mid();
			last_ = mid_;
			r_ = mid_;
			forward_ = new Duration(minTime, maxTime);
			backward_ = new Duration(minTime, maxTime);
			delayOff_ = new Duration(0, delayOff);
			delayOn_ = new Duration(0, delayOn);
			easing_ = new SinusoidalEasing();
			resetBetween_ = resetBetween;

			Reset();
		}

		public string Name
		{
			get
			{
				if (morph_ == null)
					return $"{id_} (not found)";
				else
					return morph_.morphName;
			}
		}

		public override string ToString()
		{
			return
				$"start={start_:0.##} end={end_:0.##} mid={mid_} last={last_}\n" +
				$"fwd={forward_} bwd={backward_} dOff={delayOff_} dOn={delayOn_}\n" +
				$"state={state_} r={r_:0.##} mag={mag_:0.##} f={finished_} morphValue={morph_?.morphValue ?? -1}";
		}

		public IEasing Easing
		{
			get { return easing_; }
			set { easing_ = value; }
		}

		public bool Finished
		{
			get { return finished_; }
		}

		public bool CloseToMid
		{
			get { return closeToMid_; }
		}

		public float DisableBlinkAbove
		{
			get { return disableBlinkAbove_; }
			set { disableBlinkAbove_ = value; }
		}

		public void Reset()
		{
			state_ = NoState;
			finished_ = false;

			forward_.Reset();
			backward_.Reset();
			delayOff_.Reset();
			delayOn_.Reset();

			if (morph_ != null)
				morph_.morphValue = morph_.startValue;
		}

		public void Update(float s, bool limitHit)
		{
			if (morph_ == null)
			{
				finished_ = true;
				return;
			}

			finished_ = false;
			timeActive_ += s;

			switch (state_)
			{
				case NoState:
				{
					mag_ = 0;
					state_ = ForwardState;
					Next(limitHit);
					break;
				}

				case ForwardState:
				{
					forward_.Update(s);

					if (forward_.Finished)
					{
						mag_ = 1;

						if (delayOn_.Enabled)
							state_ = DelayOnState;
						else
							state_ = BackwardState;
					}
					else
					{
						mag_ = forward_.Progress;
					}

					break;
				}

				case DelayOnState:
				{
					delayOn_.Update(s);

					if (delayOn_.Finished)
						state_ = BackwardState;

					break;
				}

				case BackwardState:
				{
					if (!resetBetween_)
					{
						Next(limitHit);
						mag_ = 0;
						state_ = ForwardState;
						finished_ = true;
						break;
					}

					backward_.Update(s);
					mag_ = 1 - backward_.Progress;

					if (backward_.Finished)
					{
						Next(limitHit);

						if (delayOff_.Enabled)
						{
							state_ = DelayOffState;
						}
						else
						{
							state_ = ForwardState;
							finished_ = true;
						}
					}

					break;
				}

				case DelayOffState:
				{
					delayOff_.Update(s);

					if (delayOff_.Finished)
					{
						state_ = ForwardState;
						finished_ = true;
					}

					break;
				}
			}
		}

		private float Mid()
		{
			return morph_.startValue;
		}

		public float Set(float intensity, float max)
		{
			closeToMid_ = false;

			if (morph_ == null)
				return 0;

			var v = Mathf.Lerp(last_, r_, easing_.Magnitude(mag_)) * intensity;
			if (Math.Abs(v - mid_) > max)
				v = mid_ + Math.Sign(v) * max;

			var d = v - mid_;

			morph_.morphValue = v;

			if (disableBlinkAbove_ != NoDisableBlink)
				person_.Gaze.Eyes.Blink = (d < disableBlinkAbove_);

			d = Math.Abs(d);

			if (d < 0.01f)
				timeActive_ = 0;

			if (awayFromMid_ && d < 0.01f)
			{
				closeToMid_ = true;
				awayFromMid_ = false;
			}
			else if (d > 0.01f)
			{
				awayFromMid_ = true;
			}

			return d;
		}

		private void Next(bool limitHit)
		{
			if (resetBetween_)
				last_ = mid_;
			else
				last_ = r_;

			if (limitHit && timeActive_ >= 10)
			{
				Cue.LogVerbose(
					$"{person_.ID} {Name}: active for {timeActive_:0.00}, " +
					$"too long, forcing to 0");

				// force a reset to allow other morphs to take over the prio
				r_ = mid_;
				timeActive_ = 0;
			}
			else
			{
				r_ = U.RandomFloat(start_, end_);
			}
		}
	}


	interface IProceduralMorphGroup
	{
		void Reset();
		void Update(float s);
		void Set(float intensity);
		List<ProceduralMorph> Morphs { get; }
	}


	class ConcurrentProceduralMorphGroup : IProceduralMorphGroup
	{
		private readonly List<ProceduralMorph> morphs_ = new List<ProceduralMorph>();
		private float maxMorphs_ = 1.0f;
		private bool limited_ = false;

		public List<ProceduralMorph> Morphs
		{
			get { return morphs_; }
		}

		public float Max
		{
			get { return maxMorphs_; }
		}

		public void Add(ProceduralMorph m)
		{
			morphs_.Add(m);
		}

		public void Reset()
		{
			for (int i = 0; i < morphs_.Count; ++i)
				morphs_[i].Reset();
		}

		public void Update(float s)
		{
			int i = 0;
			int count = morphs_.Count;

			while (i < count)
			{
				var m = morphs_[i];

				m.Update(s, limited_);

				// move morphs that are close to the start value to the end of
				// the list so they don't always have prio for max morph
				if (limited_ && (i < (count - 1)) && m.CloseToMid)
				{
					Cue.LogVerbose($"moving {m.Name} to end");
					morphs_.RemoveAt(i);
					morphs_.Add(m);
					--count;
				}
				else
				{
					++i;
				}
			}
		}

		public void Set(float intensity)
		{
			float remaining = maxMorphs_;
			for (int i = 0; i < morphs_.Count; ++i)
				remaining -= morphs_[i].Set(intensity, remaining);

			limited_ = (remaining <= 0);
		}

		public override string ToString()
		{
			return $"concurrent max={maxMorphs_}";
		}
	}


	class SequentialProceduralMorphGroup : IProceduralMorphGroup
	{
		private const int ActiveState = 1;
		private const int DelayState = 2;

		private readonly List<ProceduralMorph> morphs_ = new List<ProceduralMorph>();
		private int i_ = 0;
		private Duration delay_;
		private int state_ = ActiveState;

		public SequentialProceduralMorphGroup(Duration delay = null)
		{
			delay_ = delay ?? new Duration(0, 0);
		}

		public List<ProceduralMorph> Morphs
		{
			get { return morphs_; }
		}

		public int Current
		{
			get { return i_; }
		}

		public Duration Delay
		{
			get { return delay_; }
		}

		public int State
		{
			get { return state_; }
		}

		public void Add(ProceduralMorph m)
		{
			morphs_.Add(m);
		}

		public void Reset()
		{
			i_ = 0;
			state_ = ActiveState;

			for (int i = 0; i < morphs_.Count; ++i)
				morphs_[i].Reset();
		}

		public void Update(float s)
		{
			if (morphs_.Count == 0)
				return;

			switch (state_)
			{
				case ActiveState:
				{
					if (morphs_[i_].Finished)
					{
						++i_;
						if (i_ >= morphs_.Count)
						{
							i_ = 0;
							if (delay_.Enabled)
								state_ = DelayState;
						}

						if (state_ == ActiveState)
							morphs_[i_].Update(s, false);
					}
					else
					{
						morphs_[i_].Update(s, false);
					}

					break;
				}

				case DelayState:
				{
					delay_.Update(s);

					if (delay_.Finished)
						state_ = ActiveState;

					break;
				}
			}
		}

		public void Set(float intensity)
		{
			if (morphs_.Count == 0)
				return;

			morphs_[i_].Set(intensity, float.MaxValue);
		}

		public override string ToString()
		{
			return $"sequential i={i_} delay={delay_} state={state_}";
		}
	}


	class ProceduralExpressionType
	{
		private readonly int type_;
		private float intensity_ = 0;
		private readonly List<IProceduralMorphGroup> groups_ =
			new List<IProceduralMorphGroup>();

		public ProceduralExpressionType(Person p, int type)
		{
			type_ = type;
		}

		public int Type
		{
			get { return type_; }
		}

		public float Intensity
		{
			get { return intensity_; }
			set { intensity_ = value; }
		}

		public List<IProceduralMorphGroup> Groups
		{
			get { return groups_; }
		}

		public void Reset()
		{
			for (int i = 0; i < groups_.Count; ++i)
				groups_[i].Reset();
		}

		public void Update(float s)
		{
			for (int i = 0; i < groups_.Count; ++i)
				groups_[i].Update(s);

			for (int i = 0; i < groups_.Count; ++i)
				groups_[i].Set(intensity_);
		}

		public override string ToString()
		{
			return
				Expressions.ToString(type_) + " " +
				"intensity=" + intensity_.ToString();
		}
	}


	class PE
	{
		public static IProceduralMorphGroup Smile(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();
			g.Add(new ProceduralMorph(p, "Smile Open Full Face", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup CornerSmile(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();
			g.Add(new ProceduralMorph(p, "Mouth Smile Simple Left", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Pleasure(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();

			g.Add(new ProceduralMorph(p, "07-Extreme Pleasure", 0, 1, 1, 5, 2, 2));
			g.Add(new ProceduralMorph(p, "Pain", 0, 0.5f, 1, 5, 2, 2));
			g.Add(new ProceduralMorph(p, "Shock", 0, 1, 1, 5, 2, 2));
			g.Add(new ProceduralMorph(p, "Scream", 0, 1, 1, 5, 2, 2));
			g.Add(new ProceduralMorph(p, "Angry", 0, 0.3f, 1, 5, 2, 2));

			return g;
		}

		public static IProceduralMorphGroup EyesRollBack(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();

			if (p.Sex == Sexes.Female)
				g.Add(new ProceduralMorph(p, "Eye Roll Back_DD", 0, 0.5f, 1, 5, 2, 2));

			return g;
		}

		public static IProceduralMorphGroup EyesClosed(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();

			var eyesClosed = new ProceduralMorph(p, "Eyes Closed", 0, 1, 0.5f, 5, 2, 2);
			eyesClosed.DisableBlinkAbove = 0.5f;
			g.Add(eyesClosed);

			return g;
		}

		public static IProceduralMorphGroup Swallow(Person p)
		{
			var g = new SequentialProceduralMorphGroup(
				new Duration(40, 60));


			var m = new ProceduralMorph(p, "Mouth Open",
				-0.1f, -0.1f, 0.3f, 0.3f, 0, 0, true);
			m.Easing = new SineOutEasing();

			g.Add(m);


			m = new ProceduralMorph(p, "deepthroat",
				0.1f, 0.1f, 0.2f, 0.2f, 0, 0, true);

			g.Add(m);


			m = new ProceduralMorph(p, "Mouth Open",
				0.1f, 0.2f, 0.3f, 0.3f, 0, 0, true);
			m.Easing = new SineOutEasing();

			g.Add(m);

			return g;
		}
	}


	class ProceduralExpression : IExpression
	{
		private Person person_;
		private bool enabled_ = true;

		private readonly List<ProceduralExpressionType> expressions_ =
			new List<ProceduralExpressionType>();


		public ProceduralExpression(Person p)
		{
			person_ = p;
			expressions_.Add(CreateCommon(p));
			expressions_.Add(CreateHappy(p));
			expressions_.Add(CreateMischievous(p));
			expressions_.Add(CreatePleasure(p));
		}

		public List<ProceduralExpressionType> All
		{
			get { return expressions_; }
		}

		private ProceduralExpressionType CreateCommon(Person p)
		{
			var e = new ProceduralExpressionType(p, Expressions.Common);
			e.Intensity = 1;

			e.Groups.Add(PE.Swallow(p));

			return e;
		}

		private ProceduralExpressionType CreateHappy(Person p)
		{
			var e = new ProceduralExpressionType(p, Expressions.Happy);

			e.Groups.Add(PE.Smile(p));

			return e;
		}

		private ProceduralExpressionType CreateMischievous(Person p)
		{
			var e = new ProceduralExpressionType(p, Expressions.Mischievous);

			e.Groups.Add(PE.CornerSmile(p));

			return e;
		}

		private ProceduralExpressionType CreatePleasure(Person p)
		{
			var e = new ProceduralExpressionType(p, Expressions.Pleasure);

			e.Groups.Add(PE.Pleasure(p));

			return e;
		}

		public void Set(Pair<int, float>[] intensities, bool resetOthers = false)
		{
			// todo: let morphs go back to normal

			for (int i = 0; i < expressions_.Count; ++i)
			{
				bool found = false;

				for (int j = 0; j < intensities.Length; ++j)
				{
					if (intensities[j].first == expressions_[i].Type)
					{
						expressions_[i].Intensity = intensities[j].second;
						found = true;
						break;
					}
				}

				if (!found && resetOthers)
					expressions_[i].Intensity = 0;
			}
		}

		public bool Enabled
		{
			get
			{
				return enabled_;
			}

			set
			{
				enabled_ = value;
			}
		}

		public void MakeNeutral()
		{
			Reset();
		}

		public void Reset()
		{
			for (int i = 0; i < expressions_.Count; ++i)
				expressions_[i].Reset();
		}

		public void Update(float s)
		{
			if (!enabled_)
				return;

			for (int i = 0; i < expressions_.Count; ++i)
				expressions_[i].Update(s);
		}

		public void OnPluginState(bool b)
		{
			Reset();
		}

		public void DumpActive()
		{
			var mui = Cue.Instance.VamSys.GetMUI(person_.VamAtom.Atom);

			foreach (var m in mui.GetMorphs())
			{
				if (m.morphValue != m.startValue)
					Cue.LogInfo($"name='{m.morphName}' dname='{m.displayName}'");
			}
		}
	}
}
