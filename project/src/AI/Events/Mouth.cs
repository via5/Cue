namespace Cue
{
	class MouthEvent : BasicEvent<EmptyEventData>
	{
		private const float ManualStartDistance = 0.4f;
		private const float AutoStartDistance = 0.025f;

		private BodyPart head_ = null;

		private Person target_ = null;
		private BodyPartLock[] sourceLocks_ = null;
		private BodyPartLock[] targetStrongLocks_ = null;
		private BodyPartLock[] targetWeakLocks_ = null;
		private bool hasForcedTrigger_ = false;
		private bool wasGrabbed_;

		public MouthEvent()
			: base("Head")
		{
		}

		protected override void DoInit()
		{
			base.DoInit();
			head_ = person_.Body.Get(BP.Head);
		}

		protected override void DoDebug(DebugLines debug)
		{
			debug.Add("active", $"{Active}");
			debug.Add("bjTarget", $"{target_}");

			if (sourceLocks_ != null)
			{
				for (int i = 0; i < sourceLocks_.Length; ++i)
					debug.Add($"srcLock", sourceLocks_[i].ToString());
			}

			if (targetStrongLocks_ != null)
			{
				for (int i = 0; i < targetStrongLocks_.Length; ++i)
					debug.Add($"tarStrLk", targetStrongLocks_[i].ToString());
			}

			if (targetWeakLocks_ != null)
			{
				for (int i = 0; i < targetWeakLocks_.Length; ++i)
					debug.Add($"tarWkLk", targetWeakLocks_[i].ToString());
			}
		}

		public override bool Active
		{
			get
			{
				return (target_ != null);
			}

			set
			{
				if (value && Enabled)
					Check(ManualStartDistance);
				else
					Stop();
			}
		}

		public override bool CanToggle { get { return true; } }
		public override bool CanDisable { get { return false; } }

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

				person_.Options.GetAnimationOption(AnimationType.Blowjob).Trigger(false);
			}

			person_.Animator.StopType(AnimationType.Blowjob, stopFlags);
		}

		private void Unlock()
		{
			if (sourceLocks_ != null)
			{
				for (int i = 0; i < sourceLocks_.Length; ++i)
					sourceLocks_[i].Unlock();

				sourceLocks_ = null;
			}

			if (targetStrongLocks_ != null)
			{
				for (int i = 0; i < targetStrongLocks_.Length; ++i)
					targetStrongLocks_[i].Unlock();

				targetStrongLocks_ = null;
			}

			if (targetWeakLocks_ != null)
			{
				for (int i = 0; i < targetWeakLocks_.Length; ++i)
					targetWeakLocks_[i].Unlock();

				targetWeakLocks_ = null;
			}
		}

		protected override void DoUpdate(float s)
		{
			if (!Enabled)
				return;

			CheckAutoStart();

			if (!CheckAnim())
				Stop();
		}

		private void CheckAutoStart()
		{
			if (!Cue.Instance.Options.AutoHead)
				return;

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

		protected override void DoForceStop()
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
				new BodyPartType[] { BP.Head, BP.Lips, BP.Mouth, BP.Chest },
				BodyPartLock.Anim, "bj", BodyPartLock.Strong);

			if (sourceLocks_ == null)
			{
				Log.Error("failed to lock sources");
				Unlock();
				return false;
			}

			targetStrongLocks_ = BodyPartLock.LockMany(
				t,
				new BodyPartType[] { BP.Penis },
				BodyPartLock.Anim, "bj", BodyPartLock.Strong);

			if (targetStrongLocks_ == null)
			{
				Log.Error("failed to lock strong targets");
				Unlock();
				return false;
			}

			targetWeakLocks_ = BodyPartLock.LockMany(
				t,
				new BodyPartType[] { BP.Hips },
				BodyPartLock.Anim, "bj", BodyPartLock.Weak);

			if (targetWeakLocks_ == null)
			{
				Log.Error("failed to lock weak targets");
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
			person_.Options.GetAnimationOption(AnimationType.Blowjob).Trigger(true);

			return true;
		}

		private bool CheckAnim()
		{
			if (target_ != null && person_.Options.GetAnimationOption(AnimationType.Blowjob).Play)
			{
				AnimationStatus state = person_.Animator.PlayingStatus(
					AnimationType.Blowjob);

				if (state == AnimationStatus.Playing)
				{
					if (Mood.ShouldStopSexAnimation(person_, target_))
						person_.Animator.StopType(AnimationType.Blowjob);
				}
				else if (state == AnimationStatus.NotPlaying || state == AnimationStatus.Paused)
				{
					if (Mood.CanStartSexAnimation(person_, target_))
					{
						if (!person_.Animator.PlayType(
								AnimationType.Blowjob, new AnimationContext(
									target_, targetStrongLocks_[0].Key)))
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		private Person FindTarget(float maxDistance)
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				var bp = p.Body.Get(BP.Penis);

				if (bp.Exists)
				{
					Log.Verbose($"ok, {p} has penis");

					ulong key = BodyPartLock.NoKey;
					if (targetStrongLocks_ != null && targetStrongLocks_.Length > 0)
						key = targetStrongLocks_[0].Key;

					if (!bp.LockedFor(BodyPartLock.Anim, key))
					{
						Log.Verbose($"ok, {p} is not locked");

						var d = bp.DistanceToSurface(head_);
						if (d < maxDistance)
						{
							Log.Verbose($"ok, d={d}, max={maxDistance}");
							return p;
						}
						else
						{
							Log.Verbose($"too far, d={d}, max={maxDistance}");
						}
					}
					else
					{
						Log.Verbose($"{p} is locked");
					}
				}
				else
				{
					Log.Verbose($"{p} has no penis");
				}
			}

			return null;
		}
	}
}
