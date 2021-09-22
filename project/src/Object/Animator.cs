using System.Collections.Generic;

namespace Cue
{
	class Animation
	{
		private readonly int type_ = Animations.None;
		private readonly int from_ = PersonState.None;
		private readonly int to_ = PersonState.None;
		private readonly int state_ = PersonState.None;
		private readonly int style_ = MovementStyles.Any;
		private readonly IAnimation anim_ = null;

		private IPlayer player_ = null;

		public Animation(int type, int from, int to, int state, int ms, IAnimation anim)
		{
			type_ = type;
			from_ = from;
			to_ = to;
			state_ = state;
			style_ = ms;
			anim_ = anim;
		}

		public int Type { get { return type_; } }
		public int TransitionFrom { get { return from_; } }
		public int TransitionTo { get { return to_; } }
		public int State { get { return state_; } }
		public int MovementStyle { get { return style_; } }
		public bool HasMovement { get { return anim_.HasMovement; } }
		public IAnimation Real { get { return anim_; } }

		public IPlayer Player
		{
			get { return player_; }
		}

		public void SetPlayer(IPlayer p)
		{
			player_ = p;
		}

		public override string ToString()
		{
			string s = Animations.ToString(type_) + " ";

			switch (type_)
			{
				case Animations.Transition:
				{
					s +=
						PersonState.StateToString(from_) + "->" +
						PersonState.StateToString(to_) + " ";

					break;
				}

				case Animations.Sex:
				case Animations.Idle:
				{
					s += PersonState.StateToString(state_) + " ";
					break;
				}

				default:
				{
					break;
				}
			}

			s += "ms=" + MovementStyles.ToString(style_) + " " + anim_.ToString();

			return s;
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
		private readonly List<Animation> playing_ = new List<Animation>();
		//private IPlayer currentPlayer_ = null;
		//private Animation currentAnimation_ = null;
		private int activeFlags_ = 0;

		public Animator(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Animation, p, "Animator");
			players_.AddRange(Integration.CreateAnimationPlayers(p));
		}

		public List<IPlayer> Players
		{
			get { return players_; }
		}

		public bool IsPlayingTransitionTo(int state)
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				var a = playing_[i];

				if (a.Type == Animations.Transition)
				{
					if (a.TransitionTo == state)
						return true;
				}
			}

			return false;
		}

		public bool IsPlayingTransition()
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				var a = playing_[i];

				if (a.Type == Animations.Transition)
					return true;
			}

			return false;
		}

		public bool CanPlayType(int type)
		{
			// todo
			if (IsPlayingTransition())
				return false;

			return true;
		}

		public bool IsPlaying(Animation a)
		{
			return playing_.Contains(a);
		}

		public bool IsPlayingType(int type)
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				if (playing_[i].Type == type)
					return true;
			}

			return false;
		}

		public bool PlayTransition(int from, int to, object ps = null, int flags = 0)
		{
			var a = Resources.Animations.GetAnyTransition(
				from, to, person_.MovementStyle);

			if (a == null)
			{
				log_.Error(
					$"no transition animation from " +
					$"from {PersonState.StateToString(from)} " +
					$"to {PersonState.StateToString(to)}");

				return false;
			}

			return Play(a, ps, flags);
		}

		public bool PlayType(int type, object ps = null, int flags = 0)
		{
			if (IsPlayingType(type))
				return false;

			var a = Resources.Animations.GetAny(type, person_.MovementStyle);
			if (a == null)
			{
				log_.Error($"no animation for type {Animations.ToString(type)}");
				return false;
			}

			return Play(a, ps, flags);
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
			if (a.Type == Animations.Transition)
			{
				if (IsPlayingTransition())
					return false;
			}

			if (Bits.IsSet(activeFlags_, Exclusive))
			{
				if (Bits.IsSet(flags, Exclusive))
				{
					// allow exclusive to override exclusive, happens for walk
					// and turn, for example
					return true;
				}
				else
				{
					if (!silent)
					{
						log_.Error(
							$"cannot play {a}, " +
							$"current animation is exclusive");
					}

					return false;
				}
			}

			return true;
		}

		public bool Play(Animation a, object ps = null, int flags = 0)
		{
			log_.Info("playing " + a.ToString());

			if (!Cue.Instance.Options.AllowMovement && a.HasMovement)
			{
				log_.Info("not playing animation, movement not allowed");
				return false;
			}

			if (!CanPlay(a, flags, false))
				return false;

			for (int i=0; i<players_.Count;++i)
			{
				var p = players_[i];

				if (p.Play(a.Real, ps, flags))
				{
					playing_.Add(a);
					a.SetPlayer(p);
					activeFlags_ = flags;
					return true;
				}
			}

			log_.Error("no player can play " + a.ToString());
			return false;
		}

		public void Stop()
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				var a = playing_[i];

				a.Player.Stop(a.Real, Bits.IsSet(activeFlags_, Rewind));
				a.SetPlayer(null);
			}

			playing_.Clear();
			activeFlags_ = 0;
		}

		public void StopType(int type)
		{
			int i = 0;

			while (i < playing_.Count)
			{
				var a = playing_[i];

				if (a.Type == type)
				{
					a.Player.Stop(a.Real, Bits.IsSet(activeFlags_, Rewind));
					playing_.RemoveAt(i);
				}
				else
				{
					++i;
				}
			}
		}

		public void FixedUpdate(float s)
		{
			for (int i = 0; i < players_.Count; ++i)
				players_[i].FixedUpdate(s);

			RemoveFinished();
		}

		public void Update(float s)
		{
			for (int i = 0; i < players_.Count; ++i)
				players_[i].Update(s);

			RemoveFinished();
		}

		private void RemoveFinished()
		{
			int i = 0;

			while (i < playing_.Count)
			{
				var a = playing_[i];

				if (!a.Player.IsPlaying(a.Real))
					playing_.RemoveAt(i);
				else
					++i;
			}

			if (playing_.Count == 0)
				activeFlags_ = 0;
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
			return $"playing {playing_.Count} anims";

			//if (currentPlayer_ != null)
			//	return currentPlayer_.ToString();
			//else
			//	return "(none)";
		}
	}
}
