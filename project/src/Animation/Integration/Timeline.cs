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

		public bool HasMovement
		{
			get { return true; }
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
		private Sys.Vam.ActionParameter stop_;
		private Sys.Vam.BoolParameter playing_;
		private TimelineAnimation current_ = null;
		private Sys.Vam.ActionParameter play_ = null;
		private int flags_ = 0;
		private Logger log_;

		public TimelinePlayer(Person p)
		{
			person_ = p;
			stop_ = new Sys.Vam.ActionParameter(p, "VamTimeline.AtomPlugin", "Stop");
			playing_ = new Sys.Vam.BoolParameter(p, "VamTimeline.AtomPlugin", "Is Playing");
			log_ = new Logger(Logger.Animation, p, "timeline");
		}

		public Logger Log
		{
			get { return log_; }
		}

		public bool IsPlaying(IAnimation a)
		{
			return playing_.Value;
		}

		public IAnimation[] GetPlaying()
		{
			if (playing_.Value)
				return new IAnimation[] { current_ };
			else
				return new IAnimation[0];
		}

		public string Name
		{
			get { return "timeline"; }
		}

		public bool UsesFrames
		{
			get { return false; }
		}

		public void Seek(IAnimation a, float f)
		{
			// todo
		}

		public bool CanPlay(IAnimation a)
		{
			return (a is TimelineAnimation);
		}

		public bool Play(IAnimation a, int flags, AnimationContext cx)
		{
			current_ = (a as TimelineAnimation);
			flags_ = flags;

			if (current_ == null)
				return false;

			SetControllers();
			play_ = new Sys.Vam.ActionParameter(
				person_, "VamTimeline.AtomPlugin", "Play " + current_.Name);

			if (!play_.Check(true))
			{
				Log.Error("timeline animation '" + current_.Name + "' not found");
				play_ = null;
				return true;
			}

			play_.Fire();
			return true;
		}

		public void StopNow(IAnimation a)
		{
			stop_.Fire();
			play_ = null;
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
			// no-op
		}

		public void Update(float s)
		{
			if (current_ != null && !playing_.Value)
				current_ = null;
		}

		public void OnPluginState(bool b)
		{
		}

		public override string ToString()
		{
			string s = "Timeline: ";

			if (current_ != null)
			{
				s += current_.ToString();

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

			var atom = person_.VamAtom?.Atom;
			if (atom != null)
			{
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
			}

			foreach (var c in notFound)
				Log.Error("timeline: controller '" + c + "' not found");
		}
	}
}
