using System;

namespace Cue
{
	interface IFinishLookAt
	{
		void Set();
	}

	abstract class FinishBasicLookAt : IFinishLookAt
	{
		protected Person person_;

		protected FinishBasicLookAt(Person p)
		{
			person_ = p;
		}

		public abstract void Set();
	}

	class FinishLookAtNothing : FinishBasicLookAt
	{
		public FinishLookAtNothing(Person p)
			: base(p)
		{
		}

		public override void Set()
		{
			// no-op
		}

		public override string ToString()
		{
			return "look at nothing";
		}
	}

	class FinishLookAtPlayer : FinishBasicLookAt
	{
		public FinishLookAtPlayer(Person p)
			: base(p)
		{
		}

		public override void Set()
		{
			var player = Cue.Instance.Player;
			if (player != null)
			{
				person_.Gaze.SetTemporaryTarget(
					person_.Gaze.Targets.GetEyes(player.PersonIndex),
					Cue.Instance.Finish.GetTotalTime(person_));
			}
		}

		public override string ToString()
		{
			return "look at player";
		}
	}

	class FinishLookAtAvoidPlayer : FinishBasicLookAt
	{
		public FinishLookAtAvoidPlayer(Person p)
			: base(p)
		{
		}

		public override void Set()
		{
			var player = Cue.Instance.Player;
			if (player != null)
			{
				person_.Gaze.SetTemporaryAvoid(
					player, Cue.Instance.Finish.GetTotalTime(person_));
			}
		}

		public override string ToString()
		{
			return "avoid player";
		}
	}


	interface IFinishOrgasm
	{
		void Set(float ep);
	}

	abstract class FinishBasicOrgasm : IFinishOrgasm
	{
		protected Person person_;

		protected FinishBasicOrgasm(Person p)
		{
			person_ = p;
		}

		public abstract void Set(float ep);
	}

	class FinishOrgasmNothing : FinishBasicOrgasm
	{
		public FinishOrgasmNothing(Person p)
			: base(p)
		{
		}

		public override void Set(float ep)
		{
			// no-op
		}

		public override string ToString()
		{
			return "no orgasm";
		}
	}

	class FinishOrgasmSet : FinishBasicOrgasm
	{
		private float initialExcitement_ = 0;

		public FinishOrgasmSet(Person p)
			: base(p)
		{
			initialExcitement_ = p.Mood.Get(MoodType.Excited);
		}

		public override void Set(float ep)
		{
			float range = 1 - initialExcitement_;
			float e = initialExcitement_ + range * ep;

			person_.Mood.GetValue(MoodType.Excited).Value = e;

			if (ep >= 1)
				person_.Mood.ForceOrgasm();
		}

		public override string ToString()
		{
			return $"orgasm, initialExcitement={initialExcitement_:0.00}";
		}
	}


	interface IFinishMood
	{
		void Set();
	}

	abstract class FinishBasicMood : IFinishMood
	{
		protected Person person_;

		protected FinishBasicMood(Person p)
		{
			person_ = p;
		}

		public abstract void Set();
	}

	class FinishMoodNothing : FinishBasicMood
	{
		public FinishMoodNothing(Person p)
			: base(p)
		{
		}

		public override void Set()
		{
			// no-op
		}

		public override string ToString()
		{
			return $"no mood";
		}
	}

	class FinishMoodSet : FinishBasicMood
	{
		public FinishMoodSet(Person p)
			: base(p)
		{
		}

		public override void Set()
		{
			var ps = person_.Personality;

			float happy   = ps.Get(PS.FinishMoodHappy);
			float playful = ps.Get(PS.FinishMoodPlayful);
			float angry   = ps.Get(PS.FinishMoodAngry);
			float tired   = ps.Get(PS.FinishMoodTired);

			SetMood(MoodType.Happy, happy);
			SetMood(MoodType.Playful, playful);
			SetMood(MoodType.Angry, angry);
			SetMood(MoodType.Tired, tired);
		}

