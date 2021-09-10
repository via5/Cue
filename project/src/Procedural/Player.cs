using System.Collections.Generic;

namespace Cue.Proc
{
	interface ITarget
	{
		ITarget Parent { get; set; }
		ISync Sync { get; }
		bool Done { get; }

		ITarget Clone();
		void Reset();
		void Start(Person p);
		void FixedUpdate(float s);
		string ToDetailedString();
	}


	abstract class BasicTarget : ITarget
	{
		private ITarget parent_ = null;
		private ISync sync_;

		protected BasicTarget(ISync sync)
		{
			sync_ = sync;
			sync_.Target = this;
		}

		public ITarget Parent
		{
			get { return parent_; }
			set { parent_ = value; }
		}

		public ISync Sync { get { return sync_; } }
		public abstract bool Done { get; }

		public abstract ITarget Clone();
		public abstract void FixedUpdate(float s);
		public abstract void Start(Person p);
		public abstract string ToDetailedString();

		public virtual void Reset()
		{
			sync_.Reset();
		}
	}


	class Player : IPlayer
	{
		class Playing
		{
			public ProcAnimation proto, anim;

			public Playing(ProcAnimation proto, ProcAnimation anim)
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

		private ProcAnimation Find(IAnimation a)
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				if (playing_[i].proto == a)
					return playing_[i].anim;
			}

			return null;
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

		public bool Play(IAnimation a, int flags)
		{
			var proto = (a as ProcAnimation);
			if (proto == null)
				return false;

			person_.Atom.SetDefaultControls("playing proc anim");

			var p = new Playing(proto, proto.Clone());

			playing_.Add(p);
			p.anim.Start(person_);

			log_.Info($"playing {a}");

			return true;
		}

		public void Stop(IAnimation a, bool rewind)
		{
			for (int i = 0; i < playing_.Count; ++i)
			{
				if (playing_[i].proto == a)
				{
					playing_[i].anim.Reset();
					playing_.RemoveAt(i);
					return;
				}
			}
		}

		public void FixedUpdate(float s)
		{
			int i = 0;

			while (i < playing_.Count)
			{
				playing_[i].anim.FixedUpdate(s);

				if (playing_[i].anim.Done)
					playing_.RemoveAt(i);
				else
					++i;
			}
		}

		public void Update(float s)
		{
		}

		public override string ToString()
		{
			return $"Procedural: {playing_.Count} anims";
		}
	}
}
