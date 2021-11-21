namespace Cue
{
	class MouthEvent : BasicEvent
	{
		private const float MaxDistanceToStart = 0.4f;
		private const float CheckTargetsInterval = 2;

		private bool active_ = false;
		private float checkElapsed_ = CheckTargetsInterval;

		private Person bjTarget_ = null;
		private BodyPartLock[] bjLocks_ = null;


		public MouthEvent()
			: base("mouth")
		{
		}

		public override string[] Debug()
		{
			return new string[]
			{
				$"active     {active_}",
				$"elapsed    {checkElapsed_:0.00}",
				$"bjTarget   {bjTarget_}",
				$"bjLocks    {bjLocks_?.ToString() ?? ""}"
			};
		}

		public bool Active
		{
			get
			{
				return active_;
			}

			set
			{
				if (!active_ && value)
					checkElapsed_ = CheckTargetsInterval;
				else if (active_ && !value)
					Stop();

				active_ = value;
			}
		}

		public Person Target
		{
			get { return bjTarget_; }
		}

		private void Stop()
		{
			UnlockBJ();
			bjTarget_ = null;
			person_.Animator.StopType(Animations.Blowjob);
		}

		private void UnlockBJ()
		{
			if (bjLocks_ != null)
			{
				for (int i = 0; i < bjLocks_.Length; ++i)
					bjLocks_[i].Unlock();

				bjLocks_ = null;
			}
		}

		public override void Update(float s)
		{
			if (active_)
			{
				checkElapsed_ += s;
				if (checkElapsed_ >= CheckTargetsInterval)
				{
					checkElapsed_ = 0;
					CheckBJ();

					if (bjTarget_ == null)
						active_ = false;
				}
			}
		}

		private void CheckBJ()
		{
			// todo, make it dynamic
			if (bjTarget_ != null)
				return;

			var t = FindTarget();
			if (t == null)
				return;

			bjLocks_ = BodyPartLock.LockMany(
				person_,
				new int[] { BP.Head, BP.Lips, BP.Mouth, BP.Eyes },
				BodyPartLock.Anim, "bj");

			if (bjLocks_ != null)
			{
				if (!person_.Animator.PlayType(
					Animations.Blowjob,
					new AnimationContext(t, bjLocks_[0].Key)))
				{
					UnlockBJ();
					return;
				}

				bjTarget_ = t;
			}
		}

		private Person FindTarget()
		{
			var head = person_.Body.Get(BP.Head);

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_ || !p.Body.HasPenis)
					continue;

				var g = p.Body.Get(BP.Penis);
				var d = Vector3.Distance(head.Position, g.Position);

				if (d < MaxDistanceToStart)
					return p;
			}

			return null;
		}
	}
}
