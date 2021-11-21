namespace Cue
{
	class ThrustEvent : BasicEvent
	{
		private const float FrottageDistance = 0.1f;

		private BodyPart receiver_ = null;
		private bool active_ = false;
		private bool running_ = false;
		private int anim_ = Animations.None;

		public ThrustEvent()
			: base("thrust")
		{
		}

		public override string[] Debug()
		{
			return new string[]
			{
				$"receiver    {receiver_}",
				$"active      {active_}",
				$"running     {running_}",
				$"anim        {Animations.ToString(anim_)}"
			};
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

		private bool Start()
		{
			// four cases:
			//  1) no receiver, but this person has a penis: disallow, too
			//     easy to trigger and the frottage animation doesn't work well
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

			if (receiver_ == null && person_.Body.HasPenis)
			{
				// don't allow frottage from characters that can penetrate
				Log.Info($"no valid receiver");
				active_ = false;
				anim_ = Animations.None;
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
					.AddForcedTrigger(-1, -1);

				anim_ = Animations.Frottage;
			}
			else
			{
				if (person_.Body.HasPenis || receiver_.Person.Body.HasPenis)
				{
					// penetration
					Log.Info($"starting sex with {receiver_.Person.ID}.{receiver_}");

					person_.Clothing.GenitalsVisible = true;
					receiver_.Person.Clothing.GenitalsVisible = true;

					// play the penetrated animation now, don't bother if it can't
					// start; see also Penetrated.OnIn()
					person_.Animator.PlayType(Animations.Penetrated);
					receiver_.Person.Animator.PlayType(Animations.Penetrated);

					anim_ = Animations.Sex;
				}
				else
				{
					// frottage
					Log.Info($"starting frottage with {receiver_.Person.ID}.{receiver_}");

					anim_ = Animations.Frottage;
				}

				person_.Body.Get(person_.Body.GenitalsBodyPart)
					.AddForcedTrigger(
						receiver_.Person.PersonIndex, receiver_.Type);
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
					.RemoveForcedTrigger(-1, -1);
			}
			else
			{
				person_.Body.Get(person_.Body.GenitalsBodyPart)
					.RemoveForcedTrigger(
						receiver_.Person.PersonIndex, receiver_.Type);
			}

			SetZoneEnabled(false);

			running_ = false;
			receiver_ = null;
			anim_ = Animations.None;
		}

		private void SetZoneEnabled(bool b)
		{
			person_.Excitement.GetSource(SS.Genitals).EnabledForOthers = b;
			if (receiver_ != null)
				receiver_.Person.Excitement.GetSource(SS.Genitals).EnabledForOthers = b;
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
						anim_, new AnimationContext(receiver_?.Person));
				}
			}
		}

		private BodyPart FindPenetrationReceiver()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				if (person_.Status.PenetratedBy(p) || p.Status.PenetratedBy(person_))
					return p.Body.Get(p.Body.GenitalsBodyPart);
			}

			return null;
		}

		private static int[] frottageParts_ = new int[]
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
