﻿using System.Collections.Generic;

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
		void Update(float s);
	}

	interface ISpeaker
	{
		void Say(string s);
	}

	class Sexes
	{
		public const int Any = 0;
		public const int Male = 1;
		public const int Female = 2;

		public static int FromString(string os)
		{
			var s = os.ToLower();

			if (s == "male")
				return Male;
			else if (s == "female")
				return Female;
			else if (s == "")
				return Any;

			Cue.LogError("bad sex value '" + os + "'");
			return Any;
		}

		public static string ToString(int i)
		{
			switch (i)
			{
				case Male:
					return "male";

				case Female:
					return "female";

				default:
					return "any";
			}
		}

		public static bool Match(int a, int b)
		{
			if (a == Any || b == Any)
				return true;

			return (a == b);
		}
	}

	interface IAnimation
	{
		int Sex { get; set; }
	}

	abstract class BasicAnimation : IAnimation
	{
		private int sex_ = Sexes.Any;

		public int Sex
		{
			get
			{
				return sex_;
			}

			set
			{
				sex_ = value;
			}
		}
	}

	interface IPlayer
	{
		bool Playing { get; }
		bool Play(IAnimation a, int flags);
		void Stop();
		void FixedUpdate(float s);
	}

	class Animator
	{
		public const int Loop = 0x01;
		public const int Reverse = 0x02;

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

		public void Play(IAnimation a, int flags=0)
		{
			foreach (var p in players_)
			{
				if (p.Play(a, flags))
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


	interface IKisser
	{
		void Update(float s);
	}


	interface IHandjob
	{
		bool Active { get; set; }
		Person Target { get; set; }
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
		private IObject locked_ = null;

		private Animator animator_;
		private IBreather breathing_;
		private ISpeaker speech_;
		private IGazer gaze_;
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
			if (locked_ != o)
			{
				if (!o.Lock(this))
					return false;

				if (locked_ != null)
					locked_.Unlock(this);

				locked_ = o;
			}

			actions_.Clear();

			if (o.SitSlot != null)
			{
				PushAction(new SitAction(o));
				PushAction(new MoveAction(o.Position, NoBearing));
			}
			else if (o.StandSlot != null)
			{
				PushAction(new MakeIdleAction());
				PushAction(new MoveAction(o.Position, o.Bearing));
			}

			return true;
		}

		public override void Update(float s)
		{
			base.Update(s);

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
				else if (state_ == StandingUpState)
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

		public virtual void Say(string s)
		{
			speech_.Say(s);
		}

		public override string ToString()
		{
			return base.ToString() + " (" + Sexes.ToString(Sex) + ")";
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
						animator_.Play(
							Resources.Animations.GetAny(
								Resources.Animations.StandFromSitting, Sex));
					}

					return false;
				}

				case MoveWalk:
				{
					state_ = WalkingState;

					if (lastMoveState_ != MoveWalk)
					{
						animator_.Play(
							Resources.Animations.GetAny(Resources.Animations.Walk, Sex),
							Animator.Loop);
					}

					break;
				}

				case MoveTurnLeft:
				{
					if (lastMoveState_ != MoveTurnLeft)
						animator_.Play(Resources.Animations.GetAny(Resources.Animations.TurnLeft, Sex));

					break;
				}

				case MoveTurnRight:
				{
					if (lastMoveState_ != MoveTurnRight)
						animator_.Play(Resources.Animations.GetAny(Resources.Animations.TurnRight, Sex));

					break;
				}
			}

			lastMoveState_ = i;
			return true;
		}
	}
}
