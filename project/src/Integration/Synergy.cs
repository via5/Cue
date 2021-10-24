using SimpleJSON;
using System;
using System.Collections.Generic;

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

		public bool UsesFrames
		{
			get { return false; }
		}

		public IAnimation[] GetPlaying()
		{
			if (playing_.Value)
				return new IAnimation[] { anim_ };
			else
				return new IAnimation[0];
		}

		public bool IsPlaying(IAnimation a)
		{
			return playing_.Value;
		}

		public void Seek(IAnimation a, float f)
		{
			// todo
		}

		public bool Play(IAnimation a, object ps, int flags)
		{
			anim_ = a as SynergyAnimation;
			if (anim_ == null)
				return false;

			step_.Value = anim_.Name;
			return true;
		}

		public void Stop(IAnimation a, bool rewind)
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

		public string[] GetAllForcesDebug()
		{
			return null;
		}

		public string[] Debug()
		{
			return null;
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
