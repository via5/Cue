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
				new HandLocker(p)
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
				if (p == hand.Person)
					continue;

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
