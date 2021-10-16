using System;
using System.Collections.Generic;
using System.Text;

namespace Cue
{
	class HandLocker : BasicEvent
	{
		class HandInfo
		{
			public BodyPartLock lk = null;
			public bool grabbed = false;
		}

		private readonly HandInfo left_ = new HandInfo();
		private readonly HandInfo right_ = new HandInfo();

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
					if (info.lk != null)
					{
						info.lk.Unlock();
						info.lk = null;
					}

					info.lk = hand.Lock(BodyPartLock.Move);
					if (info.lk != null)
					{
						Cue.LogInfo($"linking {hand} with {close}");
						hand.LinkTo(close);
					}
				}
				else
				{
					//Cue.LogInfo($"unlinking {thisHand}");
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
