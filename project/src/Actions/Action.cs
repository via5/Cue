using Leap.Unity;
using System.Collections.Generic;

namespace Cue
{
	interface IAction
	{
		bool IsIdle { get; }
		bool Tick(IObject o, float s);
	}

	abstract class BasicAction : IAction
	{
		protected readonly List<IAction> children_ = new List<IAction>();
		private bool started_ = false;

		public bool IsIdle
		{
			get { return children_.Count == 0; }
		}

		public void Push(IAction a)
		{
			children_.Add(a);
		}

		public void Pop()
		{
			if (children_.Count == 0)
			{
				Cue.LogError("no action to pop");
				return;
			}

			children_.RemoveAt(children_.Count - 1);
		}

		public void Clear()
		{
			children_.Clear();
		}

		public bool Tick(IObject o, float s)
		{
			if (!started_)
			{
				started_ = true;
				if (!DoStart(o, s))
					return false;
			}

			if (!DoTick(o, s))
				return false;

			TickChildren(o, s);
			return true;
		}

		protected virtual bool TickChildren(IObject o, float s)
		{
			while (children_.Count > 0)
			{
				var a = children_[children_.Count - 1];

				bool b = a.Tick(o, s);
				if (b)
					return true;

				children_.Remove(a);
			}

			return false;
		}

		public override string ToString()
		{
			if (children_.Count == 0)
				return "(none)";
			else
				return children_[children_.Count - 1].ToString();
		}

		protected virtual bool DoStart(IObject o, float s)
		{
			// no-op
			return true;
		}

		protected virtual bool DoTick(IObject o, float s)
		{
			// no-op
			return false;
		}
	}

	class ConcurrentAction : BasicAction
	{
		protected override bool DoTick(IObject o, float s)
		{
			// no-op
			return (children_.Count > 0);
		}

		protected override bool TickChildren(IObject o, float s)
		{
			int i = 0;

			while (i < children_.Count)
			{
				var a = children_[i];

				if (a.Tick(o, s))
					++i;
				else
					children_.RemoveAt(i);
			}

			return (children_.Count > 0);
		}
	}

	class RootAction : BasicAction
	{
		protected override bool DoTick(IObject o, float s)
		{
			// no-op
			return true;
		}
	}


	class MakeIdleAction : BasicAction
	{
		protected override bool DoStart(IObject o, float s)
		{
			if (o is Person)
				((Person)o).MakeIdle();
			return false;
		}
	}


	class MoveAction : BasicAction
	{
		private Vector3 to_;
		private float finalBearing_;

		public MoveAction(Vector3 to, float finalBearing)
		{
			to_ = to;
			finalBearing_ = finalBearing;
		}

		protected override bool DoStart(IObject o, float s)
		{
			o.MoveTo(to_, finalBearing_);
			return o.HasTarget;
		}

		protected override bool DoTick(IObject o, float s)
		{
			return o.HasTarget;
		}

		public override string ToString()
		{
			return "MoveAction " + to_.ToString();
		}
	}

	class SitAction : BasicAction
	{
		private Slot chair_;
		private bool moving_ = false;

		public SitAction(Slot chair)
		{
			chair_ = chair;
		}

		protected override bool DoStart(IObject o, float s)
		{
			var p = o as Person;
			p.MoveTo(chair_.Position, chair_.Bearing);
			moving_ = true;
			return true;
		}

		protected override bool DoTick(IObject o, float s)
		{
			var p = o as Person;

			if (moving_)
			{
				if (p.HasTarget)
				{
					return true;
				}
				else
				{
					moving_ = false;
					p.SetState(PersonState.Sitting);
					return true;
				}
			}
			else
			{
				return p.Animator.Playing;
			}
		}

		public override string ToString()
		{
			return "SitAction on " + chair_.ToString();
		}
	}


	class RandomDialogAction : BasicAction
	{
		private List<string> phrases_;
		private float e_ = 0;
		private int i_ = 0;

		public RandomDialogAction(List<string> phrases)
		{
			phrases_ = new List<string>(phrases);
		}

		protected override bool DoStart(IObject o, float s)
		{
			e_ = 0;
			i_ = 0;
			phrases_.Shuffle();
			return true;
		}

		protected override bool DoTick(IObject o, float s)
		{
			var p = ((Person)o);

			e_ += s;

			if (e_ > 1)
			{
				p.Say(phrases_[i_]);

				++i_;
				if (i_ >= phrases_.Count)
				{
					i_ = 0;
					phrases_.Shuffle();
				}

				e_ = 0;
			}

			return true;
		}

		public override string ToString()
		{
			return "RandomDialogAction";
		}
	}


	class RandomAnimationAction : BasicAction
	{
		private List<Animation> anims_;
		private float e_ = 0;
		private int i_ = -1;
		private const float Delay = 10;
		private bool reverse_ = true;

		public RandomAnimationAction(List<Animation> anims)
		{
			anims_ = new List<Animation>(anims);
		}

		protected override bool DoStart(IObject o, float s)
		{
			e_ = 0;
			i_ = -1;
			anims_.Shuffle();
			reverse_ = false;
			return true;
		}

		protected override bool DoTick(IObject o, float s)
		{
			if (anims_.Count == 0)
				return true;

			var p = ((Person)o);

			if (i_ == -1)
			{
				if (!p.Animator.Playing)
				{
					i_ = 0;
					PlayNext(p);
				}
			}
			else
			{
				if (!p.Animator.Playing)
				{
					e_ += s;
					if (e_ >= Delay)
					{
						PlayNext(p);
						e_ = 0;
					}
				}
			}

			return true;
		}

		private void PlayNext(Person p)
		{
			if (!reverse_)
			{
				p.Animator.Play(anims_[i_], Animator.Rewind);
				reverse_ = true;
			}
			else
			{
				reverse_ = false;
				p.Animator.Play(anims_[i_], Animator.Rewind | Animator.Reverse);

				++i_;
				if (i_ >= anims_.Count)
				{
					i_ = 0;
					anims_.Shuffle();
				}
			}
		}

		public override string ToString()
		{
			return "RandomAnimationAction";
		}
	}


	class LookAroundAction : BasicAction
	{
		private float e_ = 0;
		private const float Delay = 1;

		public LookAroundAction()
		{
		}

		protected override bool DoStart(IObject o, float s)
		{
			e_ = Delay + 1;
			return true;
		}

		protected override bool DoTick(IObject o, float s)
		{
			var p = ((Person)o);

			e_ += s;

			if (e_ >= Delay)
			{
				var t = new Vector3(
					U.RandomFloat(-1.0f, 1.0f),
					U.RandomFloat(-1.0f, 1.0f),
					1);

				p.Gaze.LookAt(p.Body.Head.Position + Vector3.Rotate(t, p.Bearing));

				e_ = 0;
			}

			return true;
		}

		public override string ToString()
		{
			return "LookAroundAction";
		}
	}
}
