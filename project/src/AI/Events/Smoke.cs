namespace Cue
{
	class SmokeEvent : BasicEvent
	{
		private bool enabled_ = true;

		public SmokeEvent(Person p)
			: base("smoke", p)
		{
			enabled_ = p.HasTrait("smoker");

			if (!enabled_)
			{
				//log_.Info("not a smoker");
				return;
			}
		}

		public override void FixedUpdate(float s)
		{
			if (!enabled_)
				return;

			if (CanRun())
			{
				if (person_.Animator.CanPlayType(Animations.Smoke))
					person_.Animator.PlayType(Animations.Smoke);
			}
		}

		private bool CanRun()
		{
			var b = person_.Body;
			var head = b.Get(BP.Head);
			var lips = b.Get(BP.Lips);

			bool busy =
				person_.Body.Get(BP.RightHand).Busy ||
				head.Busy || head.Triggered ||
				lips.Busy || lips.Triggered;

			if (busy)
				return false;

			if (b.GropedByAny(BP.Head))
				return false;

			return true;
		}
	}
}
