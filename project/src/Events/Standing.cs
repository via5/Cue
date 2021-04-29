using System.Collections.Generic;

namespace Cue
{
	class StandEvent : BasicEvent
	{
		const int NoState = 0;
		const int Moving = 1;
		const int Idling = 2;

		private IObject o_ = null;
		private Slot slot_ = null;
		private int state_ = NoState;

		public StandEvent(Person p)
			: base(p)
		{
		}

		public StandEvent(Person p, IObject o)
			: base(p)
		{
			o_ = o;
		}

		public StandEvent(Person p, Slot s)
			: base(p)
		{
			o_ = s.ParentObject;
			slot_ = s;
		}

		public override bool Update(float s)
		{
			if (slot_ == null && o_ != null)
			{
				slot_ = o_.Slots.GetAny(Slot.Stand);
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
					Cue.LogInfo("going to stand");
					person_.LookInFront();
					person_.PushAction(new MoveAction(pos, bearing));
					state_ = Moving;
					break;
				}

				case Moving:
				{
					if (person_.Idle)
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
						//		Resources.Animations.StandIdle, person_.Sex)));

						cc.Push(new LookAroundAction());
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
