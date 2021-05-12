using System.Collections.Generic;

namespace Cue
{
	class Animation
	{
		public const int NoType = 0;
		public const int WalkType = 1;
		public const int TurnLeftType = 2;
		public const int TurnRightType = 3;
		public const int TransitionType = 4;
		public const int SexType = 5;
		public const int IdleType = 6;

		private readonly int type_ = NoType;
		private readonly int from_ = PersonState.None;
		private readonly int to_ = PersonState.None;
		private readonly int state_ = PersonState.None;
		private readonly int sex_ = Sexes.Any;
		private readonly IAnimation anim_ = null;

		public Animation(int type, int from, int to, int state, int sex, IAnimation anim)
		{
			type_ = type;
			from_ = from;
			to_ = to;
			state_ = state;
			sex_ = sex;
			anim_ = anim;
		}

		public int Type { get { return type_; } }
		public int TransitionFrom { get { return from_; } }
		public int TransitionTo { get { return to_; } }
		public int State { get { return state_; } }
		public int Sex { get { return sex_; } }
		public IAnimation Real { get { return anim_; } }

		public override string ToString()
		{
			string s = TypeToString(type_) + " ";

			switch (type_)
			{
				case TransitionType:
				{
					s +=
						PersonState.StateToString(from_) + "->" +
						PersonState.StateToString(to_) + " ";

					break;
				}

				case SexType:
				case IdleType:
				{
					s += PersonState.StateToString(state_) + " ";
					break;
				}
			}

			s += "sex=" + Sexes.ToString(sex_) + " " + anim_.ToString();

			return s;
		}

		public static int TypeFromString(string os)
		{
			string s = os.ToLower();

			if (s == "walk")
				return WalkType;
			else if (s == "turnleft")
				return TurnLeftType;
			else if (s == "turnright")
				return TurnRightType;
			else if (s == "transition")
				return TransitionType;
			else if (s == "sex")
				return SexType;
			else if (s == "idle")
				return IdleType;

			Cue.LogError($"unknown anim type '{os}'");
			return NoType;
		}

		public static string TypeToString(int t)
		{
			switch (t)
			{
				case NoType: return "none";
				case WalkType: return "walk";
				case TurnLeftType: return "turnLeft";
				case TurnRightType: return "turnRight";
				case TransitionType: return "transition";
				case SexType: return "sex";
				case IdleType: return "idle";
				default: return $"?{t}";
			}
		}
	}


	class Animator
	{
		public const int Loop = 0x01;
		public const int Reverse = 0x02;
		public const int Rewind = 0x04;
		public const int Exclusive = 0x08;

		private Person person_;
		private Logger log_;
		private readonly List<IPlayer> players_ = new List<IPlayer>();
		private IPlayer currentPlayer_ = null;
		private Animation currentAnimation_ = null;
		private int activeFlags_ = 0;

		public Animator(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Animation, p, "Animator");
			players_.AddRange(Integration.CreateAnimationPlayers(p));
		}

		public bool Playing
		{
			get { return (currentAnimation_ != null); }
		}

		public IPlayer CurrentPlayer
		{
			get { return currentPlayer_; }
		}

		public Animation CurrentAnimation
		{
			get { return currentAnimation_; }
		}

		public bool PlayTransition(int from, int to, int flags = 0)
		{
			var a = Resources.Animations.GetAnyTransition(
				from, to, person_.Sex);

			if (a == null)
			{
				log_.Error(
					$"no transition animation from " +
					$"from {PersonState.StateToString(from)} " +
					$"to {PersonState.StateToString(to)}");

				return false;
			}

			Play(a, flags);
			return true;
		}

		public bool PlaySex(int state, int flags = 0)
		{
			var a = Resources.Animations.GetAnySex(state, person_.Sex);

			if (a == null)
			{
				log_.Error(
					$"no sex animation for " +
					$"state {PersonState.StateToString(state)}");

				return false;
			}

			return Play(a, flags);
		}

		public bool PlayType(int type, int flags = 0)
		{
			return Play(Resources.Animations.GetAny(type, person_.Sex), flags);
		}

		public bool PlayNeutral()
		{
			if (person_.State.IsCurrently(PersonState.Standing))
			{
				return PlayTransition(
					PersonState.Standing, PersonState.Standing);
			}

			return true;
		}

		public bool CanPlay(Animation a, int flags = 0, bool silent = true)
		{
			if (currentAnimation_ == null)
				return true;

			if (Bits.IsSet(activeFlags_, Exclusive))
			{
				if (Bits.IsSet(flags, Exclusive))
				{
					if (!silent)
					{
						log_.Error(
							$"cannot exclusively play {a}, " +
							$"current animation {currentAnimation_} is also " +
							$"exclusive");
					}

					return false;
				}
				else
				{
					if (!silent)
					{
						log_.Error(
							$"cannot play {a}, " +
							$"current animation {currentAnimation_} is exclusive");
					}

					return false;
				}
			}

			return true;
		}

		public bool Play(Animation a, int flags = 0)
		{
			log_.Info("playing " + a.ToString());

			if (!CanPlay(a, flags, false))
				return false;

			for (int i=0; i<players_.Count;++i)
			{
				var p = players_[i];

				if (p.Play(a.Real, flags))
				{
					currentPlayer_ = p;
					currentAnimation_ = a;
					activeFlags_ = flags;

					return true;
				}
			}

			log_.Error("no player can play " + a.ToString());
			return true;
		}

		public void Stop()
		{
			if (currentPlayer_ != null)
				currentPlayer_.Stop(Bits.IsSet(activeFlags_, Rewind));
		}

		public void FixedUpdate(float s)
		{
			if (currentPlayer_ != null)
			{
				currentPlayer_.FixedUpdate(s);
				if (!currentPlayer_.Playing)
				{
					currentPlayer_ = null;
					currentAnimation_ = null;
				}
			}
		}

		public void Update(float s)
		{
			if (currentPlayer_ != null)
			{
				currentPlayer_.Update(s);
				if (!currentPlayer_.Playing)
				{
					currentPlayer_ = null;
					currentAnimation_ = null;
				}
			}
		}

		public void OnPluginState(bool b)
		{
			if (!b)
			{
				foreach (var p in players_)
				{
					if (p is BVH.Player)
						((BVH.Player)p).HideSkeleton();
				}
			}
		}

		public override string ToString()
		{
			if (currentPlayer_ != null)
				return currentPlayer_.ToString();
			else
				return "(none)";
		}
	}
}
