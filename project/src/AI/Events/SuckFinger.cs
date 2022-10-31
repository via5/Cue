using System;

namespace Cue
{
	class SuckFingerEvent : BasicEvent
	{
		private const float CheckInterval = 0.5f;
		private const float WaitAfterFailure = 2;

		private BodyPartLock mouthLock_ = null;
		private float wait_ = 0;
		private bool active_ = false;

		public SuckFingerEvent()
			: base("SuckFinger")
		{
		}

		public override bool Active
		{
			get { return false; }
			set { }
		}

		public override bool CanToggle { get { return false; } }
		public override bool CanDisable { get { return true; } }

		public override void Debug(DebugLines debug)
		{
			debug.Add("active", $"{active_}");
			debug.Add("mouthLock", $"{mouthLock_}");
		}

		protected override void DoUpdate(float s)
		{
			if (!Enabled)
			{
				Stop();
				return;
			}

			wait_ = Math.Max(wait_ - s, 0);
			if (wait_ > 0)
				return;

			var triggers = person_.Body.Get(BP.Mouth).GetTriggers();

			if (active_)
				UpdateActive(triggers);
			else
				UpdateInactive(triggers);

			if (wait_ == 0)
				wait_ = CheckInterval;
		}

		private void Start()
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

				active_ = true;
			}
		}

		private void Stop()
		{
			if (active_)
			{
				person_.Animator.StopType(AnimationType.SuckFinger);

				if (mouthLock_ != null)
				{
					mouthLock_.Unlock();
					mouthLock_ = null;
				}

				active_ = false;
			}
		}

		private void UpdateActive(Sys.TriggerInfo[] triggers)
		{
			if (triggers != null)
			{
				for (int i = 0; i < triggers.Length; ++i)
				{
					if (triggers[i].BodyPart == BP.LeftHand ||
						triggers[i].BodyPart == BP.RightHand)
					{
						// still triggered
						return;
					}
				}
			}

			// not triggered anymore
			Stop();
		}

		private void UpdateInactive(Sys.TriggerInfo[] triggers)
		{
			if (triggers != null)
			{
				for (int i = 0; i < triggers.Length; ++i)
				{
					if (triggers[i].BodyPart == BP.LeftHand ||
						triggers[i].BodyPart == BP.RightHand)
					{
						// hand triggering mouth, start
						Start();
						break;
					}
				}
			}
		}
	}
}
