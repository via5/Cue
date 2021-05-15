using System.Collections.Generic;

namespace Cue
{
	class PersonClothing
	{
		private Person person_;

		public PersonClothing(Person p)
		{
			person_ = p;
		}

		public void Init()
		{
			person_.Atom.Clothing.Init();
		}

		public float HeelsAngle
		{
			get { return person_.Atom.Clothing.HeelsAngle; }
		}

		public float HeelsHeight
		{
			get { return person_.Atom.Clothing.HeelsHeight; }
		}

		public bool GenitalsVisible
		{
			get { return person_.Atom.Clothing.GenitalsVisible; }
			set { person_.Atom.Clothing.GenitalsVisible = value; }
		}

		public bool BreastsVisible
		{
			get { return person_.Atom.Clothing.BreastsVisible; }
			set { person_.Atom.Clothing.BreastsVisible = value; }
		}

		public void Dump()
		{
			person_.Atom.Clothing.Dump();
		}

		public override string ToString()
		{
			return person_.Atom.Clothing.ToString();
		}
	}

	class Person : BasicObject
	{
		private readonly RootAction actions_;
		private PersonState state_;
		private bool deferredTransition_ = false;
		private int deferredState_ = PersonState.None;
		private int lastNavState_ = W.NavStates.None;
		private Vector3 uprightPos_ = new Vector3();

		private Animator animator_;
		private Excitement excitement_;
		private Body body_;
		private Gaze gaze_;
		private IAI ai_ = null;
		private PersonClothing clothing_;
		private IPersonality personality_;

		private IBreather breathing_;
		private IOrgasmer orgasmer_;
		private ISpeaker speech_;
		private IKisser kisser_;
		private IHandjob handjob_;
		private IBlowjob blowjob_;
		private IExpression expression_;

		public Person(W.IAtom atom)
			: base(atom)
		{
			actions_ = new RootAction(this);
			state_ = new PersonState(this);
			animator_ = new Animator(this);
			excitement_ = new Excitement(this);
			body_ = new Body(this);
			gaze_ = new Gaze(this);
			ai_ = new PersonAI(this);
			clothing_ = new PersonClothing(this);
			personality_ = new NeutralPersonality(this);

			breathing_ = Integration.CreateBreather(this);
			orgasmer_ = Integration.CreateOrgasmer(this);
			speech_ = Integration.CreateSpeaker(this);
			kisser_ = Integration.CreateKisser(this);
			handjob_ = Integration.CreateHandjob(this);
			blowjob_ = Integration.CreateBlowjob(this);
			expression_ = Integration.CreateExpression(this);

			Atom.SetDefaultControls("init");
		}

		public bool Idle
		{
			get { return actions_.IsIdle; }
		}

		public Vector3 UprightPosition
		{
			get { return uprightPos_; }
		}

		public Animator Animator { get { return animator_; } }
		public PersonState State { get { return state_; } }
		public Excitement Excitement { get { return excitement_; } }
		public Body Body { get { return body_; } }
		public Gaze Gaze { get { return gaze_; } }
		public IAI AI { get { return ai_; } }
		public PersonClothing Clothing { get { return clothing_; } }
		public RootAction Actions { get { return actions_; } }

		public IBreather Breathing { get { return breathing_; } }
		public IOrgasmer Orgasmer { get { return orgasmer_; } }
		public ISpeaker Speech { get { return speech_; } }
		public IKisser Kisser { get { return kisser_; } }
		public IHandjob Handjob { get { return handjob_; } }
		public IBlowjob Blowjob { get { return blowjob_; } }
		public IExpression Expression { get { return expression_; } }

		public int Sex
		{
			get { return Atom.Sex; }
		}

		public override Vector3 EyeInterest
		{
			get
			{
				return body_.Get(BodyParts.Eyes)?.Position ?? base.EyeInterest;
			}
		}

		public IPersonality Personality
		{
			get { return personality_; }
			set { personality_ = value; }
		}

		public bool CanMoveHead
		{
			get
			{
				return !Kisser.Active && !Blowjob.Active;
			}
		}

		public bool CanMove
		{
			get
			{
				return !Kisser.Active && !Blowjob.Active && !Handjob.Active;
			}
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
			Kisser.Stop();
			Handjob.Stop();
			Blowjob.Stop();

			actions_.Clear();
			animator_.Stop();

			Atom.SetDefaultControls("make idle");
		}

		public override void MakeIdleForMove()
		{
			if (state_.IsCurrently(PersonState.Walking))
				state_.CancelTransition();

			Kisser.Stop();
			Handjob.Stop();
			Blowjob.Stop();

			actions_.Clear();
			ai_.MakeIdle();
		}

