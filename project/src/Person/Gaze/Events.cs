using System.Collections.Generic;

namespace Cue
{
	public interface IGazeEvent
	{
		int Check(int flags);
		bool HasEmergency(float s);
	}


	abstract class BasicGazeEvent : IGazeEvent
	{
		public const int Continue = 0x00;
		public const int NoGazer = 0x01;
		public const int NoRandom = 0x02;
		public const int Busy = 0x04;

		protected Person person_;
		protected Gaze g_;
		protected GazeTargets targets_;

		protected BasicGazeEvent(Person p)
		{
			person_ = p;
			g_ = p.Gaze;
			targets_ = p.Gaze.Targets;
		}

		public static IGazeEvent[] All(Person p)
		{
			return new List<IGazeEvent>()
			{
				new GazeAbove(p),
				new GazeGrabbed(p),
				new GazeZapped(p),
				new GazeKissing(p),
				new GazeMouth(p),
				new GazeHands(p),
				new GazeInteractions(p),
				new GazeRandom(p),
				new GazeOtherPersons(p)
			}.ToArray();
		}

		public int Check(int flags)
		{
			return DoCheck(flags);
		}

		protected virtual int DoCheck(int flags)
		{
			return Continue;
		}

		public bool HasEmergency(float s)
		{
			return DoHasEmergency(s);
		}

		protected virtual bool DoHasEmergency(float s)
		{
			return false;
		}

		public override abstract string ToString();
	}


	class GazeAbove : BasicGazeEvent
	{
		public GazeAbove(Person p)
			: base(p)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;

			if (person_.Mood.State == Mood.OrgasmState)
			{
				targets_.SetAboveWeight(
					person_.Mood.GazeEnergy * ps.Get(PS.LookAboveMaxWeightOrgasm),
					"orgasm state");
			}
			else
			{
				if (person_.Mood.Get(MoodType.Excited) >= ps.Get(PS.LookAboveMinExcitement))
				{
					if (person_.Excitement.PhysicalRate >= ps.Get(PS.LookAboveMinPhysicalRate))
					{
						targets_.SetAboveWeight(
							person_.Mood.GazeEnergy * ps.Get(PS.LookAboveMaxWeight),
							"normal state");
					}
				}
			}

			return Continue;
		}

