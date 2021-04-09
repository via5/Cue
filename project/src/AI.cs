using Battlehub.RTSaveLoad;
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
					events_.Add(new SitAndThinkEvent(o));
				if (o.SleepSlot != null)
					events_.Add(new SleepEvent(o));
				if (o.StandSlot != null)
					events_.Add(new StandAndThinkEvent(o));
			}
		}

		public void Tick(Person p, float s)
		{
			if (events_.Count == 0)
				return;

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


	class SitAndThinkEvent : BasicEvent
	{
		const int NoState = 0;
		const int Moving = 1;
		const int Sitting = 2;
		const int Thinking = 3;

		const float ThinkTime = 5;

		private IObject o_;
		private int state_ = NoState;
		private float thunk_ = 0;

		public SitAndThinkEvent(IObject o)
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
					p.PushAction(new MoveAction(pos, BasicObject.NoBearing));
					state_ = Moving;
					thunk_ = 0;
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
						Cue.LogError("thinking");

						var cc = new ConcurrentAction();

						cc.Push(new RandomDialogAction(new List<string>()
						{
							"I'm thinking.",
							"I'm still thinking.",
							"Hmm...",
							"I think..."
						}));

						//cc.Push(new LookAroundAction());

						cc.Push(new RandomAnimationAction(Resources.Animations.SitIdles()));

						p.PushAction(cc);

						state_ = Thinking;
					}

					break;
				}

				case Thinking:
				{
					thunk_ += s;
					if (thunk_ >= ThinkTime)
					{
						Cue.LogError("done");
						p.PopAction();
						state_ = NoState;
						return false;
					}

					break;
				}
			}

			return true;
		}
	}


	class StandAndThinkEvent : BasicEvent
	{
		const int NoState = 0;
		const int Moving = 1;
		const int Thinking = 2;

		const float ThinkTime = 5;

		private IObject o_;
		private int state_ = NoState;
		private float thunk_ = 0;

		public StandAndThinkEvent(IObject o)
		{
			o_ = o;
		}

		public override bool Tick(Person p, float s)
		{
			var ss = o_.StandSlot;
			if (ss == null)
			{
				Cue.LogError("can't stand on object " + o_.ToString());
				return false;
			}

			var pos = o_.Position + Vector3.Rotate(ss.positionOffset, o_.Bearing);

			switch (state_)
			{
				case NoState:
				{
					Cue.LogError("going to stand");
					p.PushAction(new MoveAction(pos, o_.Bearing + ss.bearingOffset));
					state_ = Moving;
					thunk_ = 0;
					break;
				}

				case Moving:
				{
					if (p.Idle)
					{
						Cue.LogError("thinking");

						var cc = new ConcurrentAction();

						cc.Push(new RandomDialogAction(new List<string>()
						{
							"I'm thinking.",
							"I'm still thinking.",
							"Hmm...",
							"I think..."
						}));

						//cc.Push(new LookAroundAction());

						cc.Push(new RandomAnimationAction(Resources.Animations.StandIdles()));
						p.PushAction(cc);

						state_ = Thinking;
					}

					break;
				}

				case Thinking:
				{
					thunk_ += s;
					if (thunk_ >= ThinkTime)
					{
						Cue.LogError("done");
						p.PopAction();
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
					p.PushAction(new MoveAction(o_.Position, BasicObject.NoBearing));
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
