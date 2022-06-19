namespace Cue
{
	class MouthEvent : BasicEvent
	{
		private const float MaxDistanceToStart = 0.4f;
		private const float CheckTargetsInterval = 2;

		private bool active_ = false;
		private float checkElapsed_ = CheckTargetsInterval;

		private Person bjTarget_ = null;
		private BodyPartLock[] sourceLocks_ = null;
		private BodyPartLock[] targetLocks_ = null;
		private bool hasForcedTrigger_ = false;


		public MouthEvent()
			: base("mouth")
		{
		}

		public override string[] Debug()
		{
			return new string[]
			{
				$"active       {active_}",
				$"elapsed      {checkElapsed_:0.00}",
				$"bjTarget     {bjTarget_}",
				$"sourceLocks  {sourceLocks_?.ToString() ?? ""}",
				$"targetLocks  {targetLocks_?.ToString() ?? ""}"
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
			Unlock();

			if (hasForcedTrigger_)
			{
				bjTarget_.Body.Get(bjTarget_.Body.GenitalsBodyPart)
					.RemoveForcedTrigger(person_.PersonIndex, BP.Mouth);

				bjTarget_.Excitement.GetSource(SS.Genitals).RemoveEnabledForOthers();

				hasForcedTrigger_ = false;
			}

			bjTarget_.Homing.Mouth = false;

			bjTarget_ = null;
			person_.Animator.StopType(Animations.Blowjob);
		}

		private void Unlock()
		{
			if (sourceLocks_ != null)
			{
				for (int i = 0; i < sourceLocks_.Length; ++i)
					sourceLocks_[i].Unlock();

				sourceLocks_ = null;
			}

			if (targetLocks_ != null)
			{
				for (int i = 0; i < targetLocks_.Length; ++i)
					targetLocks_[i].Unlock();

				targetLocks_ = null;
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
			{
				Log.Info("no target");
				return;
			}

			sourceLocks_ = BodyPartLock.LockMany(
				person_,
				new int[] { BP.Head, BP.Lips, BP.Mouth, BP.Chest },
				BodyPartLock.Anim, "bj", BodyPartLock.Strong);

			targetLocks_ = BodyPartLock.LockMany(
				t,
				new int[] { BP.Hips },
				BodyPartLock.Anim, "bj", BodyPartLock.Strong);

			if (sourceLocks_ == null || targetLocks_ == null)
			{
				Unlock();
				return;
			}

			if (!person_.Animator.PlayType(
				Animations.Blowjob,
				new AnimationContext(t, sourceLocks_[0].Key)))
			{
				Unlock();
				return;
			}

			if (t.Body.PenisSensitive)
			{
				t.Body.Get(t.Body.GenitalsBodyPart)
					.AddForcedTrigger(person_.PersonIndex, BP.Mouth);

				t.Excitement.GetSource(SS.Genitals).AddEnabledForOthers();

				hasForcedTrigger_ = true;
			}

			t.Homing.Mouth = true;

			bjTarget_ = t;
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
