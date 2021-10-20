using Leap.Unity;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Cue
{
	interface IAction
	{
		bool IsIdle { get; }
		bool Tick(float s);
	}

	abstract class BasicAction : IAction
	{
		protected IObject o_;
		protected Logger log_;
		protected readonly List<IAction> children_ = new List<IAction>();
		private bool started_ = false;

		protected BasicAction(IObject o, string name)
		{
			o_ = o;
			log_ = new Logger(Logger.Action, o, name + "Action");
		}

		public IObject Object
		{
			get { return o_; }
		}

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
				log_.Error("no action to pop");
				return;
			}

			children_.RemoveAt(children_.Count - 1);
		}

		public void Clear()
		{
			children_.Clear();
		}

		public bool Tick(float s)
		{
			if (!started_)
			{
				started_ = true;
				if (!DoStart(s))
					return false;
			}

			if (!DoTick(s))
				return false;

			TickChildren(s);
			return true;
		}

		protected virtual bool TickChildren(float s)
		{
			while (children_.Count > 0)
			{
				var a = children_[children_.Count - 1];

				bool b = a.Tick(s);
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

		protected virtual bool DoStart(float s)
		{
			// no-op
			return true;
		}

		protected virtual bool DoTick(float s)
		{
			// no-op
			return false;
		}
	}

	class ConcurrentAction : BasicAction
	{
		public ConcurrentAction(IObject o)
			: base(o, "Concurrent")
		{
		}

		protected override bool DoTick(float s)
		{
			// no-op
			return (children_.Count > 0);
		}

		protected override bool TickChildren(float s)
		{
			int i = 0;

			while (i < children_.Count)
			{
				var a = children_[i];

				if (a.Tick(s))
					++i;
				else
					children_.RemoveAt(i);
			}

			return (children_.Count > 0);
		}
	}

	class RootAction : BasicAction
	{
		public RootAction(IObject o)
			: base(o, "root")
		{
		}

		protected override bool DoTick(float s)
		{
			// no-op
			return true;
		}
	}


	class RandomAnimationAction : BasicAction
	{
		private List<Animation> anims_;
		private float e_ = 0;
		private int i_ = -1;
		private const float Delay = 5;
		private Animation playing_ = null;
		//private bool wasClose_ = false;

		public RandomAnimationAction(IObject o, List<Animation> anims)
			: base(o, "RandomAnimation")
		{
			anims_ = new List<Animation>(anims);
		}

		protected override bool DoStart(float s)
		{
			e_ = 0;
			i_ = -1;
			anims_.Shuffle();
			return true;
		}

		protected override bool DoTick(float s)
		{
			if (anims_.Count == 0)
				return true;

			var p = Object as Person;
			if (p == null)
			{
				log_.Error("object is not a person");
				return false;
			}


		//	bool close = p.Body.PlayerIsClose;
		//
		//	if (close != wasClose_)
		//	{
		//		if (close)
		//		{
		//			if (playing_)
		//			{
		//				//p.Animator.PlayNeutral();
		//				playing_ = false;
		//			}
		//		}
		//
		//		wasClose_ = close;
		//	}
		//
		//	if (close)
		//		return true;


			if (i_ == -1)
			{
				if (playing_ == null)
				{
					i_ = 0;
					PlayNext(p);
				}
			}
			else
			{
				if (playing_ != null)
				{
					if (!p.Animator.IsPlaying(playing_) && p.Animator.CanPlayType(Animations.Idle))
					{
						//p.Animator.PlayNeutral();
						playing_ = null;
					}
				}
				else
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
			if (p.Animator.CanPlayType(Animations.Idle))
			{
				if (p.Animator.CanPlay(anims_[i_]))
				{
					p.Animator.Play(anims_[i_]);
					playing_ = anims_[i_];

					++i_;
					if (i_ >= anims_.Count)
					{
						i_ = 0;
						anims_.Shuffle();
					}
				}
			}
		}

		public override string ToString()
		{
			return "RandomAnimationAction";
		}
	}
}
