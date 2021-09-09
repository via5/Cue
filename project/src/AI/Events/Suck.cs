﻿namespace Cue
{
	class FingerSuckEvent : BasicEvent
	{
		private bool busy_ = false;

		public FingerSuckEvent(Person p)
			: base("fsuck", p)
		{
		}

		public override void Update(float s)
		{
			var mouthTriggered = person_.Body.Get(BodyParts.Mouth).Triggered;
			var head = person_.Body.Get(BodyParts.Head);

			if (!busy_ && mouthTriggered && !head.Busy)
			{
				busy_ = true;
				head.ForceBusy(true);
				person_.Animator.PlayType(Animation.SuckType, Animator.Loop);
			}
			else if (busy_ && !mouthTriggered)
			{
				busy_ = false;
				head.ForceBusy(false);
				person_.Animator.StopType(Animation.SuckType);
			}
		}
	}
}
