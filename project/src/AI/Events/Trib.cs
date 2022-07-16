namespace Cue
{
	class TribEvent : BasicEvent
	{
		private const float TribDistance = 0.1f;

		private BodyPart receiver_ = null;
		private bool active_ = false;
		private bool running_ = false;
		private BodyPartLock lock_ = null;

		public TribEvent()
			: base("trib")
		{
		}

		public override void Debug(DebugLines debug)
		{
			debug.Add("receiver", $"{receiver_}");
			debug.Add("active", $"{active_}");
			debug.Add("running", $"{running_}");
		}

		public bool Active
		{
			get { return active_; }
			set { active_ = value; }
		}

		public override void Update(float s)
		{
			if (active_)
			{
				if (!running_)
				{
					if (!Start())
						return;
				}

				CheckAnim();
			}
			else if (running_)
			{
				Stop();
			}
		}

		public override void ForceStop()
		{
			if (Active)
				Stop();
		}

		private bool Start()
		{
			receiver_ = FindReceiver();

			if (receiver_ == null)
			{
				Log.Info($"no contact for trib");
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

			Log.Info($"starting trib with {receiver_}");
			receiver_.Person.Body.Zapped(person_, SS.Genitals);

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
			Log.Verbose($"trib: stopping");
			person_.Animator.StopType(AnimationType.Trib);

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
				person_.Excitement.GetSource(SS.Genitals).AddEnabledFor(receiver_?.Person);
				if (receiver_ != null)
					receiver_.Person.Excitement.GetSource(SS.Genitals).AddEnabledFor(person_);
			}
			else
			{
				person_.Excitement.GetSource(SS.Genitals).RemoveEnabledFor(receiver_?.Person);
				if (receiver_ != null)
					receiver_.Person.Excitement.GetSource(SS.Genitals).RemoveEnabledFor(person_);
			}
		}

		private void CheckAnim()
		{
			AnimationStatus state = person_.Animator.PlayingStatus(AnimationType.Trib);

			if (state == AnimationStatus.Playing)
			{
				if (Mood.ShouldStopSexAnimation(person_, receiver_?.Person))
					person_.Animator.StopType(AnimationType.Trib);
			}
			else if (state == AnimationStatus.NotPlaying || state == AnimationStatus.Paused)
			{
				if (Mood.CanStartSexAnimation(person_, receiver_?.Person))
				{
					person_.Animator.PlayType(
						AnimationType.Trib, new AnimationContext(
							receiver_?.Person, lock_.Key));
				}
			}
		}

		private BodyPart FindReceiver()
		{
			var selfGen = person_.Body.Get(person_.Body.GenitalsBodyPart);

			BodyPart closest = null;
			float closestD = float.MaxValue;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				if (PersonStatus.EitherPenetrating(person_, p))
				{
					Log.Info($"won't trib {p.ID} because penetration is active");
					continue;
				}

				foreach (BodyPartType bp in BodyPartType.Values)
				{
					var otherPart = p.Body.Get(bp);
					var d = selfGen.DistanceToSurface(otherPart);

					if (d > TribDistance)
						continue;

					if (d < closestD)
					{
						if (BetterReceiver(closest, otherPart))
						{
							closest = otherPart;
							closestD = d;
						}
					}
				}
			}

			return closest;
		}

		private bool BetterReceiver(BodyPart tentative, BodyPart check)
		{
			if (tentative == null)
			{
				// first
				return true;
			}

			if (check.Type == check.Person.Body.GenitalsBodyPart)
			{
				// prioritize genitals
				return true;
			}

			return false;
		}
	}
}
