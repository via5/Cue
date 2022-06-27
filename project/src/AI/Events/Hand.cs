namespace Cue
{
	class HandEvent : BasicEvent
	{
		class HandInfo
		{
			public readonly string name;
			public readonly BodyPart bp;
			public readonly AnimationType fingeringAnimType;
			public readonly AnimationType hjAnimType;
			public readonly BodyPartType[] sourceLockTypes;

			public BodyPart targetBodyPart = null;
			public bool groped = false;
			public BodyPartLock[] sourceLock = null;
			public BodyPartLock[] targetLock = null;
			public AnimationType anim = AnimationType.None;
			public bool forcedTrigger = false;
			public bool wasGrabbed = false;

			public HandInfo(
				string name, BodyPart part,
				AnimationType fingeringAnim, AnimationType hjAnim,
				BodyPartType[] sourceLockTypes)
			{
				this.name = name;
				bp = part;
				fingeringAnimType = fingeringAnim;
				hjAnimType = hjAnim;
				this.sourceLockTypes = sourceLockTypes;
			}

			public void Debug(DebugLines debug)
			{
				debug.Add($"{name} target", $"{targetBodyPart} groped={groped} anim={anim}");

				if (sourceLock != null)
				{
					for (int i = 0; i < sourceLock.Length; ++i)
						debug.Add($"{name} srcLock", sourceLock[i].ToString());
				}

				if (targetLock != null)
				{
					for (int i = 0; i < targetLock.Length; ++i)
						debug.Add($"{name} tarLock", targetLock[i].ToString());
				}
			}
		}

		private const float ManualStartDistance = 0.09f;
		private const float AutoStartDistance = 0.02f;

		private bool doManualCheck_ = false;
		private HandInfo left_ = null;
		private HandInfo right_ = null;


		public HandEvent()
			: base("hand")
		{
		}

		protected override void DoInit()
		{
			base.DoInit();

			left_ = new HandInfo(
				"left", person_.Body.Get(BP.LeftHand),
				AnimationType.LeftFinger, AnimationType.HandjobLeft,
				new BodyPartType[] { BP.LeftArm, BP.LeftForearm, BP.LeftHand });

			right_ = new HandInfo(
				"right", person_.Body.Get(BP.RightHand),
				AnimationType.RightFinger, AnimationType.HandjobRight,
				new BodyPartType[] { BP.RightArm, BP.RightForearm, BP.RightHand });
		}

		public override void Debug(DebugLines debug)
		{
			debug.Add("active", $"{Active}");
			left_.Debug(debug);
			right_.Debug(debug);
		}

		public bool Active
		{
			get
			{
				return (left_.targetBodyPart != null || right_.targetBodyPart != null);
			}

			set
			{
				if (value)
					doManualCheck_ = true;
				else
					Stop();
			}
		}

		public Person LeftTarget
		{
			get { return left_.targetBodyPart?.Person; }
		}

		public Person RightTarget
		{
			get { return right_.targetBodyPart?.Person; }
		}

		public override void Update(float s)
		{
			CheckAutoStart();
			CheckAnim();

			if (doManualCheck_)
			{
				doManualCheck_ = false;
				CheckManualStart();
			}
		}

		private void CheckAutoStart()
		{
			if (!Cue.Instance.Options.AutoHands)
				return;

			bool checkLeft = GrabEnded(left_);
			bool checkRight = GrabEnded(right_);

			if (checkLeft || checkRight)
			{
				Log.Info($"checking auto start, left={checkLeft} right={checkRight}");
				Check(AutoStartDistance, checkLeft, checkRight, Animation.StopNoReturn);
			}
		}

		private void CheckManualStart()
		{
			Log.Info("checking manual start");
			Check(ManualStartDistance, true, true);
		}

		private bool TargetsForDouble(BodyPart left, BodyPart right)
		{
			if (left == null || right == null)
				return false;

			if (left.Type != BP.Penis || right.Type != BP.Penis)
				return false;

			return (left.Person == right.Person);
		}

		private void Check(
			float maxDistance, bool canStartLeft, bool canStartRight,
			int stopFlags = Animation.NoStopFlags)
		{
			var leftTarget = FindTarget(left_.bp.Type, maxDistance);
			var rightTarget = FindTarget(right_.bp.Type, maxDistance);

			if (leftTarget == null && rightTarget == null)
			{
				Stop(stopFlags);
				Log.Info("no target");
				return;
			}

			if (TargetsForDouble(leftTarget, rightTarget))
			{
				Log.Info("check found double targets");
				Stop(stopFlags);
				StartDoubleHJ(leftTarget);
			}
			else
			{
				if (left_.anim == AnimationType.HandjobBoth)
				{
					Log.Info("stopping because current is both and one target is gone");
					Stop(stopFlags);
					canStartLeft = true;
					canStartRight = true;
				}

				if (leftTarget == null)
				{
					Log.Info("no left target");
					Stop(left_, stopFlags);
				}
				else
				{
					if (leftTarget == left_.targetBodyPart)
					{
						Log.Info("left target is the same as before");
					}
					else if (canStartLeft)
					{
						Log.Info($"new left target {leftTarget}");

						Stop(left_, stopFlags);

						if (leftTarget.Type == BP.Penis)
							StartHJ(left_, leftTarget);
						else if (leftTarget.Type == BP.Labia)
							StartFinger(left_, leftTarget);
					}
				}

				if (rightTarget == null)
				{
					Log.Info("no right target");
					Stop(right_, stopFlags);
				}
				else
				{
					if (rightTarget == right_.targetBodyPart)
					{
						Log.Info("right target is the same as before");
					}
					else if (canStartRight)
					{
						Log.Info($"right target now {rightTarget}");

						Stop(right_, stopFlags);

						if (rightTarget.Type == BP.Penis)
							StartHJ(right_, rightTarget);
						else if (rightTarget.Type == BP.Labia)
							StartFinger(right_, rightTarget);
					}
				}
			}
		}

		private bool GrabEnded(HandInfo hand)
		{
			if (hand.bp.Grabbed && !hand.wasGrabbed)
			{
				hand.wasGrabbed = true;
			}
			else if (!hand.bp.Grabbed && hand.wasGrabbed)
			{
				hand.wasGrabbed = false;
				return true;
			}

			return false;
		}

		private void Stop(int stopFlags = Animation.NoStopFlags)
		{
			Stop(left_, stopFlags);
			Stop(right_, stopFlags);
		}

		private void Stop(HandInfo hand, int stopFlags = Animation.NoStopFlags)
		{
			Log.Info($"stopping {hand.name}");

			if (hand.anim != AnimationType.None)
			{
				person_.Animator.StopType(hand.anim, stopFlags);
				hand.anim = AnimationType.None;
			}

			if (hand.targetBodyPart != null)
			{
				if (hand.forcedTrigger)
				{
					hand.targetBodyPart.Person.Body.Get(hand.targetBodyPart.Person.Body.GenitalsBodyPart)
						.RemoveForcedTrigger(person_.PersonIndex, hand.bp.Type);

					hand.forcedTrigger = false;
				}

				SetZoneEnabled(hand.targetBodyPart.Person, false);

				if (hand.bp.Type == BP.LeftHand)
					hand.targetBodyPart.Person.Homing.LeftHand = false;
				else
					hand.targetBodyPart.Person.Homing.RightHand = false;

				hand.groped = false;
				hand.targetBodyPart = null;
			}

			Unlock(hand);
		}

		private void Unlock(HandInfo hand)
		{
			if (hand.sourceLock != null)
			{
				for (int i = 0; i < hand.sourceLock.Length; ++i)
					hand.sourceLock[i].Unlock();

				hand.sourceLock = null;
			}

			if (hand.targetLock != null)
			{
				for (int i = 0; i < hand.targetLock.Length; ++i)
					hand.targetLock[i].Unlock();

				hand.targetLock = null;
			}
		}

		private void StartDoubleHJ(BodyPart targetBodyPart)
		{
			Log.Info($"double hj {targetBodyPart}");

			if (LockBoth("double hj", targetBodyPart.Person))
			{
				left_.targetBodyPart = targetBodyPart;
				left_.anim = AnimationType.HandjobBoth;

				right_.targetBodyPart = targetBodyPart;
				right_.anim = AnimationType.None;

				// once for each hand, since Stop() will try to remove it twice
				SetZoneEnabled(targetBodyPart.Person, true);
				SetZoneEnabled(targetBodyPart.Person, true);

				// just one hand
				//target.Homing.LeftHand = true;

				if (targetBodyPart.Person.Body.PenisSensitive)
				{
					targetBodyPart.Person.Body.Get(targetBodyPart.Person.Body.GenitalsBodyPart)
						.AddForcedTrigger(person_.PersonIndex, BP.LeftHand);

					left_.forcedTrigger = true;
				}
			}
			else
			{
				Log.Info("failed to double hj, can't lock");
			}

		}

		private void StartHJ(HandInfo hand, BodyPart targetBodyPart)
		{
			Log.Info($"{hand.name} hj {targetBodyPart}");

			if (Lock("hj", hand, targetBodyPart.Person))
			{
				hand.targetBodyPart = targetBodyPart;
				hand.anim = hand.hjAnimType;
				SetZoneEnabled(targetBodyPart.Person, true);

				targetBodyPart.Person.Homing.LeftHand = true;

				if (hand.targetBodyPart.Person.Body.PenisSensitive)
				{
					hand.targetBodyPart.Person.Body.Get(hand.targetBodyPart.Person.Body.GenitalsBodyPart)
						.AddForcedTrigger(person_.PersonIndex, hand.bp.Type);

					hand.forcedTrigger = true;
				}
			}
			else
			{
				Log.Info($"failed to start {hand.name} hj, can't lock");
			}
		}

		private void StartFinger(HandInfo hand, BodyPart targetBodyPart)
		{
			Log.Info($"{hand.name} finger with {targetBodyPart}");

			if (Lock("fingering", hand, targetBodyPart.Person))
			{
				hand.targetBodyPart = targetBodyPart;
				hand.anim = hand.fingeringAnimType;
				hand.groped = true;

				SetZoneEnabled(targetBodyPart.Person, true);

				hand.targetBodyPart.Person.Body.Get(hand.targetBodyPart.Person.Body.GenitalsBodyPart)
					.AddForcedTrigger(person_.PersonIndex, hand.bp.Type);

				hand.forcedTrigger = true;
			}
			else
			{
				Log.Info($"failed to start {hand.name} fingering, can't lock");
			}
		}

		private void CheckAnim()
		{
			if (!CheckAnim(left_))
				Stop(left_);

			if (!CheckAnim(right_))
				Stop(right_);
		}

		private bool CheckAnim(HandInfo hand)
		{
			if (hand.anim != AnimationType.None)
			{
				int state = person_.Animator.PlayingStatus(hand.anim);

				if (state == Animator.Playing)
				{
					if (Mood.ShouldStopSexAnimation(person_, hand.targetBodyPart.Person))
						person_.Animator.StopType(hand.anim);
				}
				else if (state == Animator.NotPlaying)
				{
					if (Mood.CanStartSexAnimation(person_, hand.targetBodyPart.Person))
					{
						if (!person_.Animator.PlayType(
								hand.anim, new AnimationContext(hand.targetBodyPart.Person, hand.sourceLock[0].Key)))
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
				target.Excitement.GetSource(SS.Genitals).AddEnabledFor(target);
			else
				target.Excitement.GetSource(SS.Genitals).RemoveEnabledFor(target);
		}

		private bool LockBoth(string why, Person target)
		{
			if (!LockSources(why, left_))
				return false;

			if (!LockTargets(why, left_, target))
				return false;

			if (!LockSources(why, right_))
				return false;

			// don't lock targets for right hand, it's the same ones as the
			// left, which would fail because they're locked above

			return true;
		}

		private bool Lock(string why, HandInfo hand, Person target)
		{
			if (!LockSources(why, hand))
				return false;

			if (!LockTargets(why, hand, target))
				return false;

			return true;
		}

		private bool LockSources(string why, HandInfo hand)
		{
			hand.sourceLock = BodyPartLock.LockMany(
				person_, hand.sourceLockTypes,
				BodyPartLock.Anim, why, BodyPartLock.Strong);

			if (hand.sourceLock == null)
			{
				Log.Error($"failed to lock sources for {why}");
				Unlock(hand);
				return false;
			}

			return true;
		}

		private bool LockTargets(string why, HandInfo hand, Person target)
		{
			BodyPartType[] locks;

			// don't lock genitals for females
			if (target.Body.Get(BP.Penis).Exists)
				locks = new BodyPartType[] { BP.Hips, BP.Penis };
			else
				locks = new BodyPartType[] { BP.Hips };

			hand.targetLock = BodyPartLock.LockMany(
				target, locks,
				BodyPartLock.Anim, $"{hand.name} {why}", BodyPartLock.Strong);

			if (hand.targetLock == null)
			{
				Log.Error($"failed to lock targets for {why}");
				Unlock(hand);
				return false;
			}

			return true;
		}

		private BodyPart FindTarget(BodyPartType handPart, float maxDistance)
		{
			var hand = person_.Body.Get(handPart);

			BodyPart tentative = null;
			float tentativeDistance = float.MaxValue;

			Log.Verbose($"finding target for {hand}");

			foreach (var p in Cue.Instance.ActivePersons)
			{
				var g = p.Body.Get(p.Body.GenitalsBodyPart);
				var d = hand.DistanceToSurface(g);

				if (d > maxDistance)
				{
					Log.Verbose($"{g} too far");
					continue;
				}

				if (BetterTarget(tentative, g))
				{
					if (d < tentativeDistance)
					{
						Log.Verbose($"{g} better target");
						tentative = g;
					}
					else
					{
						Log.Verbose($"{g} is farther than {tentative}");
					}
				}
			}

			return tentative;
		}

		private bool BetterTarget(BodyPart tentative, BodyPart check)
		{
			if (check.LockedFor(BodyPartLock.Anim))
			{
				// locked
				Log.Verbose($"BetterTarget: {check} is locked");
				return false;
			}

			if (tentative == null)
			{
				// first
				Log.Verbose($"BetterTarget: {check} is first");
				return true;
			}

			//if (tentative.Person == person_ && check.Person != person_)
			//{
			//	Log.Verbose($"BetterTarget: tentative {tentative} is self, {check} is not");
			//	return true;
			//}

			Log.Verbose($"BetterTarget: {tentative} still better than {check}");

			return false;
		}
	}
}
