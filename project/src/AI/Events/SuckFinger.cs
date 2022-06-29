using System;

namespace Cue
{
	class SuckFingerEvent : BasicEvent
	{
		private const float WaitAfterFailure = 2;

		private BodyPartLock mouthLock_ = null;
		private float wait_ = 0;

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
			wait_ = Math.Max(wait_ - s, 0);
			if (wait_ > 0)
				return;

			bool triggered = person_.Body.Get(BP.Mouth).Triggered;

			if (mouthLock_ == null && triggered)
			{
				var head = person_.Body.Get(BP.Head);

				mouthLock_ = head.Lock(
					BodyPartLock.Anim, "SuckFinger", BodyPartLock.Strong);

				if (mouthLock_ == null)
				{
					wait_ = WaitAfterFailure;
				}
				else
				{
					person_.Animator.PlayType(
						AnimationType.SuckFinger,
						new AnimationContext(mouthLock_.Key));
				}
			}
			else if (mouthLock_ != null && !triggered)
			{
				mouthLock_.Unlock();
				mouthLock_ = null;
				person_.Animator.StopType(AnimationType.SuckFinger);
			}
		}
	}
}
