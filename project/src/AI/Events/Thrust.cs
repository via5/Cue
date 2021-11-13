namespace Cue
{
	class ThrustEvent : BasicEvent
	{
		private const float FrottageDistance = 0.06f;

		private Person receiver_ = null;
		private bool active_ = false;
		private bool running_ = false;
		private int anim_ = Animations.None;

		public ThrustEvent(Person p)
			: base("thrust", p)
		{
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
			else if (person_.Body.HasPenis || receiver_.Body.HasPenis)
			{
				// penetration

				Log.Info($"starting sex with {receiver_.ID}");

				person_.Clothing.GenitalsVisible = true;
				receiver_.Clothing.GenitalsVisible = true;

				person_.Body.Get(person_.Body.GenitalsBodyPart)
					.AddForcedTrigger(
						receiver_.PersonIndex,
						receiver_.Body.GenitalsBodyPart);

				// play the penetrated animation now, don't bother if it can't
				// start; see also Penetrated.OnIn()
				person_.Animator.PlayType(Animations.Penetrated);
				receiver_.Animator.PlayType(Animations.Penetrated);

				anim_ = Animations.Sex;
			}
			else
			{
				// frottage

				Log.Info($"starting frottage with {receiver_.ID}");

				person_.Body.Get(person_.Body.GenitalsBodyPart)
					.AddForcedTrigger(-1, -1);

				anim_ = Animations.Frottage;
			}

			person_.Atom.SetBodyDamping(Sys.BodyDamping.Sex);
			running_ = true;

			return true;
		}

		private void Stop()
		{
			Log.Verbose($"thrust: stopping");
			person_.Animator.StopType(anim_);

			if (receiver_ != null && (person_.Body.HasPenis ||receiver_.Body.HasPenis))
			{
				person_.Body.Get(person_.Body.GenitalsBodyPart)
					.RemoveForcedTrigger(
						receiver_.PersonIndex,
						receiver_.Body.GenitalsBodyPart);
			}
			else
			{
				// frottage
				person_.Body.Get(person_.Body.GenitalsBodyPart)
					.RemoveForcedTrigger(-1, -1);
			}

			running_ = false;
			receiver_ = null;
			anim_ = Animations.None;
		}

		private void CheckAnim()
		{
			int state = person_.Animator.PlayingStatus(anim_);

			if (state == Animator.Playing)
			{
				if (person_.Mood.State == Mood.OrgasmState ||
					(receiver_ != null && receiver_.Mood.State == Mood.OrgasmState))
				{
					person_.Animator.StopType(anim_);
				}
			}
			else if (state == Animator.NotPlaying)
			{
				if (person_.Mood.State == Mood.NormalState &&
					(receiver_ == null || receiver_.Mood.State == Mood.NormalState))
				{
					person_.Animator.PlayType(
						anim_, new AnimationContext(receiver_));
				}
			}
		}

		private Person FindPenetrationReceiver()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				if (person_.Body.PenetratedBy(p) || p.Body.PenetratedBy(person_))
					return p;
			}

			return null;
		}

		private Person FindFrottageReceiver()
		{
			var selfHips = person_.Body.Get(BP.Hips);

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				var otherHips = p.Body.Get(BP.Hips);
				var d = selfHips.DistanceToSurface(otherHips);

				if (d <= FrottageDistance)
					return p;
			}

			return null;
		}
	}
}
