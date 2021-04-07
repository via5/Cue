using System.Collections.Generic;

namespace Cue
{
	interface IAction
	{
		bool IsIdle { get; }
		bool Start(IObject o, float s);
		bool Tick(IObject o, float s);
	}

	abstract class BasicAction : IAction
	{
		private readonly List<IAction> children_ = new List<IAction>();
		private IAction current_ = null;

		public virtual bool IsIdle
		{
			get { return children_.Count == 0; }
		}

		public void Add(IAction a)
		{
			children_.Add(a);
		}

		public virtual bool Start(IObject o, float s)
		{
			// no-op
			return true;
		}

		public virtual bool Tick(IObject o, float s)
		{
			while (children_.Count > 0)
			{
				var a = children_[children_.Count - 1];

				if (current_ != a)
				{
					current_ = a;
					bool b = current_.Start(o, s);
					if (b)
						return true;

					children_.RemoveAt(children_.Count - 1);
					current_ = null;
				}
				else
				{
					bool b = current_.Tick(o, s);
					if (b)
						return true;

					children_.RemoveAt(children_.Count - 1);
					current_ = null;
				}
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
	}

	class RootAction : BasicAction
	{
		public override bool Tick(IObject o, float s)
		{
			base.Tick(o, s);
			return true;
		}
	}

	class MoveAction : BasicAction
	{
		private Vector3 to_;
		private List<Vector3> wps_ = new List<Vector3>();
		private int i_ = 0;

		public MoveAction(Vector3 to)
		{
			to_ = to;
		}

		public override bool Start(IObject o, float s)
		{
			wps_ = Cue.Instance.Sys.Nav.Calculate(o.Position, to_);

			if (wps_.Count == 0)
			{
				SuperController.LogError(
					o.ToString() + " cannot reach " + to_.ToString());
				return false;
			}

			SuperController.LogError(
				o.ToString() + " to " + to_.ToString() + ", " +
				wps_.Count.ToString() + " waypoints");

			o.MoveTo(wps_[0]);

			if (o.HasTarget)
				return true;
			else if (wps_.Count > 1)
				return true;
			else
				return false;
		}

		public override bool Tick(IObject o, float s)
		{
			if (!o.HasTarget)
			{
				++i_;
				if (i_ >= wps_.Count)
				{
					Cue.LogError(o.ToString() + " has reached " + to_.ToString());
					return false;
				}

				Cue.LogError(o.ToString() + " next waypoint " + wps_[i_].ToString());
				o.MoveTo(wps_[i_]);
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

		public SitAction(IObject chair)
		{
			chair_ = chair;
		}

		public override bool Start(IObject o, float s)
		{
			var p = o as Person;
			p.Bearing = chair_.Bearing + chair_.SitSlot.bearingOffset;
			p.Sit();
			return o.Animating;
		}

		public override bool Tick(IObject o, float s)
		{
			return o.Animating;
		}

		public override string ToString()
		{
			return "SitAction on " + chair_.ToString();
		}
	}
}
