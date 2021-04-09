using System.Collections.Generic;

namespace Cue
{
	interface IBreather
	{
		float Intensity { get; set; }
		float Speed { get; set; }
	}

	class GazeSettings
	{
		public const int LookAtDisabled = 0;
		public const int LookAtTarget = 1;
		public const int LookAtPlayer = 2;
	}

	interface IGazer
	{
		int LookAt { get; set; }
		Vector3 Target { get; set; }
	}

	interface ISpeaker
	{
		void Say(string s);
	}

	interface IAnimation
	{
	}

	interface IPlayer
	{
		bool Playing { get; }
		bool Play(IAnimation a, bool reverse);
		void Stop();
		void FixedUpdate(float s);
	}

	class Animator
	{
		private readonly List<IPlayer> players_ = new List<IPlayer>();
		private IPlayer active_ = null;

		public Animator(Person p)
		{
			players_.Add(new BVH.Player(((W.VamAtom)p.Atom).Atom));
			players_.Add(new TimelinePlayer(p));
		}

		public bool Playing
		{
			get { return (active_ != null); }
		}

		public void Play(IAnimation a, bool reverse=false)
		{
			foreach (var p in players_)
			{
				if (p.Play(a, reverse))
				{
					active_ = p;
					break;
				}
			}
		}

		public void Stop()
		{
			if (active_ != null)
				active_.Stop();
		}

		public void FixedUpdate(float s)
		{
			if (active_ != null)
			{
				active_.FixedUpdate(s);
				if (!active_.Playing)
					active_ = null;
			}
		}

		public override string ToString()
		{
			if (active_ != null)
				return active_.ToString();
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
				animator_.Stop();
				return true;
			}
		}
	}
}