		private void SetMood(MoodType t, float f)
		{
			if (f < 0)
				return;

			f = U.Clamp(f, 0, 1);

			person_.Mood.SetTemporary(
				t, f, Cue.Instance.Finish.GetTotalTime(person_));
		}

		public override string ToString()
		{
			var ps = person_.Personality;

			float happy = ps.Get(PS.FinishMoodHappy);
			float playful = ps.Get(PS.FinishMoodPlayful);
			float angry = ps.Get(PS.FinishMoodAngry);
			float tired = ps.Get(PS.FinishMoodTired);

			return
				$"mood set, " +
				$"happy={FS(happy)} playful={FS(playful)} " +
				$"angry={FS(angry)} tired={FS(tired)}";
		}

		private string FS(float f)
		{
			if (f < 0)
				return "no";
			else
				return $"{f:0.00}";
		}
	}


	interface IFinishEvents
	{
		void Set();
	}

	abstract class FinishBasicEvents : IFinishEvents
	{
		protected Person person_;

		protected FinishBasicEvents(Person p)
		{
			person_ = p;
		}

		public abstract void Set();
	}

	class FinishEventsNothing : FinishBasicEvents
	{
		public FinishEventsNothing(Person p)
			: base(p)
		{
		}

		public override void Set()
		{
			// no-op
		}

		public override string ToString()
		{
			return $"no events";
		}
	}

	class FinishEventsStop : FinishBasicEvents
	{
		public FinishEventsStop(Person p)
			: base(p)
		{
		}

		public override void Set()
		{
			person_.AI.StopAllEvents();
		}

		public override string ToString()
		{
			return $"events stop";
		}
	}


	class PersonFinish
	{
		private Person person_;
		private IFinishLookAt lookAt_ = null;
		private IFinishOrgasm orgasm_ = null;
		private IFinishMood mood_ = null;
		private IFinishEvents events_ = null;

		public PersonFinish(Person p)
		{
			person_ = p;
		}

		public Person Person
		{
			get { return person_; }
		}

		public void Start(
			IFinishLookAt lookAt, IFinishOrgasm orgasm,
			IFinishMood mood, IFinishEvents events)
		{
			lookAt_ = lookAt;
			orgasm_ = orgasm;
			mood_ = mood;
			events_ = events;
		}

		public void Debug(DebugLines lines)
		{
			if (person_.IsPlayer)
				lines.Add($"{person_} (player)");
			else
				lines.Add($"{person_}");

			lines.Add($"  - {lookAt_}");
			lines.Add($"  - {orgasm_}");
			lines.Add($"  - {mood_}");
			lines.Add($"  - {events_}");
		}

		public void SetLookAt()
		{
			lookAt_.Set();
		}

		public void SetMood()
		{
			mood_.Set();
		}

		public void SetExcitement(float ep)
		{
			orgasm_.Set(ep);
		}

		public void SetEvents()
		{
			events_.Set();
		}
	}


	class Finish
	{
		public const int LookAtNothing = 0;
		public const int LookAtPlayerInvolved = 1;
		public const int LookAtPlayerAll = 2;
		public const int LookAtPersonality = 3;

		public const int OrgasmsNothing = 0;
		public const int OrgasmsInvolved = 1;
		public const int OrgasmsAll = 2;
		public const int OrgasmsPersonality = 3;

		public const int StopEventsNothing = 0;
		public const int StopEventsInvolved = 1;
		public const int StopEventsAll = 2;

		private const int NoState = 0;
		private const int DelayState = 1;
		private const int ExcitementUpState = 2;

		private Logger log_;
		private float initialDelay_ = 0;
		private int lookAt_ = LookAtPersonality;
		private int orgasms_ = OrgasmsPersonality;
		private float orgasmsTime_ = 1;
		private int events_ = StopEventsAll;

		private int state_ = NoState;
		private float elapsed_ = 0;
		private PersonFinish[] infos_;


		public Finish()
		{
			log_ = new Logger(Logger.Object, "finish");
		}

		public void Init()
		{
			infos_ = new PersonFinish[Cue.Instance.ActivePersons.Length];
			for (int i = 0; i < Cue.Instance.ActivePersons.Length; ++i)
				infos_[i] = new PersonFinish(Cue.Instance.ActivePersons[i]);
		}

