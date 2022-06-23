using System;
using System.Collections.Generic;

namespace Cue
{
	public abstract class BuiltinAnimation : IAnimation
	{
		private readonly string name_;
		private bool hasMovement_;
		private Person person_ = null;
		private Logger log_;

		protected BuiltinAnimation(string name)
		{
			name_ = name;
			hasMovement_ = false;
			log_ = new Logger(Logger.Animation, "builtinAnim." + name);
		}

		public virtual void Reset(Person p)
		{
			// no-op
		}

		public Logger Log
		{
			get { return log_; }
		}

		public string Name
		{
			get { return name_; }
		}

		public Person Person
		{
			get { return person_; }
		}

		// todo
		public float InitFrame { get { return -1; } }
		public float FirstFrame { get { return -1; } }
		public float LastFrame { get { return -1; } }

		public bool HasMovement
		{
			get { return hasMovement_; }
			set { hasMovement_ = value; }
		}

		public abstract bool Done { get; }

		public abstract BuiltinAnimation Clone();

		protected virtual void CopyFrom(BuiltinAnimation o)
		{
			// no-op
		}

		public virtual bool Start(Person p, AnimationContext cx)
		{
			person_ = p;
			log_.Set(p, "builtinAnim." + name_);
			return true;
		}

		public virtual void Reset()
		{
			// no-op
		}

		public virtual void RequestStop()
		{
			// no-op
		}

		public virtual void Stopped()
		{
			// no-op
		}

		public virtual void FixedUpdate(float s)
		{
			// no-op
		}

		public virtual void Update(float s)
		{
			// no-op
		}

		public virtual string[] GetAllForcesDebug()
		{
			return null;
		}

		public virtual string[] Debug()
		{
			return null;
		}

		public override string ToString()
		{
			return name_;
		}

		public virtual string ToDetailedString()
		{
			return ToString();
		}
	}


	class BuiltinPlayer : IPlayer
	{
		class Playing
		{
			public BuiltinAnimation proto, anim;
			public bool forceStop = false;

			public Playing(BuiltinAnimation proto, BuiltinAnimation anim)
			{
				this.proto = proto;
				this.anim = anim;
			}
		}

		private Person person_;
		private Logger log_;
		private readonly List<Playing> playing_ = new List<Playing>();

		public BuiltinPlayer(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Animation, person_, "builtinPlayer");
		}

		public Logger Log
		{
			get { return log_; }
		}

		private BuiltinAnimation Find(IAnimation a)
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				if (playing_[i].proto == a)
					return playing_[i].anim;
			}

			return null;
		}

		public string Name
		{
			get { return "proc"; }
		}

		public bool UsesFrames
		{
			get { return false; }
		}

		public IAnimation[] GetPlaying()
		{
			var a = new IAnimation[playing_.Count];

			for (int i = 0; i < playing_.Count; ++i)
				a[i] = playing_[i].anim;

			return a;
		}

		public bool IsPlaying(IAnimation a)
		{
			return !Find(a)?.Done ?? false;
		}

		public void Seek(IAnimation a, float f)
		{
			// todo
		}

		public bool CanPlay(IAnimation a)
		{
			return (a is BuiltinAnimation);
		}

		public bool Play(IAnimation a, int flags, AnimationContext cx)
		{
			var proto = (a as BuiltinAnimation);
			if (proto == null)
				return false;

			var p = new Playing(proto, proto.Clone());

			if (!p.anim.Start(person_, cx))
				return false;

			playing_.Add(p);
			Log.Info($"playing {a}");

			return true;
		}

		public void StopNow(IAnimation a)
		{
			Log.Verbose(
				$"stopping animation now {a}, " +
				$"looking for proto, count={playing_.Count}");

			for (int i = 0; i < playing_.Count; ++i)
			{
				if (playing_[i].proto == a)
				{
					Log.Verbose($"found animation at {i}");
					DoStopNow(i);
					return;
				}
			}
		}

		public void RequestStop(IAnimation a)
		{
			Log.Verbose(
				$"requesting stop for animation {a}, " +
				$"looking for proto, count={playing_.Count}");

			for (int i = 0; i < playing_.Count; ++i)
			{
				if (playing_[i].proto == a)
				{
					Log.Verbose($"found animation at {i}");
					DoRequestStop(i);
					return;
				}
			}
		}

		public void MainSyncStopping(IAnimation a, Proc.ISync s)
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				if (playing_[i].proto == a)
				{
					// todo
					var pa = playing_[i].anim as Proc.BasicProcAnimation;
					if (pa != null)
						pa.MainSyncStopping(s);

					return;
				}
			}
		}

		private void DoRequestStop(int i)
		{
			Log.Verbose(
				$"requesting stop for {i}, proto is {playing_[i].proto}, " +
				$"anim is {playing_[i].anim}");

			playing_[i].anim.RequestStop();
		}

		public void FixedUpdate(float s)
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				try
				{
					playing_[i].anim.FixedUpdate(s);
				}
				catch (Exception e)
				{
					Log.Error(e.ToString());

					Log.Error(
						$"proc: exception during FixedUpdate " +
						$"{playing_[i].anim}, stopping");

					playing_[i].forceStop = true;
				}
			}
		}

		public void Update(float s)
		{
			int i = 0;

			while (i < playing_.Count)
			{
				try
				{
					playing_[i].anim.Update(s);
				}
				catch (Exception e)
				{
					Log.Error(e.ToString());

					Log.Error(
						$"proc: exception during Update " +
						$"{playing_[i].anim}, stopping");

					playing_[i].forceStop = true;
				}

				if (playing_[i].anim.Done || playing_[i].forceStop)
					DoStopNow(i);
				else
					++i;
			}
		}

		private void DoStopNow(int i)
		{
			Log.Verbose(
				$"stopping now {i}, proto is {playing_[i].proto}, " +
				$"anim is {playing_[i].anim}");

			playing_[i].anim.Stopped();
			playing_.RemoveAt(i);
		}

		public void OnPluginState(bool b)
		{
			if (!b)
			{
				for (int i = 0; i < playing_.Count; ++i)
					playing_[i].anim.Reset();
			}
		}

		public override string ToString()
		{
			return $"Procedural: {playing_.Count} anims";
		}
	}
}
