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

		public void Reset()
		{
			state_ = NoState;
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
				return 0;

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
							state_ = DelayOffState;
						else
							state_ = ForwardState;
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
						state_ = ForwardState;

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


	class ProceduralMorphGroup
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


	class ProceduralExpressionType
	{
		private readonly int type_;
		private float intensity_ = 0;
		private readonly List<ProceduralMorphGroup> groups_ = new List<ProceduralMorphGroup>();

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

		public List<ProceduralMorphGroup> Groups
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


	class ProceduralExpression : IExpression
	{
		private bool enabled_ = true;
		private readonly List<ProceduralExpressionType> expressions_ =
			new List<ProceduralExpressionType>();

		public ProceduralExpression(Person p)
		{
			var e = new ProceduralExpressionType(p, Expressions.Happy);
			var g = new ProceduralMorphGroup();
			g.Add(new ProceduralMorph(p, "Smile Open Full Face", 0, 1, 1, 5, 2, 2));
			e.Groups.Add(g);

			expressions_.Add(e);
		}

		public void Set(int type, float f)
		{
			for (int i = 0; i < expressions_.Count; ++i)
			{
				if (expressions_[i].Type == type)
					expressions_[i].Intensity = f;
				else
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
