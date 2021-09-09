using System.Collections.Generic;

namespace Cue.Proc
{
	interface IProceduralMorphGroup
	{
		string Name { get; }
		List<MorphTarget> Morphs { get; }

		void Reset();
		void FixedUpdate(float s, float intensity);
		void ForceChange();
		void Set();
	}


	abstract class BasicProceduralMorphGroup : IProceduralMorphGroup
	{
		private string name_;
		protected readonly List<MorphTarget> morphs_ = new List<MorphTarget>();

		protected BasicProceduralMorphGroup(string name)
		{
			name_ = name;
		}

		public string Name { get { return name_; } }

		public List<MorphTarget> Morphs
		{
			get { return morphs_; }
		}

		public void Add(MorphTarget m)
		{
			m.AutoSet = false;
			morphs_.Add(m);
		}

		public virtual void Reset()
		{
			for (int i = 0; i < morphs_.Count; ++i)
				morphs_[i].Reset();
		}

		public abstract void FixedUpdate(float s, float intensity);
		public abstract void ForceChange();
		public abstract void Set();
	}


	class ConcurrentProceduralMorphGroup : BasicProceduralMorphGroup
	{
		private float maxMorphs_ = 1.0f;
		private bool limited_ = false;

		public ConcurrentProceduralMorphGroup(string name)
			: base(name)
		{
		}

		public float Max
		{
			get { return maxMorphs_; }
		}

		public override void FixedUpdate(float s, float intensity)
		{
			int i = 0;
			int count = morphs_.Count;

			while (i < count)
			{
				var m = morphs_[i];

				m.Intensity = intensity;
				m.LimitHit = limited_;
				m.FixedUpdate(s);

				// move morphs that are close to the start value to the end of
				// the list so they don't always have prio for max morph
				if (limited_ && (i < (count - 1)) && m.CloseToMid)
				{
					Cue.LogVerbose($"moving {m} to end");
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

		public override void ForceChange()
		{
			for (int i = 0; i < morphs_.Count; ++i)
				morphs_[i].ForceChange();
		}

		public override void Set()
		{
			float remaining = maxMorphs_;
			for (int i = 0; i < morphs_.Count; ++i)
				remaining -= morphs_[i].Set(remaining);

			limited_ = (remaining <= 0);
		}

		public override string ToString()
		{
			return $"con {Name} max={maxMorphs_}";
		}
	}


	class SequentialProceduralMorphGroup : BasicProceduralMorphGroup
	{
		private const int ActiveState = 1;
		private const int DelayState = 2;

		private int i_ = 0;
		private Duration delay_;
		private int state_ = ActiveState;

		public SequentialProceduralMorphGroup(string name)
			: this(name, new Duration())
		{
		}

		public SequentialProceduralMorphGroup(string name, Duration delay)
			: base(name)
		{
			delay_ = delay;
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

		public override void Reset()
		{
			base.Reset();
			i_ = 0;
			state_ = ActiveState;
		}

		public override void FixedUpdate(float s, float intensity)
		{
			if (morphs_.Count == 0)
				return;

			switch (state_)
			{
				case ActiveState:
				{
					if (morphs_[i_].Done)
					{
						++i_;
						if (i_ >= morphs_.Count)
						{
							i_ = 0;
							if (delay_.Enabled)
								state_ = DelayState;
						}

						if (state_ == ActiveState)
						{
							morphs_[i_].Intensity = intensity;
							morphs_[i_].FixedUpdate(s);
						}
					}
					else
					{
						morphs_[i_].Intensity = intensity;
						morphs_[i_].FixedUpdate(s);
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

		public override void ForceChange()
		{
			for (int i = 0; i < morphs_.Count; ++i)
				morphs_[i].ForceChange();
		}

		public override void Set()
		{
			if (morphs_.Count == 0)
				return;

			morphs_[i_].Set(float.MaxValue);
		}

		public override string ToString()
		{
			return $"seq {Name} i={i_} delay={delay_} state={state_}";
		}
	}
}
