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

		protected override void DoInit()
		{
			head_ = person_.Body.Get(BP.Head);
		}

		public override string[] Debug()
		{
			return new string[]
			{
			};
		}

		public override void Update(float s)
		{
			if (head_.GrabbedByPlayer)
			{
				if (!wasGrabbed_)
				{
					wasGrabbed_ = true;
					headLock_ = head_.Lock(BodyPartLock.Anim, "grabbed");
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
