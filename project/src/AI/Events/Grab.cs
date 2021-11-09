namespace Cue
{
	class GrabEvent : BasicEvent
	{
		private BodyPartLock headLock_ = null;
		private BodyPart head_;
		private bool wasGrabbed_ = false;

		public GrabEvent(Person p)
			: base("grab", p)
		{
			head_ = p.Body.Get(BP.Head);
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
