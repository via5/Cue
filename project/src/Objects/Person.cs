namespace Cue
{
	interface IAnimation
	{
	}


	class Animator
	{
		private readonly BVH.Player bvh_;
		private bool playing_ = false;

		public Animator(Person p)
		{
			bvh_ = new BVH.Player(((W.VamAtom)p.Atom).Atom);
		}

		public bool Playing
		{
			get { return playing_; }
		}

		public void Play(IAnimation a, bool reverse=false)
		{
			if (a is BVH.Animation)
			{
				bvh_.Play((BVH.Animation)a, reverse);
				playing_ = bvh_.playing;
			}
		}

		public void FixedUpdate(float s)
		{
			if (playing_ || bvh_.playing)
			{
				bvh_.FixedUpdate(s);
				playing_ = bvh_.playing;
			}
		}

		public override string ToString()
		{
			if (playing_)
				return bvh_.ToString();
			else
				return "(none)";
		}
	}


	class Person : BasicObject
	{
		public const int StandingState = 0;
		public const int WalkingState = 1;
		public const int SittingDownState = 2;
		public const int SitState = 3;
		public const int StandingUpState = 4;

		private readonly RootAction actions_ = new RootAction();
		private readonly PersonAI ai_ = new PersonAI();
		private readonly IAnimation walk_ = null;
		private readonly IAnimation sit_ = null;
		private int state_ = StandingState;

		private Animator animator_;
		private IBreather breathing_;
		private ISpeaker speech_;
		private IGazer gaze_;

		public Person(W.IAtom atom)
			: base(atom)
		{
			walk_ = new BVH.Animation(
				"Custom\\Scripts\\VAMDeluxe\\Synthia Movement System\\Animations\\StandToWalk.bvh",
				true, false, false, 67, 150);

			sit_ = new BVH.Animation(
				"Custom\\Animations\\bvh_files\\avatar_sit_female.bvh",
				false, true, true, 0, 30);

			animator_ = new Animator(this);
			breathing_ = new MacGruberBreather(this);
			speech_ = new VamSpeaker(this);
			gaze_ = new MacGruberGaze(this);
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

		public string StateString
		{
			get
			{
				string[] names = new string[]
				{
					"standing", "walking", "sitting-down",
					"sitting", "standing up"
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

		public void PushAction(IAction a)
		{
			actions_.Push(a);
		}

		public void PopAction()
		{
			actions_.Pop();
		}

		public override void Update(float s)
		{
			base.Update(s);
			ai_.Tick(this, s);
			actions_.Tick(this, s);
		}

		public override void FixedUpdate(float s)
		{
			animator_.FixedUpdate(s);

			if (!animator_.Playing)
			{
				if (state_ == SittingDownState)
					state_ = SitState;
				else if (state_ == StandingUpState)
					state_ = StandingState;
			}
		}

		public void Sit()
		{
			animator_.Play(sit_, false);
			state_ = SittingDownState;
		}

		public virtual void Say(string s)
		{
			speech_.Say(s);
		}

		protected override bool SetMoving(bool b)
		{
			if (b)
			{
				if (state_ == SitState)
				{
					state_ = StandingUpState;
					animator_.Play(sit_, true);
					return false;
				}
				else if (state_ == StandingState)
				{
					state_ = WalkingState;
					animator_.Play(walk_);
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				state_ = StandingState;
				return true;
			}
		}
	}
}
