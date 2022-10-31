namespace Cue
{
	class GrabEvent : BasicEvent
	{
		private BodyPartLock headLock_ = null;
		private BodyPart head_ = null;
		private bool wasGrabbed_ = false;

		public GrabEvent()
			: base("Grab")
		{
		}

		public override bool Active
		{
			get { return wasGrabbed_; }
			set { }
		}

		public override bool CanToggle { get { return false; } }
		public override bool CanDisable { get { return false; } }

		protected override void DoInit()
		{
			head_ = person_.Body.Get(BP.Head);
		}

		public override void Debug(DebugLines debug)
		{
			debug.Add("grabbed", $"{head_.GrabbedByPlayer}");
			debug.Add("wasGrabbed", $"{wasGrabbed_}");
			debug.Add("lock", $"{headLock_}");
		}

		protected override void DoUpdate(float s)
		{
			if (!Enabled)
			{
				Stop();
				return;
			}

			if (head_.GrabbedByPlayer)
			{
				if (!wasGrabbed_)
					Start();
			}
			else
			{
				if (wasGrabbed_)
				{
					wasGrabbed_ = false;
					Stop();
				}
			}
		}

		private void Start()
		{
			wasGrabbed_ = true;

			headLock_ = head_.Lock(
				BodyPartLock.Anim, "grabbed", BodyPartLock.Strong);
		}

		private void Stop()
		{
			if (headLock_ != null)
			{
				headLock_.Unlock();
				headLock_ = null;
			}
		}
	}
}
