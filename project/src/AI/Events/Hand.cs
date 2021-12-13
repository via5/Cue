namespace Cue
{
	class HandEvent : BasicEvent
	{
		private const float MaxDistanceToStart = 0.06f;
		private const float CheckTargetsInterval = 2;

		private bool active_ = false;
		private float checkElapsed_ = CheckTargetsInterval;

		private Person leftTarget_ = null;
		private bool leftGroped_ = false;
		private BodyPartLock[] leftLock_ = null;
		private int leftAnim_ = Animations.None;
		private bool leftForcedTrigger_ = false;

		private Person rightTarget_ = null;
		private bool rightGroped_ = false;
		private BodyPartLock[] rightLock_ = null;
		private int rightAnim_ = Animations.None;
		private bool rightForcedTrigger_ = false;


		public HandEvent()
			: base("hand")
		{
		}

		public override string[] Debug()
		{
			return new string[]
			{
				$"active        {active_}",
				$"elapsed       {checkElapsed_:0.00}",
				$"left target   {leftTarget_}",
				$"left groped   {leftGroped_}",
				$"left lock     {leftLock_?.ToString() ?? ""}",
				$"right target  {rightTarget_}",
				$"right groped  {rightGroped_}",
				$"right lock    {rightLock_?.ToString() ?? ""}"
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

		public Person LeftTarget
		{
			get { return leftTarget_; }
		}

		public Person RightTarget
		{
			get { return rightTarget_; }
		}

		public override void Update(float s)
		{
			if (!active_)
			{
				UnlockLeft();
				UnlockRight();
				return;
			}

			CheckAnim();

			checkElapsed_ += s;
			if (checkElapsed_ >= CheckTargetsInterval)
			{
				checkElapsed_ = 0;
				Check();

				if (leftTarget_ == null)
					UnlockLeft();

				if (rightTarget_ == null)
					UnlockRight();

				if (leftTarget_ == null && rightTarget_ == null)
					active_ = false;
			}
		}

		private void Stop()
		{
			if (leftAnim_ != Animations.None)
			{
				person_.Animator.StopType(leftAnim_);
				leftAnim_ = Animations.None;
			}

			if (rightAnim_ != Animations.None)
			{
				person_.Animator.StopType(rightAnim_);
				rightAnim_ = Animations.None;
			}

			if (leftTarget_ != null)
			{
				if (leftForcedTrigger_)
				{
					leftTarget_.Body.Get(leftTarget_.Body.GenitalsBodyPart)
						.RemoveForcedTrigger(person_.PersonIndex, BP.LeftHand);

					leftForcedTrigger_ = false;
				}

				UnlockLeft();
				SetZoneEnabled(leftTarget_, false);

				leftGroped_ = false;
				leftTarget_ = null;
			}

			if (rightTarget_ != null)
			{
				if (rightForcedTrigger_)
				{
					rightTarget_.Body.Get(rightTarget_.Body.GenitalsBodyPart)
						.RemoveForcedTrigger(person_.PersonIndex, BP.RightHand);

				}

				UnlockRight();
				SetZoneEnabled(rightTarget_, false);

				rightGroped_ = false;
				rightTarget_ = null;
			}
		}

		private void UnlockLeft()
		{
			if (leftLock_ != null)
			{
				for (int i = 0; i < leftLock_.Length; ++i)
					leftLock_[i].Unlock();

				leftLock_ = null;
			}
		}

		private void UnlockRight()
		{
			if (rightLock_ != null)
			{
				for (int i = 0; i < rightLock_.Length; ++i)
					rightLock_[i].Unlock();

				rightLock_ = null;
			}
		}

		private void Check()
		{
			// todo, make it dynamic
			if (leftTarget_ != null || rightTarget_ != null)
				return;

			var rightTarget = FindTarget(BP.RightHand);
			var leftTarget = FindTarget(BP.LeftHand);

			if (leftTarget == null && rightTarget == null)
			{
				Log.Info("no target");
				return;
			}

			if ((rightTarget != null && rightTarget.Type == BP.Penis) &&
				(leftTarget != null && leftTarget.Type == BP.Penis) &&
				(leftTarget.Person == rightTarget.Person))
			{
				StartDoubleHJ(leftTarget.Person, rightTarget.Person);
			}
			else
			{
				if (leftTarget != null)
				{
					if (leftTarget.Type == BP.Penis)
						StartLeftHJ(leftTarget.Person);
					else if (leftTarget.Type == BP.Labia)
						StartLeftFinger(leftTarget.Person);
				}

				if (rightTarget != null)
				{
					if (rightTarget.Type == BP.Penis)
						StartRightHJ(rightTarget.Person);
					else if (rightTarget.Type == BP.Labia)
						StartRightFinger(rightTarget.Person);
				}
			}
		}

		private void StartDoubleHJ(Person left, Person right)
		{
			Log.Info($"double hj {left?.ID}");

			if (LockBoth("double hj"))
			{
				leftTarget_ = left;
				leftAnim_ = Animations.HandjobBoth;

				rightTarget_ = right;
				rightAnim_ = Animations.None;

				SetZoneEnabled(left, true);
				SetZoneEnabled(right, true);

				if (leftTarget_.Body.PenisSensitive)
				{
					leftTarget_.Body.Get(leftTarget_.Body.GenitalsBodyPart)
						.AddForcedTrigger(person_.PersonIndex, BP.LeftHand);

					leftForcedTrigger_ = true;
				}

				if (leftTarget_ != rightTarget_ && rightTarget_.Body.PenisSensitive)
				{
					rightTarget_.Body.Get(rightTarget_.Body.GenitalsBodyPart)
						.AddForcedTrigger(person_.PersonIndex, BP.LeftHand);

					rightForcedTrigger_ = true;
				}
			}
		}

		private void StartLeftHJ(Person target)
		{
			Log.Info($"left hj {target?.ID}");

			if (LockLeft("left hj"))
			{
				leftTarget_ = target;
				leftAnim_ = Animations.HandjobLeft;
				SetZoneEnabled(target, true);

				if (leftTarget_.Body.PenisSensitive)
				{
					leftTarget_.Body.Get(leftTarget_.Body.GenitalsBodyPart)
						.AddForcedTrigger(person_.PersonIndex, BP.LeftHand);

					leftForcedTrigger_ = true;
				}
			}
		}

		private void StartLeftFinger(Person target)
		{
			Log.Info($"left finger with {target?.ID}");

			if (LockLeft("left fingering"))
			{
				leftTarget_ = target;
				leftAnim_ = Animations.LeftFinger;
				leftGroped_ = true;

				SetZoneEnabled(target, true);

				leftTarget_.Body.Get(leftTarget_.Body.GenitalsBodyPart)
					.AddForcedTrigger(person_.PersonIndex, BP.LeftHand);

				leftForcedTrigger_ = true;
			}
		}

		private void StartRightHJ(Person target)
		{
			Log.Info($"right hj {target?.ID}");

			if (LockRight("right hj"))
			{
				rightTarget_ = target;
				rightAnim_ = Animations.HandjobRight;
				SetZoneEnabled(target, true);

				if (rightTarget_.Body.PenisSensitive)
				{
					rightTarget_.Body.Get(rightTarget_.Body.GenitalsBodyPart)
						.AddForcedTrigger(person_.PersonIndex, BP.LeftHand);

					rightForcedTrigger_ = true;
				}
			}
		}

		private void StartRightFinger(Person target)
		{
			Log.Info($"right finger {target?.ID}");

			if (LockRight("right fingering"))
			{
				rightTarget_ = target;
				rightAnim_ = Animations.RightFinger;
				rightGroped_ = true;

				SetZoneEnabled(target, true);

				rightTarget_.Body.Get(rightTarget_.Body.GenitalsBodyPart)
					.AddForcedTrigger(person_.PersonIndex, BP.RightHand);

				rightForcedTrigger_ = true;
			}
		}

		private void CheckAnim()
		{
			CheckAnim(leftAnim_, leftTarget_, leftLock_);
			CheckAnim(rightAnim_, rightTarget_, rightLock_);
		}

		private void CheckAnim(int anim, Person target, BodyPartLock[] locks)
		{
			if (anim != Animations.None)
			{
				int state = person_.Animator.PlayingStatus(anim);

				if (state == Animator.Playing)
				{
					if (Mood.ShouldStopSexAnimation(person_, target))
						person_.Animator.StopType(anim);
				}
				else if (state == Animator.NotPlaying)
				{
					if (Mood.CanStartSexAnimation(person_, target))
					{
						person_.Animator.PlayType(
							anim, new AnimationContext(target, locks[0].Key));
					}
				}
			}
		}

		private void SetZoneEnabled(Person target, bool b)
		{
			target.Excitement.GetSource(SS.Genitals).EnabledForOthers = b;
		}

		private bool LockBoth(string why)
		{
			return LockLeft(why) && LockRight(why);
		}

		private bool LockLeft(string why)
		{
			leftLock_ = BodyPartLock.LockMany(
				person_,
				new int[] { BP.LeftArm, BP.LeftForearm, BP.LeftHand },
				BodyPartLock.Anim, why, BodyPartLock.Strong);

			return (leftLock_ != null);
		}

		private bool LockRight(string why)
		{
			rightLock_ = BodyPartLock.LockMany(
				person_,
				new int[] { BP.RightArm, BP.RightForearm, BP.RightHand },
				BodyPartLock.Anim, why, BodyPartLock.Strong);

			return (rightLock_ != null);
		}

		private BodyPart FindTarget(int handPart)
		{
			var hand = person_.Body.Get(handPart);

			BodyPart tentative = null;
			float tentativeD = float.MaxValue;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				var g = p.Body.Get(p.Body.GenitalsBodyPart);
				var d = hand.DistanceToSurface(g);

				if (d > MaxDistanceToStart)
					continue;

				if (d < tentativeD)
				{
					if (BetterTarget(tentative, g))
					{
						tentative = g;
						tentativeD = d;
					}
				}
			}

			return tentative;
		}

		private bool BetterTarget(BodyPart tentative, BodyPart check)
		{
			if (tentative == null)
			{
				// first
				return true;
			}

			if (tentative.Person != person_)
			{
				// prioritize others
				return true;
			}

			if (!tentative.Person.Status.Penetrating())
			{
				// prioritize genitals that are not currently
				// penetrating
				return true;
			}

			return false;
		}
	}
}
