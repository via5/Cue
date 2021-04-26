using System;

namespace Cue
{
	using NavStates = W.NavStates;

	class PersonState
	{
		public const int None = 0;
		public const int Standing = 1;
		public const int Walking = 2;
		public const int Sitting = 3;
		public const int Kneeling = 4;
		public const int SittingStraddling = 5;

		private Person self_;
		private int current_ = Standing;
		private int next_ = None;

		public PersonState(Person self)
		{
			self_ = self;
		}

		public bool IsUpright
		{
			get
			{
				return current_ == Standing || current_ == Walking;
			}
		}

		public bool Is(int type)
		{
			return current_ == type || next_ == type;
		}

		public bool IsCurrently(int type)
		{
			return current_ == type;
		}

		public bool Transitioning
		{
			get { return next_ != None; }
		}

		public void Set(int state)
		{
			if (current_ == state)
				return;

			string before = ToString();

			current_ = state;
			next_ = None;

			string after = ToString();

			Cue.LogInfo(
				self_.ID + ": " +
				"state changed from " + before + " to " + after);
		}

		public void StartTransition(int next)
		{
			if (next_ == next)
				return;

			next_ = next;
			Cue.LogInfo(self_.ID + ": new transition, " + ToString());
		}

		public void FinishTransition()
		{
			if (next_ == None)
				return;

			string before = StateToString(current_);

			current_ = next_;
			next_ = None;

			string after = StateToString(current_);

			Cue.LogInfo(
				self_.ID + ": " +
				"transition finished from " + before + " to " + after);
		}

		public override string ToString()
		{
			string s = StateToString(current_);

			if (next_ != None)
				s += "->" + StateToString(next_);

			return s;
		}

		public static string StateToString(int state)
		{
			string[] names = new string[]
			{
				"none", "standing", "walking", "sitting", "kneeling",
				"sitting-straddling"
			};

			if (state < 0 || state >= names.Length)
				return "?" + state.ToString();

			return names[state];
		}
	}


	class Excitement
	{
		private Person person_;
		private float lip_ = 0;
		private float mouth_ = 0;
		private float lBreast_ = 0;
		private float rBreast_ = 0;
		private float labia_ = 0;
		private float vagina_ = 0;
		private float deep_ = 0;
		private float deeper_ = 0;

		private float decay_ = 1;

		public Excitement(Person p)
		{
			person_ = p;
		}

		public void Update(float s)
		{
			lip_ = Check(s, person_.Atom.Triggers.Lip, lip_);
			mouth_ = Check(s, person_.Atom.Triggers.Mouth, mouth_);
			lBreast_ = Check(s, person_.Atom.Triggers.LeftBreast, lBreast_);
			rBreast_ = Check(s, person_.Atom.Triggers.RightBreast, rBreast_);
			labia_ = Check(s, person_.Atom.Triggers.Labia, labia_);
			vagina_ = Check(s, person_.Atom.Triggers.Vagina, vagina_);
			deep_ = Check(s, person_.Atom.Triggers.DeepVagina, deep_);
			deeper_ = Check(s, person_.Atom.Triggers.DeeperVagina, deeper_);
		}

		private float Check(float s, bool trigger, float v)
		{
			if (trigger)
				return 1;
			else
				return Math.Max(v - s * decay_, 0);
		}

		public float Genitals
		{
			get
			{
				return Math.Min(1,
					labia_ * 0.005f +
					vagina_ * 0.01f +
					deep_ * 0.2f +
					deeper_ * 1);
			}
		}

		public float Mouth
		{
			get
			{
				return
					lip_ * 0.1f +
					mouth_ * 0.9f;
			}
		}

		public float Breasts
		{
			get
			{
				return
					lBreast_ * 0.5f +
					rBreast_ * 0.5f;
			}
		}

		public override string ToString()
		{
			return
				$"l={lip_:0.00} m={mouth_:0.00} " +
				$"lb={lBreast_:0.00} rb={rBreast_:0.00} " +
				$"L={labia_:0.00} V={vagina_:0.00} " +
				$"VV={deep_:0.00} VVV={deeper_:0.00}";
		}
	}


