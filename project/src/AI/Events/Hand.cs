namespace Cue
{
	class HandEvent : BasicEvent
	{
		private const float MaxDistanceToStart = 0.2f;
		private const float CheckTargetsInterval = 2;

		private bool active_ = false;
		private float checkElapsed_ = CheckTargetsInterval;

		private Person leftTarget_ = null;
		private bool leftGroped_ = false;
		private BodyPartLock leftLock_ = null;

		private Person rightTarget_ = null;
		private bool rightGroped_ = false;
		private BodyPartLock rightLock_ = null;


		public HandEvent(Person p)
			: base("hand", p)
		{
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
			person_.Handjob.Stop();
			person_.Animator.StopType(Animations.RightFinger);
			person_.Animator.StopType(Animations.LeftFinger);

			if (leftTarget_ != null)
			{
				if (leftGroped_)
				{
					leftTarget_.Body.Get(leftTarget_.Body.GenitalsBodyPart)
						.RemoveForcedTrigger(person_.PersonIndex, BP.LeftHand);

					leftGroped_ = false;
				}

				if (leftLock_ != null)
				{
					leftLock_.Unlock();
					leftLock_ = null;
				}

				leftTarget_ = null;
			}

			if (rightTarget_ != null)
			{
				if (rightGroped_)
				{
					rightTarget_.Body.Get(rightTarget_.Body.GenitalsBodyPart)
						.RemoveForcedTrigger(person_.PersonIndex, BP.RightHand);

					rightGroped_ = false;
				}

				if (rightLock_ != null)
				{
					rightLock_.Unlock();
					rightLock_ = null;
				}

				rightTarget_ = null;
			}
		}

		public override void Update(float s)
		{
			if (!active_)
				return;

			checkElapsed_ += s;
			if (checkElapsed_ >= CheckTargetsInterval)
			{
				checkElapsed_ = 0;
				Check();

				if (leftTarget_ == null && rightTarget_ == null)
					active_ = false;
			}
		}

		private void Check()
		{
			// todo, make it dynamic
			if (leftTarget_ != null || rightTarget_ != null)
				return;

			var rightTarget = FindTarget(BP.RightHand);
			var leftTarget = FindTarget(BP.LeftHand);

			if ((rightTarget != null && rightTarget.Type == BP.Penis) &&
				(leftTarget != null && leftTarget.Type == BP.Penis) &&
				(leftTarget.Person == rightTarget.Person))
			{
				if (person_.Handjob.StartBoth(leftTarget.Person))
				{
					Cue.LogError($"double hj");

					leftLock_ = person_.Body.Get(BP.LeftHand).Lock(
						BodyPartLock.Anim);

					if (leftLock_ != null)
						leftTarget_ = leftTarget.Person;

					rightLock_ = person_.Body.Get(BP.RightHand).Lock(
						BodyPartLock.Anim);

					if (rightLock_ != null)
						rightTarget_ = rightTarget.Person;
				}
			}
			else
			{
				if (leftTarget != null)
				{
					if (leftTarget.Type == BP.Penis)
					{
						if (person_.Handjob.StartLeft(leftTarget.Person))
						{
							Cue.LogError($"left hj");

							leftLock_ = person_.Body.Get(BP.LeftHand).Lock(
								BodyPartLock.Anim);

							if (leftLock_ != null)
								leftTarget_ = leftTarget.Person;
						}
					}
					else if (leftTarget.Type == BP.Labia)
					{
						if (person_.Animator.PlayType(Animations.LeftFinger, leftTarget.Person))
						{
							Cue.LogError($"left finger");

							leftLock_ = person_.Body.Get(BP.LeftHand).Lock(
								BodyPartLock.Anim);

							if (leftLock_ != null)
							{
								leftTarget_ = leftTarget.Person;
								leftGroped_ = true;
								leftTarget_.Body.Get(leftTarget_.Body.GenitalsBodyPart)
									.AddForcedTrigger(person_.PersonIndex, BP.LeftHand);
							}
						}
					}
				}

				if (rightTarget != null)
				{
					if (rightTarget.Type == BP.Penis)
					{
						if (person_.Handjob.StartRight(rightTarget.Person))
						{
							Cue.LogError($"right hj");

							rightLock_ = person_.Body.Get(BP.RightHand).Lock(
								BodyPartLock.Anim);

							if (rightLock_ != null)
								rightTarget_ = rightTarget.Person;
						}
					}
					else if (rightTarget.Type == BP.Labia)
					{
						if (person_.Animator.PlayType(Animations.RightFinger, rightTarget.Person))
						{
							Cue.LogError($"right finger");

							rightLock_ = person_.Body.Get(BP.RightHand).Lock(
								BodyPartLock.Anim);

							if (rightLock_ != null)
							{
								rightTarget_ = rightTarget.Person;
								rightGroped_ = true;

								rightTarget_.Body.Get(rightTarget_.Body.GenitalsBodyPart)
									.AddForcedTrigger(person_.PersonIndex, BP.RightHand);
							}
						}
					}
				}
			}
		}

		private BodyPart FindTarget(int handPart)
		{
			var hand = person_.Body.Get(handPart);

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				var g = p.Body.Get(p.Body.GenitalsBodyPart);
				var d = Vector3.Distance(hand.Position, g.Position);

				if (d < MaxDistanceToStart)
					return g;
			}

			return null;
		}
	}
}
