using System.Collections.Generic;

namespace Cue
{
	class StandAndThinkEvent : BasicEvent
	{
		const int NoState = 0;
		const int Moving = 1;
		const int Thinking = 2;

		const float ThinkTime = 5;

		private IObject o_ = null;
		private Slot slot_ = null;
		private int state_ = NoState;
		private float thunk_ = 0;

		public StandAndThinkEvent(Person p)
			: base(p)
		{
		}

		public StandAndThinkEvent(Person p, IObject o)
			: base(p)
		{
			o_ = o;
		}

		public StandAndThinkEvent(Person p, Slot s)
			: base(p)
		{
			o_ = s.ParentObject;
			slot_ = s;
		}

		public override bool Update(float s)
		{
			if (slot_ == null && o_ != null)
			{
				slot_ = o_.Slots.Get(Slot.Stand);
				if (slot_ == null)
				{
					Cue.LogError("can't stand on object " + o_.ToString());
					return false;
				}

				if (!person_.TryLockSlot(slot_))
					return false;
			}

			Vector3 pos;
			float bearing = BasicObject.NoBearing;

			if (o_ == null)
			{
				pos = person_.UprightPosition;
			}
			else
			{
				pos = o_.Position;
				bearing = o_.Bearing;
			}

			switch (state_)
			{
				case NoState:
				{
					Cue.LogError("going to stand");
					person_.Gaze.LookInFront();
					person_.PushAction(new MoveAction(pos, bearing));
					state_ = Moving;
					thunk_ = 0;
					break;
				}

				case Moving:
				{
					if (person_.Idle)
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

						cc.Push(new RandomAnimationAction(
							Resources.Animations.GetAll(
								Resources.Animations.StandIdle, person_.Sex)));

						cc.Push(new LookAroundAction());
						person_.PushAction(cc);

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
						person_.PopAction();
						Unlock();
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