	class Person : BasicObject
	{
		private readonly RootAction actions_ = new RootAction();
		private PersonState state_;
		private int lastNavState_ = NavStates.None;
		private Vector3 uprightPos_ = new Vector3();
		private Slot locked_ = null;
		private Animator animator_;
		private Excitement excitement_;

		private IAI ai_ = null;
		private IBreather breathing_;
		private IOrgasmer orgasmer_;
		private IGazer gaze_;
		private ISpeaker speech_;
		private IKisser kisser_;
		private IHandjob handjob_;
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

			breathing_ = Integration.CreateBreather(this);
			orgasmer_ = Integration.CreateOrgasmer(this);
			speech_ = Integration.CreateSpeaker(this);
			gaze_ = Integration.CreateGazer(this);
			kisser_ = Integration.CreateKisser(this);
			handjob_ = Integration.CreateHandjob(this);
			clothing_ = Integration.CreateClothing(this);
			personality_ = new NeutralPersonality(this);
			expression_ = Integration.CreateExpression(this);

			Gaze.LookAt = GazeSettings.LookAtDisabled;
		}

		public bool Idle
		{
			get { return actions_.IsIdle; }
		}

		public Vector3 UprightPosition
		{
			get { return uprightPos_; }
		}

		public Vector3 HeadPosition
		{
			get { return Atom.HeadPosition; }
		}

		public Slot LockedSlot
		{
			get { return locked_; }
			set { locked_ = value; }
		}

		public IAI AI { get { return ai_; } }
		public IBreather Breathing { get { return breathing_; } }
		public IOrgasmer Orgasmer { get { return orgasmer_; } }
		public IGazer Gaze { get { return gaze_; } }
		public ISpeaker Speech { get { return speech_; } }
		public IKisser Kisser { get { return kisser_; } }
		public IHandjob Handjob { get { return handjob_; } }
		public IClothing Clothing { get { return clothing_; } }
		public IExpression Expression { get { return expression_; } }
		public IAction Actions { get { return actions_; } }

		public Animator Animator { get { return animator_; } }
		public PersonState State { get { return state_; } }
		public Excitement Excitement { get { return excitement_; } }

		public int Sex
		{
			get { return Atom.Sex; }
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

		public void MakeIdle()
		{
			handjob_.Active = false;
			actions_.Clear();
			animator_.Stop();
			Atom.SetDefaultControls();
		}

		public bool InteractWith(IObject o)
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

			if (State.IsUpright && !State.Transitioning)
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
			expression_.Update(s);
			excitement_.Update(s);

			if (ai_ != null)
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

		public override string ToString()
		{
			return
				base.ToString() + ", " +
			//	Sexes.ToString(Sex) + ", " +
			//	state_.ToString() + ", " +
				excitement_.ToString() + ", " +
				ai_.Mood.ToString();
		}

		protected override bool StartMove()
		{
			if (State.IsUpright)
				return true;

			Stand();
			return false;
		}

		private void CheckNavState()
		{
			var navState = Atom.NavState;

			switch (navState)
			{
				case NavStates.None:
				{
					if (state_.IsCurrently(PersonState.Walking))
						state_.Set(PersonState.Standing);

					if (lastNavState_ != NavStates.None)
						animator_.Stop();

					break;
				}

				case NavStates.Moving:
				{
					state_.Set(PersonState.Walking);

					if (lastNavState_ != NavStates.Moving || !animator_.Playing)
						animator_.Play(Resources.Animations.Walk, Animator.Loop);

					break;
				}

				case NavStates.TurningLeft:
				{
					if (lastNavState_ != NavStates.TurningLeft || !animator_.Playing)
						animator_.Play(Resources.Animations.TurnLeft);

					break;
				}

				case NavStates.TurningRight:
				{
					if (lastNavState_ != NavStates.TurningRight || !animator_.Playing)
						animator_.Play(Resources.Animations.TurnRight);

					break;
				}
			}

			lastNavState_ = navState;
		}
	}
}
