namespace Cue
{
	class HandEvent : BasicEvent
	{
		private const float MaxDistanceToStart = 0.09f;
		private const float CheckTargetsInterval = 2;
		private const float AutoStartDistance = 0.05f;
		private const float StopDistance = 0.05f;

		private bool active_ = false;
		private float checkElapsed_ = CheckTargetsInterval;

		private Person leftTarget_ = null;
		private bool leftGroped_ = false;
		private BodyPartLock[] leftSourceLock_ = null;
		private BodyPartLock[] leftTargetLock_ = null;
		private int leftAnim_ = Animations.None;
		private bool leftForcedTrigger_ = false;
		private bool leftWasGrabbed_ = false;

		private Person rightTarget_ = null;
		private bool rightGroped_ = false;
		private BodyPartLock[] rightSourceLock_ = null;
		private BodyPartLock[] rightTargetLock_ = null;
		private int rightAnim_ = Animations.None;
		private bool rightForcedTrigger_ = false;
		private bool rightWasGrabbed_ = false;


		public HandEvent()
			: base("hand")
		{
		}

		public override string[] Debug()
		{
			return new string[]
			{
				$"active         {active_}",
				$"elapsed        {checkElapsed_:0.00}",
				$"left target    {leftTarget_}",
				$"left groped    {leftGroped_}",
				$"left src lock  {leftSourceLock_?.ToString() ?? ""}",
				$"left tar lock  {leftTargetLock_?.ToString() ?? ""}",
				$"right target   {rightTarget_}",
				$"right groped   {rightGroped_}",
				$"right src lock {rightSourceLock_?.ToString() ?? ""}",
				$"right tar lock {rightTargetLock_?.ToString() ?? ""}"
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
				if (CheckAutoStart())
					return;

				UnlockLeft();
				UnlockRight();
				return;
			}

			if (!CheckAnim())
				Active = false;

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

		private bool CheckAutoStart()
		{
			if (!Cue.Instance.Options.AutoHands)
				return false;

			bool check = false;

			var left = person_.Body.Get(BP.LeftHand);
			var right = person_.Body.Get(BP.RightHand);

			{
				if (left.Grabbed && !leftWasGrabbed_)
				{
					leftWasGrabbed_ = true;
				}
				else if (!left.Grabbed && leftWasGrabbed_)
				{
					leftWasGrabbed_ = false;
					check = true;
				}
			}

			{
				if (right.Grabbed && !rightWasGrabbed_)
				{
					rightWasGrabbed_ = true;
				}
				else if (!right.Grabbed && rightWasGrabbed_)
				{
					rightWasGrabbed_ = false;
					check = true;
				}
			}


			if (!check)
				return false;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				var g = p.Body.Get(p.Body.GenitalsBodyPart);
				BodyPart bp = null;

				float d = g.DistanceToSurface(left);
				if (d < AutoStartDistance)
				{
					bp = left;
				}
				else
				{
					d = g.DistanceToSurface(right);
					if (d < AutoStartDistance)
						bp = right;
				}

				if (bp != null)
				{
					Log.Info($"autostart: activating with {bp} and {g}, d={d}");
					Active = true;
					return true;
				}
			}

			return false;
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

				leftTarget_.Homing.LeftHand = false;

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

				rightTarget_.Homing.RightHand = false;

				rightGroped_ = false;
				rightTarget_ = null;
			}
		}

		private void UnlockLeft()
		{
			if (leftSourceLock_ != null)
			{
				for (int i = 0; i < leftSourceLock_.Length; ++i)
					leftSourceLock_[i].Unlock();

				leftSourceLock_ = null;
			}

			if (leftTargetLock_ != null)
			{
				for (int i = 0; i < leftTargetLock_.Length; ++i)
					leftTargetLock_[i].Unlock();

				leftTargetLock_ = null;
			}
		}

