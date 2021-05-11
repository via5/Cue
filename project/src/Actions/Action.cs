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


	class MakeIdleAction : BasicAction
	{
		public MakeIdleAction(IObject o)
			: base(o, "MakeIdle")
		{
		}

		protected override bool DoStart(float s)
		{
			var p = Object as Person;
			if (p == null)
			{
				log_.Error("object is not a person");
				return false;
			}

			p.MakeIdle();
			return false;
		}
	}


	class CallAction : BasicAction
	{
		private Person caller_;

		public CallAction(IObject o, Person caller)
			: base(o, "CallAction")
		{
			caller_ = caller;
		}

		protected override bool DoStart(float s)
		{
			var p = Object as Person;
			if (p == null)
			{
				log_.Error("object is not a person");
				return false;
			}

			var target =
				caller_.UprightPosition +
				Vector3.Rotate(new Vector3(0, 0, 0.5f), caller_.Bearing);

			log_.Info($"CallAction: {p} moving to {caller_}");
			p.MoveTo(target, caller_.Bearing + 180);
			p.Gaze.LookAt(caller_);

			return true;
		}

		protected override bool DoTick(float s)
		{
			var p = Object as Person;
			if (p == null)
			{
				log_.Error("object is not a person");
				return false;
			}

			if (!p.HasTarget)
			{
				log_.Info($"CallAction: {p} reached {caller_}, event finished");
				return false;
			}

			return true;
		}
	}


	class MoveAction : BasicAction
	{
		private Vector3 to_;
		private float finalBearing_;

		public MoveAction(IObject o, Vector3 to, float finalBearing)
			: base(o, "Move")
		{
			to_ = to;
			finalBearing_ = finalBearing;
		}

		protected override bool DoStart(float s)
		{
			Object.MoveTo(to_, finalBearing_);
			return Object.HasTarget;
		}

		protected override bool DoTick(float s)
		{
			return Object.HasTarget;
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

		public SitAction(IObject o, Slot chair)
			: base(o, "Sit")
		{
			chair_ = chair;
		}

		protected override bool DoStart(float s)
		{
			Object.MoveTo(chair_.Position, chair_.Bearing);
			moving_ = true;
			return true;
		}

		protected override bool DoTick(float s)
		{
			var p = Object as Person;
			if (p == null)
			{
				log_.Error("object is not a person");
				return false;
			}

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

		public RandomDialogAction(IObject o, List<string> phrases)
			: base(o, "RandomDialog")
		{
			phrases_ = new List<string>(phrases);
		}

		protected override bool DoStart(float s)
		{
			e_ = 0;
			i_ = 0;
			phrases_.Shuffle();
			return true;
		}

		protected override bool DoTick(float s)
		{
			var p = Object as Person;
			if (p == null)
			{
				log_.Error("object is not a person");
				return false;
			}

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
		private const float Delay = 5;
		private bool playing_ = false;
		private bool wasClose_ = false;

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


			bool close = p.Body.PlayerIsClose;

			if (close != wasClose_)
			{
				if (close)
				{
					if (playing_)
					{
						p.Animator.PlayNeutral();
						playing_ = false;
					}
				}

				wasClose_ = close;
			}

			if (close)
				return true;


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
				if (playing_)
				{
					if (!p.Animator.Playing)
					{
						p.Animator.PlayNeutral();
						playing_ = false;
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
			p.Animator.Play(anims_[i_]);
			playing_ = true;

			++i_;
			if (i_ >= anims_.Count)
			{
				i_ = 0;
				anims_.Shuffle();
			}
		}

		public override string ToString()
		{
			return "RandomAnimationAction";
		}
	}
}
