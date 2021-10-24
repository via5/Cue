namespace Cue
{
	class SexEvent : BasicEvent
	{
		private const int NotPenetrated = 0;
		private const int TentativePenetration = 1;
		private const int Penetrated = 2;

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

					if (receiver_ == null)
						log_.Info($"no valid receiver");
					else
						log_.Info($"starting sex, receiver={receiver_.ID}");

					if (receiver_ != null)
					{
						person_.Clothing.GenitalsVisible = true;
						receiver_.Clothing.GenitalsVisible = true;

						person_.Body.Get(person_.Body.GenitalsBodyPart)
							.AddForcedTrigger(
								receiver_.PersonIndex,
								receiver_.Body.GenitalsBodyPart);
					}
					else
					{
						// frottage
						person_.Body.Get(person_.Body.GenitalsBodyPart)
							.AddForcedTrigger(-1, -1);
					}

					person_.Atom.SetBodyDamping(Sys.BodyDamping.Sex);

					running_ = true;
				}

				if (!person_.Animator.IsPlayingType(Animations.Sex))
				{
					if (person_.Mood.State == Mood.NormalState)
					{
						person_.Animator.PlayType(
							Animations.Sex, new AnimationContext(receiver_));
					}
				}
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
						Cue.LogVerbose("tentative in");
					}
					else
					{
						elapsedNotPenetrated_ += s;
					}

					break;
				}

				case TentativePenetration:
				{
					var p = person_.Body.PenetratedBy();

					if (p != null)
					{
						elapsedTentative_ += s;

						if (elapsedTentative_ > 1)
						{
							penetration_ = Penetrated;
							OnIn(p);
						}
					}
					else
					{
						Cue.LogVerbose("tentative out");
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

		private void OnIn(Person by)
		{
			Cue.LogVerbose("in");

			//if (elapsedNotPenetrated_ > 10)
			{
				elapsedNotPenetrated_ = 0;
			//	person_.Animator.PlayType(Animations.Penetrated);
			//	Cue.LogError("emote");
			}
		}

		private void OnOut()
		{
			Cue.LogVerbose("out");
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