		public override string ToString()
		{
			return "look above";
		}
	}


	class GazeGrabbed : BasicGazeEvent
	{
		private BodyPart head_;
		private bool active_ = false;
		private float activeElapsed_ = 0;

		public GazeGrabbed(Person p)
			: base(p)
		{
			head_ = person_.Body.Get(BP.Head);
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;

			if (active_)
			{
				targets_.SetWeight(
					Cue.Instance.Player, BP.Eyes,
					ps.Get(PS.LookAtPlayerOnGrabWeight), "head grabbed");

				// don't disable gazer, mg won't affect the head while it's
				// being grabbed, and it snaps the head back to its original
				// position when it's re-enabled
			}

			return Continue;
		}

		protected override bool DoHasEmergency(float s)
		{
			var ps = person_.Personality;

			if (ps.Get(PS.LookAtPlayerOnGrabWeight) != 0)
			{
				if (head_.GrabbedByPlayer && Cue.Instance.Player.IsInteresting)
				{
					active_ = true;
					activeElapsed_ = 0;
				}
				else if (active_)
				{
					activeElapsed_ += s;

					if (activeElapsed_ > ps.Get(PS.LookAtPlayerTimeAfterGrab))
						active_ = false;
				}
			}

			return active_;
		}

		public override string ToString()
		{
			return "head grabbed";
		}
	}


	class GazeZapped : BasicGazeEvent
	{
		private bool active_ = false;
		private float gazeDuration_ = -1;

		public GazeZapped(Person p)
			: base(p)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;

			if (active_)
			{
				Person other = null;
				float otherIntensity = 0;
				bool self = false;

				for (int i = 0; i < Cue.Instance.ActivePersons.Length; ++i)
				{
					var p = Cue.Instance.ActivePersons[i];
					if (p.Body.Zap.Intensity <= 0)
						continue;

					if (p == person_)
					{
						// self was zapped
						self = true;
						break;
					}
					else
					{
						if (p.Body.Zap.Source != person_)
						{
							if (p.Body.Zap.Intensity > otherIntensity)
							{
								// other was zapped
								other = p;
								otherIntensity = p.Body.Zap.Intensity;
							}
						}
					}
				}

				if (self)
				{
					DoSelfZapped();
					active_ = true;
				}
				else if (other != null)
				{
					DoOtherZapped(other);
					active_ = true;
				}
				else
				{
					active_ = false;
					gazeDuration_ = -1;
				}

				if (active_)
					person_.Gaze.Gazer.Duration = gazeDuration_;
			}

			return Continue;
		}

		protected override bool DoHasEmergency(float s)
		{
			active_ = false;

			// find anybody zapped, including self
			for (int i = 0; i < Cue.Instance.ActivePersons.Length; ++i)
			{
				var p = Cue.Instance.ActivePersons[i];

				if (p.Body.Zap.Intensity > 0 && p.Body.Zap.Source != person_)
				{
					active_ = true;
					gazeDuration_ = -1;
					break;
				}
			}

			return active_;
		}

		private void DoSelfZapped()
		{
			var ps = person_.Personality;
			var z = person_.Body.Zap;

			// look at person zapping
			float w = GetEyesWeight(z.Source.IsPlayer, z.Zone) * z.Intensity;
			if (w >= 0)
			{
				targets_.SetWeight(
					z.Source, BP.Eyes, w, $"self zapped by {z.Source}");
			}

			// look at body part being zapped
			var targetPart = person_.Body.Zone(z.Zone).MainBodyPart;
			if (targetPart != null)
			{
				w = GetTargetWeight(z.Source.IsPlayer, z.Zone) * z.Intensity;
				if (w >= 0)
				{
					targets_.SetWeight(
						person_, targetPart.Type, w,
						$"self zapped by {z.Source}");
				}
			}

			// look up
			if (z.Source.IsPlayer)
			{
				targets_.SetAboveWeight(
					ps.Get(PS.ZappedByPlayerLookUpWeight) * z.Intensity,
					$"self zapped by {z.Source}");
			}
			else
			{
				targets_.SetAboveWeight(
					ps.Get(PS.ZappedByOtherLookUpWeight) * z.Intensity,
					$"self zapped by {z.Source}");
			}

			if (z.Source.IsPlayer)
				SetGazeDuration(PS.ZappedByPlayerGazeDuration, z.Intensity);
			else
				SetGazeDuration(PS.ZappedByOtherGazeDuration, z.Intensity);
		}

		private void DoOtherZapped(Person other)
		{
			var ps = person_.Personality;
			var z = other.Body.Zap;

			// look at person being zapped
			targets_.SetWeight(
				other, BP.Eyes, ps.Get(PS.OtherZappedEyesWeight) * z.Intensity,
				$"other {other} zapped by {z.Source}");

			// look at body part being zapped
			var targetPart = other.Body.Zone(z.Zone).MainBodyPart;
			if (targetPart != null)
			{
				targets_.SetWeight(
					other, targetPart.Type,
					ps.Get(PS.OtherZappedTargetWeight) * z.Intensity,
					$"other {other} zapped by {z.Source}");
			}

			// look at person zapping
			targets_.SetWeight(
				z.Source, BP.Eyes,
				ps.Get(PS.OtherZappedSourceWeight) * z.Intensity,
				$"{z.Source} is zapping {other}");

			SetGazeDuration(PS.OtherZappedGazeDuration, z.Intensity);
		}

		private void SetGazeDuration(PS.DurationIndex di, float intensity)
		{
			if (gazeDuration_ < 0)
			{
				var d = person_.Personality.GetDuration(di);
				d.Reset(intensity);
				person_.Log.Info($"d: {d.ToDetailedString()}");
				gazeDuration_ = d.Current;
				person_.Log.Info($"new gaze duration {gazeDuration_}");
			}
		}

		private float GetEyesWeight(bool player, ZoneType zone)
		{
			var ps = person_.Personality;

			if (player)
			{
				if (zone == SS.Genitals)
					return ps.Get(PS.ZappedByPlayerGenitalsEyesWeight);
				else if (zone == SS.Breasts)
					return ps.Get(PS.ZappedByPlayerBreastsEyesWeight);
				else if (zone == SS.Penetration)
					return ps.Get(PS.ZappedByPlayerPenetrationEyesWeight);
				else if (zone == SS.Mouth)
					return ps.Get(PS.ZappedByPlayerMouthEyesWeight);
			}
			else
			{
				if (zone == SS.Genitals)
					return ps.Get(PS.ZappedByOtherGenitalsEyesWeight);
				else if (zone == SS.Breasts)
					return ps.Get(PS.ZappedByOtherBreastsEyesWeight);
				else if (zone == SS.Penetration)
					return ps.Get(PS.ZappedByOtherPenetrationEyesWeight);
				else if (zone == SS.Mouth)
					return ps.Get(PS.ZappedByOtherMouthEyesWeight);
			}

			return -1;
		}

		private float GetTargetWeight(bool player, ZoneType zone)
		{
			var ps = person_.Personality;

			if (player)
			{
				if (zone == SS.Genitals)
					return ps.Get(PS.ZappedByPlayerGenitalsTargetWeight);
				else if (zone == SS.Breasts)
					return ps.Get(PS.ZappedByPlayerBreastsTargetWeight);
				else if (zone == SS.Penetration)
					return ps.Get(PS.ZappedByPlayerPenetrationTargetWeight);
			}
			else
			{
				if (zone == SS.Genitals)
					return ps.Get(PS.ZappedByOtherGenitalsTargetWeight);
				else if (zone == SS.Breasts)
					return ps.Get(PS.ZappedByOtherBreastsTargetWeight);
				else if (zone == SS.Penetration)
					return ps.Get(PS.ZappedByOtherPenetrationTargetWeight);
			}

			return -1;
		}

		public override string ToString()
		{
			return "zapped";
		}
	}


	class GazeKissing : BasicGazeEvent
	{
		public GazeKissing(Person p)
			: base(p)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;
			var k = person_.AI.GetEvent<KissEvent>();

			if (k.Active)
			{
				var t = k.Target;

				if (t != null)
				{
					if (g_.ShouldAvoidInsidePersonalSpace(t))
					{
						targets_.SetRandomWeight(
							1, $"kissing {t.ID}, but avoid in ps");

						targets_.SetShouldAvoid(
							t, true, g_.AvoidWeight(t),
							$"kissing, but avoid in ps");
					}
					else
					{
						targets_.SetWeight(
							t, BP.Eyes, GazeTargets.ExclusiveWeight, "kissing");
					}

					// don't use NoGazer:
					//  - although the gazer does have to be disabled while
					//    kissing, the head will become busy anyway, which is
					//    checked in Gaze.Update()
					//
					//  - returning NoGazer would disable the gazer until a new
					//    target is picked, which doesn't happen immediately
					//    after kissing stops because the timer just continues
					//    running
					//
					//    since it might take several seconds before a new
					//    target is picked, the gazer would stay disabled and
					//    the head would stay still for a while
				}
			}

			return Continue;
		}

		public override string ToString()
		{
			return "kissing";
		}
	}


	class GazeMouth : BasicGazeEvent
	{
		public GazeMouth(Person p)
			: base(p)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;
			var e = person_.AI.GetEvent<MouthEvent>();

			if (e.Active)
			{
				var t = e.Target;

				if (t != null)
				{
					if (g_.ShouldAvoidInsidePersonalSpace(t))
					{
						targets_.SetShouldAvoid(
							t, true, g_.AvoidWeight(t),
							$"mouthevent, but avoid in ps");

						return Continue | NoGazer | Busy;
					}
					else
					{
						targets_.SetWeight(
							t, BP.Eyes,
							ps.Get(PS.BlowjobEyesWeight), "mouthevent");

						targets_.SetWeight(
							t, t.Body.GenitalsBodyPart,
							ps.Get(PS.BlowjobGenitalsWeight), "mouthevent");

						return Continue | NoGazer | Busy | NoRandom;
					}
				}
			}

			return Continue;
		}

		public override string ToString()
		{
			return "mouthevent";
		}
	}


	class GazeHands : BasicGazeEvent
	{
		public GazeHands(Person p)
			: base(p)
		{
		}

		protected override int DoCheck(int flags)
		{
			int ret = Continue;
			var e = person_.AI.GetEvent<HandEvent>();

			if (e.Active)
			{
				if (e.LeftTarget != null && e.LeftTarget == e.RightTarget)
				{
					ret |= CheckTarget(e.LeftTarget);
				}
				else
				{
					if (e.LeftTarget != null)
						ret |= CheckTarget(e.LeftTarget);

					if (e.RightTarget != null)
						ret |= CheckTarget(e.RightTarget);
				}
			}


			foreach (var t in Cue.Instance.ActivePersons)
			{
				if (t == person_)
					continue;

				e = t.AI.GetEvent<HandEvent>();

				if (e.LeftTarget == person_ || e.RightTarget == person_)
					ret |= CheckTarget(t);
			}

			return ret;
		}

		private int CheckTarget(Person t)
		{
			var ps = person_.Personality;

			if (g_.ShouldAvoidInsidePersonalSpace(t))
			{
				targets_.SetShouldAvoid(
					t, true, g_.AvoidWeight(t),
					"handevent, but avoid in ps");

				return Continue | Busy;
			}
			else
			{
				targets_.SetWeight(
					t, BP.Eyes,
					ps.Get(PS.HandjobEyesWeight), "handevent");

				targets_.SetWeight(
					t, t.Body.GenitalsBodyPart,
					ps.Get(PS.HandjobGenitalsWeight), "handevent");

				return Continue | Busy | NoRandom;
			}
		}

		public override string ToString()
		{
			return "handevent";
		}
	}


	class GazeInteractions : BasicGazeEvent
	{
		struct WeightInfo
		{
			public float weight;
			public string why;
			public bool set;

			public void Set(float w, string why)
			{
				weight = w;
				this.why = why;
				set = true;
			}
		}


		public GazeInteractions(Person p)
			: base(p)
		{
		}

		protected override int DoCheck(int flags)
		{
			int r = Continue;

			foreach (var t in Cue.Instance.ActivePersons)
			{
				if (t == person_ || !t.IsInteresting)
					continue;

				r |= CheckPerson(t);
			}

			return r;
		}

		private int CheckPerson(Person t)
		{
			var ps = person_.Personality;

			if (person_.Status.InsidePersonalSpace(t))
			{
				// person is close

				// check avoidance for closeness
				if (g_.ShouldAvoidInsidePersonalSpace(t))
				{
					targets_.SetShouldAvoid(t, true, g_.AvoidWeight(t), "avoid in ps");
					return Continue;
				}

				// more expensive checks that are impossible without being
				// close; returns true when getting groped or penetrated by this
				// person
				if (CheckInsidePS(t))
					return Busy;
			}

			// these two persons are not currently interacting
			CheckNotInteracting(t);

			return Continue;
		}

		private bool CheckInsidePS(Person t)
		{
			var ps = person_.Personality;

			WeightInfo eyes = new WeightInfo();
			WeightInfo ownChest = new WeightInfo();
			WeightInfo otherChest = new WeightInfo();
			WeightInfo ownGenitals = new WeightInfo();
			WeightInfo otherGenitals = new WeightInfo();

			if (person_.Status.PenetratedBy(t))
			{
				// is being penetrated by this person

				eyes.Set(ps.Get(PS.PenetratedEyesWeight), "penetrated");

				// todo
				if (t.Gaze.Gazer is MacGruber.Gaze)
				{
					otherChest.Set(
						ps.Get(PS.PenetratedGenitalsWeight),
						"penetrated (mg's gaze fix)");
				}
				else
				{
					ownGenitals.Set(
						ps.Get(PS.PenetratedGenitalsWeight),
						"penetrated");
				}
			}
			else if (t.Status.PenetratedBy(person_))
			{
				// is penetrating this person

				eyes.Set(ps.Get(PS.PenetratingEyesWeight), "penetrating");

				// todo
				if (t.Gaze.Gazer is MacGruber.Gaze)
				{
					otherChest.Set(
						ps.Get(PS.PenetratingGenitalsWeight),
						"penetrating (mg's gaze fix)");
				}
				else
				{
					otherGenitals.Set(
						ps.Get(PS.PenetratingGenitalsWeight),
						"penetrating");
				}
			}
			else
			{
				PersonStatus.PartResult pr;

				// check if head being groped
				if (pr = person_.Status.GropedBy(t, BP.Head))
				{
					eyes.Set(ps.Get(PS.GropedEyesWeight), $"head groped ({pr})");
				}
				else if (pr = t.Status.GropedBy(person_, BP.Head))
				{
					eyes.Set(ps.Get(PS.GropedEyesWeight), $"groping head ({pr})");
				}

				// check if breasts being groped
				if (pr = person_.Status.GropedBy(t, BodyParts.BreastParts))
				{
					eyes.Set(ps.Get(PS.GropedEyesWeight), $"chest groped ({pr})");
					ownChest.Set(ps.Get(PS.GropedTargetWeight), $"chest groped ({pr})");
				}
				else if (pr = t.Status.GropedBy(person_, BodyParts.BreastParts))
				{
					eyes.Set(ps.Get(PS.GropedEyesWeight), $"groping chest ({pr})");
					otherChest.Set(ps.Get(PS.GropedTargetWeight), $"groping chest ({pr})");
				}

				// check if genitals being groped
				if (pr = person_.Status.GropedBy(t, BodyParts.GenitalParts))
				{
					eyes.Set(ps.Get(PS.GropedEyesWeight), $"genitals groped ({pr})");
					ownGenitals.Set(ps.Get(PS.GropedTargetWeight), $"genitals groped ({pr})");
				}
				else if (pr = t.Status.GropedBy(person_, BodyParts.GenitalParts))
				{
					eyes.Set(ps.Get(PS.GropedEyesWeight), $"groping genitals ({pr})");
					otherGenitals.Set(ps.Get(PS.GropedTargetWeight), $"groping genitals ({pr})");
				}
			}

			if (eyes.set || ownChest.set || otherChest.set || ownGenitals.set || otherGenitals.set)
			{
				// this character is being interacted with

				if (g_.ShouldAvoidDuringSex(t))
				{
					targets_.SetShouldAvoid(t, true, g_.AvoidWeight(t), "avoid during sex");
				}
				else
				{
					if (eyes.set)
					{
						targets_.SetWeightIfZero(
							t, BP.Eyes, eyes.weight, eyes.why);
					}

					if (ownChest.set)
					{
						targets_.SetWeightIfZero(
							person_, BP.Chest,
							ownChest.weight, ownChest.why);
					}

					if (otherChest.set)
					{
						targets_.SetWeightIfZero(
							t, BP.Chest,
							otherChest.weight, otherChest.why);
					}

					if (ownGenitals.set)
					{
						targets_.SetWeightIfZero(
							person_, person_.Body.GenitalsBodyPart,
							ownGenitals.weight, ownGenitals.why);
					}

					if (otherGenitals.set)
					{
						targets_.SetWeightIfZero(
							t, t.Body.GenitalsBodyPart,
							otherGenitals.weight, otherGenitals.why);
					}
				}

				return true;
			}

			return false;
		}

		private void CheckNotInteracting(Person t)
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == t || p == person_ || !p.IsInteresting)
					continue;

				CheckOtherInteraction(p, t);
			}
		}

		private void CheckOtherInteraction(Person source, Person target)
		{
			var ps = person_.Personality;

			WeightInfo sourceEyes = new WeightInfo();
			WeightInfo sourceChest = new WeightInfo();
			WeightInfo sourceGenitals = new WeightInfo();

			WeightInfo targetEyes = new WeightInfo();
			WeightInfo targetChest = new WeightInfo();
			WeightInfo targetGenitals = new WeightInfo();


			// check if being penetrated by this person
			if (target.Status.PenetratedBy(source))
			{
				sourceEyes.Set(
					ps.Get(PS.OtherPenetrationSourceEyesWeight),
					$"penetrated by {source.ID}");

				targetEyes.Set(
					ps.Get(PS.OtherPenetrationEyesWeight),
					$"penetrated by {source.ID}");

				sourceGenitals.Set(
					ps.Get(PS.OtherPenetrationSourceGenitalsWeight),
					$"penetrated by {source.ID}");
			}
			else
			{
				PersonStatus.PartResult pr;

				// check if head being groped
				if (pr = target.Status.GropedBy(source, BP.Head))
				{
					targetEyes.Set(
						ps.Get(PS.OtherGropedEyesWeight),
						$"head groped by {source.ID} ({pr})");

					sourceEyes.Set(
						ps.Get(PS.OtherGropedSourceEyesWeight),
						$"head groped by {source.ID} ({pr})");
				}

				// check if breasts being groped
				if (pr = target.Status.GropedBy(source, BodyParts.BreastParts))
				{
					targetEyes.Set(
						ps.Get(PS.OtherGropedEyesWeight),
						$"breasts groped by {source.ID} ({pr})");

					sourceEyes.Set(
						ps.Get(PS.OtherGropedSourceEyesWeight),
						$"breasts groped by {source.ID} ({pr})");

					targetChest.Set(
						ps.Get(PS.OtherGropedTargetWeight),
						$"breasts groped by {source.ID} ({pr})");
				}

				// check if genitals being groped
				if (pr = target.Status.GropedBy(source, BodyParts.GenitalParts))
				{
					targetEyes.Set(
						ps.Get(PS.OtherGropedEyesWeight),
						$"genitals groped by {source.ID} ({pr})");

					sourceEyes.Set(
						ps.Get(PS.OtherGropedSourceEyesWeight),
						$"genitals groped by {source.ID} ({pr})");

					targetGenitals.Set(
						ps.Get(PS.OtherGropedTargetWeight),
						$"genitals groped by {source.ID} ({pr})");
				}
			}

			if (sourceEyes.set || sourceChest.set || sourceGenitals.set ||
				targetEyes.set || targetChest.set || targetGenitals.set)
			{
				// this character is being interacted with by someone else

				if (g_.ShouldAvoidUninvolvedHavingSex(source))
				{
					targets_.SetShouldAvoid(
						source, true, g_.AvoidWeight(source),
						"avoid others during sex");
				}
				else
				{
					if (sourceEyes.set)
					{
						targets_.SetWeightIfZero(
							source, BP.Eyes,
							sourceEyes.weight, sourceEyes.why);
					}

					if (sourceChest.set)
					{
						targets_.SetWeightIfZero(
							source, BP.Chest,
							sourceChest.weight, sourceChest.why);
					}

					if (sourceGenitals.set)
					{
						targets_.SetWeightIfZero(
							source, source.Body.GenitalsBodyPart,
							sourceGenitals.weight, sourceGenitals.why);
					}
				}


				if (g_.ShouldAvoidUninvolvedHavingSex(target))
				{
					targets_.SetShouldAvoid(
						target, true, g_.AvoidWeight(target),
						"avoid others during sex");
				}
				else
				{
					if (targetEyes.set)
					{
						targets_.SetWeightIfZero(
							target, BP.Eyes,
							targetEyes.weight, targetEyes.why);
					}

					if (targetChest.set)
					{
						targets_.SetWeightIfZero(
							target, BP.Chest,
							targetChest.weight, targetChest.why);
					}

					if (targetGenitals.set)
					{
						targets_.SetWeightIfZero(
							target, target.Body.GenitalsBodyPart,
							targetGenitals.weight, targetGenitals.why);
					}
				}
			}
		}

		public override string ToString()
		{
			return "interactions";
		}
	}


	class GazeRandom : BasicGazeEvent
	{
		public GazeRandom(Person p)
			: base(p)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;

			if (Bits.IsSet(flags, NoRandom))
			{
				targets_.SetRandomWeightIfZero(0, "random, but busy");
			}
			else
			{
				if (person_.Mood.GazeTiredness >= ps.Get(PS.MaxTirednessForRandomGaze))
				{
					targets_.SetRandomWeightIfZero(0, "random, but tired");
				}
				else
				{
					if (IsSceneIdle())
					{
						if (IsSceneEmpty())
						{
							targets_.SetRandomWeightIfZero(
								ps.Get(PS.IdleEmptyRandomWeight), "random, scene idle and empty");
						}
						else
						{
							targets_.SetRandomWeightIfZero(
								ps.Get(PS.IdleNaturalRandomWeight), "random, scene idle");
						}
					}
					else
					{
						targets_.SetRandomWeightIfZero(
							ps.Get(PS.NaturalRandomWeight), "random");
					}
				}
			}

			return Continue;
		}

		private bool IsSceneIdle()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (!p.Mood.IsIdle)
					return false;
			}

			return true;
		}

		private bool IsSceneEmpty()
		{
			return (Cue.Instance.ActivePersons.Length == 2);
		}

		public override string ToString()
		{
			return "random";
		}
	}


	class GazeOtherPersons : BasicGazeEvent
	{
		public GazeOtherPersons(Person p)
			: base(p)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_ || !p.IsInteresting)
					continue;

				if (g_.ShouldAvoid(p))
				{
					targets_.SetShouldAvoid(p, true, g_.AvoidWeight(p), "avoid");
				}
				else if (person_.Mood.GazeTiredness >= ps.Get(PS.MaxTirednessForRandomGaze))
				{
					// doesn't do anything, just to get the why in the ui
					targets_.SetWeightIfZero(
						p, BP.Eyes, 0, "random person, but tired");
				}
				else if (p.Mood.State == Mood.OrgasmState)
				{
					person_.Gaze.Clear();

					targets_.SetWeightIfZero(
						p, BP.Eyes,
						ps.Get(PS.OtherEyesOrgasmWeight), "orgasming");
				}
				else
				{
					float w = 0;

					if (p.IsPlayer)
					{
						if (Bits.IsSet(flags, Busy))
							w = ps.Get(PS.BusyPlayerEyesWeight);
						else
							w = ps.Get(PS.NaturalPlayerEyesWeight);
					}
					else
					{
						if (Bits.IsSet(flags, Busy))
							w = ps.Get(PS.BusyOtherEyesWeight);
						else
							w = ps.Get(PS.NaturalOtherEyesWeight);
					}

					w += p.Mood.GazeEnergy * ps.Get(PS.OtherEyesExcitementWeight);

					targets_.SetWeightIfZero(
						p, BP.Eyes, w, "random person");
				}
			}

			return Continue;
		}

		protected override bool DoHasEmergency(float s)
		{
			var ps = person_.Personality;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_ || !p.IsInteresting)
					continue;

				if (!g_.ShouldAvoid(p) &&
					person_.Mood.GazeTiredness >= ps.Get(PS.MaxTirednessForRandomGaze) &&
					p.Mood.State == Mood.OrgasmState)
				{
					return true;
				}
			}

			return false;
		}

		public override string ToString()
		{
			return "others";
		}
	}
}
