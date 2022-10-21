using System.Collections.Generic;

namespace Cue
{
	class Finish
	{
		class OrgasmInfo
		{
			public Person person;
			public float initialExcitement;

			public OrgasmInfo(Person p, float e)
			{
				person = p;
				initialExcitement = e;
			}
		}

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
		private int lookAt_ = LookAtPlayerInvolved;
		private int orgasms_ = OrgasmsInvolved;
		private float orgasmsTime_ = 1;
		private int events_ = StopEventsAll;

		private int state_ = NoState;
		private float elapsed_ = 0;
		private List<OrgasmInfo> orgasmInfos_ = new List<OrgasmInfo>();


		public Finish()
		{
			log_ = new Logger(Logger.Object, "finish");
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

			GatherOrgasmInfos();

			Log.Info("orgasm infos:");
			foreach (var o in orgasmInfos_)
				Log.Info($"{o.person} {o.initialExcitement:0.00}");

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

						DoLookAt();
						elapsed_ = 0;
						state_ = ExcitementUpState;

						Log.Info($"running excitement up");
					}

					break;
				}

				case ExcitementUpState:
				{
					elapsed_ += s;

					float p;

					if (orgasmsTime_ <= 0)
					{
						Log.Info($"orgasmsTime_ is 0, setting to 100%");
						p = 1;
					}
					else
					{
						p = U.Clamp(elapsed_ / orgasmsTime_, 0, 1);
					}

					for (int i = 0; i < orgasmInfos_.Count; ++i)
					{
						float range = 1 - orgasmInfos_[i].initialExcitement;
						float e = orgasmInfos_[i].initialExcitement + range * p;

						orgasmInfos_[i].person.Mood.GetValue(MoodType.Excited).Value = e;
					}

					if (p >= 1)
					{
						Log.Info($"orgasms time finished");

						// add reactions per personality
						DoEvents();
						state_ = NoState;
					}

					break;
				}
			}
		}

		private void GatherOrgasmInfos()
		{
			orgasmInfos_.Clear();

			switch (orgasms_)
			{
				case OrgasmsAll:
				{
					foreach (var p in Cue.Instance.ActivePersons)
					{
						if (p.IsPlayer)
							continue;

						orgasmInfos_.Add(new OrgasmInfo(p, p.Mood.Get(MoodType.Excited)));
					}

					break;
				}

				case OrgasmsInvolved:
				{
					var player = Cue.Instance.Player;
					if (player == null)
						return;

					foreach (var p in Cue.Instance.ActivePersons)
					{
						if (p.IsPlayer)
							continue;

						if (PersonStatus.EitherPenetrating(player, p))
							orgasmInfos_.Add(new OrgasmInfo(p, p.Mood.Get(MoodType.Excited)));
					}

					break;
				}
			}
		}

		private void DoLookAt()
		{
			Log.Info($"look at");

			switch (lookAt_)
			{
				case LookAtPlayerAll:
				{
					var player = Cue.Instance.Player;

					if (player != null)
					{
						foreach (var p in Cue.Instance.ActivePersons)
						{
							if (p.IsPlayer)
								continue;

							Log.Info($"look at player for {p} (all)");

							p.Gaze.SetTemporaryTarget(
								p.Gaze.Targets.GetEyes(player.PersonIndex),
								orgasmsTime_ + p.Personality.Get(PS.OrgasmTime));
						}
					}

					break;
				}

				case LookAtPlayerInvolved:
				{
					var player = Cue.Instance.Player;

					if (player != null)
					{
						foreach (var p in Cue.Instance.ActivePersons)
						{
							if (p.IsPlayer)
								continue;

							if (PersonStatus.EitherPenetrating(player, p))
							{
								Log.Info($"look at player for {p} (involved)");

								p.Gaze.SetTemporaryTarget(
									p.Gaze.Targets.GetEyes(player.PersonIndex),
									orgasmsTime_ + p.Personality.Get(PS.OrgasmTime));
							}
						}
					}

					break;
				}
			}
		}

		private void DoEvents()
		{
			Log.Info($"doing events");

			switch (events_)
			{
				case StopEventsAll:
				{
					foreach (var p in Cue.Instance.ActivePersons)
					{
						if (p.IsPlayer)
							continue;

						Log.Info($"stop events for {p} (all)");
						p.AI.StopAllEvents();
					}

					break;
				}

				case StopEventsInvolved:
				{
					var player = Cue.Instance.Player;
					if (player == null)
						return;

					foreach (var p in Cue.Instance.ActivePersons)
					{
						if (p.IsPlayer)
							continue;

						if (PersonStatus.EitherPenetrating(player, p))
						{
							Log.Info($"stop events for {p} (involved)");
							p.AI.StopAllEvents();
						}
					}

					break;
				}
			}
		}
	}
}
