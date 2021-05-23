using System.Collections.Generic;

namespace Cue.Proc
{
	interface IProceduralMorphGroup
	{
		void Reset();
		void FixedUpdate(float s);
		void Set(float intensity);
		List<ClampableMorph> Morphs { get; }
	}


	class ConcurrentProceduralMorphGroup : IProceduralMorphGroup
	{
		private readonly List<ClampableMorph> morphs_ = new List<ClampableMorph>();
		private float maxMorphs_ = 1.0f;
		private bool limited_ = false;

		public List<ClampableMorph> Morphs
		{
			get { return morphs_; }
		}

		public float Max
		{
			get { return maxMorphs_; }
		}

		public void Add(ClampableMorph m)
		{
			morphs_.Add(m);
		}

		public void Reset()
		{
			for (int i = 0; i < morphs_.Count; ++i)
				morphs_[i].Reset();
		}

		public void FixedUpdate(float s)
		{
			int i = 0;
			int count = morphs_.Count;

			while (i < count)
			{
				var m = morphs_[i];

				m.FixedUpdate(s, limited_);

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

		private readonly List<ClampableMorph> morphs_ = new List<ClampableMorph>();
		private int i_ = 0;
		private Duration delay_;
		private int state_ = ActiveState;

		public SequentialProceduralMorphGroup()
			: this(new Duration())
		{
		}

		public SequentialProceduralMorphGroup(Duration delay)
		{
			delay_ = delay;
		}

		public List<ClampableMorph> Morphs
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

		public void Add(ClampableMorph m)
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

		public void FixedUpdate(float s)
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
							morphs_[i_].FixedUpdate(s, false);
					}
					else
					{
						morphs_[i_].FixedUpdate(s, false);
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
}
