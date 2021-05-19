using System.Collections.Generic;
using UnityEngine;

namespace Cue.Proc
{
	interface ITarget
	{
		bool Done { get; }
		ITarget Clone();
		void Reset();
		void Start(Person p);
		void FixedUpdate(float s);
		string ToDetailedString();
	}


	class Player : IPlayer
	{
		private Person person_;
		private Logger log_;
		private ProcAnimation proto_ = null;
		private ProcAnimation anim_ = null;

		public Player(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Animation, person_, "ProcPlayer");
		}

		public ProcAnimation Current
		{
			get { return anim_; }
		}

		public ProcAnimation Proto
		{
			get { return proto_; }
		}

		public bool Playing
		{
			get { return (anim_ != null); }
		}

		// todo
		public bool Paused
		{
			get { return false; }
			set { }
		}

		public bool UsesFrames
		{
			get { return false; }
		}

		public void Seek(float f)
		{
			// todo
		}

		public bool Play(IAnimation a, int flags)
		{
			anim_ = null;
			proto_ = (a as ProcAnimation);
			if (proto_ == null)
				return false;

			person_.Atom.SetDefaultControls("playing proc anim");

			anim_ = proto_.Clone();
			anim_.Start(person_);

			log_.Info($"playing {a}");

			return true;
		}

		public void Stop(bool rewind)
		{
			anim_ = null;
			proto_ = null;
		}

		public void FixedUpdate(float s)
		{
			if (anim_ != null)
			{
				anim_.FixedUpdate(s);
				if (anim_.Done)
					Stop(false);
			}
		}

		public void Update(float s)
		{
		}

		public override string ToString()
		{
			return "Procedural: " + (anim_ == null ? "(none)" : anim_.ToString());
		}
	}
}
