﻿using System.Collections.Generic;

namespace Cue
{
	class Animation
	{
		private readonly int type_ = Animations.None;
		private readonly int style_ = MovementStyles.Any;
		private readonly IAnimation anim_ = null;

		public Animation(int type, int ms, IAnimation anim)
		{
			type_ = type;
			style_ = ms;
			anim_ = anim;
		}

		public int Type { get { return type_; } }
		public int MovementStyle { get { return style_; } }
		public bool HasMovement { get { return anim_.HasMovement; } }
		public IAnimation Real { get { return anim_; } }

		public void GetAllForcesDebug(List<string> list)
		{
			var fs = anim_.GetAllForcesDebug();

			if (fs != null)
			{
				for (int i = 0; i < fs.Count; ++i)
					list.Add($"{anim_.Name}: {fs[i]}");
			}
		}

		public override string ToString()
		{
			string s = Animations.ToString(type_) + " ";

			s += "ms=" + MovementStyles.ToString(style_) + " " + anim_.ToString();

			return s;
		}
	}


	class Animator
	{
		class PlayingAnimation
		{
			public Animation anim;
			public IPlayer player;

			public PlayingAnimation(Animation a, IPlayer p)
			{
				anim = a;
				player = p;
			}
		}

		public const int Loop = 0x01;
		public const int Reverse = 0x02;
		public const int Rewind = 0x04;
		public const int Exclusive = 0x08;

		private Person person_;
		private Logger log_;
		private readonly List<IPlayer> players_ = new List<IPlayer>();
		private readonly List<PlayingAnimation> playing_ = new List<PlayingAnimation>();
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

		public bool CanPlayType(int type)
		{
			// todo
			return true;
		}

		public bool IsPlaying(Animation a)
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				if (playing_[i].anim == a)
					return true;
			}

			return false;
		}

		public bool IsPlayingType(int type)
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				if (playing_[i].anim.Type == type)
					return true;
			}

			return false;
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

		public bool CanPlay(Animation a, int flags = 0, bool silent = true)
		{
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
					playing_.Add(new PlayingAnimation(a, p));
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
				a.player.Stop(a.anim.Real, Bits.IsSet(activeFlags_, Rewind));
			}

			playing_.Clear();
			activeFlags_ = 0;
		}

		public void StopType(int type)
		{
			log_.Info($"stopping animation {Animations.ToString(type)}");
			int i = 0;
			int removed = 0;

			while (i < playing_.Count)
			{
				var a = playing_[i];

				if (a.anim.Type == type)
				{
					log_.Info($"stopping animation {a}");
					a.player.Stop(a.anim.Real, Bits.IsSet(activeFlags_, Rewind));
					playing_.RemoveAt(i);
					++removed;
				}
				else
				{
					++i;
				}
			}

			if (removed == 0)
			{
				log_.Error(
					$"no animation {Animations.ToString(type)} found to stop, " +
					$"count={playing_.Count}");
			}
			else
			{
				log_.Info($"stopped {removed} animations");
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

				if (!a.player.IsPlaying(a.anim.Real))
				{
					a.player.Stop(a.anim.Real, false);
					playing_.RemoveAt(i);
				}
				else
				{
					++i;
				}
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

		public void DumpAllForces()
		{
			var fs = GetAllForcesDebug();

			if (fs != null && fs.Count > 0)
			{
				Cue.LogError("forces being applied right now:");

				for (int i = 0; i < fs.Count; ++i)
					Cue.LogError($"  - {fs[i]}");
			}
		}

		private List<string> GetAllForcesDebug()
		{
			var list = new List<string>();

			for (int i = 0; i < playing_.Count; ++i)
				playing_[i].anim.GetAllForcesDebug(list);

			return list;
		}
	}
}
