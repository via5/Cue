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
			: base(p, "Stand")
		{
		}

		public StandEvent(Person p, IObject o)
			: this(p)
		{
			o_ = o;
		}

		public StandEvent(Person p, Slot s)
			: this(p)
		{
			o_ = s.ParentObject;
			slot_ = s;
		}

		public override bool Update(float s)
		{
			switch (state_)
			{
				case NoState:
				{
					if (slot_ == null && o_ == null)
					{
						// don't move
						state_ = Moving;
						break;
					}


					log_.Info("going to stand");

					if (slot_ == null)
					{
						slot_ = o_.Slots.GetAny(Slot.Stand);
						if (slot_ == null)
						{
							log_.Error("can't stand on object " + o_.ToString());
							return false;
						}

						if (!person_.TryLockSlot(slot_))
							return false;
					}

					person_.PushAction(new MoveAction(
						person_, slot_.ParentObject, slot_.Position, slot_.Bearing));
					state_ = Moving;

					break;
				}

				case Moving:
				{
					if (person_.Idle)
					{
						log_.Info("thinking");

						var cc = new ConcurrentAction(person_);

						//cc.Push(new RandomDialogAction(new List<string>()
						//{
						//	"I'm thinking.",
						//	"I'm still thinking.",
						//	"Hmm...",
						//	"I think..."
						//}));

						cc.Push(new RandomAnimationAction(person_,
							Resources.Animations.GetAllIdles(
								PersonState.Standing, person_.Sex)));

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
