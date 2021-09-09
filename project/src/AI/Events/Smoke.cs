namespace Cue
{
	class SmokingEvent : BasicEvent
	{
		private bool enabled_ = true;

		public SmokingEvent(Person p)
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
				if (person_.Animator.CanPlayType(Animation.SmokeType))
					person_.Animator.PlayType(Animation.SmokeType);
			}
		}

		private bool CanRun()
		{
			var b = person_.Body;
			var head = b.Get(BodyParts.Head);
			var lips = b.Get(BodyParts.Lips);

			bool busy =
				person_.Body.Get(BodyParts.RightHand).Busy ||
				head.Busy || head.Triggered ||
				lips.Busy || lips.Triggered;

			if (busy)
				return false;

			if (b.GropedByAny(BodyParts.Head))
				return false;

			return true;
		}
	}
}
