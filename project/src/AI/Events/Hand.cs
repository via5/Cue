namespace Cue
{
	class HandEvent : BasicEvent
	{
		class HandInfo
		{
			public readonly string name;
			public readonly BodyPart bp;
			public readonly int fingeringAnimType;
			public readonly int hjAnimType;
			public readonly int[] sourceLockTypes;

			public Person target = null;
			public bool groped = false;
			public BodyPartLock[] sourceLock = null;
			public BodyPartLock[] targetLock = null;
			public int anim = Animations.None;
			public bool forcedTrigger = false;
			public bool wasGrabbed = false;

			public HandInfo(string name, BodyPart part, int fingeringAnim, int hjAnim, int[] sourceLockTypes)
			{
				this.name = name;
				bp = part;
				fingeringAnimType = fingeringAnim;
				hjAnimType = hjAnim;
				this.sourceLockTypes = sourceLockTypes;
			}

			public void Debug(DebugLines debug)
			{
				debug.Add($"{name} target", $"{target} groped={groped}");

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

		private const float MaxDistanceToStart = 0.09f;
		private const float CheckTargetsInterval = 2;
		private const float AutoStartDistance = 0.05f;
		private const float StopDistance = 0.05f;

		private bool active_ = false;
		private float checkElapsed_ = CheckTargetsInterval;

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
				Animations.LeftFinger, Animations.HandjobLeft,
				new int[] { BP.LeftArm, BP.LeftForearm, BP.LeftHand });

			right_ = new HandInfo(
				"right", person_.Body.Get(BP.RightHand),
				Animations.RightFinger, Animations.HandjobRight,
				new int[] { BP.RightArm, BP.RightForearm, BP.RightHand });
		}

		public override void Debug(DebugLines debug)
		{
			debug.Add("active",  $"{active_}");
			debug.Add("elapsed", $"{checkElapsed_:0.00}");

			left_.Debug(debug);
			right_.Debug(debug);
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
			get { return left_.target; }
		}

		public Person RightTarget
		{
			get { return right_.target; }
		}

		public override void Update(float s)
		{
			if (!active_)
			{
				if (CheckAutoStart())
					return;

				Unlock(left_);
				Unlock(right_);

				return;
			}

			if (!CheckAnim())
				Active = false;

			checkElapsed_ += s;
			if (checkElapsed_ >= CheckTargetsInterval)
			{
				checkElapsed_ = 0;
				Check();

				if (left_.target == null)
					Unlock(left_);

				if (right_.target == null)
					Unlock(right_);

				if (left_.target == null && right_.target == null)
					active_ = false;
			}
		}

		private bool CheckAutoStart()
		{
			if (!Cue.Instance.Options.AutoHands)
				return false;

			bool check = false;
			check = check || CheckAutoStart(left_);
			check = check || CheckAutoStart(right_);

			if (!check)
				return false;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				var g = p.Body.Get(p.Body.GenitalsBodyPart);
				BodyPart bp = null;

				float d = g.DistanceToSurface(left_.bp);
				if (d < AutoStartDistance)
				{
					bp = left_.bp;
				}
				else
				{
					d = g.DistanceToSurface(right_.bp);
					if (d < AutoStartDistance)
						bp = right_.bp;
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

		private bool CheckAutoStart(HandInfo hand)
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

		private void Stop()
		{
			Stop(left_);
			Stop(right_);
		}

		private void Stop(HandInfo hand)
		{
			if (hand.anim != Animations.None)
			{
				person_.Animator.StopType(hand.anim);
				hand.anim = Animations.None;
			}

			if (hand.target != null)
			{
				if (hand.forcedTrigger)
				{
					hand.target.Body.Get(hand.target.Body.GenitalsBodyPart)
						.RemoveForcedTrigger(person_.PersonIndex, hand.bp.Type);

					hand.forcedTrigger = false;
				}

				Unlock(hand);
				SetZoneEnabled(hand.target, false);

				if (hand.bp.Type == BP.LeftHand)
					hand.target.Homing.LeftHand = false;
				else
					hand.target.Homing.RightHand = false;

				hand.groped = false;
				hand.target = null;
			}
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

		private void Check()
		{
			// todo, make it dynamic
			if (left_.target != null || right_.target != null)
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
						StartHJ(left_, leftTarget.Person);
					else if (leftTarget.Type == BP.Labia)
						StartFinger(left_, leftTarget.Person);
				}

				if (rightTarget != null)
				{
					if (rightTarget.Type == BP.Penis)
						StartHJ(right_, rightTarget.Person);
					else if (rightTarget.Type == BP.Labia)
						StartFinger(right_, rightTarget.Person);
				}
			}
		}

		private void StartDoubleHJ(Person left, Person right)
		{
			Log.Info($"double hj {left?.ID}");

			if (Lock("double hj", left_, left) && Lock("double hj", right_, right))
			{
				left_.target = left;
				left_.anim = Animations.HandjobBoth;

				right_.target = right;
				right_.anim = Animations.None;

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

				if (left_.target.Body.PenisSensitive)
				{
					left_.target.Body.Get(left_.target.Body.GenitalsBodyPart)
						.AddForcedTrigger(person_.PersonIndex, BP.LeftHand);

					left_.forcedTrigger = true;
				}

				if (left_.target != right_.target && right_.target.Body.PenisSensitive)
				{
					right_.target.Body.Get(right_.target.Body.GenitalsBodyPart)
						.AddForcedTrigger(person_.PersonIndex, BP.RightHand);

					right_.forcedTrigger = true;
				}
			}
			else
			{
				Log.Info("failed to double hj, can't lock");
			}

		}

		private void StartHJ(HandInfo hand, Person target)
		{
			Log.Info($"{hand.name} hj {target?.ID}");

			if (Lock("hj", hand, target))
			{
				hand.target = target;
				hand.anim = hand.hjAnimType;
				SetZoneEnabled(target, true);

				target.Homing.LeftHand = true;

				if (hand.target.Body.PenisSensitive)
				{
					hand.target.Body.Get(hand.target.Body.GenitalsBodyPart)
						.AddForcedTrigger(person_.PersonIndex, hand.bp.Type);

					hand.forcedTrigger = true;
				}
			}
			else
			{
				Log.Info($"failed to start {hand.name} hj, can't lock");
			}
		}

		private void StartFinger(HandInfo hand, Person target)
		{
			Log.Info($"{hand.name} finger with {target?.ID}");

			if (Lock("fingering", hand, target))
			{
				hand.target = target;
				hand.anim = hand.fingeringAnimType;
				hand.groped = true;

				SetZoneEnabled(target, true);

				hand.target.Body.Get(hand.target.Body.GenitalsBodyPart)
					.AddForcedTrigger(person_.PersonIndex, hand.bp.Type);

				hand.forcedTrigger = true;
			}
			else
			{
				Log.Info($"failed to start {hand.name} fingering, can't lock");
			}
		}

		private bool CheckAnim()
		{
			if (!CheckAnim(left_))
				return false;

			if (!CheckAnim(right_))
				return false;

			return true;
		}

		private bool CheckAnim(HandInfo hand)
		{
			if (hand.anim != Animations.None)
			{
				int state = person_.Animator.PlayingStatus(hand.anim);

				if (state == Animator.Playing)
				{
					if (Mood.ShouldStopSexAnimation(person_, hand.target))
						person_.Animator.StopType(hand.anim);
				}
				else if (state == Animator.NotPlaying)
				{
					if (Mood.CanStartSexAnimation(person_, hand.target))
					{
						if (!person_.Animator.PlayType(
								hand.anim, new AnimationContext(hand.target, hand.sourceLock[0].Key)))
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

		private bool Lock(string why, HandInfo hand, Person target)
		{
			hand.sourceLock = BodyPartLock.LockMany(
				person_, hand.sourceLockTypes,
				BodyPartLock.Anim, why, BodyPartLock.Strong);

			hand.targetLock = BodyPartLock.LockMany(
				target,
				new int[] { BP.Hips },
				BodyPartLock.Anim, $"{hand.name} {why}", BodyPartLock.Strong);

			if (hand.sourceLock == null || hand.targetLock == null)
			{
				Unlock(hand);
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
