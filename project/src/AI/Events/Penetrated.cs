﻿namespace Cue
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


		public PenetratedEvent(Person p)
			: base("penetrated", p)
		{
		}

		public override void Update(float s)
		{
			switch (penetration_)
			{
				case NotPenetrated:
				{
					if (person_.Body.Penetrated())
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
					if (person_.Body.Penetrated())
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
					if (!person_.Body.Penetrated())
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
				person_.Animator.PlayType(Animations.Penetrated);
			}
		}

		private void OnOut()
		{
		}
	}
}