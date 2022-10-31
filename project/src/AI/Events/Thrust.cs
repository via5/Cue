namespace Cue
{
	class ThrustEvent : BasicEvent
	{
		private BodyPart receiver_ = null;
		private bool active_ = false;
		private bool running_ = false;
		private BodyPartLock lock_ = null;

		private BodyPartLock idleLock_ = null;

		public ThrustEvent()
			: base("Thrust")
		{
		}

		public override void Debug(DebugLines debug)
		{
			debug.Add("receiver", $"{receiver_}");
			debug.Add("active", $"{active_}");
			debug.Add("running", $"{running_}");
		}

		public override bool Active
		{
			get { return active_; }
			set { active_ = value; }
		}

		public override bool CanToggle { get { return true; } }
		public override bool CanDisable { get { return false; } }

		protected override void DoUpdate(float s)
		{
			if (active_)
			{
				if (!running_)
				{
					if (!Start())
						return;

					if (idleLock_ != null)
					{
						idleLock_.Unlock();
						idleLock_ = null;
					}
				}

				CheckAnim();
			}
			else if (running_)
			{
				Stop();
			}
			else
			{
				if (idleLock_ == null)
				{
					if (person_.Status.Penetrated() || person_.Status.Penetrating())
					{
						idleLock_ = person_.Body.Get(BP.Hips).Lock(
							BodyPartLock.Anim, "idle lock for pen",
							BodyPartLock.Weak);
					}
				}
				else
				{
					if (!person_.Status.Penetrated() && !person_.Status.Penetrating())
					{
						idleLock_.Unlock();
						idleLock_ = null;
					}
				}
			}
		}

		protected override void DoForceStop()
		{
			if (Active)
				Stop();
		}

		private bool Start()
		{
			receiver_ = FindReceiver();

			if (receiver_ == null)
			{
				Log.Info($"no penetration detected");
				active_ = false;
				return false;
			}

			// weak lock, just to inhibit idle animation
			lock_ = person_.Body.Get(BP.Hips).Lock(
				BodyPartLock.Anim, "thrust", BodyPartLock.Weak);

			if (lock_ == null)
			{
				Log.Info($"hips are busy");
				return false;
			}

			Log.Info($"starting thrust with {receiver_}");
			receiver_.Person.Body.Zapped(person_, SS.Penetration);

			person_.Body.Get(person_.Body.GenitalsBodyPart)
				.AddForcedTrigger(
					receiver_.Person.PersonIndex, receiver_.Type);

			receiver_.AddForcedTrigger(
				person_.PersonIndex, person_.Body.GenitalsBodyPart);

			receiver_.Person.Atom.SetBodyDamping(Sys.BodyDamping.SexReceiver);

			SetZoneEnabled(true);
			running_ = true;

			return true;
		}

		private void Stop()
		{
			Log.Verbose($"thrust: stopping");
			person_.Animator.StopType(AnimationType.Thrust);

			person_.Body.Get(person_.Body.GenitalsBodyPart)
				.RemoveForcedTrigger(
					receiver_.Person.PersonIndex, receiver_.Type);

			receiver_.RemoveForcedTrigger(
				person_.PersonIndex, person_.Body.GenitalsBodyPart);

			receiver_.Person.Atom.SetBodyDamping(Sys.BodyDamping.Normal);

			SetZoneEnabled(false);

			if (lock_ != null)
			{
				lock_.Unlock();
				lock_ = null;
			}

			running_ = false;
			receiver_ = null;
		}

		private void SetZoneEnabled(bool b)
		{
			if (b)
			{
				person_.Excitement.GetSource(SS.Penetration).AddEnabledFor(receiver_?.Person);
				if (receiver_ != null)
					receiver_.Person.Excitement.GetSource(SS.Penetration).AddEnabledFor(person_);
			}
			else
			{
				person_.Excitement.GetSource(SS.Penetration).RemoveEnabledFor(receiver_?.Person);
				if (receiver_ != null)
					receiver_.Person.Excitement.GetSource(SS.Penetration).RemoveEnabledFor(person_);
			}
		}

		private void CheckAnim()
		{
			AnimationStatus state = person_.Animator.PlayingStatus(AnimationType.Thrust);

			if (state == AnimationStatus.Playing)
			{
				if (Mood.ShouldStopSexAnimation(person_, receiver_?.Person))
					person_.Animator.StopType(AnimationType.Thrust);
			}
			else if (state == AnimationStatus.NotPlaying || state == AnimationStatus.Paused)
			{
				if (Mood.CanStartSexAnimation(person_, receiver_?.Person))
				{
					person_.Animator.PlayType(
						AnimationType.Thrust, new AnimationContext(
							receiver_?.Person, lock_.Key));
				}
			}
		}

		private BodyPart FindReceiver()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				if (PersonStatus.EitherPenetrating(person_, p))
					return p.Body.Get(p.Body.GenitalsBodyPart);
			}

			return null;
		}
	}
}
