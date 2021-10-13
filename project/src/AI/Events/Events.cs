namespace Cue
{
	interface IEvent
	{
		void OnPluginState(bool b);
		void FixedUpdate(float s);
		void Update(float s);
	}


	abstract class BasicEvent : IEvent
	{
		protected Person person_;
		protected Logger log_;

		protected BasicEvent(string name, Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Event, p, "int." + name);
		}

		public static IEvent[] All(Person p)
		{
			return new IEvent[]
			{
				new SuckEvent(p),
				new KissEvent(p),
				new SmokeEvent(p),
				new SexEvent(p),
				new HandLocker(p),
				new HandEvent(p)
			};
		}

		public virtual void OnPluginState(bool b)
		{
			// no-op
		}

		public virtual void FixedUpdate(float s)
		{
			// no-op
		}

		public virtual void Update(float s)
		{
			// no-op
		}
	}


	class HandEvent : BasicEvent
	{
		private const float MaxDistanceToStart = 0.2f;
		private const float CheckTargetsInterval = 2;

		private bool active_ = false;
		private float checkElapsed_ = CheckTargetsInterval;

		private Person leftTarget_ = null;
		private bool leftGroped_ = false;

		private Person rightTarget_ = null;
		private bool rightGroped_ = false;

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

				person_.Body.Get(BP.LeftHand).ForceBusy(false);
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

				person_.Body.Get(BP.RightHand).ForceBusy(false);
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

					leftTarget_ = leftTarget.Person;
					person_.Body.Get(BP.LeftHand).ForceBusy(true);

					rightTarget_ = rightTarget.Person;
					person_.Body.Get(BP.RightHand).ForceBusy(true);
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
							leftTarget_ = leftTarget.Person;
							person_.Body.Get(BP.LeftHand).ForceBusy(true);
						}
					}
					else if (leftTarget.Type == BP.Labia)
					{
						if (person_.Animator.PlayType(Animations.LeftFinger, leftTarget.Person))
						{
							Cue.LogError($"left finger");
							leftTarget_ = leftTarget.Person;
							leftGroped_ = true;

							leftTarget_.Body.Get(leftTarget_.Body.GenitalsBodyPart)
								.AddForcedTrigger(person_.PersonIndex, BP.LeftHand);

							person_.Body.Get(BP.LeftHand).ForceBusy(true);
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
							rightTarget_ = rightTarget.Person;
							person_.Body.Get(BP.RightHand).ForceBusy(true);
						}
					}
					else if (rightTarget.Type == BP.Labia)
					{
						if (person_.Animator.PlayType(Animations.RightFinger, rightTarget.Person))
						{
							Cue.LogError($"right finger");
							rightTarget_ = rightTarget.Person;
							rightGroped_ = true;

							rightTarget_.Body.Get(rightTarget_.Body.GenitalsBodyPart)
								.AddForcedTrigger(person_.PersonIndex, BP.RightHand);

							person_.Body.Get(BP.RightHand).ForceBusy(true);
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


	class HandLocker : BasicEvent
	{
		class HandInfo
		{
			public bool locked = false;
			public bool grabbed = false;
		}

		private HandInfo left_ = new HandInfo();
		private HandInfo right_ = new HandInfo();

		public HandLocker(Person p)
			: base("handlocker", p)
		{
		}

		public override void Update(float s)
		{
			if (person_ == Cue.Instance.Player)
				return;

			var leftHand = person_.Body.Get(BP.LeftHand);
			var rightHand = person_.Body.Get(BP.RightHand);

			Check(leftHand, left_);
			Check(rightHand, right_);
		}

		private void Check(BodyPart hand, HandInfo info)
		{
			bool grabbed = hand.Grabbed;

			if (grabbed && !info.grabbed)
			{
				// grab started
				info.grabbed = true;

				// unlink other hands if they're linked to this one
				UnlinkOthers(hand);
			}
			else if (!grabbed && info.grabbed)
			{
				// grab stopped
				info.grabbed = false;

				var close = FindClose(hand);
				if (close != null)
				{
					if (!hand.Busy)
					{
						Cue.LogInfo($"linking {hand} with {close}");
						hand.LinkTo(close);
						hand.ForceBusy(true);
					}
				}
				else
				{
					//Cue.LogInfo($"unlinking {thisHand}");
					hand.Unlink();
					hand.ForceBusy(false);
				}
			}
		}

		private BodyPart FindClose(BodyPart hand)
		{
			int[] selfIgnore = new int[]
			{
				BP.LeftShoulder, BP.LeftArm, BP.LeftForearm,
				BP.RightShoulder, BP.RightArm, BP.RightForearm
			};

			BodyPart closest = null;
			float closestDistance = float.MaxValue;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				foreach (var bp in p.Body.Parts)
				{
					if (bp == hand)
						continue;

					bool ignore = false;

					if (p == person_)
					{
						for (int i = 0; i < selfIgnore.Length; ++i)
						{
							if (selfIgnore[i] == bp.Type)
							{
								ignore = true;
								break;
							}
						}
					}

					if (ignore)
						continue;

					float d = bp.DistanceToSurface(hand);

					if (d < 0.07f)
					{
						if (d < closestDistance)
						{
							closestDistance = d;
							closest = bp;
						}
					}
				}
			}

		//	Cue.LogError($"{closest} {closestDistance}");

			return closest;
		}

		private void UnlinkOthers(BodyPart hand)
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				var left = p.Body.Get(BP.LeftHand);
				if (left.IsLinkedTo(hand))
					left.Unlink();

				var right = p.Body.Get(BP.RightHand);
				if (right.IsLinkedTo(hand))
					right.Unlink();
			}
		}
	}
}
