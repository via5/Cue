﻿namespace Cue
{
	using NavStates = W.NavStates;

	class Person : BasicObject
	{
		public const int StandingState = 0;
		public const int WalkingState = 1;

		public const int SittingDownState = 2;
		public const int SitState = 3;
		public const int StandingFromSittingState = 4;

		public const int KneelingDownState = 5;
		public const int KneelState = 6;
		public const int StandingFromKneelingState = 7;


		private readonly RootAction actions_ = new RootAction();
		private IAI ai_ = null;
		private int state_ = StandingState;
		private int lastNavState_ = NavStates.None;
		private Vector3 standingPos_ = new Vector3();
		private Slot locked_ = null;

		private Animator animator_;
		private IBreather breathing_;
		private IGazer gaze_;
		private ISpeaker speech_;
		private IKisser kisser_;
		private IHandjob handjob_;

		public Person(W.IAtom atom)
			: base(atom)
		{
			ai_ = new PersonAI();
			animator_ = new Animator(this);
			breathing_ = new MacGruberBreather(this);
			speech_ = new VamSpeaker(this);
			gaze_ = new MacGruberGaze(this);
			kisser_ = new ClockwiseSilverKiss(this);
			handjob_ = new ClockwiseSilverHandjob(this);

			Gaze.LookAt = GazeSettings.LookAtDisabled;
		}

		public IAI AI
		{
			get { return ai_; }
			set { ai_ = value; }
		}

		public bool Idle
		{
			get { return actions_.IsIdle; }
		}

		public IAction Action
		{
			get { return actions_; }
		}

		public Animator Animator
		{
			get { return animator_; }
		}

		public Vector3 StandingPosition
		{
			get { return standingPos_; }
		}

		public Vector3 HeadPosition
		{
			get { return Atom.HeadPosition; }
		}

		public int State
		{
			get { return state_; }
		}

		public string StateString
		{
			get
			{
				string[] names = new string[]
				{
					"standing", "walking", "sitting-down",
					"sitting", "standing-up"
				};

				return names[state_];
			}
		}

		public IBreather Breathing
		{
			get { return breathing_; }
		}

		public ISpeaker Speech
		{
			get { return speech_; }
		}

		public IGazer Gaze
		{
			get { return gaze_; }
		}

		public IKisser Kisser
		{
			get { return kisser_; }
		}

		public IHandjob Handjob
		{
			get { return handjob_; }
		}

		public int Sex
		{
			get { return Atom.Sex; }
		}

		public void PushAction(IAction a)
		{
			actions_.Push(a);
		}

		public void PopAction()
		{
			actions_.Pop();
		}

		public void MakeIdle()
		{
			handjob_.Active = false;
			actions_.Clear();
			animator_.Stop();
			Atom.SetDefaultControls();
		}

		public void Call(Person caller)
		{
			AI.Enabled = false;
			AI.RunEvent(new CallEvent(caller));
		}

		public bool InteractWith(IObject o)
		{
			Slot s = o.Slots.GetLockedBy(this);

			if (s == null)
			{
				// object is not currently locked by this object

				if (o.Slots.AnyLocked)
				{
					// a slot is already locked, fail
					return false;
				}

				// take a random slot
				s = o.Slots.RandomUnlocked();

				if (s == null)
				{
					// no free slots
					return false;
				}

				if (!s.Lock(this))
				{
					// this object can't lock this slot
					return false;
				}

				// slot has been locked successfully, unlock the current slot,
				// if any
				if (locked_ != null)
					locked_.Unlock(this);

				locked_ = s;
			}

			MakeIdle();

			if (s.Type == Slot.Sit)
			{
				PushAction(new SitAction(s));
				PushAction(new MoveAction(s.Position, NoBearing));
			}
			else if (s.Type == Slot.Stand)
			{
				PushAction(new MakeIdleAction());
				PushAction(new MoveAction(s.Position, s.Bearing));
			}

			return true;
		}

		public override void Update(float s)
		{
			base.Update(s);
			CheckNavState();

			if (ai_ != null)
				ai_.Tick(this, s);

			actions_.Tick(this, s);
			gaze_.Update(s);
			kisser_.Update(s);
		}

		public override void OnPluginState(bool b)
		{
			base.OnPluginState(b);
			Atom.NavEnabled = b;
		}

		public override void SetPaused(bool b)
		{
			base.SetPaused(b);
			Atom.NavPaused = b;
		}

		public override void FixedUpdate(float s)
		{
			base.FixedUpdate(s);
			animator_.FixedUpdate(s);

			if (!animator_.Playing)
			{
				if (state_ == SittingDownState)
					state_ = SitState;
				else if (state_ == StandingFromSittingState)
					state_ = StandingState;
				else if (state_ == KneelingDownState)
					state_ = KneelState;
				else if (state_ == StandingFromKneelingState)
					state_ = StandingState;
			}

			if (state_ == StandingState || state_ == WalkingState)
				standingPos_ = Position;
		}

		public void Sit()
		{
			animator_.Play(
				Resources.Animations.GetAny(
					Resources.Animations.SitFromStanding, Sex));

			state_ = SittingDownState;
		}

		public void Kneel()
		{
			animator_.Play(
				Resources.Animations.GetAny(
					Resources.Animations.KneelFromStanding, Sex));

			state_ = KneelingDownState;
		}

		public void Stand()
		{
			if (state_ == SitState)
			{
				state_ = StandingFromSittingState;
				animator_.Play(
					Resources.Animations.GetAny(
						Resources.Animations.StandFromSitting, Sex));
			}
			else if (state_ == KneelState)
			{
				state_ = StandingFromKneelingState;
				animator_.Play(
					Resources.Animations.GetAny(
						Resources.Animations.StandFromKneeling, Sex));
			}
		}

		public virtual void Say(string s)
		{
			speech_.Say(s);
		}

		public override string ToString()
		{
			return base.ToString() + " (" + Sexes.ToString(Sex) + ")";
		}

		protected override bool StartMove()
		{
			if (state_ == StandingState || state_ == WalkingState)
				return true;

			if (state_ == SitState)
			{
				state_ = StandingFromSittingState;
				animator_.Play(
					Resources.Animations.GetAny(
						Resources.Animations.StandFromSitting, Sex));
			}
			else if (state_ == KneelState)
			{
				state_ = StandingFromKneelingState;
				animator_.Play(
					Resources.Animations.GetAny(
						Resources.Animations.StandFromKneeling, Sex));
			}

			return false;
		}

		private void CheckNavState()
		{
			var navState = Atom.NavState;

			switch (navState)
			{
				case NavStates.None:
				{
					state_ = StandingState;

					if (lastNavState_ != NavStates.None)
						animator_.Stop();

					break;
				}

				case NavStates.Moving:
				{
					state_ = WalkingState;

					if (lastNavState_ != NavStates.Moving || !animator_.Playing)
					{
						animator_.Play(
							Resources.Animations.GetAny(Resources.Animations.Walk, Sex),
							Animator.Loop);
					}

					break;
				}

				case NavStates.TurningLeft:
				{
					if (lastNavState_ != NavStates.TurningLeft || !animator_.Playing)
						animator_.Play(Resources.Animations.GetAny(Resources.Animations.TurnLeft, Sex));

					break;
				}

				case NavStates.TurningRight:
				{
					if (lastNavState_ != NavStates.TurningRight || !animator_.Playing)
						animator_.Play(Resources.Animations.GetAny(Resources.Animations.TurnRight, Sex));

					break;
				}
			}

			lastNavState_ = navState;
		}
	}
}
