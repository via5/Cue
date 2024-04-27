namespace Cue
{
	class HandLinker : BasicEvent<EmptyEventData>
	{
		private const float Distance = 0.07f;


		class HandInfo
		{
			private const float MaxAtomMoveDistance = 1.0f;

			private BodyPart hand_;
			private BodyPartLock lk_ = null;
			private BodyPart target_ = null;
			private Vector3 lastAtomPos_;
			private bool grabbed_ = false;
			private AnimationType anim_ = AnimationType.None;

			public HandInfo(BodyPart h)
			{
				hand_ = h;
			}

			public Logger Log
			{
				get { return hand_.Log; }
			}

			public Person Person
			{
				get { return hand_.Person; }
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
				target_ = Body.ResolveBodyPart(region);
				lk_ = lk;

				if (target_ == null)
				{
					lastAtomPos_ = Vector3.Zero;
				}
				else
				{
					lastAtomPos_ = target_.Person.Position;

					if (hand_.Type == BP.LeftHand)
					{
						if (region.BodyPart.Type == BP.LeftBreast ||
							region.BodyPart.Type == BP.RightBreast)
						{
							Log.Verbose("will start left hand on breast");
							anim_ = AnimationType.LeftHandOnBreast;
						}
						else if (
							region.BodyPart.Type == BP.Chest ||
							region.BodyPart.Type == BP.Belly)
						{
							Log.Verbose("will start left hand on chest");
							anim_ = AnimationType.LeftHandOnChest;
						}
					}
					else if (hand_.Type == BP.RightHand)
					{
						if (region.BodyPart.Type == BP.LeftBreast ||
							region.BodyPart.Type == BP.RightBreast)
						{
							Log.Verbose("will start right hand on breast");
							anim_ = AnimationType.RightHandOnBreast;
						}
						else if (
							region.BodyPart.Type == BP.Chest ||
							region.BodyPart.Type == BP.Belly)
						{
							Log.Verbose("will start right hand on chest");
							anim_ = AnimationType.RightHandOnChest;
						}
					}
				}

				if (anim_ != AnimationType.None)
					Person.Options.GetAnimationOption(anim_)?.Trigger(true);
			}

			public void Unlink()
			{
				if (lk_ != null)
				{
					Log.Info("unlinking");

					lk_.Unlock();
					lk_ = null;
					hand_.Unlink();

					if (anim_ != AnimationType.None)
					{
						Log.Verbose($"stopping {anim_}");

						Person.Animator.StopType(anim_);
						Person.Options.GetAnimationOption(anim_)?.Trigger(false);

						anim_ = AnimationType.None;
					}

					target_ = null;
				}
			}

			public void Update(float s)
			{
				if (target_ != null)
				{
					// try to catch large movements like resetting pose

					var d = Vector3.Distance(target_.Person.Position, lastAtomPos_);

					if (d >= MaxAtomMoveDistance)
					{
						Log.Info(
							$"target moved too far, unlinking: " +
							$"was at {lastAtomPos_}, " +
							$"now at {target_.Person.Position}, " +
							$"distance is {d}, max is {MaxAtomMoveDistance}");

						Unlink();
						return;
					}

					lastAtomPos_ = target_.Person.Position;
				}


				if (anim_ != AnimationType.None)
				{
					bool play = Person.Options.GetAnimationOption(anim_)?.Play ?? true;

					if (play)
					{
						AnimationStatus state = Person.Animator.PlayingStatus(anim_);

						if (state == AnimationStatus.Playing)
						{
							if (Mood.ShouldStopSexAnimation(Person, target_.Person))
								Person.Animator.PauseType(anim_);
						}
						else if (state == AnimationStatus.NotPlaying || state == AnimationStatus.Paused)
						{
							if (Mood.CanStartSexAnimation(Person, target_.Person))
							{
								if (!Person.Animator.PlayType(anim_, new AnimationContext(target_.Person, lk_.Key )))
								{
									anim_ = AnimationType.None;
								}
							}
						}
					}
				}
			}

			public void CheckExpired()
			{
				if (lk_ != null && lk_.Expired)
					Unlink();
			}
		}

		struct DebugPart
		{
			public BodyPartType bp;
			public string why;
			public float distance;
		}

		private HandInfo left_ = null;
		private HandInfo right_ = null;
		private bool grabbingPerson_ = false;
		private bool wasEnabled_ = false;
		private bool firstUpdate_ = true;

		private DebugPart[,] debug_;

		public HandLinker()
			: base("HandLinker")
		{
		}

		public override bool Active
		{
			get { return false; }
			set { }
		}

		public override bool CanToggle { get { return false; } }
		public override bool CanDisable { get { return false; } }

		protected override void DoDebug(DebugLines debug)
		{
			for (int i = 0; i < debug_.GetLength(0); ++i)
			{
				for (int j = 0; j < debug_.GetLength(1); ++j)
				{
					var d = debug_[i, j];
					debug.Add(
						$"{Cue.Instance.ActivePersons[i]} " +
						$"{BodyPartType.ToString(d.bp)} " +
						$"{d.distance:0.000} {d.why}");
				}
			}
		}

		protected override void DoInit()
		{
			debug_ = new DebugPart[Cue.Instance.ActivePersons.Length, BP.Count];
			for (int i = 0; i < debug_.GetLength(0); ++i)
			{
				foreach (var bp in BodyPartType.Values)
				{
					debug_[i, bp.Int] = new DebugPart();
					debug_[i, bp.Int].bp = bp;
				}
			}

			left_ = new HandInfo(person_.Body.Get(BP.LeftHand));
			right_ = new HandInfo(person_.Body.Get(BP.RightHand));
		}

		protected override void DoUpdatePaused(float s)
		{
			if (!Enabled)
				return;

			RealUpdate(s);
		}

		protected override void DoUpdate(float s)
		{
			if (!Enabled)
				return;

			RealUpdate(s);
		}

		private void RealUpdate(float s)
		{
			if (firstUpdate_)
			{
				firstUpdate_ = false;
				if (Cue.Instance.Options.HandLinking)
				{
					DoCheck(left_);
					DoCheck(right_);
				}
			}

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

			left_.Update(s);
			right_.Update(s);
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
				DoCheck(info);
			}
		}

		private void DoCheck(HandInfo info)
		{
			if (!info.Hand.CanApplyForce())
				return;

			var close = FindClose(info.Hand);

			if (close != null)
			{
				var lk = info.Hand.Lock(
					BodyPartLock.Anim, "hand linker", BodyPartLock.Weak);

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

		private void ClearDebug(int p)
		{
			for (int b = 0; b < debug_.GetLength(1); ++b)
			{
				debug_[p, b].why = "";
				debug_[p, b].distance = 0;
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
				{
					ClearDebug(p.PersonIndex);
					continue;
				}

				foreach (var bp in p.Body.Parts)
				{
					debug_[p.PersonIndex, bp.Type.Int].distance = 0;

					if (!Valid(hand, bp, ref debug_[p.PersonIndex, bp.Type.Int].why))
						continue;

					var r = bp.ClosestBodyPartRegion(hand.Center);
					if (r.region == null)
					{
						debug_[p.PersonIndex, bp.Type.Int].why = "no region";
						continue;
					}

					debug_[p.PersonIndex, bp.Type.Int].distance = r.distance;

					if (r.distance < Distance)
					{
						if (r.distance < closestDistance)
						{
							Log.Verbose(
								$"FindClose: tentative {r.region} better " +
								$"than {(closest?.ToString() ?? "(none)")}, " +
								$"r.distance={r.distance:0.00} " +
								$"closestDistance={closestDistance} " +
								$"handPos={hand.Center} " +
								$"partPos={bp.Position}");

							closestDistance = r.distance;
							closest = r.region;

							debug_[p.PersonIndex, bp.Type.Int].why = "";
						}
						else
						{
							debug_[p.PersonIndex, bp.Type.Int].why = "farther";
						}
					}
					else
					{
						debug_[p.PersonIndex, bp.Type.Int].why = "too far";
					}
				}
			}

			return closest;
		}

		private bool Valid(BodyPart hand, BodyPart other, ref string why)
		{
			Log.Verbose($"check invalid: {hand} => {other}");

			if (!ValidSelfLink(hand, other, ref why))
				return false;

			if (!ValidOtherLink(hand, other, ref why))
				return false;

			return true;
		}

		private bool ValidSelfLink(BodyPart hand, BodyPart other, ref string why)
		{
			// don't link a hand to parts of the same arm
			if (hand.Person == other.Person)
			{
				if (hand.Type == BP.LeftHand)
				{
					if (IsBodyPartOnLeftArm(other.Type))
					{
						string s = $"invalid, {hand} is on same arm as {other}";

						if (why != null)
							why = s;

						Log.Verbose(s);
						return false;
					}
				}
				else
				{
					if (IsBodyPartOnRightArm(other.Type))
					{
						string s = $"invalid, {hand} is on same arm as {other}";

						if (why != null)
							why = s;

						Log.Verbose(s);
						return false;
					}
				}
			}

			return true;
		}

		private bool ValidOtherLink(BodyPart hand, BodyPart other, ref string why)
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
					{
						if (why != null)
							why = "left arm with left hand";

						return false;
					}
				}
				else
				{
					// trying to link to a left arm with a right hand

					if (IsHandLinkedToArm(otherLeftHand, BodyParts.FullRightArm))
					{
						if (why != null)
							why = "left arm with right hand";

						return false;
					}
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
					{
						if (why != null)
							why = "right arm with left hand";

						return false;
					}
				}
				else
				{
					// trying to link to a right arm with a right hand

					if (IsHandLinkedToArm(otherRightHand, BodyParts.FullRightArm))
					{
						if (why != null)
							why = "right arm with right hand";

						return false;
					}
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
