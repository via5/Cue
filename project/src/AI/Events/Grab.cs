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

		public GrabEvent()
			: base("Grab")
		{
		}

		public override bool Active
		{
			get { return headWasGrabbed_ || neckWasGrabbed_; }
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
			debug.Add("neck lock", $"{neckLock_}");
		}

		protected override void DoUpdate(float s)
		{
			if (!Enabled)
			{
				StopHead();
				StopNeck();
				return;
			}

			if (head_.GrabbedByPlayer)
			{
				if (!headWasGrabbed_)
					StartHead();
			}
			else
			{
				if (headWasGrabbed_)
				{
					headWasGrabbed_ = false;
					StopHead();
				}
			}

			if (neck_.GrabbedByPlayer)
			{
				if (!neckWasGrabbed_)
					StartNeck();
			}
			else
			{
				if (neckWasGrabbed_)
				{
					neckWasGrabbed_ = false;
					StopNeck();
				}
			}
		}

		private void StartHead()
		{
			headWasGrabbed_ = true;

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

		private void StartNeck()
		{
			neckWasGrabbed_ = true;

			// force a grab on the head to disable mg's gaze, see also
			// GazeGrabbed.DoCheck()
			head_.VamSys.Controller.isGrabbing = true;

			neckLock_ = head_.Lock(
				BodyPartLock.Anim, "grabbed", BodyPartLock.Strong);

			person_.Body.Breathing = false;
		}

		private void StopNeck()
		{
			if (neckLock_ != null)
			{
				neckLock_.Unlock();
				neckLock_ = null;
			}

			// see StartNeck()
			if (!headWasGrabbed_)
				head_.VamSys.Controller.isGrabbing = true;

			person_.Body.Breathing = true;
		}
	}
}
