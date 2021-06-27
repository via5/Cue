﻿using SimpleJSON;
using System;

namespace Cue
{
	class SynergyPlayer : IPlayer
	{
		private readonly Person person_;
		private Sys.Vam.BoolParameter playing_;
		private Sys.Vam.StringParameter step_;
		private SynergyAnimation anim_ = null;

		public SynergyPlayer(Person p)
		{
			person_ = p;
			playing_ = new Sys.Vam.BoolParameter(p, "Synergy.Synergy", "Is Playing");
			step_ = new Sys.Vam.StringParameter(p, "Synergy.Synergy", "Force Play Step");
		}

		public bool Playing
		{
			get { return playing_.Value; }
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
			anim_ = a as SynergyAnimation;
			if (anim_ == null)
				return false;

			step_.Value = anim_.Name;
			return true;
		}

		public void Stop(bool rewind)
		{
			step_.Value = "";
			anim_ = null;
		}

		public void FixedUpdate(float s)
		{
		}

		public void Update(float s)
		{
		}

		public override string ToString()
		{
			return "Synergy: " + (anim_ == null ? "(none)" : anim_.ToString());
		}
	}


	class SynergyAnimation : IAnimation
	{
		private string name_;

		public SynergyAnimation(string name)
		{
			name_ = name;
		}

		public static SynergyAnimation Create(JSONClass o)
		{
			return new SynergyAnimation(o["step"]);
		}

		public string Name
		{
			get { return name_; }
		}

		// todo
		public float InitFrame { get { return -1; } }
		public float FirstFrame { get { return -1; } }
		public float LastFrame { get { return -1; } }

		public bool HasMovement
		{
			get { return false; }
		}

		public override string ToString()
		{
			return "synergy " + name_;
		}

		public string ToDetailedString()
		{
			return ToString();
		}
	}
}
