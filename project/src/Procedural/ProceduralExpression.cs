using System;
using System.Collections.Generic;

namespace Cue
{
	class ProceduralMorph
	{
		private const int NoState = 0;
		private const int ForwardState = 1;
		private const int DelayOnState = 2;
		private const int BackwardState = 3;
		private const int DelayOffState = 4;

		private DAZMorph morph_ = null;
		private float start_, end_;
		private Duration forward_, backward_;
		private Duration delayOff_, delayOn_;
		private int state_ = NoState;
		private float r_ = 0;
		private float mag_ = 0;
		private IEasing easing_;
		private bool finished_ = false;

		public ProceduralMorph(
			Person p, string id, float start, float end, float minTime, float maxTime,
			float delayOff, float delayOn)
		{
			morph_ = p.VamAtom.FindMorph(id);
			if (morph_ == null)
				Cue.LogError($"{p.ID}: morph '{id}' not found");

			start_ = start;
			end_ = end;
			forward_ = new Duration(minTime, maxTime);
			backward_ = new Duration(minTime, maxTime);
			delayOff_ = new Duration(0, delayOff);
			delayOn_ = new Duration(0, delayOn);
			easing_ = new SinusoidalEasing();

			Reset();
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

		public float Update(float s, float max)
		{
			if (morph_ == null)
			{
				finished_ = true;
				return 0;
			}

			finished_ = false;

			switch (state_)
			{
				case NoState:
				{
					mag_ = 0;
					state_ = ForwardState;
					Next(max);
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
					backward_.Update(s);

					if (backward_.Finished)
					{
						mag_ = 0;
						Next(max);

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
					else
					{
						mag_ = 1 - backward_.Progress;
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

			return Math.Abs(Mid() - r_);
		}

		private float Mid()
		{
			float sv = morph_.startValue;

			if (sv >= start_ && sv <= end_)
				return sv;
			else
				return start_ + (end_ - start_) / 2;
		}

		public void Set(float intensity)
		{
			if (morph_ == null)
				return;

			morph_.morphValue =
				morph_.startValue +
				easing_.Magnitude(mag_) * r_ * intensity;
		}

		private void Next(float max)
		{
			r_ = U.RandomFloat(start_, end_);

			float mid = Mid();
			var d = Math.Abs(mid - r_);

			if (d > max)
				r_ = mid + Math.Sign(r_) * max;
		}
	}


	interface IProceduralMorphGroup
	{
		void Reset();
		void Update(float s);
		void Set(float intensity);
	}


	class ConcurrentProceduralMorphGroup : IProceduralMorphGroup
	{
		private readonly List<ProceduralMorph> morphs_ = new List<ProceduralMorph>();
		private float maxMorphs_ = 1.0f;

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
			float remaining = maxMorphs_;

			for (int i = 0; i < morphs_.Count; ++i)
				remaining -= morphs_[i].Update(s, remaining);
		}

		public void Set(float intensity)
		{
			for (int i = 0; i < morphs_.Count; ++i)
				morphs_[i].Set(intensity);
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
					morphs_[i_].Update(s, float.MaxValue);

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
							morphs_[i_].Update(s, float.MaxValue);
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

			morphs_[i_].Set(intensity);
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

		public static IProceduralMorphGroup Swallow(Person p)
		{
			var g = new SequentialProceduralMorphGroup(
				new Duration(40, 60));


			var m = new ProceduralMorph(p, "Mouth Open",
				-0.1f, -0.1f, 0.3f, 0.3f, 0, 0);
			m.Easing = new SineOutEasing();

			g.Add(m);


			m = new ProceduralMorph(p, "deepthroat",
				0.1f, 0.1f, 0.2f, 0.2f, 0, 0);

			g.Add(m);


			m = new ProceduralMorph(p, "Mouth Open",
				0.0f, 0.1f, 0.3f, 0.3f, 0, 0);
			m.Easing = new SineOutEasing();

			g.Add(m);

			return g;
		}
	}


	class ProceduralExpression : IExpression
	{
		private bool enabled_ = true;

		private readonly List<ProceduralExpressionType> expressions_ =
			new List<ProceduralExpressionType>();


		public ProceduralExpression(Person p)
		{
			expressions_.Add(CreateCommon(p));
			expressions_.Add(CreateHappy(p));
			expressions_.Add(CreateMischievous(p));
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
	}
}
