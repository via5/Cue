﻿using System.Collections.Generic;

namespace Cue
{
	class Gaze
	{
		private Person person_;

		// controls where the eyes are looking at
		private IEyes eyes_;

		// controls whether the head should move to follow the eyes
		private IGazer gazer_;

		// a list of valid, weighted targets in the scene and objects to avoid
		private GazeTargets targets_;

		// individual decision-making units that assign weights to targets
		private IGazeEvent[] events_ = new IGazeEvent[0];

		// chooses a random target, handles avoidance
		private GazeTargetPicker picker_;

		// index of last emergency event, if any
		private int lastEmergency_ = -1;

		// debug
		private string lastString_ = "";


		public Gaze(Person p)
		{
			person_ = p;
			eyes_ = Integration.CreateEyes(p);
			gazer_ = Integration.CreateGazer(p);
			targets_ = new GazeTargets(p);
			picker_ = new GazeTargetPicker(p);
		}

		public IEyes Eyes { get { return eyes_; } }
		public IGazer Gazer { get { return gazer_; } }
		public GazeTargets Targets { get { return targets_; } }
		public string LastString { get { return lastString_; } }

		public void Init()
		{
			targets_.Init();
			picker_.SetTargets(targets_.All);
			events_ = BasicGazeEvent.All(person_);
		}

		public void Clear()
		{
			targets_.Clear();
		}

		public GazeTargetPicker Picker
		{
			get { return picker_; }
		}

		public void Update(float s)
		{
			var emergency = UpdateEmergencyTargets();
			if (emergency != -1)
			{
				if (lastEmergency_ != emergency)
					picker_.ForceNextTarget();

				lastEmergency_ = emergency;
			}
			else
			{
				if (picker_.Update(s))
				{
					UpdateTargets();
					picker_.NextTarget();
					gazer_.Duration = person_.Personality.GazeDuration;
				}
			}

			if (picker_.HasTarget)
				eyes_.LookAt(picker_.Position);
			// else ?

			eyes_.Update(s);
			gazer_.Update(s);
		}

		private int UpdateEmergencyTargets()
		{
			for (int i = 0; i < events_.Length; ++i)
			{
				var e = events_[i];
				int flags = e.CheckEmergency();

				if (Bits.IsSet(flags, BasicGazeEvent.Exclusive))
				{
					gazer_.Enabled = !Bits.IsSet(flags, BasicGazeEvent.NoGazer);
					return i;
				}
			}

			return -1;
		}

		private void UpdateTargets()
		{
			var ps = person_.Personality;

			Clear();
			lastString_ = "";

			bool gazerEnabled = !person_.Body.Get(BodyParts.Head).Busy;
			int flags = 0;

			for (int i = 0; i < events_.Length; ++i)
			{
				var e = events_[i];
				flags |= e.Check(flags);

				if (Bits.IsSet(flags, BasicGazeEvent.NoGazer))
					gazerEnabled = false;

				if (Bits.IsSet(flags, BasicGazeEvent.Exclusive))
					break;
			}


			if (Bits.IsSet(flags, BasicGazeEvent.Exclusive))
				lastString_ += "exclusive ";

			if (Bits.IsSet(flags, BasicGazeEvent.NoGazer))
				lastString_ += "nogazer ";

			if (Bits.IsSet(flags, BasicGazeEvent.NoRandom))
				lastString_ += "norandom ";

			if (Bits.IsSet(flags, BasicGazeEvent.Busy))
				lastString_ += "busy ";

			if (lastString_ == "")
				lastString_ = "no flags";

			gazer_.Enabled = gazerEnabled;
		}

		public bool ShouldAvoidPlayer()
		{
			var ps = person_.Personality;

			if (!ps.GetBool(PSE.AvoidGazePlayer))
				return false;

			return IsBored();
		}

		public bool IsBored()
		{
			var ps = person_.Personality;

			if (person_.Mood.RawExcitement >= ps.Get(PSE.MaxExcitementForAvoid))
				return false;

			if (person_.Mood.TimeSinceLastOrgasm < ps.Get(PSE.AvoidDelayAfterOrgasm))
				return false;

			return true;
		}

		public bool ShouldAvoidInsidePersonalSpace()
		{
			var ps = person_.Personality;

			if (!ps.GetBool(PSE.AvoidGazeInsidePersonalSpace))
				return false;

			return IsBored();
		}

		public bool ShouldAvoidDuringSex()
		{
			var ps = person_.Personality;

			if (!ps.GetBool(PSE.AvoidGazeDuringSex))
				return false;

			return IsBored();
		}

		public bool ShouldAvoidOthersDuringSex()
		{
			var ps = person_.Personality;

			if (!ps.GetBool(PSE.AvoidGazeDuringSexOthers))
				return false;

			return IsBored();
		}

		public override string ToString()
		{
			return picker_.ToString();
		}
	}
}
