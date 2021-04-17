using System;
using System.Collections.Generic;

namespace Cue
{
	class TimelineAnimation : BasicAnimation
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
		private JSONStorableAction stop_ = null;
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

		public bool Play(IAnimation a, int flags)
		{
			var ta = (a as TimelineAnimation);
			if (ta == null)
				return false;

			SetControllers();

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

		public void Stop()
		{
			GetParameters();
			stop_?.actionCallback?.Invoke();
			play_ = null;
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

			stop_ = vsys.GetActionParameter(
				person_, "VamTimeline.AtomPlugin", "Stop");
		}

		private void SetControllers()
		{
			var cs = new Dictionary<string, bool>() {
				{ "headControl", true },
				{ "hipControl", true },
				{ "chestControl", true },
				{ "lHandControl", true },
				{ "rHandControl", true },
				{ "lFootControl", true },
				{ "rFootControl", true },
				{ "lKneeControl", false },
				{ "rKneeControl", false },
				{ "lElbowControl", false },
				{ "rElbowControl", false },
				{ "lArmControl", false },
				{ "rArmControl", false },
				{ "lShoulderControl", false },
				{ "rShoulderControl", false },
				{ "abdomenControl", false },
				{ "abdomen2Control", false },
				{ "pelvisControl", false },
				{ "lThighControl", false },
				{ "rThighControl", false },
			};

			var notFound = new List<string>();

			foreach (var c in cs)
				notFound.Add(c.Key);

			foreach (var c in ((W.VamAtom)person_.Atom).Atom.freeControllers)
			{
				bool b;
				if (cs.TryGetValue(c.name, out b))
				{
					if (b)
					{
						c.currentRotationState = FreeControllerV3.RotationState.On;
						c.currentPositionState = FreeControllerV3.PositionState.On;
					}
					else
					{
						c.currentRotationState = FreeControllerV3.RotationState.Off;
						c.currentPositionState = FreeControllerV3.PositionState.Off;
					}

					notFound.Remove(c.name);
				}
			}

			foreach (var c in notFound)
				Cue.LogError("timeline: controller '" + c + "' not found");
		}
	}
}
