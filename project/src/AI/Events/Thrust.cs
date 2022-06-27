namespace Cue
{
	class ThrustEvent : BasicEvent
	{
		private const float FrottageDistance = 0.1f;

		private BodyPart receiver_ = null;
		private bool active_ = false;
		private bool running_ = false;
		private AnimationType anim_ = AnimationType.None;
		private BodyPartLock lock_ = null;


		public ThrustEvent()
			: base("thrust")
		{
		}

		public override void Debug(DebugLines debug)
		{
			debug.Add("receiver", $"{receiver_}");
			debug.Add("active", $"{active_}");
			debug.Add("running", $"{running_}");
			debug.Add("anim", $"{AnimationType.ToString(anim_)}");
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
			// four cases:
			//  1) no receiver, but this person has a penis that's currently
			//     visible: disallow, too easy to trigger and the frottage
			//     animation doesn't work well
			//
			//  2) no receiver and this person does not have a penis: frottage
			//     animation, but won't be able to sync
			//
			//  3) has receiver and either is penetrated by the other:
			//     penetration animation
			//
			//  4) has receiver but without penetration: frottage animation with
			//     possible sync


			// start by checking penetration
			receiver_ = FindPenetrationReceiver();

			if (receiver_ == null && person_.Body.HasPenis && person_.Clothing.GenitalsVisible)
			{
				// don't allow frottage from characters that can penetrate
				Log.Info($"no valid receiver");
				active_ = false;
				anim_ = AnimationType.None;
				return false;
			}

			lock_ = person_.Body.Get(BP.Hips).Lock(
				BodyPartLock.Anim, "thrust", BodyPartLock.Strong);

			if (lock_ == null)
			{
				Log.Info($"hips are busy");
				return false;
			}


			if (receiver_ == null)
			{
				// no penetration, look for something to rub on
				receiver_ = FindFrottageReceiver();
			}

			if (receiver_ == null)
			{
				// frottage with nobody
				Log.Info($"starting frottage with nobody");

				person_.Body.Get(person_.Body.GenitalsBodyPart)
					.AddForcedTrigger(-1, BP.None);

				anim_ = AnimationType.Frottage;
			}
			else
			{
				if (PersonStatus.EitherPenetrating(person_, receiver_.Person))
				{
					// penetration
					Log.Info($"starting sex with {receiver_.Person.ID}.{receiver_}");
					receiver_.Person.Body.Zapped(person_, SS.Penetration);
					anim_ = AnimationType.Sex;
				}
				else
				{
					// frottage
					Log.Info($"starting frottage with {receiver_.Person.ID}.{receiver_}");
					receiver_.Person.Body.Zapped(person_, SS.Genitals);
					anim_ = AnimationType.Frottage;
				}

				person_.Body.Get(person_.Body.GenitalsBodyPart)
					.AddForcedTrigger(
						receiver_.Person.PersonIndex, receiver_.Type);

				receiver_.AddForcedTrigger(
					person_.PersonIndex, person_.Body.GenitalsBodyPart);
			}

			SetZoneEnabled(true);

			person_.Atom.SetBodyDamping(Sys.BodyDamping.Sex);
			running_ = true;

			return true;
		}

		private void Stop()
		{
			Log.Verbose($"thrust: stopping");
			person_.Animator.StopType(anim_);

			if (receiver_ == null)
			{
				// frottage with nobody
				person_.Body.Get(person_.Body.GenitalsBodyPart)
					.RemoveForcedTrigger(-1, BP.None);
			}
			else
			{
				person_.Body.Get(person_.Body.GenitalsBodyPart)
					.RemoveForcedTrigger(
						receiver_.Person.PersonIndex, receiver_.Type);

				receiver_.RemoveForcedTrigger(
					person_.PersonIndex, person_.Body.GenitalsBodyPart);
			}

			SetZoneEnabled(false);

			if (lock_ != null)
			{
				lock_.Unlock();
				lock_ = null;
			}

			running_ = false;
			receiver_ = null;
			anim_ = AnimationType.None;
		}

		private void SetZoneEnabled(bool b)
		{
			var ss = (anim_ == AnimationType.Frottage ? SS.Genitals : SS.Penetration);

			if (b)
			{
				person_.Excitement.GetSource(ss).AddEnabledFor(receiver_?.Person);
				if (receiver_ != null)
					receiver_.Person.Excitement.GetSource(ss).AddEnabledFor(person_);
			}
			else
			{
				person_.Excitement.GetSource(ss).RemoveEnabledFor(receiver_?.Person);
				if (receiver_ != null)
					receiver_.Person.Excitement.GetSource(ss).RemoveEnabledFor(person_);
			}
		}

		private void CheckAnim()
		{
			int state = person_.Animator.PlayingStatus(anim_);

			if (state == Animator.Playing)
			{
				if (Mood.ShouldStopSexAnimation(person_, receiver_?.Person))
					person_.Animator.StopType(anim_);
			}
			else if (state == Animator.NotPlaying)
			{
				if (Mood.CanStartSexAnimation(person_, receiver_?.Person))
				{
					person_.Animator.PlayType(
						anim_, new AnimationContext(
							receiver_?.Person, lock_.Key));
				}
			}
		}

		private BodyPart FindPenetrationReceiver()
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

		private static BodyPartType[] frottageParts_ = new BodyPartType[]
		{
			BP.Head, BP.Chest, BP.Hips, BP.Labia, BP.Penis,
			BP.LeftThigh, BP.RightThigh,
			BP.LeftShin, BP.RightShin,
			BP.LeftArm, BP.RightArm
		};

		private BodyPart FindFrottageReceiver()
		{
			var selfGen = person_.Body.Get(person_.Body.GenitalsBodyPart);

			BodyPart closest = null;
			float closestD = float.MaxValue;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				for (int i = 0; i < frottageParts_.Length; ++i)
				{
					var otherPart = p.Body.Get(frottageParts_[i]);
					var d = selfGen.DistanceToSurface(otherPart);

					if (d > FrottageDistance)
						continue;

					if (d < closestD)
					{
						if (BetterFrottageReceiver(closest, otherPart))
						{
							closest = otherPart;
							closestD = d;
						}
					}
				}
			}

			return closest;
		}

		private bool BetterFrottageReceiver(BodyPart tentative, BodyPart check)
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
