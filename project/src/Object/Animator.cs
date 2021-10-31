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
		public IAnimation Sys { get { return anim_; } }

		public void GetAllForcesDebug(List<string> list)
		{
			var fs = anim_.GetAllForcesDebug();

			if (fs != null)
			{
				for (int i = 0; i < fs.Length; ++i)
					list.Add($"{anim_.Name}: {fs[i]}");
			}
		}

		public string[] Debug()
		{
			return anim_.Debug();
		}

		public override string ToString()
		{
			return $"{anim_.Name} ({Animations.ToString(type_)})";
		}
	}


	class Animator
	{
		public class PlayingAnimation
		{
			public readonly Animation anim;
			public readonly IPlayer player;
			public bool stopping = false;
			public float stopElapsed = 0;

			public PlayingAnimation(Animation a, IPlayer p)
			{
				anim = a;
				player = p;
			}
		}

		public const int Loop = 0x01;

		private const float StopGracePeriod = 5;

		private Person person_;
		private Logger log_;
		private readonly List<IPlayer> players_ = new List<IPlayer>();
		private readonly List<PlayingAnimation> playing_ = new List<PlayingAnimation>();

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

		public Animation[] GetPlaying()
		{
			var list = new List<Animation>();

			for (int i = 0; i < playing_.Count; ++i)
				list.Add(playing_[i].anim);

			return list.ToArray();
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

		public bool PlayType(int type, AnimationContext cx = null)
		{
			if (IsPlayingType(type))
				return false;

			var a = Resources.Animations.GetAny(type, person_.MovementStyle);
			if (a == null)
			{
				log_.Error($"no animation for type {Animations.ToString(type)}");
				return false;
			}

			return Play(a, cx);
		}

		public bool Play(Animation a, AnimationContext cx = null)
		{
			log_.Info("playing " + a.ToString());
			int flags = 0;

			if (!Cue.Instance.Options.AllowMovement && a.HasMovement)
			{
				log_.Info("not playing animation, movement not allowed");
				return false;
			}

			for (int i=0; i<players_.Count;++i)
			{
				var p = players_[i];

				if (p.Play(a.Sys, flags, cx))
				{
					playing_.Add(new PlayingAnimation(a, p));
					return true;
				}
			}

			log_.Error("no player can play " + a.ToString());
			return false;
		}

		public void StopNow()
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				var a = playing_[i];
				a.player.StopNow(a.anim.Sys);
			}

			playing_.Clear();
		}

		public void StopType(int type)
		{
			log_.Info($"stopping animation {Animations.ToString(type)}");

			int stopped = 0;

			for (int i=0; i<playing_.Count;++i)
			{
				var a = playing_[i];

				if (a.anim.Type == type)
				{
					log_.Info($"stopping animation {a}");
					a.stopping = true;
					a.stopElapsed = 0;
					a.player.RequestStop(a.anim.Sys);
					++stopped;
				}
			}

			if (stopped == 0)
			{
				log_.Error(
					$"no animation {Animations.ToString(type)} found to stop, " +
					$"count={playing_.Count}");
			}
			else
			{
				log_.Info($"stopped {stopped} animations");
			}
		}

		public void FixedUpdate(float s)
		{
			for (int i = 0; i < players_.Count; ++i)
				players_[i].FixedUpdate(s);
		}

		public void Update(float s)
		{
			for (int i = 0; i < players_.Count; ++i)
				players_[i].Update(s);

			RemoveFinished(s);
		}

		private void RemoveFinished(float s)
		{
			int i = 0;

			while (i < playing_.Count)
			{
				var a = playing_[i];
				bool remove = false;

				if (!a.player.IsPlaying(a.anim.Sys))
				{
					remove = true;
				}
				else if (a.stopping)
				{
					a.stopElapsed += s;

					if (a.stopElapsed >= StopGracePeriod)
					{
						log_.Error(
							$"force stopping animation {a.anim}, took too " +
							$"long to stop by itself");

						remove = true;
					}
				}

				if (remove)
				{
					a.player.StopNow(a.anim.Sys);
					playing_.RemoveAt(i);
				}
				else
				{
					++i;
				}
			}
		}

		public void OnPluginState(bool b)
		{
			foreach (var p in players_)
				p.OnPluginState(b);
		}

		public override string ToString()
		{
			return $"playing {playing_.Count} anims";
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