		public override bool InteractWith(IObject o)
		{
			if (ai_ == null)
				return false;

			return ai_.InteractWith(o);
		}

		public override void FixedUpdate(float s)
		{
			base.FixedUpdate(s);
			animator_.FixedUpdate(s);
		}

		public override void Update(float s)
		{
			base.Update(s);

			CheckNavState();

			if (deferredTransition_)
			{
				if (StartTransition())
					PlayTransition();
			}

			if (State.IsUpright)
				uprightPos_ = Position;

			animator_.Update(s);

			if (!deferredTransition_ && !animator_.Playing)
			{
				state_.FinishTransition();
				if (deferredState_ != PersonState.None)
				{
					log_.Info("animation finished, setting deferred state");

					var ds = deferredState_;
					deferredState_ = PersonState.None;
					SetState(ds);
				}
			}

			actions_.Tick(s);
			gaze_.Update(s);
			Kisser.Update(s);
			Handjob.Update(s);
			Blowjob.Update(s);
			expression_.Update(s);
			excitement_.Update(s);
			personality_.Update(s);

			if (this != Cue.Instance.Player)
				body_.Update(s);

			if (ai_ != null && !Atom.Teleporting)
				ai_.Update(s);
		}

		public override void OnPluginState(bool b)
		{
			base.OnPluginState(b);

			Atom.NavEnabled = b;

			animator_.OnPluginState(b);
			expression_.OnPluginState(b);
			Kisser.OnPluginState(b);
			Handjob.OnPluginState(b);
			Blowjob.OnPluginState(b);

			ai_.OnPluginState(b);
		}

		public override void SetPaused(bool b)
		{
			base.SetPaused(b);
			Atom.NavPaused = b;
		}

		public void SetState(int s)
		{
			if (state_.Next == s || deferredState_ == s)
			{
				// already transitioning to that state
				return;
			}

			deferredState_ = PersonState.None;
			state_.StartTransition(s);

			if (!StartTransition())
			{
				deferredTransition_ = true;
				return;
			}

			animator_.Stop();
			PlayTransition();
		}

		private void PlayTransition()
		{
			deferredTransition_ = false;

			if (!animator_.PlayTransition(state_.Current, state_.Next, Animator.Exclusive))
			{
				// no animation for this transition, stand first if not already
				// trying to stand
				if (state_.Next != PersonState.Standing)
				{
					log_.Info(
						$"no animation for transition " +
						$"{PersonState.StateToString(state_.Current)}->" +
						$"{PersonState.StateToString(state_.Next)}, standing first");

					deferredState_ = state_.Next;
					state_.StartTransition(PersonState.Standing);

					animator_.PlayTransition(
						state_.Current, PersonState.Standing,
						Animator.Exclusive);
				}
			}
		}

		public virtual void Say(string s)
		{
			speech_.Say(s);
		}

		protected bool StartTransition()
		{
			bool canStart = true;

			if (Kisser.Active)
			{
				Kisser.Stop();
				canStart = false;
			}

			if (Handjob.Active)
			{
				Handjob.Stop();
				canStart = false;
			}

			if (Blowjob.Active)
			{
				Blowjob.Stop();
				canStart = false;
			}

			return canStart;
		}

		protected override bool StartMove()
		{
			if (!StartTransition())
				return false;

			if (!State.IsUpright)
			{
				SetState(PersonState.Standing);
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
					if (lastNavState_ != W.NavStates.None)
					{
						// force the state to standing first, there are no
						// animations for walk->stand
						state_.Set(PersonState.Standing);

						// must stop manually, it's exclusive
						animator_.Stop();

						SetState(PersonState.Standing);
					}

					break;
				}

				case W.NavStates.Calculating:
				{
					// wait
					break;
				}

				case W.NavStates.Moving:
				{
					state_.Set(PersonState.Walking);

					if ((
							lastNavState_ != W.NavStates.Moving &&
							lastNavState_ != W.NavStates.Calculating
						)
						|| !animator_.Playing)
					{
						animator_.PlayType(
							Animation.WalkType,
							Animator.Loop | Animator.Exclusive);
					}

					break;
				}

				case W.NavStates.TurningLeft:
				{
					if (lastNavState_ != W.NavStates.TurningLeft || !animator_.Playing)
					{
						if (CanMove)
						{
							animator_.PlayType(
								Animation.TurnLeftType, Animator.Exclusive);
						}
					}

					break;
				}

				case W.NavStates.TurningRight:
				{
					if (lastNavState_ != W.NavStates.TurningRight || !animator_.Playing)
					{
						if (CanMove)
						{
							animator_.PlayType(
								Animation.TurnRightType, Animator.Exclusive);
						}
					}

					break;
				}
			}

			lastNavState_ = navState;
		}
	}
}
