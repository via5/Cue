using System;

namespace Cue
{
	class SynergyPlayer : IPlayer
	{
		private readonly Person person_;
		private SynergyAnimation anim_ = null;
		private JSONStorableBool playing_ = null;
		private JSONStorableString step_ = null;

		public SynergyPlayer(Person p)
		{
			person_ = p;
		}

		public bool Playing
		{
			get
			{
				GetParameters();
				if (playing_ == null)
					return false;

				try
				{
					return playing_.val;
				}
				catch (Exception e)
				{
					Cue.LogError("SynergyPlayer: can't get playing status, " + e.Message);
					playing_ = null;
					return false;
				}
			}
		}

		public bool Play(IAnimation a, int flags)
		{
			anim_ = a as SynergyAnimation;
			if (anim_ == null)
				return false;

			GetParameters();
			if (step_ == null)
				return true;

			step_.val = anim_.Name;

			return true;
		}

		public void Stop(bool rewind)
		{
			GetParameters();
			if (step_ == null)
				return;

			step_.val = "";
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

		private void GetParameters()
		{
			if (playing_ == null)
			{
				playing_ = Cue.Instance.VamSys.GetBoolParameter(
					person_, "Synergy.Synergy", "Is Playing");
			}

			if (step_ == null)
			{
				step_ = Cue.Instance.VamSys.GetStringParameter(
					person_, "Synergy.Synergy", "Force Play Step");
			}
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
