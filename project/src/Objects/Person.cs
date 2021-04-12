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
		void LookInFront();
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
		private Person person_;
		private readonly List<IPlayer> players_ = new List<IPlayer>();
		private IPlayer active_ = null;

		public Animator(Person p)
		{
			person_ = p;
			players_.Add(new BVH.Player(p));
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
		private IAI ai_ = null;
		private int state_ = StandingState;
		private int lastMoveState_ = MoveNone;
		private Vector3 standingPos_ = new Vector3();

		private Animator animator_;
		private IBreather breathing_;
		private ISpeaker speech_;
		private IGazer gaze_;

		public Person(W.IAtom atom)
			: base(atom)
		{
			animator_ = new Animator(this);
			breathing_ = new MacGruberBreather(this);
			speech_ = new VamSpeaker(this);
			gaze_ = new MacGruberGaze(this);

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

		public void InteractWith(IObject o)
		{
			actions_.Clear();

			if (o.SitSlot != null)
			{
				PushAction(new SitAction(o));
				PushAction(new MoveAction(o.Position, NoBearing));
			}
			else if (o.StandSlot != null)
			{
				PushAction(new MoveAction(o.Position, NoBearing));
			}
		}

		public override void Update(float s)
		{
			base.Update(s);

			if (ai_ != null)
				ai_.Tick(this, s);

			actions_.Tick(this, s);
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
				else if (state_ == StandingUpState)
					state_ = StandingState;
			}

			if (state_ == StandingState || state_ == WalkingState)
				standingPos_ = Position;
		}

		public void Sit()
		{
			animator_.Play(Resources.Animations.Sit(), false);
			state_ = SittingDownState;
		}

		public virtual void Say(string s)
		{
			speech_.Say(s);
		}

		protected override bool SetMoving(int i)
		{
			switch (i)
			{
				case MoveNone:
				{
					state_ = StandingState;
					animator_.Stop();
					break;
				}

				case MoveTentative:
				{
					if (state_ == StandingState || state_ == WalkingState)
					{
						return true;
					}

					if (state_ == SitState)
					{
						state_ = StandingUpState;
						animator_.Play(Resources.Animations.Sit(), true);
					}

					return false;
				}

				case MoveWalk:
				{
					state_ = WalkingState;

					if (lastMoveState_ != MoveWalk)
						animator_.Play(Resources.Animations.Walk());

					break;
				}

				case MoveTurnLeft:
				{
					if (lastMoveState_ != MoveTurnLeft)
						animator_.Play(Resources.Animations.TurnLeft());

					break;
				}

				case MoveTurnRight:
				{
					if (lastMoveState_ != MoveTurnRight)
						animator_.Play(Resources.Animations.TurnRight());

					break;
				}
			}

			lastMoveState_ = i;
			return true;
		}
	}
}
