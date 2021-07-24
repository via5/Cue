using System.Collections.Generic;

namespace Cue.Proc
{
	interface IProceduralMorphGroup
	{
		string Name { get; }
		List<ClampableMorph> Morphs { get; }

		void Reset();
		void FixedUpdate(float s, float intensity);
		void ForceChange();
		void Set();
	}


	abstract class BasicProceduralMorphGroup : IProceduralMorphGroup
	{
		private string name_;

		protected BasicProceduralMorphGroup(string name)
		{
			name_ = name;
		}

		public string Name { get { return name_; } }

		public abstract List<ClampableMorph> Morphs { get; }
		public abstract void FixedUpdate(float s, float intensity);
		public abstract void ForceChange();
		public abstract void Reset();
		public abstract void Set();
	}


	class ConcurrentProceduralMorphGroup : BasicProceduralMorphGroup
	{
		private readonly List<ClampableMorph> morphs_ = new List<ClampableMorph>();
		private float maxMorphs_ = 1.0f;
		private bool limited_ = false;

		public ConcurrentProceduralMorphGroup(string name)
			: base(name)
		{
		}

		public override List<ClampableMorph> Morphs
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

		public override void Reset()
		{
			for (int i = 0; i < morphs_.Count; ++i)
				morphs_[i].Reset();
		}

		public override void FixedUpdate(float s, float intensity)
		{
			int i = 0;
			int count = morphs_.Count;

			while (i < count)
			{
				var m = morphs_[i];

				m.FixedUpdate(s, intensity, limited_);

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

		private readonly List<ClampableMorph> morphs_ = new List<ClampableMorph>();
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

		public override List<ClampableMorph> Morphs
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

		public override void Reset()
		{
			i_ = 0;
			state_ = ActiveState;

			for (int i = 0; i < morphs_.Count; ++i)
				morphs_[i].Reset();
		}

		public override void FixedUpdate(float s, float intensity)
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
							morphs_[i_].FixedUpdate(s, intensity, false);
					}
					else
					{
						morphs_[i_].FixedUpdate(s, intensity, false);
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
