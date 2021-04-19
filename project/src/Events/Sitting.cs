using System.Collections.Generic;

namespace Cue
{
	class SitAndThinkEvent : BasicEvent
	{
		const int NoState = 0;
		const int Moving = 1;
		const int Sitting = 2;
		const int Thinking = 3;

		const float ThinkTime = 5;

		private IObject o_;
		private Slot slot_ = null;
		private int state_ = NoState;
		private float thunk_ = 0;

		public SitAndThinkEvent(Person p, IObject o)
			: base(p)
		{
			o_ = o;
		}

		public SitAndThinkEvent(Person p, Slot s)
			: base(p)
		{
			o_ = s.ParentObject;
			slot_ = s;
		}

		public override bool Update(float s)
		{
			if (slot_ == null)
			{
				slot_ = o_.Slots.Get(Slot.Sit);
				if (slot_ == null)
				{
					Cue.LogError("can't sit on object " + o_.ToString());
					return false;
				}

				if (!person_.TryLockSlot(slot_))
					return false;
			}

			var pos = o_.Position;

			switch (state_)
			{
				case NoState:
				{
					Cue.LogInfo("going to sit");
					person_.Gaze.LookInFront();
					person_.PushAction(new MoveAction(pos, BasicObject.NoBearing));
					state_ = Moving;
					thunk_ = 0;
					break;
				}

				case Moving:
				{
					if (person_.Idle)
					{
						person_.PushAction(new SitAction(slot_));
						Cue.LogInfo("sitting");
						state_ = Sitting;
					}

					break;
				}

				case Sitting:
				{
					if (person_.Idle)
					{
						Cue.LogInfo("thinking");

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
								Resources.Animations.SitIdle, person_.Sex)));

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
						Cue.LogInfo("done");
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
