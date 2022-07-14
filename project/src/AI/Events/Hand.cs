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
			public BodyPartLock[] sourceLocks = null;
			public BodyPartLock[] targetStrongLocks = null;
			public BodyPartLock[] targetWeakLocks = null;
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

				if (sourceLocks != null)
				{
					for (int i = 0; i < sourceLocks.Length; ++i)
						debug.Add($"{name} srcLock", sourceLocks[i].ToString());
				}

				if (targetStrongLocks != null)
				{
					for (int i = 0; i < targetStrongLocks.Length; ++i)
						debug.Add($"{name} tarStrLk", targetStrongLocks[i].ToString());
				}

				if (targetWeakLocks != null)
				{
					for (int i = 0; i < targetWeakLocks.Length; ++i)
						debug.Add($"{name} tarWkLk", targetWeakLocks[i].ToString());
				}
			}
		}

		// double hj is disabled for now, cwhj doesn't work very well, and
		// it's just annoying
		private const bool EnableDoubleHJ = false;

		private const float ManualStartDistance = 0.09f;
		private const float AutoStartDistance = 0.06f;

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
				BodyParts.FullLeftArm);

			right_ = new HandInfo(
				"right", person_.Body.Get(BP.RightHand),
				AnimationType.RightFinger, AnimationType.HandjobRight,
				BodyParts.FullRightArm);
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
				Log.Verbose($"checking auto start, left={checkLeft} right={checkRight}");
				Check(AutoStartDistance, checkLeft, checkRight, Animation.StopNoReturn);
			}
		}

		private void CheckManualStart()
		{
			Log.Verbose("checking manual start");
			Check(ManualStartDistance, true, true);
		}

		private bool TargetsForDouble(BodyPart left, BodyPart right)
		{
#pragma warning disable 0162
			if (EnableDoubleHJ)
			{
				if (left == null || right == null)
					return false;

				if (left.Type != BP.Penis || right.Type != BP.Penis)
					return false;

				return (left.Person == right.Person);
			}
#pragma warning restore 0162

			return false;
		}

		private void Check(
			float maxDistance, bool canStartLeft, bool canStartRight,
			int stopFlags = Animation.NoStopFlags)
		{
			var leftTarget = FindTarget(left_, maxDistance);
			var rightTarget = FindTarget(right_, maxDistance);

			if (leftTarget == null && rightTarget == null)
			{
				Stop(stopFlags);
				Log.Verbose("no target");
				return;
			}

			if (TargetsForDouble(leftTarget, rightTarget))
			{
				Log.Verbose("check found double targets");
				Stop(stopFlags);
				StartDoubleHJ(leftTarget);
			}
			else
			{
				if (left_.anim == AnimationType.HandjobBoth)
				{
					Log.Verbose("stopping because current is both and one target is gone");
					Stop(stopFlags);
					canStartLeft = true;
					canStartRight = true;
				}

				if (rightTarget == null)
				{
					Log.Verbose("no right target");
					Stop(right_, stopFlags);
				}
				else
				{
					if (rightTarget == right_.targetBodyPart)
					{
						Log.Verbose("right target is the same as before");
					}
					else if (canStartRight)
					{
						Log.Verbose($"right target now {rightTarget}");

						Stop(right_, stopFlags);

						if (rightTarget.Type == BP.Penis)
							StartHJ(right_, rightTarget);
						else if (rightTarget.Type == BP.Vagina)
							StartFinger(right_, rightTarget);
					}
				}

				if (leftTarget == null)
				{
					Log.Verbose("no left target");
					Stop(left_, stopFlags);
				}
				else
				{
					if (leftTarget == left_.targetBodyPart)
					{
						Log.Verbose("left target is the same as before");
					}
					else if (canStartLeft)
					{
						// this can happen if double hj is disabled; since cwhj
						// doesn't support two hands on two different persons,
						// this is forbidden
						if (rightTarget == null)
						{
							Log.Verbose($"new left target {leftTarget}");

							Stop(left_, stopFlags);

							if (leftTarget.Type == BP.Penis)
								StartHJ(left_, leftTarget);
							else if (leftTarget.Type == BP.Vagina)
								StartFinger(left_, leftTarget);
						}
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
			Log.Verbose($"stopping {hand.name}");

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
				SetHoming(hand, false);

				hand.groped = false;
				hand.targetBodyPart = null;
			}

			Unlock(hand);
		}

		private void Unlock(HandInfo hand)
		{
			if (hand.sourceLocks != null)
			{
				for (int i = 0; i < hand.sourceLocks.Length; ++i)
					hand.sourceLocks[i].Unlock();

				hand.sourceLocks = null;
			}

			if (hand.targetStrongLocks != null)
			{
				for (int i = 0; i < hand.targetStrongLocks.Length; ++i)
					hand.targetStrongLocks[i].Unlock();

				hand.targetStrongLocks = null;
			}

			if (hand.targetWeakLocks != null)
			{
				for (int i = 0; i < hand.targetWeakLocks.Length; ++i)
					hand.targetWeakLocks[i].Unlock();

				hand.targetWeakLocks = null;
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

				// no homing, cw does weird stuff

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
				SetHoming(hand, true);

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
				AnimationStatus state = person_.Animator.PlayingStatus(hand.anim);

				if (state == AnimationStatus.Playing)
				{
					if (Mood.ShouldStopSexAnimation(person_, hand.targetBodyPart.Person))
						person_.Animator.PauseType(hand.anim);
				}
				else if (state == AnimationStatus.NotPlaying || state == AnimationStatus.Paused)
				{
					if (Mood.CanStartSexAnimation(person_, hand.targetBodyPart.Person))
					{
						if (!person_.Animator.PlayType(
								hand.anim, new AnimationContext(hand.targetBodyPart.Person, hand.sourceLocks[0].Key)))
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

		private void SetHoming(HandInfo hand, bool b)
		{
			if (hand.bp.Type == BP.LeftHand)
				hand.targetBodyPart.Person.Homing.LeftHand = b;
			else
				hand.targetBodyPart.Person.Homing.RightHand = b;
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
			hand.sourceLocks = BodyPartLock.LockMany(
				person_, hand.sourceLockTypes,
				BodyPartLock.Anim, why, BodyPartLock.Strong);

			if (hand.sourceLocks == null)
			{
				Log.Error($"failed to lock sources for {why}");
				Unlock(hand);
				return false;
			}

			return true;
		}

		private bool LockTargets(string why, HandInfo hand, Person target)
		{
			BodyPartType[] strongLocks = null;
			BodyPartType[] weakLocks = new BodyPartType[] { BP.Hips };

			// don't lock genitals for females
			if (target.Body.Get(BP.Penis).Exists)
				strongLocks = new BodyPartType[] { BP.Penis };

			if (strongLocks != null)
			{
				hand.targetStrongLocks = BodyPartLock.LockMany(
					target, strongLocks,
					BodyPartLock.Anim, $"{hand.name} {why}", BodyPartLock.Strong);

				if (hand.targetStrongLocks == null)
				{
					Log.Error($"failed to lock strong targets for {why}");
					Unlock(hand);
					return false;
				}
			}

			if (weakLocks != null)
			{
				hand.targetWeakLocks = BodyPartLock.LockMany(
					target, weakLocks,
					BodyPartLock.Anim, $"{hand.name} {why}", BodyPartLock.Weak);

				if (hand.targetWeakLocks == null)
				{
					Log.Error($"failed to lock weak targets for {why}");
					Unlock(hand);
					return false;
				}
			}

			return true;
		}

		private BodyPart FindTarget(HandInfo hand, float maxDistance)
		{
			BodyPart tentative = null;
			float tentativeDistance = float.MaxValue;

			Log.Verbose($"finding target for {hand}");

			foreach (var p in Cue.Instance.ActivePersons)
			{
				var g = p.Body.Get(p.Body.GenitalsBodyPart);
				var d = hand.bp.DistanceToSurface(g);

				if (d > maxDistance)
				{
					Log.Verbose($"{g} too far {d}");
					continue;
				}

				ulong key = BodyPartLock.NoKey;
				if (hand.targetWeakLocks != null && hand.targetWeakLocks.Length > 0)
					key = hand.targetWeakLocks[0].Key;

				if (BetterTarget(tentative, g, key))
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

		private bool BetterTarget(BodyPart tentative, BodyPart check, ulong key)
		{
			if (check.LockedFor(BodyPartLock.Anim, key))
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

			Log.Verbose($"BetterTarget: {tentative} still better than {check}");
			return false;
		}
	}
}
