using System.Collections.Generic;

namespace Cue
{
	class SitEvent : BasicEvent
	{
		const int NoState = 0;
		const int Moving = 1;
		const int Sitting = 2;
		const int Idling = 3;

		private IObject o_;
		private Slot slot_ = null;
		private int state_ = NoState;

		public SitEvent(Person p, IObject o)
			: base(p)
		{
			o_ = o;
		}

		public SitEvent(Person p, Slot s)
			: base(p)
		{
			o_ = s.ParentObject;
			slot_ = s;
		}

		public override bool Update(float s)
		{
			if (slot_ == null)
			{
				slot_ = o_.Slots.GetAny(Slot.Sit);
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
					person_.PushAction(new MoveAction(pos, slot_.Bearing));
					state_ = Moving;
					break;
				}

				case Moving:
				{
					if (person_.Idle)
					{
						person_.SetState(PersonState.Sitting);
						Cue.LogInfo("sitting");
						state_ = Sitting;
					}

					break;
				}

				case Sitting:
				{
					if (person_.State.IsCurrently(PersonState.Sitting))
					{
						Cue.LogInfo("thinking");

						var cc = new ConcurrentAction();

						//cc.Push(new RandomDialogAction(new List<string>()
						//{
						//	"I'm thinking.",
						//	"I'm still thinking.",
						//	"Hmm...",
						//	"I think..."
						//}));

						//cc.Push(new RandomAnimationAction(
						//	Resources.Animations.GetAll(
						//		Resources.Animations.SitIdle, person_.Sex)));

						//cc.Push(new LookAroundAction());

						person_.PushAction(cc);

						state_ = Idling;
					}

					break;
				}

				case Idling:
				{
					break;
				}
			}

			return true;
		}
	}
}
