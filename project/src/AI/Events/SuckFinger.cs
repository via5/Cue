namespace Cue
{
	class SuckFingerEvent : BasicEvent
	{
		private BodyPartLock mouthLock_ = null;

		public SuckFingerEvent()
			: base("suckFinger")
		{
		}

		public override void Debug(DebugLines debug)
		{
			debug.Add("mouthLock", $"{mouthLock_}");
		}

		public override void Update(float s)
		{
			var mouthTriggered = person_.Body.Get(BP.Mouth).Triggered;
			var head = person_.Body.Get(BP.Head);

			if (mouthLock_ == null && mouthTriggered)
			{
				mouthLock_ = head.Lock(
					BodyPartLock.Anim, "SuckFinger", BodyPartLock.Strong);

				if (mouthLock_ != null)
				{
					person_.Animator.PlayType(
						Animations.SuckFinger,
						new AnimationContext(mouthLock_.Key));
				}
			}
			else if (mouthLock_ != null && !mouthTriggered)
			{
				mouthLock_.Unlock();
				mouthLock_ = null;
				person_.Animator.StopType(Animations.SuckFinger);
			}
		}
	}
}
