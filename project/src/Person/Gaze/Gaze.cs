using System.Collections.Generic;
using System.Text;

namespace Cue
{
	// debugging
	//
	public static class ForceLooks
	{
		public const int None = 0;
		public const int Camera = 1;
		public const int Up = 2;
		public const int Freeze = 3;

		public static string[] Names
		{
			get { return new string[] { "Free", "Camera", "Up", "Freeze" }; }
		}
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

		// whether the gazer is enabled; overriden if the head becomes busy
		private bool gazerEnabled_ = false;

		private Duration gazeDuration_ = new Duration();

		// index of last emergency event, if any
		private int lastEmergency_ = -1;
		private bool gazerEnabledBeforeEmergency_ = false;

		// debug
		private readonly StringBuilder lastString_ = new StringBuilder();
		private int forceLook_ = ForceLooks.None;
		private GazeRender render_;


		public Gaze(Person p)
		{
			person_ = p;
			eyes_ = Integration.CreateEyes(p);
			gazer_ = Integration.CreateGazer(p);
			targets_ = new GazeTargets(p);
			picker_ = new GazeTargetPicker(p);
			render_ = new GazeRender(p);

			person_.PersonalityChanged += OnPersonalityChanged;
		}

		public GazeRender Render
		{
			get { return render_; }
		}

		public IEyes Eyes { get { return eyes_; } }
		public IGazer Gazer { get { return gazer_; } }
		public GazeTargets Targets { get { return targets_; } }
		public string LastString { get { return lastString_.ToString(); } }

		public void Init()
		{
			targets_.Init();
			picker_.SetTargets(targets_.All);
			events_ = BasicGazeEvent.All(person_);
			gazeDuration_ = person_.Personality.GetDuration(PS.GazeDuration).Clone();
		}

		public GazeTargetPicker Picker
		{
			get { return picker_; }
		}

		public IGazeEvent GetEvent<T>() where T : IGazeEvent
		{
			for (int i = 0; i < events_.Length; ++i)
			{
				if (events_[i] is T)
					return events_[i];
			}

			return null;
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
			gazeDuration_.Update(s, person_.Mood.GazeEnergy);

			if (forceLook_ != ForceLooks.None)
			{
				Clear();

				switch (forceLook_)
				{
					case ForceLooks.Camera:
					{
						targets_.SetWeight(
							Cue.Instance.FindPerson("Camera"),
							BP.Eyes, 1, "forced");

						person_.Gaze.Gazer.Enabled = true;
						break;
					}

					case ForceLooks.Up:
					{
						targets_.SetAboveWeight(1, "forced");
						person_.Gaze.Gazer.Enabled = true;
						break;
					}

					case ForceLooks.Freeze:
					{
						person_.Gaze.Gazer.Enabled = false;
						break;
					}
				}

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
						gazerEnabled_ = gazerEnabledBeforeEmergency_;
						gazer_.Duration = gazeDuration_.Current;

						lastEmergency_ = -1;
					}

					if (picker_.Update(s))
					{
						UpdateTargets();
						picker_.NextTarget();
						gazer_.Duration = gazeDuration_.Current;
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

			if (forceLook_ != ForceLooks.Freeze)
			{
				if (picker_.HasTarget)
				{
					if (person_.Body.Get(BP.Head).LockedFor(BodyPartLock.Move))
						gazer_.Enabled = false;
					else
						gazer_.Enabled = gazerEnabled_;

					gazer_.Variance = picker_.CurrentTarget.Variance;
				}

				eyes_.LookAt(picker_.Position);
				eyes_.Update(s);
				gazer_.Update(s);
			}

			render_?.Update(s);
		}

		private void OnPersonalityChanged()
		{
			gazeDuration_ = person_.Personality.GetDuration(PS.GazeDuration).Clone();
		}

		public void Clear()
		{
			targets_.Clear();
			lastString_.Length = 0;
		}

		public string DebugString()
		{
			return
				$"e={gazerEnabled_},ebe={gazerEnabledBeforeEmergency_}";
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
						gazerEnabled_ = !Bits.IsSet(flags, BasicGazeEvent.NoGazer);
						gazer_.Duration = person_.Personality.Get(PS.EmergencyGazeDuration);
						lastString_.Append("emergency ");
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

			gazerEnabled_ = true;
			int flags = 0;

			for (int i = 0; i < events_.Length; ++i)
			{
				var e = events_[i];
				flags |= e.Check(flags);

				if (Bits.IsSet(flags, BasicGazeEvent.NoGazer))
					gazerEnabled_ = false;

				if (Bits.IsSet(flags, BasicGazeEvent.Exclusive))
					break;
			}


			if (Bits.IsSet(flags, BasicGazeEvent.Exclusive))
				lastString_.Append("exclusive ");

			if (Bits.IsSet(flags, BasicGazeEvent.NoGazer))
				lastString_.Append("nogazer ");

			if (Bits.IsSet(flags, BasicGazeEvent.NoRandom))
				lastString_.Append("norandom ");

			if (Bits.IsSet(flags, BasicGazeEvent.Busy))
				lastString_.Append("busy ");

			if (lastString_.Length == 0)
				lastString_.Append("no flags ");
		}

		public bool ShouldAvoid(Person p)
		{
			return ShouldAvoidImpl(p, PS.AvoidGazePlayer, PS.AvoidGazeOthers);
		}

		public float AvoidWeight(Person p)
		{
			if (!ShouldAvoid(p))
			{
				Cue.LogError("AvoidWeight called when ShouldAvoid is false");
				return 0;
			}

			var ps = person_.Personality;

			if (p != null && p.IsPlayer)
				return ps.Get(PS.AvoidGazePlayerWeight);
			else
				return ps.Get(PS.AvoidGazeOthersWeight);
		}

		public bool ShouldAvoidInsidePersonalSpace(Person p)
		{
			return ShouldAvoidImpl(p,
				PS.AvoidGazePlayerInsidePersonalSpace,
				PS.AvoidGazeOthersInsidePersonalSpace);
		}

		public bool ShouldAvoidDuringSex(Person p)
		{
			return ShouldAvoidImpl(p,
				PS.AvoidGazePlayerDuringSex,
				PS.AvoidGazeOthersDuringSex);
		}

		public bool ShouldAvoidUninvolvedHavingSex(Person p)
		{
			return ShouldAvoidImpl(p,
				PS.AvoidGazePlayerDuringSex,
				PS.AvoidGazeUninvolvedHavingSex);
		}

		private bool ShouldAvoidImpl(Person p, int avoidPlayer, int avoidOthers)
		{
			var ps = person_.Personality;
			float ex = person_.Mood.Get(Moods.Excited);

			if (p != null && p.IsPlayer)
			{
				if (ex >= ps.Get(avoidPlayer))
					return false;

				if (person_.Mood.TimeSinceLastOrgasm < ps.Get(PS.AvoidGazePlayerDelayAfterOrgasm))
					return false;

				return true;
			}
			else
			{
				if (ex >= ps.Get(avoidOthers))
					return false;

				if (person_.Mood.TimeSinceLastOrgasm < ps.Get(PS.AvoidGazeOthersDelayAfterOrgasm))
					return false;

				return true;
			}
		}

		public override string ToString()
		{
			return picker_.ToString();
		}
	}
}