		private void UnlockRight()
		{
			if (rightSourceLock_ != null)
			{
				for (int i = 0; i < rightSourceLock_.Length; ++i)
					rightSourceLock_[i].Unlock();

				rightSourceLock_ = null;
			}

			if (rightTargetLock_ != null)
			{
				for (int i = 0; i < rightTargetLock_.Length; ++i)
					rightTargetLock_[i].Unlock();

				rightTargetLock_ = null;
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

			if (LockLeft("double hj", left) && LockRight("double hj", right))
			{
				leftTarget_ = left;
				leftAnim_ = Animations.HandjobBoth;

				rightTarget_ = right;
				rightAnim_ = Animations.None;

				SetZoneEnabled(left, true);
				SetZoneEnabled(right, true);

				if (left == right)
				{
					// just one hand
					left.Homing.LeftHand = true;
				}
				else
				{
					left.Homing.LeftHand = true;
					right.Homing.RightHand = true;
				}

				if (leftTarget_.Body.PenisSensitive)
				{
					leftTarget_.Body.Get(leftTarget_.Body.GenitalsBodyPart)
						.AddForcedTrigger(person_.PersonIndex, BP.LeftHand);

					leftForcedTrigger_ = true;
				}

				if (leftTarget_ != rightTarget_ && rightTarget_.Body.PenisSensitive)
				{
					rightTarget_.Body.Get(rightTarget_.Body.GenitalsBodyPart)
						.AddForcedTrigger(person_.PersonIndex, BP.RightHand);

					rightForcedTrigger_ = true;
				}
			}
			else
			{
				Log.Info("failed to double hj, can't lock");
			}

		}

		private void StartLeftHJ(Person target)
		{
			Log.Info($"left hj {target?.ID}");

			if (LockLeft("left hj", target))
			{
				leftTarget_ = target;
				leftAnim_ = Animations.HandjobLeft;
				SetZoneEnabled(target, true);

				target.Homing.LeftHand = true;

				if (leftTarget_.Body.PenisSensitive)
				{
					leftTarget_.Body.Get(leftTarget_.Body.GenitalsBodyPart)
						.AddForcedTrigger(person_.PersonIndex, BP.LeftHand);

					leftForcedTrigger_ = true;
				}
			}
			else
			{
				Log.Info("failed to start left hj, can't lock");
			}
		}

		private void StartLeftFinger(Person target)
		{
			Log.Info($"left finger with {target?.ID}");

			if (LockLeft("left fingering", target))
			{
				leftTarget_ = target;
				leftAnim_ = Animations.LeftFinger;
				leftGroped_ = true;

				SetZoneEnabled(target, true);

				leftTarget_.Body.Get(leftTarget_.Body.GenitalsBodyPart)
					.AddForcedTrigger(person_.PersonIndex, BP.LeftHand);

				leftForcedTrigger_ = true;
			}
			else
			{
				Log.Info("failed to start left fingering, can't lock");
			}
		}

		private void StartRightHJ(Person target)
		{
			Log.Info($"right hj {target?.ID}");

			if (LockRight("right hj", target))
			{
				rightTarget_ = target;
				rightAnim_ = Animations.HandjobRight;
				SetZoneEnabled(target, true);

				target.Homing.RightHand = true;

				if (rightTarget_.Body.PenisSensitive)
				{
					rightTarget_.Body.Get(rightTarget_.Body.GenitalsBodyPart)
						.AddForcedTrigger(person_.PersonIndex, BP.RightHand);

					rightForcedTrigger_ = true;
				}
			}
			else
			{
				Log.Info("failed to start right hj, can't lock");
			}
		}

		private void StartRightFinger(Person target)
		{
			Log.Info($"right finger {target?.ID}");

			if (LockRight("right fingering", target))
			{
				rightTarget_ = target;
				rightAnim_ = Animations.RightFinger;
				rightGroped_ = true;

				SetZoneEnabled(target, true);

				rightTarget_.Body.Get(rightTarget_.Body.GenitalsBodyPart)
					.AddForcedTrigger(person_.PersonIndex, BP.RightHand);

				rightForcedTrigger_ = true;
			}
			else
			{
				Log.Info("failed to start right finger, can't lock");
			}
		}

		private bool CheckAnim()
		{
			if (!CheckAnim(leftAnim_, leftTarget_, leftSourceLock_))
				return false;

			if (!CheckAnim(rightAnim_, rightTarget_, rightSourceLock_))
				return false;

			return true;
		}

		private bool CheckAnim(int anim, Person target, BodyPartLock[] locks)
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
						if (!person_.Animator.PlayType(
								anim, new AnimationContext(target, locks[0].Key)))
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		private void SetZoneEnabled(Person target, bool b)
		{
			if (b)
				target.Excitement.GetSource(SS.Genitals).AddEnabledForOthers();
			else
				target.Excitement.GetSource(SS.Genitals).RemoveEnabledForOthers();
		}

		private bool LockLeft(string why, Person target)
		{
			leftSourceLock_ = BodyPartLock.LockMany(
				person_,
				new int[] { BP.LeftArm, BP.LeftForearm, BP.LeftHand },
				BodyPartLock.Anim, why, BodyPartLock.Strong);

			leftTargetLock_ = BodyPartLock.LockMany(
				target,
				new int[] { BP.Hips },
				BodyPartLock.Anim, why, BodyPartLock.Strong);

			if (leftSourceLock_ == null || leftTargetLock_ == null)
			{
				UnlockLeft();
				return false;
			}

			return true;
		}

		private bool LockRight(string why, Person target)
		{
			rightSourceLock_ = BodyPartLock.LockMany(
				person_,
				new int[] { BP.RightArm, BP.RightForearm, BP.RightHand },
				BodyPartLock.Anim, why, BodyPartLock.Strong);

			rightTargetLock_ = BodyPartLock.LockMany(
				target,
				new int[] { BP.Hips },
				BodyPartLock.Anim, why, BodyPartLock.Strong);

			if (rightSourceLock_ == null || rightTargetLock_ == null)
			{
				UnlockLeft();
				return false;
			}

			return true;
		}

		private BodyPart FindTarget(int handPart)
		{
			var hand = person_.Body.Get(handPart);

			BodyPart tentative = null;
			Log.Info($"finding target for {hand}");

			foreach (var p in Cue.Instance.ActivePersons)
			{
				var g = p.Body.Get(p.Body.GenitalsBodyPart);
				var d = hand.DistanceToSurface(g);

				if (d > MaxDistanceToStart)
				{
					Log.Verbose($"{g} too far");
					continue;
				}

				if (BetterTarget(tentative, g))
				{
					Log.Verbose($"{g} better target");
					tentative = g;
				}
			}

			return tentative;
		}

		private bool BetterTarget(BodyPart tentative, BodyPart check)
		{
			if (tentative == null)
			{
				// first
				Log.Verbose($"BetterTarget: {check} is first");
				return true;
			}

			if (tentative.Person == person_ && check.Person != person_)
			{
				Log.Verbose($"BetterTarget: tentative {tentative} is self, {check} is not");

				// prioritize others that are not penetrating
				if (check.Person.Status.Penetrating())
				{
					Log.Verbose($"BetterTarget: but {check} is penetrating");
					return false;
				}
				else
				{
					Log.Verbose($"BetterTarget: and {check} is not penetrating");
					return true;
				}
			}

			if (tentative.Person.Status.Penetrating() &&
				!check.Person.Status.Penetrating())
			{
				// prioritize genitals that are not currently
				// penetrating
				Log.Verbose($"BetterTarget: tentative {tentative} is penetrating, {check} is not");
				return true;
			}

			Log.Verbose($"BetterTarget: {tentative} still better than {check}");

			return false;
		}
	}
}
