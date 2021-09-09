namespace Cue
{
	class SexEvent : BasicEvent
	{
		public const int NoState = 0;
		public const int PlayState = 1;

		private Person receiver_ = null;
		private bool active_ = false;
		private bool running_ = false;

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
			if (!active_)
			{
				if (running_)
				{
					person_.Animator.StopType(Animation.SexType);
					running_ = false;
				}

				return;
			}

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
