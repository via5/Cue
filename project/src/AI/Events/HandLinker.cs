namespace Cue
{
	class HandLinker : BasicEvent
	{
		class HandInfo
		{
			public BodyPart hand;
			public BodyPartLock lk = null;
			public bool grabbed = false;

			public HandInfo(BodyPart h)
			{
				hand = h;
			}

			public void Unlink()
			{
				if (lk != null)
				{
					lk.Unlock();
					lk = null;
					hand.Unlink();
				}
			}
		}

		private readonly HandInfo left_;
		private readonly HandInfo right_;
		private bool grabbingPerson_ = false;

		public HandLinker(Person p)
			: base("handLinker", p)
		{
			left_ = new HandInfo(person_.Body.Get(BP.LeftHand));
			right_ = new HandInfo(person_.Body.Get(BP.RightHand));
		}

		public override void Update(float s)
		{
			if (person_.IsPlayer)
				return;

			if (person_.Grabbed)
			{
				if (!grabbingPerson_)
				{
					grabbingPerson_ = true;

					left_.Unlink();
					right_.Unlink();

					foreach (var p in Cue.Instance.ActivePersons)
					{
						if (p == person_)
							continue;

						p.Body.Get(BP.LeftHand).UnlinkFrom(person_);
						p.Body.Get(BP.RightHand).UnlinkFrom(person_);
					}
				}
			}
			else
			{
				grabbingPerson_ = false;

				Check(left_);
				Check(right_);
			}
		}

		private void Check(HandInfo info)
		{
			if (info.lk != null && info.lk.Expired)
				info.Unlink();

			var hand = info.hand;
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
					if (info.lk != null)
					{
						info.lk.Unlock();
						info.lk = null;
					}

					info.lk = hand.Lock(BodyPartLock.Move, "HandLocker", false);
					if (info.lk != null)
					{
						Log.Verbose($"linking {hand} with {close}");
						hand.LinkTo(close);
					}
				}
				else
				{
					Log.Verbose($"unlinking {hand}");
					hand.Unlink();

					if (info.lk != null)
					{
						info.lk.Unlock();
						info.lk = null;
					}
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
				if (p.IsPlayer)
					continue;

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
