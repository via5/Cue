using System;
using System.Collections.Generic;

namespace Cue
{
	class BuiltinAnimations
	{
		private static List<IAnimation> list_ = new List<IAnimation>()
		{
			new SmokeAnimation(),
			new ClockwiseKissAnimation(),
			new ClockwiseBJAnimation(),
			new ClockwiseHJBothAnimation(),
			new ClockwiseHJLeftAnimation(),
			new ClockwiseHJRightAnimation(),
			new Proc.ThrustProcAnimation(),
			new Proc.TribProcAnimation(),
			new Proc.SuckFingerProcAnimation(),
			new Proc.LeftFingerProcAnimation(),
			new Proc.RightFingerProcAnimation(),
		};

		public static IAnimation Get(string name)
		{
			foreach (var a in list_)
			{
				if (a.Name == name)
					return a;
			}

			Logger.Global.Error($"builtin animation '{name}' not found");

			return null;
		}

		public static void ResetAll(Person p)
		{
			foreach (var a in list_)
				a.Reset(p);
		}
	}


	public class AnimationContext
	{
		public object ps;
		public ulong key;

		public AnimationContext(object ps, ulong key = BodyPartLock.NoKey)
		{
			this.ps = ps;
			this.key = key;
		}
	}

	public interface IPlayer
	{
		string Name { get; }
		bool UsesFrames { get; }

		IAnimation[] GetPlaying();
		bool CanPlay(IAnimation a);
		bool Play(IAnimation a, int flags, AnimationContext cx);
		void RequestStop(IAnimation a, int stopFlags = Animation.NoStopFlags);
		void StopNow(IAnimation a, int stopFlags = Animation.NoStopFlags);
		bool Pause(IAnimation a);
		bool Resume(IAnimation a);
		void MainSyncStopping(IAnimation a, Proc.ISync s);
		void Seek(IAnimation a, float where);
		void FixedUpdate(float s);
		void Update(float s);
		bool IsPlaying(IAnimation a);
		void OnPluginState(bool b);
	}

	public interface IAnimation
	{
		string Name { get; }
		float InitFrame { get; }
		float FirstFrame { get; }
		float LastFrame { get; }

		void Reset(Person p);
		string[] GetAllForcesDebug();
		void Debug(DebugLines debug);
		string ToDetailedString();
	}


	public class Animation
	{
		private readonly AnimationType type_ = AnimationType.None;
		private readonly int style_ = MovementStyles.Any;
		private readonly IAnimation anim_ = null;

		public const int NoStopFlags = 0x0;
		public const int StopNoReturn = 0x1;

		public Animation(AnimationType type, int ms, IAnimation anim)
		{
			type_ = type;
			style_ = ms;
			anim_ = anim;
		}

		public AnimationType Type { get { return type_; } }
		public int MovementStyle { get { return style_; } }
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

		public void Debug(DebugLines debug)
		{
			anim_.Debug(debug);
		}

		public override string ToString()
		{
			return $"{anim_.Name} ({AnimationType.ToString(type_)})";
		}
	}


	public class Animator
	{
		public class PlayingAnimation
		{
			public readonly Animation anim;
			public readonly IPlayer player;
			public bool stopping = false;
			public bool paused = false;
			public float stopElapsed = 0;

			public PlayingAnimation(Animation a, IPlayer p)
			{
				anim = a;
				player = p;
			}

			public override string ToString()
			{
				return anim.ToString();
			}
		}

		public const int Loop = 0x01;

		private const float StopGracePeriod = 5;

		private Person person_;
		private Logger log_;
		private readonly List<IPlayer> players_ = new List<IPlayer>();
		private readonly List<PlayingAnimation> playing_ = new List<PlayingAnimation>();
		private List<AnimationType> failed_ = new List<AnimationType>();
		private Proc.ISync sync_ = null;

		public Animator(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Animation, p, "animator");
			players_.AddRange(CreatePlayers(p));

			BuiltinAnimations.ResetAll(person_);
		}

		public Logger Log
		{
			get { return log_; }
		}

		public static List<IPlayer> CreatePlayers(Person p)
		{
			return new List<IPlayer>()
			{
				new BuiltinPlayer(p),
				new BVH.Player(p),
				new TimelinePlayer(p),
				new SynergyPlayer(p)
			};
		}

		public List<IPlayer> Players
		{
			get { return players_; }
		}

		public Proc.ISync MainSync
		{
			get { return sync_; }
		}

		public void SetMainSync(Proc.ISync s)
		{
			if (s == null)
			{
				foreach (Person p in Cue.Instance.ActivePersons)
				{
					for (int i = 0; i < p.Animator.playing_.Count; ++i)
					{
						p.Animator.playing_[i].player.MainSyncStopping(
							p.Animator.playing_[i].anim.Sys, sync_);
					}
				}
			}

			sync_ = s;
		}

