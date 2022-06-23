using SimpleJSON;

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

		public string Name
		{
			get { return "synergy"; }
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

		public bool CanPlay(IAnimation a)
		{
			return (a is SynergyAnimation);
		}

		public bool Play(IAnimation a, int flags, AnimationContext cx)
		{
			anim_ = a as SynergyAnimation;
			if (anim_ == null)
				return false;

			step_.Value = anim_.Name;
			return true;
		}

		public void StopNow(IAnimation a)
		{
			step_.Value = "";
			anim_ = null;
		}

		public void RequestStop(IAnimation a)
		{
			StopNow(a);
		}

		public void MainSyncStopping(IAnimation a, Proc.ISync s)
		{
			// no-op
		}

		public void FixedUpdate(float s)
		{
		}

		public void Update(float s)
		{
		}

		public void OnPluginState(bool b)
		{
		}

		public override string ToString()
		{
			return $"{Name}: {(anim_ == null ? "(none)" : anim_.ToString())}";
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

		public void Reset(Person p)
		{
			// no-op
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
