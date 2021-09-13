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
				if (receiver_ == null)
				{
					receiver_ = FindReceiver();
					if (receiver_ == null)
					{
						person_.Log.Error($"cannot start sex, no valid receiver");
						active_ = false;
						return;
					}

					log_.Info($"starting sex, receiver={receiver_.ID}");

					person_.Clothing.GenitalsVisible = true;
					receiver_.Clothing.GenitalsVisible = true;
					person_.Atom.SetBodyDamping(Sys.BodyDamping.Sex);

					if (person_.Animator.CanPlayType(Animation.SexType) && person_.Mood.State == Mood.NormalState)
						person_.Animator.PlaySex(person_.State.Current, receiver_);

					running_ = true;
				}
			}
			else
			{
				if (running_)
				{
					person_.Animator.StopType(Animation.SexType);
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
						Cue.LogError("tentative in");
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
						Cue.LogError("tentative out");
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
			Cue.LogError("in");

			if (elapsedNotPenetrated_ > 10)
			{
				elapsedNotPenetrated_ = 0;
				Cue.LogError("emote");
			}
		}

		private void OnOut()
		{
			Cue.LogError("out");
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
