namespace Cue
{
	class MouthEvent : BasicEvent
	{
		private const float MaxDistanceToStart = 0.4f;
		private const float CheckTargetsInterval = 2;

		private bool active_ = false;
		private float checkElapsed_ = CheckTargetsInterval;
		private BodyPartLock mouthLock_ = null;

		private Person bjTarget_ = null;
		private BodyPartLock[] bjLocks_ = null;


		public MouthEvent(Person p)
			: base("mouth", p)
		{
		}

		public override string[] Debug()
		{
			return new string[]
			{
				$"active     {active_}",
				$"elapsed    {checkElapsed_:0.00}",
				$"mouthLock  {mouthLock_}",
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

		private void Stop()
		{
			if (bjLocks_ != null)
			{
				for (int i = 0; i < bjLocks_.Length; ++i)
					bjLocks_[i].Unlock();

				bjLocks_ = null;
			}

			bjTarget_ = null;
			person_.Blowjob.Stop();
		}

		public override void Update(float s)
		{
			CheckSuckFinger();

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

		private void CheckSuckFinger()
		{
			var mouthTriggered = person_.Body.Get(BP.Mouth).Triggered;
			var head = person_.Body.Get(BP.Head);

			if (mouthLock_ == null && mouthTriggered)
			{
				mouthLock_ = head.Lock(BodyPartLock.Morph, "SuckFinger");

				if (mouthLock_ != null)
				{
					person_.Animator.PlayType(
						Animations.Suck, new AnimationContext(mouthLock_.Key));
				}
			}
			else if (mouthLock_ != null && !mouthTriggered)
			{
				mouthLock_.Unlock();
				mouthLock_ = null;
				person_.Animator.StopType(Animations.Suck);
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

			bjLocks_ = person_.Body.LockMany(
				new int[] { BP.Head, BP.Lips, BP.Mouth, BP.Eyes },
				BodyPartLock.Anim, "bj");

			if (bjLocks_ != null)
			{
				bjTarget_ = t;
				person_.Blowjob.Start(t);
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

				Cue.LogInfo($"{person_.ID} {p.ID} {d}");

				if (d < MaxDistanceToStart)
					return p;
			}

			return null;
		}
	}
}
