using System.Collections.Generic;

namespace Cue
{
	interface IAI
	{
	}

	class PersonAI : IAI
	{
		private int i_ = -1;
		private readonly List<IEvent> events_ = new List<IEvent>();

		public PersonAI()
		{
			foreach (var o in Cue.Instance.Objects)
			{
				if (o.SitSlot != null)
					events_.Add(new SitEvent(o));
				if (o.SleepSlot != null)
					events_.Add(new SleepEvent(o));
			}
		}

		public void Tick(Person p, float s)
		{
			if (i_ == -1)
			{
				i_ = 0;
			}
			else
			{
				if (!events_[i_].Tick(p, s))
				{
					++i_;
					if (i_ >= events_.Count)
						i_ = 0;
				}
			}
		}
	}


	interface IEvent
	{
		bool Tick(Person p, float s);
	}

	abstract class BasicEvent : IEvent
	{
		public abstract bool Tick(Person p, float s);
	}


	class SitEvent : BasicEvent
	{
		const int NoState = 0;
		const int Moving = 1;
		const int Sitting = 2;

		private IObject o_;
		private int state_ = NoState;

		public SitEvent(IObject o)
		{
			o_ = o;
		}

		public override bool Tick(Person p, float s)
		{
			var ss = o_.SitSlot;
			if (ss == null)
			{
				Cue.LogError("can't sit on object " + o_.ToString());
				return false;
			}

			var pos = o_.Position + Vector3.Rotate(ss.positionOffset, o_.Bearing);

			switch (state_)
			{
				case NoState:
				{
					Cue.LogError("going to sit");
					p.PushAction(new MoveAction(pos));
					state_ = Moving;
					break;
				}

				case Moving:
				{
					if (p.Idle)
					{
						p.PushAction(new SitAction(o_));
						Cue.LogError("sitting");
						state_ = Sitting;
					}

					break;
				}

				case Sitting:
				{
					if (p.Idle)
					{
						Cue.LogError("done");
						state_ = NoState;
						return false;
					}

					break;
				}
			}

			return true;
		}
	}


	class SleepEvent : BasicEvent
	{
		const int NoState = 0;
		const int Moving = 1;
		const int Sleeping = 2;

		private IObject o_;
		private int state_ = NoState;
		private float elapsed_ = 0;

		public SleepEvent(IObject o)
		{
			o_ = o;
		}

		public override bool Tick(Person p, float s)
		{
			switch (state_)
			{
				case NoState:
				{
					Cue.LogError("going to sleep");
					p.PushAction(new MoveAction(o_.Position));
					state_ = Moving;
					elapsed_ = 0;
					break;
				}

				case Moving:
				{
					if (p.Idle)
					{
						Cue.LogError("reached bed, sleeping");
						state_ = Sleeping;
					}

					break;
				}

				case Sleeping:
				{
					elapsed_ += s;
					if (elapsed_ > 0)
					{
						Cue.LogError("finished sleeping");
						state_ = NoState;
						return false;
					}

					break;
				}
			}

			return true;
		}
	}
}
