using System.Collections.Generic;
using System.Linq;

namespace Cue
{
	class Finish
	{
		class OrgasmInfo
		{
			public Person person;
			public float initialExcitement = 0;

			public OrgasmInfo(Person p)
			{
				person = p;
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
		private int lookAt_ = LookAtPersonality;
		private int orgasms_ = OrgasmsInvolved;
		private float orgasmsTime_ = 1;
		private int events_ = StopEventsAll;

		private int state_ = NoState;
		private float elapsed_ = 0;
		private OrgasmInfo[] orgasmInfos_;


		public Finish()
		{
			log_ = new Logger(Logger.Object, "finish");
		}

		public void Init()
		{
			orgasmInfos_ = new OrgasmInfo[Cue.Instance.ActivePersons.Length];
			for (int i = 0; i < Cue.Instance.ActivePersons.Length; ++i)
				orgasmInfos_[i] = new OrgasmInfo(Cue.Instance.ActivePersons[i]);
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

		private float TotalTime(Person p)
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

			Log.Info("orgasm infos:");
			foreach (var i in orgasmInfos_)
			{
				i.initialExcitement = i.person.Mood.Get(MoodType.Excited);
				Log.Info($"{i.person} {i.initialExcitement:0.00}");
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

						CheckLookAt();
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
						p = 1;
					else
						p = U.Clamp(elapsed_ / orgasmsTime_, 0, 1);

					CheckExcitement(p);

					if (p >= 1)
					{
						Log.Info($"orgasms time finished");
						CheckEvents();
						state_ = NoState;
					}

					break;
				}
			}
		}

		private void CheckLookAt()
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
								TotalTime(p));
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

							if (p.Status.IsInvolvedWith(player))
							{
								Log.Info($"look at player for {p} (involved)");

								p.Gaze.SetTemporaryTarget(
									p.Gaze.Targets.GetEyes(player.PersonIndex),
									TotalTime(p));
							}
						}
					}

					break;
				}

				case LookAtPersonality:
				{
					foreach (var p in Cue.Instance.ActivePersons)
					{
						if (p.IsPlayer)
							continue;

						SetLookAtFromPersonality(p);
					}

					break;
				}
			}
		}

		private void CheckExcitement(float p)
		{
			for (int i = 0; i < orgasmInfos_.Length; ++i)
			{
				var o = orgasmInfos_[i];
				if (o.person.IsPlayer)
					continue;

				switch (orgasms_)
				{
					case OrgasmsAll:
					{
						SetExcitement(o, p);
						break;
					}

					case OrgasmsInvolved:
					{
						var player = Cue.Instance.Player;

						if (player != null)
						{
							if (o.person.Status.IsInvolvedWith(player))
								SetExcitement(o, p);
						}

						break;
					}

					case OrgasmsPersonality:
					{
						SetExcitementFromPersonality(o, p);
						break;
					}
				}

			}
		}

		private void CheckEvents()
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

						if (p.Status.IsInvolvedWith(player))
						{
							Log.Info($"stop events for {p} (involved)");
							p.AI.StopAllEvents();
						}
					}

					break;
				}
			}
		}

		private void SetExcitement(OrgasmInfo o, float p)
		{
			float range = 1 - o.initialExcitement;
			float e = o.initialExcitement + range * p;

			o.person.Mood.GetValue(MoodType.Excited).Value = e;

			if (p >= 1)
				o.person.Mood.ForceOrgasm();
		}

		private void SetExcitementFromPersonality(OrgasmInfo o, float p)
		{
			var ps = o.person.Personality;

			var orgasm = ps.GetString(PS.FinishOrgasm);
			var minExcitement = ps.Get(PS.FinishOrgasmMinExcitement);

			if (o.person.Mood.Get(MoodType.Excited) >= minExcitement)
			{
				if (orgasm == "involved")
				{
					var player = Cue.Instance.Player;

					if (player != null)
					{
						if (o.person.Status.IsInvolvedWith(player))
							SetExcitement(o, p);
					}
				}
				else if (orgasm == "always")
				{
					SetExcitement(o, p);
				}
			}
		}

		private void SetLookAtFromPersonality(Person p)
		{
			var ps = p.Personality;

			var target = ps.GetString(PS.FinishLookAtTarget);
			var cond = ps.GetString(PS.FinishLookAtIf);

			Log.Info(
				$"look at personality for {p}: " +
				$"target={target} cond={cond}");

			if (target == "player")
			{
				var player = Cue.Instance.Player;
				if (player == null)
				{
					Log.Info($"  - target is player, but there's no player");
					return;
				}

				bool doAction = false;

				if (cond == "always")
				{
					Log.Info($"  - target is player, cond is always, doing action");
					doAction = true;
				}
				else if (cond == "involved")
				{
					if (p.Status.IsInvolvedWith(player))
					{
						Log.Info($"  - target is player, cond is involved and is true, doing action");
						doAction = true;
					}
					else
					{
						Log.Info($"  - target is player, cond is involved but is false, not doing action");
					}
				}
				else
				{
					Log.Error($"  - bad cond '{cond}', not doing action");
				}

				if (doAction)
				{
					if (p.Gaze.ShouldAvoidDuringSex(player))
					{
						Log.Info($"  - avoiding player for {p} (ps, {cond})");
						p.Gaze.SetTemporaryAvoid(player, TotalTime(p));
					}
					else
					{
						Log.Info($"  - looking at player for {p} (ps, {cond})");

						p.Gaze.SetTemporaryTarget(
							p.Gaze.Targets.GetEyes(player.PersonIndex),
							TotalTime(p));
					}
				}
			}
		}
	}
}
