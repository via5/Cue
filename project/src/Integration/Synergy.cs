using System;

namespace Cue
{
	class SynergyPlayer : IPlayer
	{
		private readonly Person person_;
		private W.VamBoolParameter playing_;
		private W.VamStringParameter step_;
		private SynergyAnimation anim_ = null;

		public SynergyPlayer(Person p)
		{
			person_ = p;
			playing_ = new W.VamBoolParameter(p, "Synergy.Synergy", "Is Playing");
			step_ = new W.VamStringParameter(p, "Synergy.Synergy", "Force Play Step");
		}

		public bool Playing
		{
			get { return playing_.GetValue(); }
		}

		public bool Play(IAnimation a, int flags)
		{
			anim_ = a as SynergyAnimation;
			if (anim_ == null)
				return false;

			step_.SetValue(anim_.Name);
			return true;
		}

		public void Stop(bool rewind)
		{
			step_.SetValue("");
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


	class SynergyAnimation : BasicAnimation
	{
		private string name_;

		public SynergyAnimation(string name)
		{
			name_ = name;
		}

		public string Name
		{
			get { return name_; }
		}

		public override string ToString()
		{
			return name_;
		}
	}
}
