namespace Cue
{
	class GrabEvent : BasicEvent
	{
		private BodyPartLock headLock_ = null;
		private BodyPart head_ = null;
		private bool wasGrabbed_ = false;

		public GrabEvent()
			: base("grab")
		{
		}

		public override bool Active
		{
			get { return wasGrabbed_; }
			set { }
		}

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

		public override void Update(float s)
		{
			if (head_.GrabbedByPlayer)
			{
				if (!wasGrabbed_)
				{
					wasGrabbed_ = true;
					headLock_ = head_.Lock(
						BodyPartLock.Anim, "grabbed", BodyPartLock.Strong);
				}
			}
			else
			{
				if (wasGrabbed_)
				{
					wasGrabbed_ = false;

					if (headLock_ != null)
					{
						headLock_.Unlock();
						headLock_ = null;
					}
				}
			}
		}
	}
}
