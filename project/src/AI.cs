using System.Collections.Generic;

namespace Cue
{
	interface IAI
	{
		void RunEvent(IEvent e);
		void Tick(Person p, float s);
		bool Enabled { get; set; }
	}

	class PersonAI : IAI
	{
		private int i_ = -1;
		private readonly List<IEvent> events_ = new List<IEvent>();
		private bool enabled_ = false;
		private Person person_ = null;
		private IEvent forced_ = null;

		public PersonAI()
		{
			foreach (var o in Cue.Instance.Objects)
			{
				if (o.Slots.Has(Slot.Sit))
					events_.Add(new SitAndThinkEvent(o));
				if (o.Slots.Has(Slot.Lie))
					events_.Add(new LieDownEvent(o));
				if (o.Slots.Has(Slot.Stand))
					events_.Add(new StandAndThinkEvent(o));
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

		public void RunEvent(IEvent e)
		{
			if (forced_ != null)
				forced_.Stop(person_);

			forced_ = e;

			if (forced_ != null)
			{
				Stop();
				person_.MakeIdle();
			}
		}

		public void Tick(Person p, float s)
		{
			person_ = p;

			if (forced_ != null)
			{
				if (!forced_.Tick(p, s))
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
				if (!events_[i_].Tick(p, s))
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
				events_[i_].Stop(person_);
				i_ = -1;
			}
		}
	}


	interface IEvent
	{
		void Stop(Person p);
		bool Tick(Person p, float s);
	}

	abstract class BasicEvent : IEvent
	{
		private Slot lockedSlot_ = null;

		public virtual void Stop(Person p)
		{
			Unlock(p);
		}

		public abstract bool Tick(Person p, float s);

		protected bool Lock(Person p, Slot s)
		{
			if (lockedSlot_ == null && !s.Lock(p))
			{
				Cue.LogError("can't lock slot " + s.ToString());
				return false;
			}

			lockedSlot_ = s;
			return true;
		}

		protected bool Unlock(Person p)
		{
			if (lockedSlot_ != null)
			{
				bool b = lockedSlot_.Unlock(p);
				lockedSlot_ = null;
				return b;
			}

			return false;
		}
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
			var ss = o_.Slots.Get(Slot.Sit);
			if (ss == null)
			{
				Cue.LogError("can't sit on object " + o_.ToString());
				return false;
			}

			if (!Lock(p, ss))
				return false;

			var pos = o_.Position;

			switch (state_)
			{
				case NoState:
				{
					Cue.LogError("going to sit");
					p.Gaze.LookInFront();
					p.PushAction(new MoveAction(pos, BasicObject.NoBearing));
					state_ = Moving;
					thunk_ = 0;
					break;
				}

				case Moving:
				{
					if (p.Idle)
					{
						p.PushAction(new SitAction(ss));
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

						cc.Push(new RandomAnimationAction(
							Resources.Animations.GetAll(
								Resources.Animations.SitIdle, p.Sex)));

						cc.Push(new LookAroundAction());

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
						Unlock(p);
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
			var ss = o_.Slots.Get(Slot.Stand);
			if (ss == null)
			{
				Cue.LogError("can't stand on object " + o_.ToString());
				return false;
			}

			if (!Lock(p, ss))
				return false;

			var pos = o_.Position;

			switch (state_)
			{
				case NoState:
				{
					Cue.LogError("going to stand");
					p.Gaze.LookInFront();
					p.PushAction(new MoveAction(pos, o_.Bearing));
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

						cc.Push(new RandomAnimationAction(
							Resources.Animations.GetAll(
								Resources.Animations.StandIdle, p.Sex)));

						cc.Push(new LookAroundAction());
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
						Unlock(p);
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

		public LieDownEvent(IObject o)
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


	class CallEvent : BasicEvent
	{
		private const int NoState = 0;
		private const int MovingState = 1;
		private const int IdlingState = 2;

		private Person caller_;
		private int state_ = NoState;

		public CallEvent(Person caller)
		{
			caller_ = caller;
		}

		public override bool Tick(Person callee, float s)
		{
			switch (state_)
			{
				case NoState:
				{
					var target =
						caller_.Position +
						Vector3.Rotate(new Vector3(0, 0, 0.5f), caller_.Bearing);

					callee.MoveTo(target, caller_.Bearing + 180);
					callee.Gaze.LookAt = GazeSettings.LookAtTarget;
					callee.Gaze.Target = caller_.Atom.HeadPosition;
					state_ = MovingState;

					break;
				}

				case MovingState:
				{
					if (!callee.HasTarget)
					{
						callee.PushAction(new RandomAnimationAction(
							Resources.Animations.GetAll(
								Resources.Animations.StandIdle, callee.Sex)));

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

		public HandjobEvent(Person receiver)
		{
			receiver_ = receiver;
		}

		public override bool Tick(Person p, float s)
		{
			switch (state_)
			{
				case NoState:
				{
					var target =
						receiver_.Position +
						Vector3.Rotate(new Vector3(0, 0, 0.5f), receiver_.Bearing);

					p.MoveTo(target, receiver_.Bearing + 180);
					p.Gaze.LookAt = GazeSettings.LookAtTarget;
					p.Gaze.Target = receiver_.Atom.HeadPosition;
					state_ = MovingState;

					break;
				}

				case MovingState:
				{
					if (!p.HasTarget)
					{
						if (receiver_.State.Is(PersonState.Sitting))
						{
							p.Kneel();
							state_ = PositioningState;
						}
						else
						{
							p.Handjob.Target = Cue.Instance.Player;
							p.Handjob.Active = true;
							state_ = ActiveState;
						}
					}

					break;
				}

				case PositioningState:
				{
					if (!p.Animator.Playing)
					{
						p.Handjob.Target = Cue.Instance.Player;
						p.Handjob.Active = true;
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
