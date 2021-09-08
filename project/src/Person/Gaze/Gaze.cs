using System.Collections.Generic;

namespace Cue
{
	public static class ForceLooks
	{
		public const int None = 0;
		public const int Camera = 1;
		public const int Up = 2;
	}


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
		private bool gazerEnabledBeforeEmergency_ = false;

		// debug
		private string lastString_ = "";
		private int forceLook_ = ForceLooks.None;


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

		public GazeTargetPicker Picker
		{
			get { return picker_; }
		}

		public bool IsEmergency
		{
			get { return lastEmergency_ != -1; }
		}

		public int ForceLook
		{
			get { return forceLook_; }
			set { forceLook_ = value; }
		}

		public void Update(float s)
		{
			if (forceLook_ != ForceLooks.None)
			{
				Clear();

				if (forceLook_ == ForceLooks.Camera)
					targets_.SetWeight(Cue.Instance.FindPerson("Camera"), BodyParts.Eyes, 1, "forced");
				else if (forceLook_ == ForceLooks.Up)
					targets_.SetAboveWeight(1, "forced");

				picker_.ForceNextTarget(false);
			}
			else
			{
				var emergency = UpdateEmergencyTargets();

				if (emergency == -1)
				{
					// no emergency

					if (lastEmergency_ != -1)
					{
						// but one has just terminated
						person_.Log.Info(
							$"gaze emergency finished: {events_[lastEmergency_]}, " +
							$"gazer now {gazerEnabledBeforeEmergency_}");

						// restore gazer state
						gazer_.Enabled = gazerEnabledBeforeEmergency_;
						gazer_.Duration = person_.Personality.GazeDuration;

						// force a next target
						//
						// todo: this is necessary to handle cases where characters
						// need to immediately look away, but a better system based
						// on the type of emergency and personality would be better
						UpdateTargets();
						picker_.ForceNextTarget(false);

						lastEmergency_ = -1;
					}

					if (picker_.Update(s))
					{
						UpdateTargets();
						picker_.NextTarget();
						gazer_.Duration = person_.Personality.GazeDuration;
					}
				}
				else if (lastEmergency_ != emergency)
				{
					// new emergency
					person_.Log.Info(
						$"gaze emergency: {events_[emergency]}, " +
						$"gazer was {gazerEnabledBeforeEmergency_}");

					picker_.ForceNextTarget(true);
					picker_.Update(s);

					lastEmergency_ = emergency;
				}
			}

			if (picker_.HasTarget)
			{
				gazer_.Variance = picker_.CurrentTarget.Variance;
				eyes_.LookAt(picker_.Position);
			}
			// else ?

			eyes_.Update(s);
			gazer_.Update(s);
		}

		public void Clear()
		{
			targets_.Clear();
			lastString_ = "";
		}

		private int UpdateEmergencyTargets()
		{
			for (int i = 0; i < events_.Length; ++i)
			{
				var e = events_[i];
				int flags = e.CheckEmergency();

				if (Bits.IsSet(flags, BasicGazeEvent.Exclusive))
				{
					if (i != lastEmergency_)
					{
						// new emergency
						gazerEnabledBeforeEmergency_ = gazer_.Enabled;
						gazer_.Enabled = !Bits.IsSet(flags, BasicGazeEvent.NoGazer);
						gazer_.Duration = person_.Personality.Get(PSE.EmergencyGazeDuration);
						lastString_ += "emergency ";
					}

					return i;
				}
			}

			return -1;
		}

		private void UpdateTargets()
		{
			Clear();

			var ps = person_.Personality;

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

			if (person_.Mood.Excitement >= ps.Get(PSE.MaxExcitementForAvoid))
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
