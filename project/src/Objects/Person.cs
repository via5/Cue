namespace Cue
{
	class Person : BasicObject
	{
		public const int StandingState = 0;
		public const int WalkingState = 1;
		public const int SittingDownState = 2;
		public const int SitState = 3;
		public const int StandingUpState = 4;

		private readonly RootAction actions_ = new RootAction();
		private readonly PersonAI ai_ = new PersonAI();
		private readonly BVH.Player player_;
		private readonly BVH.Animation walk_ = new BVH.Animation();
		private readonly BVH.Animation sit_ = new BVH.Animation();
		private bool animating_ = false;
		private int state_ = StandingState;

		public Person(W.IAtom atom)
			: base(atom)
		{
			player_ = new BVH.Player(((W.VamAtom)atom).Atom);

			walk_.file = new BVH.File(
				"Custom\\Scripts\\VAMDeluxe\\Synthia Movement System\\Animations\\StandToWalk.bvh");
			walk_.loop = true;
			walk_.start = 67;
			walk_.end = 150;

			sit_.file = new BVH.File(
				"Custom\\Animations\\bvh_files\\avatar_sit_female.bvh");
			sit_.end = 30;
			sit_.rootXZ = true;
			sit_.rootY = true;
		}

		public bool Idle
		{
			get
			{
				return actions_.IsIdle;
			}
		}

		public IAction Action
		{
			get { return actions_; }
		}

		public BVH.Player Animation
		{
			get { return player_; }
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

		public void PushAction(IAction a)
		{
			actions_.Add(a);
		}

		public override void Update(float s)
		{
			base.Update(s);
			ai_.Tick(this, s);
			actions_.Tick(this, s);
		}

		public override void FixedUpdate(float s)
		{
			if (animating_)
			{
				player_.FixedUpdate();
				player_.ApplyRootMotion();

				animating_ = player_.playing;

				if (!animating_)
				{
					if (state_ == SittingDownState)
					{
						state_ = SitState;
					}
					else if (state_ == StandingUpState)
					{
						state_ = StandingState;
					}
				}
			}
		}

		public override bool Animating
		{
			get { return animating_; }
		}

		public void Sit()
		{
			player_.Play(sit_, false);
			animating_ = true;
			state_ = SittingDownState;
		}

		public override void PlayAnimation(int i, bool loop)
		{
		}

		protected override bool SetMoving(bool b)
		{
			if (b)
			{
				if (state_ == SitState)
				{
					state_ = StandingUpState;
					player_.Play(sit_, true);
					animating_ = true;
					return false;
				}
				else if (state_ == StandingState)
				{
					state_ = WalkingState;
					player_.Play(walk_);
					animating_ = true;
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
				animating_ = false;
				return true;
			}
		}
	}
}
