using System;
using System.Collections.Generic;

namespace Cue.Proc
{
	interface ITarget
	{
		string Name { get; }
		ITarget Parent { get; set; }
		ISync Sync { get; }
		bool Done { get; }

		float MovementEnergy { get; }
		ulong LockKey { get; }

		ITarget Clone();
		void Reset();
		void Start(Person p, AnimationContext cx);
		void RequestStop();
		void FixedUpdate(float s);

		void GetAllForcesDebug(List<string> list);
		string ToDetailedString();
		ITarget FindTarget(string name);
	}


	abstract class BasicTarget : ITarget
	{
		protected Person person_ = null;
		private ITarget parent_ = null;
		private ISync sync_;
		private string name_ = "";
		private bool stopping_ = false;

		protected BasicTarget(string name, ISync sync)
		{
			sync_ = sync;
			sync_.Target = this;
			name_ = name;
		}

		public string Name
		{
			get { return name_; }
		}

		public ITarget Parent
		{
			get { return parent_; }
			set { parent_ = value; }
		}

		public ISync Sync
		{
			get { return sync_; }
			set { sync_ = value; }
		}

		public virtual float MovementEnergy
		{
			get { return parent_.MovementEnergy; }
		}

		public virtual ulong LockKey
		{
			get { return parent_.LockKey; }
		}

		public abstract bool Done { get; }

		public bool Stopping
		{
			get { return stopping_; }
		}

		public abstract ITarget Clone();
		public abstract void FixedUpdate(float s);
		public abstract string ToDetailedString();

		public void Start(Person p, AnimationContext cx)
		{
			person_ = p;
			sync_.Reset();
			DoStart(p, cx);
		}

		protected abstract void DoStart(Person p, AnimationContext cx);

		public virtual void RequestStop()
		{
			stopping_ = true;
			sync_.RequestStop();
		}

		public virtual void GetAllForcesDebug(List<string> list)
		{
			// no-op
		}

		public virtual void Reset()
		{
			sync_.Energy = MovementEnergy;
			sync_.Reset();
		}

		public virtual ITarget FindTarget(string name)
		{
			if (name_ == name)
				return this;
			else
				return null;
		}
	}


	class Player : IPlayer
	{
		class Playing
		{
			public BasicProcAnimation proto, anim;
			public bool forceStop = false;

			public Playing(BasicProcAnimation proto, BasicProcAnimation anim)
			{
				this.proto = proto;
				this.anim = anim;
			}
		}

		private Person person_;
		private Logger log_;
		private readonly List<Playing> playing_ = new List<Playing>();

		public Player(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Animation, person_, "ProcPlayer");
		}

		private BasicProcAnimation Find(IAnimation a)
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
			return (a is BasicProcAnimation);
		}

		public bool Play(IAnimation a, int flags, AnimationContext cx)
		{
			var proto = (a as BasicProcAnimation);
			if (proto == null)
				return false;

			var p = new Playing(proto, proto.Clone());

			playing_.Add(p);
			if (!p.anim.Start(person_, cx))
				return false;

			log_.Info($"playing {a}");

			return true;
		}

		public void StopNow(IAnimation a)
		{
			log_.Verbose(
				$"stopping animation now {a}, " +
				$"looking for proto, count={playing_.Count}");

			for (int i = 0; i < playing_.Count; ++i)
			{
				if (playing_[i].proto == a)
				{
					log_.Verbose($"found animation at {i}");
					DoStopNow(i);
					return;
				}
			}
		}

		public void RequestStop(IAnimation a)
		{
			log_.Verbose(
				$"requesting stop for animation {a}, " +
				$"looking for proto, count={playing_.Count}");

			for (int i = 0; i < playing_.Count; ++i)
			{
				if (playing_[i].proto == a)
				{
					log_.Verbose($"found animation at {i}");
					DoRequestStop(i);
					return;
				}
			}
		}

		private void DoStopNow(int i)
		{
			log_.Verbose(
				$"stopping now {i}, proto is {playing_[i].proto}, " +
				$"anim is {playing_[i].anim}");

			playing_[i].anim.Reset();
			playing_.RemoveAt(i);
		}

		private void DoRequestStop(int i)
		{
			log_.Verbose(
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
					Cue.LogError(e.ToString());

					Cue.LogError(
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
					Cue.LogError(e.ToString());

					Cue.LogError(
						$"proc: exception during Update " +
						$"{playing_[i].anim}, stopping");

					playing_[i].forceStop = true;
				}

				if (playing_[i].anim.Done || playing_[i].forceStop)
					playing_.RemoveAt(i);
				else
					++i;
			}
		}

		public void OnPluginState(bool b)
		{
			for (int i = 0; i < playing_.Count; ++i)
				playing_[i].anim.Reset();
		}

		public override string ToString()
		{
			return $"Procedural: {playing_.Count} anims";
		}
	}
}