		public Logger Log
		{
			get { return log_; }
		}

		public float InitialDelay
		{
			get { return initialDelay_; }
			set { initialDelay_ = value; }
		}

		public int LookAt
		{
			get { return lookAt_; }
			set { lookAt_ = value; }
		}

		public int Orgasms
		{
			get { return orgasms_; }
			set { orgasms_ = value; }
		}

		public float OrgasmsTime
		{
			get { return orgasmsTime_; }
			set { orgasmsTime_ = value; }
		}

		public int Events
		{
			get { return events_; }
			set { events_ = value; }
		}

		public float GetTotalTime(Person p)
		{
			return
				orgasmsTime_ +
				p.Personality.Get(PS.OrgasmTime) +
				p.Personality.Get(PS.PostOrgasmTime);
		}

		public void Start()
		{
			if (state_ != NoState)
			{
				Log.Error("already running");
				return;
			}

			Log.Info("starting");

			elapsed_ = 0;
			state_ = DelayState;

			Log.Info("infos:");
			foreach (var i in infos_)
			{
				if (i.Person.IsPlayer)
				{
					i.Start(
						new FinishLookAtNothing(i.Person),
						new FinishOrgasmNothing(i.Person),
						new FinishMoodNothing(i.Person),
						new FinishEventsNothing(i.Person));
				}
				else
				{
					var lookAt = CreateLookAt(i.Person, lookAt_);
					var orgasm = CreateOrgasm(i.Person, orgasms_);
					var mood = CreateMood(i.Person, orgasms_);
					var events = CreateEvents(i.Person, events_);

					i.Start(lookAt, orgasm, mood, events);
				}
			}

			Log.Info($"running delay {InitialDelay:0.00}");
		}

		public void Update(float s)
		{
			switch (state_)
			{
				case NoState:
				{
					break;
				}

				case DelayState:
				{
					elapsed_ += s;
					if (elapsed_ >= InitialDelay)
					{
						Log.Info($"delay finished");

						for (int i = 0; i < infos_.Length; ++i)
						{
							infos_[i].SetLookAt();
							infos_[i].SetMood();
						}

						elapsed_ = 0;
						state_ = NoState;// ExcitementUpState;

						Log.Info($"running excitement up");
					}

					break;
				}

				case ExcitementUpState:
				{
					elapsed_ += s;

					float p;

					if (orgasmsTime_ <= 0)
						p = 1;
					else
						p = U.Clamp(elapsed_ / orgasmsTime_, 0, 1);

					for (int i = 0; i < infos_.Length; ++i)
						infos_[i].SetExcitement(p);

					if (p >= 1)
					{
						Log.Info($"orgasms time finished");

						for (int i = 0; i < infos_.Length; ++i)
							infos_[i].SetEvents();

						state_ = NoState;
					}

					break;
				}
			}
		}

		public void Debug(DebugLines d)
		{
			d.Add(
				$"delay={initialDelay_:0.00} lookat={lookAt_} " +
				$"orgasms={orgasms_} orgasmsTime={orgasmsTime_} " +
				$"events={events_} state={state_} elapsed={elapsed_}");

			for (int i = 0; i < infos_.Length; ++i)
			{
				if (infos_[i].Person.Body.Exists)
					infos_[i].Debug(d);
			}
		}

