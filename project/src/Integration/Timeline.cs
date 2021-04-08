using System;

namespace Cue
{
	class TimelineAnimation : IAnimation
	{
		private string name_;

		public TimelineAnimation(string name)
		{
			name_ = name;
		}

		public string Name
		{
			get { return name_; }
		}
	}

	class TimelinePlayer : IPlayer
	{
		private readonly Person person_;
		private JSONStorableAction play_ = null;
		private JSONStorableBool playing_ = null;

		public TimelinePlayer(Person p)
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
					Cue.LogError("TimelinePlayer: can't get playing status, " + e.Message);
					playing_ = null;
					return false;
				}
			}
		}

		public bool Play(IAnimation a, bool reverse)
		{
			var ta = (a as TimelineAnimation);
			if (ta == null)
				return false;

			var vsys = ((W.VamSys)Cue.Instance.Sys);

			play_ = vsys.GetActionParameter(
				person_, "VamTimeline.AtomPlugin", "Play " + ta.Name);

			if (play_ == null)
			{
				Cue.LogError("timeline animation '" + ta.Name + "' not found");
				return true;
			}

			play_.actionCallback?.Invoke();

			return true;
		}

		public void FixedUpdate(float s)
		{
			// no-op
		}

		private void GetParameters()
		{
			if (playing_ != null)
				return;

			var vsys = ((W.VamSys)Cue.Instance.Sys);

			playing_ = vsys.GetBoolParameter(
				person_, "VamTimeline.AtomPlugin", "Is Playing");
		}
	}
}
