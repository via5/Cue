namespace Cue
{
	class MouthEvent : BasicEvent
	{
		private BodyPartLock lock_ = null;

		public MouthEvent(Person p)
			: base("mouth", p)
		{
		}

		public override void Update(float s)
		{
			var mouthTriggered = person_.Body.Get(BP.Mouth).Triggered;
			var head = person_.Body.Get(BP.Head);

			if (lock_ == null && mouthTriggered)
			{
				lock_ = head.Lock(BodyPartLock.Morph);

				if (lock_ != null)
					person_.Animator.PlayType(Animations.Suck, Animator.Loop);
			}
			else if (lock_ != null && !mouthTriggered)
			{
				lock_.Unlock();
				lock_ = null;
				person_.Animator.StopType(Animations.Suck);
			}
		}
	}
}