		private IFinishLookAt CreateLookAt(Person p, int lookAt)
		{
			switch (lookAt)
			{
				case LookAtNothing:
				{
					return new FinishLookAtNothing(p);
				}

				case LookAtPlayerInvolved:
				{
					var player = Cue.Instance.Player;

					if (player != null)
					{
						if (p.Status.IsInvolvedWith(player))
							return new FinishLookAtPlayer(p);
					}

					return new FinishLookAtNothing(p);
				}

				case LookAtPlayerAll:
				{
					return new FinishLookAtPlayer(p);
				}

				case LookAtPersonality:
				{
					var player = Cue.Instance.Player;
					var opt = p.Personality.GetString(PS.FinishLookAtPlayer);

					bool doAction = false;

					if (opt == "always")
					{
						doAction = true;
					}
					else if (opt == "involved")
					{
						if (player != null)
						{
							if (p.Status.IsInvolvedWith(player))
								doAction = true;
						}
					}
					else if (opt == "" || opt == "never")
					{
						doAction = false;
					}
					else
					{
						Log.Error($"unknown lookat '{opt}'");
					}

					if (doAction)
					{
						var action = p.Personality.GetString(PS.FinishLookAtPlayerAction);

						if (action == "look")
						{
							return new FinishLookAtPlayer(p);
						}
						else if (action == "avoid")
						{
							return new FinishLookAtAvoidPlayer(p);
						}
						else if (action == "gaze")
						{
							if (player != null && p.Gaze.ShouldAvoidDuringSex(player))
								return new FinishLookAtAvoidPlayer(p);
							else
								return new FinishLookAtPlayer(p);
						}
						else
						{
							Log.Error($"bad lookat action '{action}'");
							return new FinishLookAtNothing(p);
						}
					}
					else
					{
						return new FinishLookAtNothing(p);
					}
				}

				default:
				{
					Log.Error($"bad lookat {lookAt}");
					return new FinishLookAtNothing(p);
				}
			}
		}

		private IFinishOrgasm CreateOrgasm(Person p, int orgasm)
		{
			switch (orgasm)
			{
				case OrgasmsNothing:
				{
					return new FinishOrgasmNothing(p);
				}

				case OrgasmsInvolved:
				{
					var player = Cue.Instance.Player;

					if (player != null)
					{
						if (p.Status.IsInvolvedWith(player))
							return new FinishOrgasmSet(p);
					}

					return new FinishOrgasmNothing(p);
				}

				case OrgasmsAll:
				{
					return new FinishOrgasmSet(p);
				}

				case OrgasmsPersonality:
				{
					var opt = p.Personality.GetString(PS.FinishOrgasm);
					var minExcitement = p.Personality.Get(PS.FinishOrgasmMinExcitement);

					bool doAction = false;

					if (opt == "always")
					{
						doAction = true;
					}
					else if (opt == "involved")
					{
						var player = Cue.Instance.Player;

						if (player != null)
						{
							if (p.Status.IsInvolvedWith(player))
								doAction = true;
						}
					}
					else if (opt == "" || opt == "never")
					{
						doAction = false;
					}
					else
					{
						Log.Error($"unknown orgasm opt '{opt}'");
					}

					if (doAction)
					{
						if (p.Mood.Get(MoodType.Excited) >= minExcitement)
							return new FinishOrgasmSet(p);
					}

					return new FinishOrgasmNothing(p);
				}

				default:
				{
					Log.Error($"bad orgasm {orgasm}");
					return new FinishOrgasmNothing(p);
				}
			}
		}

		public IFinishMood CreateMood(Person p, int orgasm)
		{
			if (orgasm == OrgasmsPersonality)
			{
				var opt = p.Personality.GetString(PS.FinishMood);

				if (opt == "always")
				{
					return new FinishMoodSet(p);
				}
				else if (opt == "involved")
				{
					var player = Cue.Instance.Player;

					if (player != null)
					{
						if (p.Status.IsInvolvedWith(player))
							return new FinishMoodSet(p);
					}
				}
				else if (opt == "" || opt == "never")
				{
					return new FinishMoodNothing(p);
				}
				else
				{
					Log.Error($"bad mood option '{opt}'");
				}
			}

			return new FinishMoodNothing(p);
		}

		public IFinishEvents CreateEvents(Person p, int events)
		{
			switch (events)
			{
				case StopEventsAll:
				{
					return new FinishEventsStop(p);
				}

				case StopEventsInvolved:
				{
					var player = Cue.Instance.Player;
					if (player != null)
					{
						if (p.Status.IsInvolvedWith(player))
							return new FinishEventsStop(p);
					}

					return new FinishEventsNothing(p);
				}

				default:
				{
					Log.Error($"bad events {events}");
					return new FinishEventsNothing(p);
				}
			}
		}
	}
}
