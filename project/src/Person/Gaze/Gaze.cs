using System;
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


	public class QuickGlance
	{
		private bool enabled_ = true;
		private Duration duration_ = new Duration(0, 3);
		private Duration interval_ = new Duration(30, 60);
		private bool ready_ = false;
		private bool active_ = false;


		public void Reload(Person p)
		{
			var ps = p.Personality;

			enabled_ = ps.GetBool(PS.QuickGlance);
			duration_ = ps.GetDuration(PS.QuickGlanceDuration);
			interval_ = ps.GetDuration(PS.QuickGlanceInterval);

			if (!enabled_)
			{
				ready_ = false;
				active_ = false;
			}
		}

		public bool Ready
		{
			get { return ready_; }
		}

		public bool Active
		{
			get { return active_; }
		}

		public void Activate()
		{
			ready_ = false;
			active_ = true;
			duration_.Reset(1);
		}

		public void Deactivate()
		{
			ready_ = false;
			active_ = false;
			interval_.Reset(1);
		}

		public void Reset()
		{
			ready_ = false;
			active_ = false;
			interval_.Reset(1);
		}

		public void MakeReady()
		{
			ready_ = true;
		}

		public void Update(float s)
		{
			if (!enabled_)
				return;

			if (active_)
			{
				duration_.Update(s, 1);

				if (duration_.Finished)
					Deactivate();
			}
			else if (!ready_)
			{
				interval_.Update(s, 1);

				if (interval_.Finished)
					MakeReady();
			}
		}

		public string DebugString()
		{
			if (!enabled_)
				return "disabled";
			else if (active_)
				return $"active {duration_.ToLiveString()}";
			else if (ready_)
				return $"ready";
			else
				return $"next in {interval_.ToLiveString()}";
		}
	}


	public class Gaze
	{
		private Person person_;
		private Logger log_;

		private bool wasEnabled_ = false;

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

		// whether the gazer is enabled; overridden if the head becomes busy
		private bool gazerEnabled_ = false;
		private Duration gazeDuration_ = new Duration();

		// disables the gazer for a short period when picking a new target;
		// makes the character glance at something before turning the head,
		// and sometimes won't turn the head at all if the timing is right
		private QuickGlance quickGlance_ = new QuickGlance();

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
			log_ = new Logger(Logger.Object, p, "gaze");

			eyes_ = Integration.CreateEyes(p);
			gazer_ = Integration.CreateGazer(p);
			targets_ = new GazeTargets(p);
			picker_ = new GazeTargetPicker(p);
			render_ = new GazeRender(p);

			person_.PersonalityChanged += OnPersonalityChanged;
		}

		public Logger Log
		{
			get { return log_; }
		}

		public GazeRender Render
		{
			get { return render_; }
		}

		public bool Enabled
		{
			get { return person_.Personality.GetBool(PS.GazeEnabled); }
		}

		public IEyes Eyes { get { return eyes_; } }
		public IGazer Gazer { get { return gazer_; } }
		public GazeTargets Targets { get { return targets_; } }
		public string LastString { get { return lastString_.ToString(); } }
		public IGazeLookat CurrentTarget { get { return picker_.CurrentTarget; } }

		public void Init()
		{
			targets_.Init();
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

		public IGazeEvent[] Events
		{
			get { return events_; }
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

		public bool AutoBlink
		{
			get { return person_.Atom.AutoBlink; }
			set { person_.Atom.AutoBlink = value; }
		}

		public QuickGlance QuickGlance
		{
			get { return quickGlance_; }
		}

		public void SetTemporaryTarget(IGazeLookat target, float time)
		{
			picker_.SetTemporaryTarget(target, time);
		}

		public void SetTemporaryAvoid(Person p, float time)
		{
			Targets.SetTemporaryAvoid(p, time);
			Picker.AvoidNow();
		}

		public void Update(float s)
		{
			if (!Enabled)
			{
				if (wasEnabled_)
				{
					wasEnabled_ = false;
					gazer_.Enabled = false;
				}

				return;
			}

			wasEnabled_ = true;

			gazeDuration_.Update(s, person_.Mood.GazeEnergy);
			quickGlance_.Update(s);
			Targets.Update(s);

			if (forceLook_ != ForceLooks.None)
			{
				UpdateForced(s);
			}
			else
			{
				int emergency;

				Instrumentation.Start(I.GazeEmergency);
				{
					emergency = CheckEmergency(s);
				}
				Instrumentation.End();

				if (emergency >= 0)
				{
					if (lastEmergency_ != emergency)
					{
						// new emergency
						Log.Verbose(
							$"gaze emergency: {events_[emergency]}, " +
							$"gazer was {gazerEnabledBeforeEmergency_}");

						lastEmergency_ = emergency;
						CheckEvents();
						picker_.EmergencyStarted();
					}

					UpdatePicker(s);
				}
				else
				{
					if (lastEmergency_ != -1)
					{
						// an emergency has just terminated
						Log.Verbose(
							$"gaze emergency finished: {events_[lastEmergency_]}, " +
							$"gazer now {gazerEnabledBeforeEmergency_}");

						// restore gazer state
						gazerEnabled_ = gazerEnabledBeforeEmergency_;
						gazer_.Duration = gazeDuration_.Current;

						picker_.EmergencyEnded();
						quickGlance_.Reset();

						lastEmergency_ = -1;
					}

					if (UpdatePicker(s))
					{
						CheckEvents();
						picker_.NextTarget();

						if (quickGlance_.Ready)
							quickGlance_.Activate();

						gazer_.Duration = gazeDuration_.Current;
					}
				}
			}

			if (forceLook_ != ForceLooks.Freeze)
				UpdatePostTarget(s);

			render_?.Update(s);
		}

		private void UpdateForced(float s)
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

			picker_.EmergencyEnded();
		}

		private void UpdatePostTarget(float s)
		{
			Instrumentation.Start(I.GazePostTarget);
			{
				if (person_.IsPlayer)
				{
					gazer_.Enabled = false;
				}
				else if (CurrentTarget != null)
				{
					if (MustDisableGaze())
						gazer_.Enabled = false;
					else
						gazer_.Enabled = gazerEnabled_;

					gazer_.Variance = CurrentTarget.Variance;
				}

				eyes_.LookAt(CurrentTarget?.Position ?? Vector3.Zero);
				eyes_.Update(s);
				gazer_.Update(s);
			}
			Instrumentation.End();
		}

		private bool MustDisableGaze()
		{
			var head = person_.Body.Get(BP.Head);

			// the head gets locked when grabbed, but disabling gaze
			// will make the head snap when grab stops; it's fine for
			// other animations, but it's jarring for grabbing
			if (head.LockedFor(BodyPartLock.Move) && !head.GrabbedByPlayer)
				return true;

			if (!head.CanApplyForce())
				return true;

			if (quickGlance_.Active)
				return true;

			return false;
		}

		private bool UpdatePicker(float s)
		{
			bool b;

			Instrumentation.Start(I.GazePicker);
			{
				b = picker_.Update(s);
			}
			Instrumentation.End();

			return b;
		}

		private void OnPersonalityChanged()
		{
			gazeDuration_ = person_.Personality.GetDuration(PS.GazeDuration).Clone();
			quickGlance_.Reload(person_);
		}

		public void Clear()
		{
			targets_.Clear();
			lastString_.Length = 0;
		}

		public string DebugString()
		{
			return
				$"e={Enabled},ge={gazerEnabled_},ebe={gazerEnabledBeforeEmergency_}";
		}

		private int CheckEmergency(float s)
		{
			for (int i = 0; i < events_.Length; ++i)
			{
				if (events_[i].HasEmergency(s))
					return i;
			}

			return -1;
		}

		private void CheckEvents()
		{
			Instrumentation.Start(I.GazeEvents);
			{
				DoCheckEvents();
			}
			Instrumentation.End();
		}

		private void DoCheckEvents()
		{
			Clear();

			var ps = person_.Personality;

			gazerEnabled_ = true;
			int flags = 0;

			for (int i = 0; i < events_.Length; ++i)
				events_[i].ResetBeforeCheck();

			if (lastEmergency_ >= 0)
			{
				flags |= events_[lastEmergency_].Check(flags);
			}
			else
			{
				for (int i = 0; i < events_.Length; ++i)
				{
					var e = events_[i];
					flags |= e.Check(flags);

					if (Bits.IsSet(flags, BasicGazeEvent.Stop))
						break;
				}
			}

			if (Bits.IsSet(flags, BasicGazeEvent.NoGazer))
				gazerEnabled_ = false;

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
			return ShouldAvoidImpl(p,
				PS.AvoidGazePlayerMinExcitement,
				PS.AvoidGazePlayerMaxExcitement,
				PS.AvoidGazeOthersMinExcitement,
				PS.AvoidGazeOthersMaxExcitement);
		}

		public float AvoidWeight(Person p)
		{
			var ps = person_.Personality;

			if (p != null && p.IsPlayer)
				return ps.Get(PS.AvoidGazePlayerWeight);
			else
				return ps.Get(PS.AvoidGazeOthersWeight);
		}

		public bool ShouldAvoidInsidePersonalSpace(Person p)
		{
			return ShouldAvoidImpl(p,
				PS.AvoidGazePlayerInsidePersonalSpaceMinExcitement,
				PS.AvoidGazePlayerInsidePersonalSpaceMaxExcitement,
				PS.AvoidGazeOthersInsidePersonalSpaceMinExcitement,
				PS.AvoidGazeOthersInsidePersonalSpaceMaxExcitement);
		}

		public bool ShouldAvoidDuringSex(Person p)
		{
			return ShouldAvoidImpl(p,
				PS.AvoidGazePlayerDuringSexMinExcitement,
				PS.AvoidGazePlayerDuringSexMaxExcitement,
				PS.AvoidGazeOthersDuringSexMinExcitement,
				PS.AvoidGazeOthersDuringSexMaxExcitement);
		}

		public bool ShouldAvoidUninvolvedHavingSex(Person p)
		{
			return ShouldAvoidImpl(p,
				PS.AvoidGazePlayerDuringSexMinExcitement,
				PS.AvoidGazePlayerDuringSexMaxExcitement,
				PS.AvoidGazeUninvolvedHavingSexMinExcitement,
				PS.AvoidGazeUninvolvedHavingSexMaxExcitement);
		}

		public void OnPluginState(bool b)
		{
			eyes_?.OnPluginState(b);
		}

		private bool ShouldAvoidImpl(
			Person p,
			BasicEnumValues.FloatIndex avoidPlayerMinExcitement,
			BasicEnumValues.FloatIndex avoidPlayerMaxExcitement,
			BasicEnumValues.FloatIndex avoidOthersMinExcitement,
			BasicEnumValues.FloatIndex avoidOthersMaxExcitement)
		{
			var ps = person_.Personality;
			float ex = person_.Mood.Get(MoodType.Excited);

			if (p != null && p.IsPlayer)
			{
				if (ex < ps.Get(avoidPlayerMinExcitement) || ex >= ps.Get(avoidPlayerMaxExcitement))
					return false;

				if (person_.Mood.TimeSinceLastOrgasm < ps.Get(PS.AvoidGazePlayerDelayAfterOrgasm))
					return false;

				return true;
			}
			else
			{
				if (ex < ps.Get(avoidOthersMinExcitement) || ex >= ps.Get(avoidOthersMaxExcitement))
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
