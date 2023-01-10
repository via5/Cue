namespace Cue
{
	class GrabEvent : BasicEvent
	{
		private BodyPartLock headLock_ = null;
		private BodyPartLock neckLock_ = null;
		private BodyPart head_ = null;
		private BodyPart neck_ = null;
		private bool headWasGrabbed_ = false;
		private bool neckWasGrabbed_ = false;
		private bool neckWasGrabbedWithHead_ = false;

		public GrabEvent()
			: base("Grab")
		{
		}

		public override bool Active
		{
			get { return headWasGrabbed_ || neckWasGrabbed_ || neckWasGrabbedWithHead_; }
			set { }
		}

		public override bool CanToggle { get { return false; } }
		public override bool CanDisable { get { return false; } }

		protected override void DoInit()
		{
			head_ = person_.Body.Get(BP.Head);
			neck_ = person_.Body.Get(BP.Neck);
		}

		public override void Debug(DebugLines debug)
		{
			debug.Add("head grabbed", $"{head_.GrabbedByPlayer}");
			debug.Add("headWasGrabbed", $"{headWasGrabbed_}");
			debug.Add("head lock", $"{headLock_}");
			debug.Add("");
			debug.Add("neck grabbed", $"{neck_.GrabbedByPlayer}");
			debug.Add("neckWasGrabbed", $"{neckWasGrabbed_}");
			debug.Add("neckWasGrabbedWithHead", $"{neckWasGrabbedWithHead_}");
			debug.Add("neck lock", $"{neckLock_}");
		}

		protected override void DoUpdate(float s)
		{
			if (!Enabled)
			{
				headWasGrabbed_ = false;
				StopHead();

				bool fromHead = neckWasGrabbedWithHead_;
				neckWasGrabbed_ = false;
				neckWasGrabbedWithHead_ = false;
				StopNeck(fromHead);

				return;
			}


			// unfortunately, grabbing the neck is usually difficult because
			// it's off, and vam always grabs the head instead since it's
			// close enough and is on
			//
			// the first check is for a regular neck grab, which can happen,
			// but is rare
			//
			// the second check is for a head grab, but it also checks if the
			// head is being triggered by the same hand; this is perfect, just
			// barely touching the neck with the tips of the fingers will
			// trigger it, but it's good enough for now

			CheckNeck();
			CheckHead();
		}

		private void CheckNeck()
		{
			if (!neckWasGrabbed_)
			{
				if (neck_.GrabbedByPlayer)
				{
					neckWasGrabbed_ = true;
					StartNeck(false);
				}
			}
			else
			{
				if (!neck_.GrabbedByPlayer)
				{
					neckWasGrabbed_ = false;
					StopNeck(false);
				}
			}
		}

		private void CheckHead()
		{
			if (!headWasGrabbed_)
			{
				// check head
				var pr = head_.GrabbedByPlayer;

				if (pr)
				{
					headWasGrabbed_ = true;
					StartHead();
				}

				// check neck
				if (!neckWasGrabbedWithHead_)
				{
					var ts = neck_.GetTriggers(true);

					if (ts != null)
					{
						for (int i = 0; i < ts.Length; ++i)
						{
							if (ts[i].PersonIndex == Cue.Instance.Player.PersonIndex)
							{
								if (ts[i].BodyPart == pr.byBodyPart)
								{
									neckWasGrabbedWithHead_ = true;
									StartNeck(true);
								}
							}
						}
					}
				}
			}
			else
			{
				if (!head_.GrabbedByPlayer)
				{
					headWasGrabbed_ = false;
					StopHead();

					if (neckWasGrabbedWithHead_)
					{
						neckWasGrabbedWithHead_ = false;
						StopNeck(true);
					}
				}
			}
		}

		private void StartHead()
		{
			headLock_ = head_.Lock(
				BodyPartLock.Anim, "grabbed", BodyPartLock.Strong);
		}

		private void StopHead()
		{
			if (headLock_ != null)
			{
				headLock_.Unlock();
				headLock_ = null;
			}
		}

		private void StartNeck(bool fromHead)
		{
			if (!Cue.Instance.Options.Choking)
				return;

			if (!fromHead)
			{
				// force a grab on the head to disable mg's gaze, see also
				// GazeGrabbed.DoCheck(); only do this if the grab doesn't
				// actually come from the head
				head_.VamSys.Controller.isGrabbing = true;
			}

			neckLock_ = head_.Lock(
				BodyPartLock.Anim, "grabbed", BodyPartLock.Strong);

			person_.Body.Breathing = false;
		}

		private void StopNeck(bool fromHead)
		{
			if (neckLock_ != null)
			{
				neckLock_.Unlock();
				neckLock_ = null;
			}

			if (!fromHead)
			{
				// see StartNeck()
				if (!headWasGrabbed_)
					head_.VamSys.Controller.isGrabbing = true;
			}

			person_.Body.Breathing = true;
		}
	}
}
