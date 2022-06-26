namespace Cue
{
	class MouthEvent : BasicEvent
	{
		private const float ManualStartDistance = 0.4f;
		private const float AutoStartDistance = 0.3f;

		private BodyPart head_ = null;

		private Person target_ = null;
		private BodyPartLock[] sourceLocks_ = null;
		private BodyPartLock[] targetLocks_ = null;
		private bool hasForcedTrigger_ = false;
		private bool wasGrabbed_;

		public MouthEvent()
			: base("mouth")
		{
		}

		protected override void DoInit()
		{
			base.DoInit();
			head_ = person_.Body.Get(BP.Head);
		}

		public override void Debug(DebugLines debug)
		{
			debug.Add("active", $"{Active}");
			debug.Add("bjTarget", $"{target_}");

			if (sourceLocks_ != null)
			{
				for (int i = 0; i < sourceLocks_.Length; ++i)
					debug.Add($"srcLock", sourceLocks_[i].ToString());
			}

			if (targetLocks_ != null)
			{
				for (int i = 0; i < targetLocks_.Length; ++i)
					debug.Add($"tarLock", targetLocks_[i].ToString());
			}
		}

		public bool Active
		{
			get
			{
				return (target_ != null);
			}

			set
			{
				if (value)
					Check(ManualStartDistance);
				else
					Stop();
			}
		}

		public Person Target
		{
			get { return target_; }
		}

		private void Stop(int stopFlags = Animation.NoStopFlags)
		{
			Unlock();

			if (target_ != null)
			{
				if (hasForcedTrigger_)
				{
					target_.Body.Get(target_.Body.GenitalsBodyPart)
						.RemoveForcedTrigger(person_.PersonIndex, BP.Mouth);

					target_.Excitement.GetSource(SS.Genitals).RemoveEnabledFor(person_);

					hasForcedTrigger_ = false;
				}

				target_.Homing.Mouth = false;
				target_ = null;
			}

			person_.Animator.StopType(AnimationTypes.Blowjob, stopFlags);
		}

		private void Unlock()
		{
			if (sourceLocks_ != null)
			{
				for (int i = 0; i < sourceLocks_.Length; ++i)
					sourceLocks_[i].Unlock();

				sourceLocks_ = null;
			}

			if (targetLocks_ != null)
			{
				for (int i = 0; i < targetLocks_.Length; ++i)
					targetLocks_[i].Unlock();

				targetLocks_ = null;
			}
		}

		public override void Update(float s)
		{
			CheckAutoStart();
		}

		private void CheckAutoStart()
		{
			if (GrabEnded())
				Check(AutoStartDistance, Animation.StopNoReturn);
		}

		private bool GrabEnded()
		{
			if (head_.Grabbed && !wasGrabbed_)
			{
				wasGrabbed_ = true;
			}
			else if (!head_.Grabbed && wasGrabbed_)
			{
				wasGrabbed_ = false;
				return true;
			}

			return false;
		}

		public override void ForceStop()
		{
			if (Active)
				Stop();
		}

		private bool Check(float maxDistance, int stopFlags = Animation.NoStopFlags)
		{
			var t = FindTarget(maxDistance);
			if (t == null)
			{
				Log.Info("no target");
				Stop(stopFlags);
				return false;
			}

			if (t == target_)
			{
				Log.Info("same target");
				return true;
			}

			Stop(stopFlags);

			Log.Info($"found target {t}");

			sourceLocks_ = BodyPartLock.LockMany(
				person_,
				new BodyPartTypes[] { BP.Head, BP.Lips, BP.Mouth, BP.Chest },
				BodyPartLock.Anim, "bj", BodyPartLock.Strong);

			if (sourceLocks_ == null)
			{
				Log.Error("failed to lock sources");
				Unlock();
				return false;
			}

			targetLocks_ = BodyPartLock.LockMany(
				t,
				new BodyPartTypes[] { BP.Hips },
				BodyPartLock.Anim, "bj", BodyPartLock.Strong);

			if (targetLocks_ == null)
			{
				Log.Error("failed to lock targets");
				Unlock();
				return false;
			}

			if (!person_.Animator.PlayType(
				AnimationTypes.Blowjob,
				new AnimationContext(t, sourceLocks_[0].Key)))
			{
				Log.Error("failed to start animation");
				Unlock();
				return false;
			}

			if (t.Body.PenisSensitive)
			{
				t.Body.Get(t.Body.GenitalsBodyPart)
					.AddForcedTrigger(person_.PersonIndex, BP.Mouth);

				t.Excitement.GetSource(SS.Genitals).AddEnabledFor(person_);

				hasForcedTrigger_ = true;
			}

			t.Homing.Mouth = true;
			target_ = t;

			Log.Info($"started with {target_}");

			return true;
		}

		private Person FindTarget(float maxDistance)
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_ || !p.Body.HasPenis)
					continue;

				var g = p.Body.Get(BP.Penis);
				var d = Vector3.Distance(head_.Position, g.Position);

				if (d < maxDistance)
					return p;
			}

			return null;
		}
	}
}
