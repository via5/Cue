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
				// always unlink when grabbing
				info.Unlink();
			}
			else if (info.GrabStopped())
			{
				var close = FindClose(info.Hand);

				if (close != null)
				{
					var lk = info.Hand.Lock(
						BodyPartLock.Anim, "HandLocker", BodyPartLock.Weak);

					if (lk == null)
					{
						Log.Verbose($"cannot link {info.Hand} to {close}, busy");
					}
					else
					{
						Log.Info($"linking {info.Hand} => {close.BodyPart}");
						info.LinkTo(close, lk);
					}
				}
			}
		}

		private Sys.IBodyPartRegion FindClose(BodyPart hand)
		{
			Sys.IBodyPartRegion closest = null;
			float closestDistance = float.MaxValue;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				// never link anything to the player
				if (p.IsPlayer)
					continue;

				foreach (var bp in p.Body.Parts)
				{
					if (!Valid(hand, bp))
						continue;

					var r = bp.ClosestBodyPartRegion(hand.Position);
					if (r.region == null)
						continue;

					if (r.distance < Distance)
					{
						if (r.distance < closestDistance)
						{
							Log.Verbose(
								$"FindClose: tentative {r.region} better " +
								$"than {(closest?.ToString() ?? "(none)")}");

							closestDistance = r.distance;
							closest = r.region;
						}
					}
				}
			}

			return closest;
		}

		private bool Valid(BodyPart hand, BodyPart other)
		{
			Log.Verbose($"check invalid: {hand} => {other}");

			if (!ValidSelfLink(hand, other))
				return false;

			if (!ValidOtherLink(hand, other))
				return false;

			return true;
		}

		private bool ValidSelfLink(BodyPart hand, BodyPart other)
		{
			// don't link a hand to parts of the same arm
			if (hand.Person == other.Person)
			{
				if (hand.Type == BP.LeftHand)
				{
					if (IsBodyPartOnLeftArm(other.Type))
					{
						Log.Verbose($"invalid, {hand} is on same arm as {other}");
						return false;
					}
				}
				else
				{
					if (IsBodyPartOnRightArm(other.Type))
					{
						Log.Verbose($"invalid, {hand} is on same arm as {other}");
						return false;
					}
				}
			}

			return true;
		}

		private bool ValidOtherLink(BodyPart hand, BodyPart other)
		{
			// if hand A is linked to any part of arm B, never link hand B to
			// any part of arm A, or the movements will compound and the hands
			// will start moving on their own


			if (IsBodyPartOnLeftArm(other.Type))
			{
				// trying to link to a left arm
				Log.Verbose($"check invalid, {other} is on a left arm");

				var otherLeftHand = other.Person.Body.Get(BP.LeftHand);

				if (hand.Type == BP.LeftHand)
				{
					// trying to link to a left arm with a left hand

					if (IsHandLinkedToArm(otherLeftHand, BodyParts.FullLeftArm))
						return false;
				}
				else
				{
					// trying to link to a left arm with a right hand

					if (IsHandLinkedToArm(otherLeftHand, BodyParts.FullRightArm))
						return false;
				}
			}
			else if (IsBodyPartOnRightArm(other.Type))
			{
				// trying to link to a right arm
				Log.Verbose($"check invalid, {other} is on a right arm");

				var otherRightHand = other.Person.Body.Get(BP.RightHand);

				if (hand.Type == BP.LeftHand)
				{
					// trying to link to a right arm with a left hand

					if (IsHandLinkedToArm(otherRightHand, BodyParts.FullLeftArm))
						return false;
				}
				else
				{
					// trying to link to a right arm with a right hand

					if (IsHandLinkedToArm(otherRightHand, BodyParts.FullRightArm))
						return false;
				}
			}


			return true;
		}

		private bool IsBodyPartOnLeftArm(BodyPartType t)
		{
			for (int i = 0; i < BodyParts.FullLeftArm.Length; ++i)
			{
				if (BodyParts.FullLeftArm[i] == t)
					return true;
			}

			return false;
		}

		private bool IsBodyPartOnRightArm(BodyPartType t)
		{
			for (int i = 0; i < BodyParts.FullRightArm.Length; ++i)
			{
				if (BodyParts.FullRightArm[i] == t)
					return true;
			}

			return false;
		}

		private bool IsHandLinkedToArm(BodyPart otherHand, BodyPartType[] armParts)
		{
			var otherHandLink = otherHand.Link;

			if (otherHandLink == null)
			{
				// not linked
				Log.Verbose($"but other hand {otherHand} is not linked");
			}
			else if (otherHandLink.Person != person_)
			{
				// linked, but not to this person
				Log.Verbose(
					$"but other hand {otherHand} link {otherHandLink} " +
					$"is not linked to this person");
			}
			else
			{
				// linked to this person, check if it's on the correct arm

				Log.Verbose($"check invalid, hand {otherHand} is linked to same person");

				for (int i = 0; i < armParts.Length; ++i)
				{
					if (otherHandLink.Type == armParts[i])
					{
						Log.Verbose(
							$"invalid, {otherHand} is already linked to " +
							$"{otherHandLink}, which is on same arm");

						return true;
					}
				}
			}

			return false;
		}
	}
}
