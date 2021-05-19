using SimpleJSON;
using System.Collections.Generic;

namespace Cue
{
	class TimelineAnimation : IAnimation
	{
		private string name_;

		public TimelineAnimation(string name)
		{
			name_ = name;
		}

		public static TimelineAnimation Create(JSONClass o)
		{
			return new TimelineAnimation(o["name"]);
		}

		public string Name
		{
			get { return name_; }
		}

		// todo
		public float InitFrame { get { return -1; } }
		public float FirstFrame { get { return -1; } }
		public float LastFrame { get { return -1; } }

		public bool ForcesOnly
		{
			get { return false; }
		}

		public override string ToString()
		{
			return "timeline " + name_;
		}

		public string ToDetailedString()
		{
			return ToString();
		}
	}

	class TimelinePlayer : IPlayer
	{
		private readonly Person person_;
		private W.VamActionParameter stop_;
		private W.VamBoolParameter playing_;
		private TimelineAnimation current_ = null;
		private W.VamActionParameter play_ = null;
		private int flags_ = 0;

		public TimelinePlayer(Person p)
		{
			person_ = p;
			stop_ = new W.VamActionParameter(p, "VamTimeline.AtomPlugin", "Stop");
			playing_ = new W.VamBoolParameter(p, "VamTimeline.AtomPlugin", "Is Playing");
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
			current_ = (a as TimelineAnimation);
			flags_ = flags;

			if (current_ == null)
				return false;

			SetControllers();
			play_ = new W.VamActionParameter(
				person_, "VamTimeline.AtomPlugin", "Play " + current_.Name);

			if (!play_.Check(true))
			{
				Cue.LogError("timeline animation '" + current_.Name + "' not found");
				play_ = null;
				return true;
			}

			play_.Fire();
			return true;
		}

		public void Stop(bool rewind)
		{
			stop_.Fire();
			play_ = null;
		}

		public void FixedUpdate(float s)
		{
			// no-op
		}

		public void Update(float s)
		{
			if (current_ != null && !Playing)
				current_ = null;
		}

		public override string ToString()
		{
			string s = "Timeline: ";

			if (current_ != null)
			{
				s += current_.ToString();

				if ((flags_ & Animator.Reverse) != 0)
					s += " rev";

				if ((flags_ & Animator.Loop) != 0)
					s += " loop";
			}
			else
			{
				s += "(none)";
			}

			return s;
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

			var atom = ((W.VamAtom)person_.Atom).Atom;

			foreach (var c in atom.freeControllers)
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
