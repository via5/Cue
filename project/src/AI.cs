using System.Collections.Generic;

namespace Cue
{
	interface IAI
	{
		bool InteractWith(IObject o);
		void RunEvent(IEvent e);
		void Update(float s);
		bool Enabled { get; set; }
	}

	class PersonAI : IAI
	{
		private Person person_ = null;
		private int i_ = -1;
		private readonly List<IEvent> events_ = new List<IEvent>();
		private bool enabled_ = false;
		private IEvent forced_ = null;

		public PersonAI(Person person)
		{
			person_ = person;

			foreach (var o in Cue.Instance.Objects)
			{
				if (o.Slots.Has(Slot.Sit))
					events_.Add(new SitAndThinkEvent(person_, o));
				if (o.Slots.Has(Slot.Lie))
					events_.Add(new LieDownEvent(person_, o));
				if (o.Slots.Has(Slot.Stand))
					events_.Add(new StandAndThinkEvent(person_, o));
			}
		}

		public bool Enabled
		{
			get
			{
				return enabled_;
			}

			set
			{
				enabled_ = value;
				if (!enabled_)
					Stop();
			}
		}

		public bool InteractWith(IObject o)
		{
			if (!person_.TryLockSlot(o))
			{
				// can't lock the given object
				return false;
			}

			person_.MakeIdle();

			var slot = person_.LockedSlot;

			if (slot.Type == Slot.Sit)
			{
				person_.PushAction(new SitAction(slot));
				person_.PushAction(new MoveAction(slot.Position, BasicObject.NoBearing));
			}
			else if (slot.Type == Slot.Stand)
			{
				person_.PushAction(new MakeIdleAction());
				person_.PushAction(new MoveAction(slot.Position, slot.Bearing));
			}

			return true;
		}

		public void RunEvent(IEvent e)
		{
			if (forced_ != null)
				forced_.Stop();

			forced_ = e;

			if (forced_ != null)
			{
				Stop();
				person_.MakeIdle();
			}
		}

		public void Update(float s)
		{
			if (forced_ != null)
			{
				if (!forced_.Update(s))
					forced_ = null;

				return;
			}

			if (!enabled_)
				return;

			if (events_.Count == 0)
				return;

			if (i_ == -1)
			{
				i_ = 0;
			}
			else
			{
				if (!events_[i_].Update(s))
				{
					++i_;
					if (i_ >= events_.Count)
						i_ = 0;
				}
			}
		}

		private void Stop()
		{
			if (i_ >= 0 && i_ < events_.Count)
			{
				events_[i_].Stop();
				i_ = -1;
			}
		}
	}


	interface IEvent
	{
		void Stop();
		bool Update(float s);
	}

	abstract class BasicEvent : IEvent
	{
		protected Person person_;
		//private Slot lockedSlot_ = null;

		protected BasicEvent(Person p)
		{
			person_ = p;
		}

		public virtual void Stop()
		{
			Unlock();
		}

		protected void Unlock()
		{
			// todo, this event didn't necessarily lock that slot
			if (person_.LockedSlot != null)
				person_.LockedSlot.Unlock(person_);
		}

		public abstract bool Update(float s);
	}


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
					Cue.LogError("going to sit");
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
						Cue.LogError("sitting");
						state_ = Sitting;
					}

					break;
				}

				case Sitting:
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


	class StandAndThinkEvent : BasicEvent
	{
		const int NoState = 0;
		const int Moving = 1;
		const int Thinking = 2;

		const float ThinkTime = 5;

		private IObject o_;
		private Slot slot_ = null;
		private int state_ = NoState;
		private float thunk_ = 0;

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
			if (slot_ == null)
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

			var pos = o_.Position;

			switch (state_)
			{
				case NoState:
				{
					Cue.LogError("going to stand");
					person_.Gaze.LookInFront();
					person_.PushAction(new MoveAction(pos, o_.Bearing));
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


	class LieDownEvent : BasicEvent
	{
		const int NoState = 0;
		const int Moving = 1;
		const int Sleeping = 2;

		private IObject o_;
		private int state_ = NoState;
		private float elapsed_ = 0;

		public LieDownEvent(Person p, IObject o)
			: base(p)
		{
			o_ = o;
		}

		public override bool Update(float s)
		{
			switch (state_)
			{
				case NoState:
				{
					Cue.LogError("going to sleep");
					person_.PushAction(new MoveAction(o_.Position, BasicObject.NoBearing));
					state_ = Moving;
					elapsed_ = 0;
					break;
				}

				case Moving:
				{
					if (person_.Idle)
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


	class CallEvent : BasicEvent
	{
		private const int NoState = 0;
		private const int MovingState = 1;
		private const int IdlingState = 2;

		private Person caller_;
		private int state_ = NoState;

		public CallEvent(Person p, Person caller)
			: base(p)
		{
			caller_ = caller;
		}

		public override bool Update(float s)
		{
			switch (state_)
			{
				case NoState:
				{
					var target =
						caller_.Position +
						Vector3.Rotate(new Vector3(0, 0, 0.5f), caller_.Bearing);

					person_.MoveTo(target, caller_.Bearing + 180);
					person_.Gaze.LookAt = GazeSettings.LookAtTarget;
					person_.Gaze.Target = caller_.Atom.HeadPosition;
					state_ = MovingState;

					break;
				}

				case MovingState:
				{
					if (!person_.HasTarget)
					{
						person_.PushAction(new RandomAnimationAction(
							Resources.Animations.GetAll(
								Resources.Animations.StandIdle, person_.Sex)));

						state_ = IdlingState;
					}

					break;
				}

				case IdlingState:
				{
					return false;
				}
			}

			return true;
		}

		public override string ToString()
		{
			return "CallEvent";
		}
	}


	class HandjobEvent : BasicEvent
	{
		private const int NoState = 0;
		private const int MovingState = 1;
		private const int PositioningState = 2;
		private const int ActiveState = 3;

		private Person receiver_;
		private int state_ = NoState;

		public HandjobEvent(Person p, Person receiver)
			: base(p)
		{
			receiver_ = receiver;
		}

		public override bool Update(float s)
		{
			switch (state_)
			{
				case NoState:
				{
					var target =
						receiver_.Position +
						Vector3.Rotate(new Vector3(0, 0, 0.5f), receiver_.Bearing);

					person_.MoveTo(target, receiver_.Bearing + 180);
					person_.Gaze.LookAt = GazeSettings.LookAtTarget;
					person_.Gaze.Target = receiver_.Atom.HeadPosition;
					state_ = MovingState;

					break;
				}

				case MovingState:
				{
					if (!person_.HasTarget)
					{
						if (receiver_.State.Is(PersonState.Sitting))
						{
							person_.Kneel();
							state_ = PositioningState;
						}
						else
						{
							person_.Handjob.Target = Cue.Instance.Player;
							person_.Handjob.Active = true;
							state_ = ActiveState;
						}
					}

					break;
				}

				case PositioningState:
				{
					if (!person_.Animator.Playing)
					{
						person_.Handjob.Target = Cue.Instance.Player;
						person_.Handjob.Active = true;
						state_ = ActiveState;
					}

					break;
				}

				case ActiveState:
				{
					break;
				}
			}

			return true;
		}
	}
}
