namespace Cue
{
	class Person : BasicObject
	{
		private readonly RootAction actions_ = new RootAction();
		private PersonState state_;
		private int lastNavState_ = W.NavStates.None;
		private Vector3 uprightPos_ = new Vector3();
		private Slot locked_ = null;
		private Animator animator_;
		private Excitement excitement_;
		private Body body_;
		private Gaze gaze_;

		private IAI ai_ = null;
		private IBreather breathing_;
		private IOrgasmer orgasmer_;
		private ISpeaker speech_;
		private IKisser kisser_;
		private IHandjob handjob_;
		private IBlowjob blowjob_;
		private IClothing clothing_;
		private IPersonality personality_;
		private IExpression expression_;

		public Person(W.IAtom atom)
			: base(atom)
		{
			ai_ = new PersonAI(this);
			state_ = new PersonState(this);
			animator_ = new Animator(this);
			excitement_ = new Excitement(this);
			body_ = new Body(this);
			gaze_ = new Gaze(this);

			breathing_ = Integration.CreateBreather(this);
			orgasmer_ = Integration.CreateOrgasmer(this);
			speech_ = Integration.CreateSpeaker(this);
			kisser_ = Integration.CreateKisser(this);
			handjob_ = Integration.CreateHandjob(this);
			blowjob_ = Integration.CreateBlowjob(this);
			clothing_ = Integration.CreateClothing(this);
			personality_ = new NeutralPersonality(this);
			expression_ = Integration.CreateExpression(this);

			Atom.SetDefaultControls("init");
		}

		public bool Idle
		{
			get { return actions_.IsIdle; }
		}

		public bool Possessed
		{
			get { return Atom.Possessed; }
		}

		public Vector3 UprightPosition
		{
			get { return uprightPos_; }
		}

		public Slot LockedSlot
		{
			get { return locked_; }
			set { locked_ = value; }
		}

		public Animator Animator { get { return animator_; } }
		public PersonState State { get { return state_; } }
		public Excitement Excitement { get { return excitement_; } }
		public Body Body { get { return body_; } }
		public Gaze Gaze { get { return gaze_; } }

		public IAI AI { get { return ai_; } }
		public IBreather Breathing { get { return breathing_; } }
		public IOrgasmer Orgasmer { get { return orgasmer_; } }
		public ISpeaker Speech { get { return speech_; } }
		public IKisser Kisser { get { return kisser_; } }
		public IHandjob Handjob { get { return handjob_; } }
		public IBlowjob Blowjob { get { return blowjob_; } }
		public IClothing Clothing { get { return clothing_; } }
		public IExpression Expression { get { return expression_; } }
		public IAction Actions { get { return actions_; } }

		public int Sex
		{
			get { return Atom.Sex; }
		}

		public override Vector3 EyeInterest
		{
			get
			{
				return body_.Head?.Position ?? base.EyeInterest;
			}
		}

		public IPersonality Personality
		{
			get { return personality_; }
			set { personality_ = value; }
		}

		public void PushAction(IAction a)
		{
			actions_.Push(a);
		}

		public void PopAction()
		{
			actions_.Pop();
		}

		public override void MakeIdle()
		{
			kisser_.Stop();
			handjob_.Stop();
			blowjob_.Stop();
			actions_.Clear();
			animator_.Stop();
			Atom.SetDefaultControls("make idle");
		}

		public override void MakeIdleForMove()
		{
			if (state_.IsCurrently(PersonState.Walking))
				state_.CancelTransition();

			handjob_.Stop();
			blowjob_.Stop();
			actions_.Clear();
			ai_.RunEvent(null);
			Atom.SetDefaultControls("make idle for move");
		}

		public override bool InteractWith(IObject o)
		{
			if (ai_ == null)
				return false;

			return ai_.InteractWith(o);
		}

		public bool TryLockSlot(IObject o)
		{
			Slot s = o.Slots.GetLockedBy(this);

			// object is already locked by this person, reuse it
			if (s != null)
				return true;

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

			return TryLockSlot(s);
		}

		public bool TryLockSlot(Slot s)
		{
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

			return true;
		}

		public override void FixedUpdate(float s)
		{
			base.FixedUpdate(s);
			animator_.FixedUpdate(s);

			if (!animator_.Playing)
				state_.FinishTransition();

			if (State.IsUpright)
				uprightPos_ = Position;
		}

		public override void Update(float s)
		{
			base.Update(s);
			CheckNavState();

			animator_.Update(s);
			actions_.Tick(this, s);
			gaze_.Update(s);
			kisser_.Update(s);
			handjob_.Update(s);
			blowjob_.Update(s);
			expression_.Update(s);
			excitement_.Update(s);

			if (ai_ != null && !Atom.Teleporting)
				ai_.Update(s);
		}

		public override void OnPluginState(bool b)
		{
			base.OnPluginState(b);
			Atom.NavEnabled = b;
			clothing_.OnPluginState(b);
			ai_.OnPluginState(b);
			expression_.OnPluginState(b);
		}

		public override void SetPaused(bool b)
		{
			base.SetPaused(b);
			Atom.NavPaused = b;
		}

		public void Straddle()
		{
			animator_.Play(Resources.Animations.StraddleSitFromStanding);
			state_.StartTransition(PersonState.SittingStraddling);
		}

		public void Sit()
		{
			animator_.Play(Resources.Animations.SitFromStanding);
			state_.StartTransition(PersonState.Sitting);
		}

		public void Kneel()
		{
			animator_.Play(Resources.Animations.KneelFromStanding);
			state_.StartTransition(PersonState.Kneeling);
		}

		public void Stand()
		{
			// todo: let current animation finish first
			if (state_.Is(PersonState.Sitting) && !state_.Transitioning)
			{
				state_.StartTransition(PersonState.Standing);
				animator_.Play(Resources.Animations.StandFromSitting);
			}
			else if (state_.Is(PersonState.Kneeling) && !state_.Transitioning)
			{
				state_.StartTransition(PersonState.Standing);
				animator_.Play(Resources.Animations.StandFromKneeling);
			}
			else if (state_.Is(PersonState.SittingStraddling) && !state_.Transitioning)
			{
				state_.StartTransition(PersonState.Standing);
				animator_.Play(Resources.Animations.StandFromStraddleSit);
			}
		}

		public virtual void Say(string s)
		{
			speech_.Say(s);
		}

		protected override bool StartMove()
		{
			if (kisser_.Active)
			{
				kisser_.Stop();
				return false;
			}

			if (!State.IsUpright)
			{
				Stand();
				return false;
			}

			return true;
		}

		private void CheckNavState()
		{
			var navState = Atom.NavState;

			switch (navState)
			{
				case W.NavStates.None:
				{
					if (state_.IsCurrently(PersonState.Walking))
						state_.Set(PersonState.Standing);

					if (lastNavState_ != W.NavStates.None)
						animator_.Play(Resources.Animations.Stand);

					break;
				}

				case W.NavStates.Moving:
				{
					state_.Set(PersonState.Walking);

					if (lastNavState_ != W.NavStates.Moving || !animator_.Playing)
						animator_.Play(Resources.Animations.Walk, Animator.Loop);

					break;
				}

				case W.NavStates.TurningLeft:
				{
					if (lastNavState_ != W.NavStates.TurningLeft || !animator_.Playing)
						animator_.Play(Resources.Animations.TurnLeft);

					break;
				}

				case W.NavStates.TurningRight:
				{
					if (lastNavState_ != W.NavStates.TurningRight || !animator_.Playing)
						animator_.Play(Resources.Animations.TurnRight);

					break;
				}
			}

			lastNavState_ = navState;
		}
	}
}
