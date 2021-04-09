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

				children_.RemoveAt(children_.Count - 1);
			}

			return false;
		}

		public override string ToString()
		{
			if (children_.Count == 0)
				return "none";
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


	class MoveAction : BasicAction
	{
		private Vector3 to_;
		private float finalBearing_;
		private List<Vector3> wps_ = new List<Vector3>();
		private int i_ = 0;

		public MoveAction(Vector3 to, float finalBearing)
		{
			to_ = to;
			finalBearing_ = finalBearing;
		}

		protected override bool DoStart(IObject o, float s)
		{
			Vector3 pos;

			if (o is Person)
				pos = ((Person)o).StandingPosition;
			else
				pos = o.Position;


			Cue.LogError("MoveAction: from " + pos.ToString() + " to " + to_.ToString());
			wps_ = Cue.Instance.Sys.Nav.Calculate(pos, to_);

			if (wps_.Count == 0)
			{
				Cue.LogError(
					o.ToString() + " cannot reach " + to_.ToString());
				return false;
			}

			Cue.LogError(
				o.ToString() + " to " + to_.ToString() + ", " +
				wps_.Count.ToString() + " waypoints");

			if (wps_.Count == 1)
				o.MoveTo(wps_[0], finalBearing_, true);
			else
				o.MoveTo(wps_[0], BasicObject.NoBearing, false);

			if (o.HasTarget)
				return true;
			else if (wps_.Count > 1)
				return true;
			else
				return false;
		}

		protected override bool DoTick(IObject o, float s)
		{
			if (!o.HasTarget)
			{
				++i_;
				if (i_ >= wps_.Count)
				{
					Cue.LogError(o.ToString() + " has reached " + to_.ToString());
					return false;
				}

			//	Cue.LogError(o.ToString() + " next waypoint " + wps_[i_].ToString());

				if (i_ == (wps_.Count - 1))
					o.MoveTo(wps_[i_], finalBearing_, true);
				else
					o.MoveTo(wps_[i_], BasicObject.NoBearing, false);
			}

			return true;
		}

		public override string ToString()
		{
			return "MoveAction " + to_.ToString();
		}
	}

	class SitAction : BasicAction
	{
		private IObject chair_;
		private bool moving_ = false;

		public SitAction(IObject chair)
		{
			chair_ = chair;
		}

		protected override bool DoStart(IObject o, float s)
		{
			var p = o as Person;
			p.MoveTo(chair_.Position, chair_.Bearing, true);
			moving_ = true;
			return true;// p.Animator.Playing;
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
					p.Sit();
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
		private List<IAnimation> anims_;
		private float e_ = 0;
		private int i_ = -1;

		public RandomAnimationAction(List<IAnimation> anims)
		{
			anims_ = new List<IAnimation>(anims);
		}

		protected override bool DoStart(IObject o, float s)
		{
			e_ = 0;
			i_ = -1;
			anims_.Shuffle();
			return true;
		}

		protected override bool DoTick(IObject o, float s)
		{
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
					if (e_ >= 3)
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


	class LookAroundAction : BasicAction
	{
		private float e_ = 0;
		private int i_ = 0;

		public LookAroundAction()
		{
		}

		protected override bool DoStart(IObject o, float s)
		{
			e_ = 0;
			i_ = 0;

			var p = ((Person)o);
			p.Gaze.LookAt = GazeSettings.LookAtTarget;

			return true;
		}

		protected override bool DoTick(IObject o, float s)
		{
			var p = ((Person)o);

			e_ += s;

			if (e_ > 2)
			{
				++i_;

				if (i_ >= 2)
				{
					i_ = 0;
					p.Gaze.LookAt = GazeSettings.LookAtPlayer;
				}
				else
				{
					var t = new Vector3(
						UnityEngine.Random.Range(-2, 2),
						UnityEngine.Random.Range(-2, 2),
						UnityEngine.Random.Range(-2, 2));

					t += p.Position;

					p.Gaze.LookAt = GazeSettings.LookAtTarget;
					p.Gaze.Target = t;
				}

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
