namespace Cue
{
	class HandLinker : BasicEvent
	{
		private const float Distance = 0.06f;

		class HandInfo
		{
			private BodyPart hand_;
			private BodyPartLock lk_ = null;
			private bool grabbed_ = false;

			public HandInfo(BodyPart h)
			{
				hand_ = h;
			}

			public BodyPart Hand
			{
				get { return hand_; }
			}

			public bool GrabStarted()
			{
				if (hand_.Grabbed && !grabbed_)
				{
					grabbed_ = true;
					return true;
				}

				return false;
			}

			public bool GrabStopped()
			{
				if (!hand_.Grabbed && grabbed_)
				{
					grabbed_ = false;
					return true;
				}

				return false;
			}

			public void LinkTo(Sys.IBodyPartRegion region, BodyPartLock lk)
			{
				hand_.LinkTo(region);
				lk_ = lk;
			}

			public void Unlink()
			{
				if (lk_ != null)
				{
					lk_.Unlock();
					lk_ = null;
					hand_.Unlink();
				}
			}

			public void CheckExpired()
			{
				if (lk_ != null && lk_.Expired)
					Unlink();
			}
		}

		private HandInfo left_ = null;
		private HandInfo right_ = null;
		private bool grabbingPerson_ = false;
		private bool wasEnabled_ = false;

		public HandLinker()
			: base("handLinker")
		{
		}

		protected override void DoInit()
		{
			left_ = new HandInfo(person_.Body.Get(BP.LeftHand));
			right_ = new HandInfo(person_.Body.Get(BP.RightHand));
		}

		public override void Update(float s)
		{
			if (!Cue.Instance.Options.HandLinking)
			{
				if (wasEnabled_)
				{
					left_.Unlink();
					right_.Unlink();
					wasEnabled_ = false;
				}

				return;
			}

			wasEnabled_ = true;

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
			info.CheckExpired();

			bool grabbed = info.Hand.Grabbed;

			if (info.GrabStarted())
			{
				// unlink other hands if they're linked to this one
				//UnlinkOthers(info.Hand);

				// always unlink when grabbing
				info.Unlink();
			}
			else if (info.GrabStopped())
			{
				var close = FindClose(info.Hand);

				if (close != null)
				{
					Log.Verbose($"found {close}");

					var lk = info.Hand.Lock(
						BodyPartLock.Anim, "HandLocker", BodyPartLock.Weak);

					if (lk != null)
					{
						Log.Verbose($"linking {info.Hand} with {close}");
						info.LinkTo(close, lk);
					}
				}
			}
		}

		private Sys.IBodyPartRegion FindClose(BodyPart hand)
		{
			BodyPartType[] selfIgnore = new BodyPartType[]
			{
				BP.LeftShoulder, BP.LeftArm, BP.LeftElbow, BP.LeftForearm,
				BP.RightShoulder, BP.RightArm, BP.RightElbow, BP.RightForearm,
			};

			Sys.IBodyPartRegion closest = null;
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

					var r = bp.ClosestBodyPartRegion(hand.Position);
					if (r.region == null)
						continue;

					if (r.distance < Distance)
					{
						if (r.distance < closestDistance)
						{
							closestDistance = r.distance;
							closest = r.region;
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
