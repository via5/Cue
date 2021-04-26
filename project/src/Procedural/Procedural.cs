using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue
{
	class ProceduralPlayer : IPlayer
	{
		private ProceduralAnimation anim_ = null;

		public bool Playing
		{
			get
			{
				return (anim_ != null);
			}
		}

		public bool Play(IAnimation a, int flags)
		{
			if (anim_ == a)
				return true;

			anim_ = a as ProceduralAnimation;
			if (anim_ == null)
				return false;

			anim_.Reset();
			return true;
		}

		public void Stop(bool rewind)
		{
			anim_ = null;
		}

		public void FixedUpdate(float s)
		{
			if (anim_ != null)
				anim_.FixedUpdate(s);
		}

		public void Update(float s)
		{
			if (anim_ != null)
				anim_.Update(s);
		}

		public override string ToString()
		{
			return "Procedural: " + (anim_ == null ? "(none)" : anim_.ToString());
		}
	}


	class ProceduralAnimation : BasicAnimation
	{
		class Part
		{
			private Rigidbody rb_ = null;
			private Vector3 force_;
			private float time_;
			private float elapsed_ = 0;
			private bool fwd_ = true;

			public Part(Rigidbody rb, Vector3 f, float t)
			{
				rb_ = rb;
				force_ = f;
				time_ = t;
			}

			public void Reset()
			{
				elapsed_ = 0;
				fwd_ = true;
			}

			public void Update(float s)
			{
				elapsed_ += s;
				if (elapsed_ >= time_)
				{
					elapsed_ = 0;
					fwd_ = !fwd_;
				}

				float p = (fwd_ ? (elapsed_ / time_) : ((time_ - elapsed_) / time_));
				var f = force_ * p;

				rb_.AddForce(Vector3.ToUnity(f));
			}

			public override string ToString()
			{
				return rb_.name;
			}
		}


		private Person person_;
		private string name_;
		private readonly List<Part> parts_ = new List<Part>();

		public ProceduralAnimation(Person p, string name)
		{
			person_ = p;
			name_ = name;
		}

		public void Add(string rbId, Vector3 f, float time)
		{
			var rb = Cue.Instance.VamSys.FindRigidbody(
				((W.VamAtom)person_.Atom).Atom, rbId);

			parts_.Add(new Part(rb, f, time));
		}

		public void Reset()
		{
			for (int i = 0; i < parts_.Count; ++i)
				parts_[i].Reset();
		}

		public void Update(float s)
		{
			for (int i = 0; i < parts_.Count; ++i)
				parts_[i].Update(s);
		}

		public void FixedUpdate(float s)
		{
		}

		public override string ToString()
		{
			string s = name_;

			if (parts_.Count == 0)
				s += " (empty)";
			else
				s += " (" + parts_[0].ToString() + ")";

			return s;
		}
	}


	class Duration
	{
		private float min_, max_;
		private float current_ = 0;
		private float elapsed_ = 0;
		private bool finished_ = false;

		public Duration(float min, float max)
		{
			min_ = min;
			max_ = max;
			Next();
		}

		public bool Finished
		{
			get { return finished_; }
		}

		public bool Enabled
		{
			get { return min_ != max_; }
		}

		public float Progress
		{
			get { return U.Clamp(elapsed_ / current_, 0, 1); }
		}

		public void Reset()
		{
			elapsed_ = 0;
			finished_ = false;
			Next();
		}

		public void Update(float s)
		{
			finished_ = false;

			elapsed_ += s;
			if (elapsed_ > current_)
			{
				elapsed_ = 0;
				finished_ = true;
				Next();
			}
		}

		private void Next()
		{
			current_ = U.RandomFloat(min_, max_);
		}
	}


	class ProceduralExpression : IExpression
	{
		class Morph
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

			public Morph(
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

				Reset();
			}

			public void Reset()
			{
				state_= NoState;
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
				morph_.morphValue = morph_.startValue + mag_ * r_ * intensity;
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


		class MorphGroup
		{
			private readonly List<Morph> morphs_ = new List<Morph>();
			private float maxMorphs_ = 1.0f;

			public void Add(Morph m)
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


		class Expression
		{
			private readonly int type_;
			private float intensity_ = 0;
			private readonly List<MorphGroup> groups_ = new List<MorphGroup>();

			public Expression(Person p, int type)
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

			public List<MorphGroup> Groups
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


		private bool enabled_ = true;
		private readonly List<Expression> expressions_ = new List<Expression>();

		public ProceduralExpression(Person p)
		{
			var e = new Expression(p, Expressions.Happy);
			var g = new MorphGroup();
			g.Add(new Morph(p, "Smile Open Full Face", 0, 1, 1, 5, 2, 2));
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