		public Animation[] GetPlaying()
		{
			var list = new List<Animation>();

			for (int i = 0; i < playing_.Count; ++i)
				list.Add(playing_[i].anim);

			return list.ToArray();
		}

		public AnimationStatus PlayingStatus(AnimationType type)
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				if (playing_[i].anim.Type == type)
				{
					if (playing_[i].stopping)
						return AnimationStatus.Stopping;
					else if (playing_[i].paused)
						return AnimationStatus.Paused;
					else
						return AnimationStatus.Playing;
				}
			}

			return AnimationStatus.NotPlaying;
		}

		public bool IsPlaying(Animation a)
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				if (playing_[i].anim == a && !playing_[i].stopping)
					return true;
			}

			return false;
		}

		public bool IsPlayingType(AnimationType type)
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				if (playing_[i].anim.Type == type && !playing_[i].stopping)
					return true;
			}

			return false;
		}

		public bool PlayType(AnimationType type, AnimationContext cx = null)
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				if (playing_[i].anim.Type == type && playing_[i].paused)
				{
					if (Resume(playing_[i]))
						return true;
				}
			}

			var a = Resources.Animations.GetAny(type, person_.MovementStyle);
			if (a == null)
			{
				if (!failed_.Contains(type))
				{
					failed_.Add(type);
					Log.Error($"no animation for type {AnimationType.ToString(type)}");
				}

				return false;
			}

			return Play(a, cx);
		}

		public bool Play(Animation a, AnimationContext cx = null)
		{
			Log.Info("playing " + a.ToString());
			int flags = 0;

			for (int i=0; i<players_.Count;++i)
			{
				var p = players_[i];

				if (p.CanPlay(a.Sys))
				{
					try
					{
						if (p.Play(a.Sys, flags, cx))
						{
							playing_.Add(new PlayingAnimation(a, p));
							return true;
						}

						Log.Error($"player '{p.Name}' failed to start {a}");
						return false;
					}
					catch (Exception e)
					{
						Log.Error($"exception while trying to play {a} with player {p}:");
						Log.Error(e.ToString());
						return false;
					}
				}
			}

			Log.Error("no player can play " + a.ToString());
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

		public void StopType(AnimationType type, int stopFlags = Animation.NoStopFlags)
		{
			Log.Verbose($"stopping animation {AnimationType.ToString(type)}");

			int stopped = 0;

			for (int i=0; i<playing_.Count;++i)
			{
				var a = playing_[i];

				if (a.anim.Type == type && !a.stopping)
				{
					Stop(a, stopFlags);
					++stopped;
				}
			}

			if (stopped == 0)
			{
				Log.Verbose(
					$"no animation {AnimationType.ToString(type)} found to stop, " +
					$"count={playing_.Count}");
			}
			else
			{
				Log.Verbose($"stopped {stopped} animations");
			}
		}

		private void Stop(PlayingAnimation a, int stopFlags = Animation.NoStopFlags)
		{
			Log.Info($"stopping animation {a}");
			a.stopping = true;
			a.stopElapsed = 0;
			a.player.RequestStop(a.anim.Sys, stopFlags);
		}

		public void PauseType(AnimationType type)
		{
			Log.Verbose($"pausing animation {AnimationType.ToString(type)}");

			int paused = 0;
			int stopped = 0;

			for (int i = 0; i < playing_.Count; ++i)
			{
				var a = playing_[i];

				if (a.anim.Type == type && !a.stopping)
				{
					Log.Info($"pausing animation {a}");
					a.paused = true;

					if (a.player.Pause(a.anim.Sys))
					{
						++paused;
					}
					else
					{
						// not an error, not all animation support pausing
						Log.Verbose($"failed to pause {a}, stopping");
						Stop(a);
						++stopped;
					}
				}
			}

			if (paused == 0)
			{
				Log.Verbose(
					$"no animation {AnimationType.ToString(type)} found to pause, " +
					$"stopped {stopped} anims, count={playing_.Count}");
			}
			else
			{
				Log.Verbose($"paused {paused} animations, stopped {stopped}");
			}
		}

		private bool Resume(PlayingAnimation a)
		{
			Log.Info("resuming " + a.ToString());

			for (int i = 0; i < players_.Count; ++i)
			{
				var p = players_[i];

				if (p.CanPlay(a.anim.Sys))
				{
					try
					{
						if (p.Resume(a.anim.Sys))
							return true;

						Log.Error($"player '{p.Name}' failed to resume {a}, stopping");
						Stop(a);

						return false;
					}
					catch (Exception e)
					{
						Log.Error($"exception while trying to resume {a} with player {p}:");
						Log.Error(e.ToString());
						Stop(a);

						return false;
					}
				}
			}

			Log.Error("no player can resume " + a.ToString());
			return false;
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
						Log.Error(
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
				Log.Info("forces being applied right now:");

				for (int i = 0; i < fs.Count; ++i)
					Log.Info($"  - {fs[i]}");
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
