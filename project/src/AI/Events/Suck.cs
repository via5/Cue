﻿namespace Cue
{
	class SuckEvent : BasicEvent
	{
		private bool busy_ = false;

		public SuckEvent(Person p)
			: base("suck", p)
		{
		}

		public override void Update(float s)
		{
			var mouthTriggered = person_.Body.Get(BP.Mouth).Triggered;
			var head = person_.Body.Get(BP.Head);

			if (!busy_ && mouthTriggered && !head.Busy)
			{
				busy_ = true;
				head.ForceBusy(true);
				person_.Animator.PlayType(Animations.Suck, Animator.Loop);
			}
			else if (busy_ && !mouthTriggered)
			{
				busy_ = false;
				head.ForceBusy(false);
				person_.Animator.StopType(Animations.Suck);
			}
		}
	}
}
