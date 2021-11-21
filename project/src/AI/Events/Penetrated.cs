namespace Cue
{
	class PenetratedEvent : BasicEvent
	{
		private const float TentativeTime = 0.2f;
		private const float Cooldown = 30;

		private const int NotPenetrated = 0;
		private const int TentativePenetration = 1;
		private const int Penetrated = 2;

		private float elapsedTentative_ = 10000;
		private float elapsedNotPenetrated_ = 10000;
		private int penetration_ = NotPenetrated;


		public PenetratedEvent()
			: base("penetrated")
		{
		}

		public override void Update(float s)
		{
			switch (penetration_)
			{
				case NotPenetrated:
				{
					if (person_.Status.Penetrated())
					{
						penetration_ = TentativePenetration;
						elapsedTentative_ = 0;
					}
					else
					{
						elapsedNotPenetrated_ += s;
					}

					break;
				}

				case TentativePenetration:
				{
					if (person_.Status.Penetrated())
					{
						elapsedTentative_ += s;

						if (elapsedTentative_ > TentativeTime)
						{
							penetration_ = Penetrated;
							OnIn();
						}
					}
					else
					{
						penetration_ = NotPenetrated;
					}

					break;
				}

				case Penetrated:
				{
					if (!person_.Status.Penetrated())
					{
						OnOut();
						penetration_ = NotPenetrated;
					}

					break;
				}
			}
		}

		private void OnIn()
		{
			if (elapsedNotPenetrated_ > Cooldown)
			{
				elapsedNotPenetrated_ = 0;

				// disabled for now
				//
				// this used to play the penetrated animation, but it's not
				// great:
				//
				//  - alignment is manual, so the player is not typically
				//    looking at the character during penetration, making the
				//    animation useless
				//
				//  - it can trigger at weird moments, it's difficult to
				//    determine the player's intention
				//
				// the animation has been moved to the Thrust event instead

				//person_.Animator.PlayType(Animations.Penetrated);
			}
		}

		private void OnOut()
		{
		}
	}
}
