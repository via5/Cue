namespace Cue
{
	class SexEvent : BasicEvent
	{
		private const int NotPenetrated = 0;
		private const int TentativePenetration = 1;
		private const int Penetrated = 2;

		private const float TentativeTime = 0.2f;
		private const float Cooldown = 30;

		public const int NoState = 0;
		public const int PlayState = 1;

		private Person receiver_ = null;
		private bool active_ = false;
		private bool running_ = false;

		private float elapsedTentative_ = 10000;
		private float elapsedNotPenetrated_ = 10000;
		private int penetration_ = NotPenetrated;


		public SexEvent(Person p)
			: base("sex", p)
		{
		}

		public bool Active
		{
			get { return active_; }
			set { active_ = value; }
		}

		public override void Update(float s)
		{
			CheckThrust(s);
			CheckPenetration(s);
		}

		private void CheckThrust(float s)
		{
			if (active_)
			{
				if (!running_)
				{
					receiver_ = FindReceiver();

					if (receiver_ == null && person_.Body.HasPenis)
					{
						log_.Info($"no valid receiver");
						active_ = false;
						return;

					}
					else if (receiver_ != null)
					{
						log_.Info($"starting sex with {receiver_.ID}");

						person_.Clothing.GenitalsVisible = true;
						receiver_.Clothing.GenitalsVisible = true;

						person_.Body.Get(person_.Body.GenitalsBodyPart)
							.AddForcedTrigger(
								receiver_.PersonIndex,
								receiver_.Body.GenitalsBodyPart);
					}
					else
					{
						log_.Info($"starting frottage");

						person_.Body.Get(person_.Body.GenitalsBodyPart)
							.AddForcedTrigger(-1, -1);
					}

					person_.Atom.SetBodyDamping(Sys.BodyDamping.Sex);

					running_ = true;
				}

				CheckAnim();
			}
			else
			{
				if (running_)
				{
					log_.Verbose($"thrust: stopping");
					person_.Animator.StopType(Animations.Sex);

					if (receiver_ != null)
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
				}
			}
		}

		private void CheckAnim()
		{
			int state = person_.Animator.PlayingStatus(Animations.Sex);

			if (state == Animator.Playing)
			{
				if (person_.Mood.State == Mood.OrgasmState ||
					(receiver_ != null && receiver_.Mood.State == Mood.OrgasmState))
				{
					person_.Animator.StopType(Animations.Sex);
				}
			}
			else if (state == Animator.NotPlaying)
			{
				if (person_.Mood.State == Mood.NormalState &&
					(receiver_ == null || receiver_.Mood.State == Mood.NormalState))
				{
					person_.Animator.PlayType(
						Animations.Sex, new AnimationContext(receiver_));
				}
			}
		}

		private void CheckPenetration(float s)
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

		private Person FindReceiver()
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
	}
}
