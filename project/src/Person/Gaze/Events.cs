using System.Collections.Generic;

namespace Cue
{
	interface IGazeEvent
	{
		int Check(int flags);
		int CheckEmergency();
	}


	abstract class BasicGazeEvent : IGazeEvent
	{
		public const int Continue = 0x00;
		public const int Exclusive = 0x01;
		public const int NoGazer = 0x02;
		public const int NoRandom = 0x04;
		public const int Busy = 0x08;

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
				new GazeKissing(p),
				new GazeMoving(p),
				new GazeBJ(p),
				new GazeHJ(p),
				new GazeInteractions(p),
				new GazeRandom(p),
				new GazeOtherPersons(p)
			}.ToArray();
		}

		public int Check(int flags)
		{
			return DoCheck(flags);
		}

		public int CheckEmergency()
		{
			if (person_.Body.Get(BP.Head).LockedFor(BodyPartLock.Move))
				return Continue;

			return DoCheckEmergency();
		}

		protected virtual int DoCheck(int flags)
		{
			return Continue;
		}

		protected virtual int DoCheckEmergency()
		{
			return Continue;
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
					person_.Mood.GazeEnergy * ps.Get(PSE.LookAboveMaxWeightOrgasm),
					"orgasm state");
			}
			else
			{
				if (person_.Mood.Excitement >= ps.Get(PSE.LookAboveMinExcitement))
				{
					if (person_.Excitement.PhysicalRate >= ps.Get(PSE.LookAboveMinPhysicalRate))
					{
						targets_.SetAboveWeight(
							person_.Mood.GazeEnergy * ps.Get(PSE.LookAboveMaxWeight),
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
		public GazeGrabbed(Person p)
			: base(p)
		{
		}

		protected override int DoCheckEmergency()
		{
			var head = person_.Body.Get(BP.Head);

			if (head.GrabbedByPlayer)
			{
				person_.Gaze.Clear();
				targets_.SetWeight(Cue.Instance.Player, BP.Eyes, 1, "head grabbed");

				return Exclusive | NoGazer;
			}

			return Continue;
		}

		public override string ToString()
		{
			return "head grabbed";
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

			if (person_.Kisser.Active)
			{
				var t = person_.Kisser.Target;

				if (t != null)
				{
					if (ps.GetBool(PSE.AvoidGazeInsidePersonalSpace))
					{
						targets_.SetRandomWeight(1, $"kissing {t.ID}, but avoid in ps");
						targets_.SetShouldAvoid(t, true, $"kissing, but avoid in ps");
					}
					else
					{
						targets_.SetWeight(t, BP.Eyes, 1, "kissing");
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

					return Exclusive;
				}
			}

			return Continue;
		}

		public override string ToString()
		{
			return "kissing";
		}
	}


	class GazeMoving : BasicGazeEvent
	{
		public GazeMoving(Person p)
			: base(p)
		{
		}

		protected override int DoCheck(int flags)
		{
			if (person_.HasTarget)
			{
				if (person_.MoveTarget != null)
				{
					targets_.SetObjectWeight(
						person_.MoveTarget, 1, "moving to");
				}
				else
				{
					targets_.SetFrontWeight(1, "moving to pos");
				}

				return Exclusive;
			}

			return Continue;
		}

		public override string ToString()
		{
			return "moving";
		}
	}


	class GazeBJ : BasicGazeEvent
	{
		public GazeBJ(Person p)
			: base(p)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;

			if (person_.Blowjob.Active)
			{
				var t = person_.Blowjob.Target;

				if (t != null)
				{
					if (ps.GetBool(PSE.AvoidGazeInsidePersonalSpace))
					{
						targets_.SetShouldAvoid(
							t, true, $"bj, but avoid in ps");

						return Continue | NoGazer | Busy;
					}
					else
					{
						targets_.SetWeight(
							t, BP.Eyes,
							ps.Get(PSE.BlowjobEyesWeight), "bj");

						targets_.SetWeight(
							t, BP.Penis,
							ps.Get(PSE.BlowjobGenitalsWeight), "bj");

						return Continue | NoGazer | Busy | NoRandom;
					}
				}
			}

			return Continue;
		}

		public override string ToString()
		{
			return "bj";
		}
	}


	class GazeHJ : BasicGazeEvent
	{
		public GazeHJ(Person p)
			: base(p)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;

			if (person_.Handjob.Active)
			{
				var ts = person_.Handjob.Targets;

				if (ts != null && ts.Length > 0)
				{
					int ret = 0;

					for (int i = 0; i < ts.Length; ++i)
					{
						var t = ts[i];

						if (ps.GetBool(PSE.AvoidGazeInsidePersonalSpace))
						{
							targets_.SetShouldAvoid(t, true, "hj, but avoid in ps");
							ret |= Continue | Busy;
						}
						else
						{
							targets_.SetWeight(
								t, BP.Eyes,
								ps.Get(PSE.HandjobEyesWeight), "hj");

							targets_.SetWeight(
								t, BP.Penis,
								ps.Get(PSE.HandjobGenitalsWeight), "hj");

							ret |= Continue | Busy | NoRandom;
						}
					}

					return ret;
				}
			}

			return Continue;
		}

		public override string ToString()
		{
			return "hj";
		}
	}


	class GazeInteractions : BasicGazeEvent
	{
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

			// check player avoidance
			if (t.IsPlayer && g_.ShouldAvoidPlayer())
			{
				targets_.SetShouldAvoid(t, true, "avoid player");
				return Continue;
			}

			if (person_.Body.InsidePersonalSpace(t))
			{
				// person is close

				// check avoidance for closeness
				if (g_.ShouldAvoidInsidePersonalSpace())
				{
					targets_.SetShouldAvoid(t, true, "avoid in ps");
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

		private bool CheckInsidePS(Person t)
		{
			var ps = person_.Personality;

			WeightInfo eyes = new WeightInfo();
			WeightInfo ownChest = new WeightInfo();
			WeightInfo otherChest = new WeightInfo();
			WeightInfo ownGenitals = new WeightInfo();
			WeightInfo otherGenitals = new WeightInfo();

			if (person_.Body.PenetratedBy(t))
			{
				// is being penetrated by this person

				eyes.Set(ps.Get(PSE.PenetratedEyesWeight), "penetrated");

				// todo
				if (t.Gaze.Gazer is MacGruberGaze)
				{
					otherChest.Set(
						ps.Get(PSE.PenetratedGenitalsWeight),
						"penetrated (mg's gaze fix)");
				}
				else
				{
					ownGenitals.Set(
						ps.Get(PSE.PenetratedGenitalsWeight),
						"penetrated");
				}
			}
			else if (t.Body.PenetratedBy(person_))
			{
				// is penetrating this person

				eyes.Set(ps.Get(PSE.PenetratingEyesWeight), "penetrating");

				// todo
				if (t.Gaze.Gazer is MacGruberGaze)
				{
					otherChest.Set(
						ps.Get(PSE.PenetratingGenitalsWeight),
						"penetrating (mg's gaze fix)");
				}
				else
				{
					otherGenitals.Set(
						ps.Get(PSE.PenetratingGenitalsWeight),
						"penetrating");
				}
			}
			else
			{
				Body.PartResult pr;

				// check if head being groped
				if (pr = person_.Body.GropedBy(t, BP.Head))
				{
					eyes.Set(ps.Get(PSE.GropedEyesWeight), $"head groped ({pr})");
				}
				else if (pr = t.Body.GropedBy(person_, BP.Head))
				{
					eyes.Set(ps.Get(PSE.GropedEyesWeight), $"groping head ({pr})");
				}

				// check if breasts being groped
				if (pr = person_.Body.GropedBy(t, BodyParts.BreastParts))
				{
					eyes.Set(ps.Get(PSE.GropedEyesWeight), $"chest groped ({pr})");
					ownChest.Set(ps.Get(PSE.GropedTargetWeight), $"chest groped ({pr})");
				}
				else if (pr = t.Body.GropedBy(person_, BodyParts.BreastParts))
				{
					eyes.Set(ps.Get(PSE.GropedEyesWeight), $"groping chest ({pr})");
					otherChest.Set(ps.Get(PSE.GropedTargetWeight), $"groping chest ({pr})");
				}

				// check if genitals being groped
				if (pr = person_.Body.GropedBy(t, BodyParts.GenitalParts))
				{
					eyes.Set(ps.Get(PSE.GropedEyesWeight), $"genitals groped ({pr})");
					ownGenitals.Set(ps.Get(PSE.GropedTargetWeight), $"genitals groped ({pr})");
				}
				else if (pr = t.Body.GropedBy(person_, BodyParts.GenitalParts))
				{
					eyes.Set(ps.Get(PSE.GropedEyesWeight), $"groping genitals ({pr})");
					otherGenitals.Set(ps.Get(PSE.GropedTargetWeight), $"groping genitals ({pr})");
				}
			}

			if (eyes.set || ownChest.set || otherChest.set || ownGenitals.set || otherGenitals.set)
			{
				// this character is being interacted with

				if (g_.ShouldAvoidDuringSex())
				{
					targets_.SetShouldAvoid(t, true, "avoid during sex");
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
			if (target.Body.PenetratedBy(source))
			{
				sourceEyes.Set(
					ps.Get(PSE.OtherPenetrationSourceEyesWeight),
					$"penetrated by {source.ID}");

				targetEyes.Set(
					ps.Get(PSE.OtherPenetrationEyesWeight),
					$"penetrated by {source.ID}");

				sourceGenitals.Set(
					ps.Get(PSE.OtherPenetrationSourceGenitalsWeight),
					$"penetrated by {source.ID}");
			}
			else
			{
				Body.PartResult pr;

				// check if head being groped
				if (pr = target.Body.GropedBy(source, BP.Head))
				{
					targetEyes.Set(
						ps.Get(PSE.OtherGropedEyesWeight),
						$"head groped by {source.ID} ({pr})");

					sourceEyes.Set(
						ps.Get(PSE.OtherGropedSourceEyesWeight),
						$"head groped by {source.ID} ({pr})");
				}

				// check if breasts being groped
				if (pr = target.Body.GropedBy(source, BodyParts.BreastParts))
				{
					targetEyes.Set(
						ps.Get(PSE.OtherGropedEyesWeight),
						$"breasts groped by {source.ID} ({pr})");

					sourceEyes.Set(
						ps.Get(PSE.OtherGropedSourceEyesWeight),
						$"breasts groped by {source.ID} ({pr})");

					targetChest.Set(
						ps.Get(PSE.OtherGropedTargetWeight),
						$"breasts groped by {source.ID} ({pr})");
				}

				// check if genitals being groped
				if (pr = target.Body.GropedBy(source, BodyParts.GenitalParts))
				{
					targetEyes.Set(
						ps.Get(PSE.OtherGropedEyesWeight),
						$"genitals groped by {source.ID} ({pr})");

					sourceEyes.Set(
						ps.Get(PSE.OtherGropedSourceEyesWeight),
						$"genitals groped by {source.ID} ({pr})");

					targetGenitals.Set(
						ps.Get(PSE.OtherGropedTargetWeight),
						$"genitals groped by {source.ID} ({pr})");
				}
			}

			if (sourceEyes.set || sourceChest.set || sourceGenitals.set ||
				targetEyes.set || targetChest.set || targetGenitals.set)
			{
				// this character is being interacted with by someone else

				if (g_.ShouldAvoidOthersDuringSex())
				{
					targets_.SetShouldAvoid(source, true, "avoid others during sex");
					targets_.SetShouldAvoid(target, true, "avoid others during sex");
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
				if (person_.Mood.GazeTiredness >= ps.Get(PSE.MaxTirednessForRandomGaze))
				{
					targets_.SetRandomWeightIfZero(0, "random, but tired");
				}
				else
				{
					if (IsSceneIdle())
					{
						targets_.SetRandomWeightIfZero(
							ps.Get(PSE.IdleNaturalRandomWeight), "random scene idle");
					}
					else
					{
						targets_.SetRandomWeightIfZero(
							ps.Get(PSE.NaturalRandomWeight), "random");
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
				if (p == person_)
					continue;

				if (p.IsPlayer && g_.ShouldAvoidPlayer())
					continue;

				if (!p.IsInteresting)
					continue;

				if (person_.Mood.GazeTiredness >= ps.Get(PSE.MaxTirednessForRandomGaze))
				{
					// doesn't do anything, just to get the why in the ui
					targets_.SetWeightIfZero(
						p, BP.Eyes, 0, "random person, but tired");
				}
				else
				{
					float w = 0;

					if (p.IsPlayer)
					{
						if (Bits.IsSet(flags, Busy))
							w = ps.Get(PSE.BusyPlayerEyesWeight);
						else
							w = ps.Get(PSE.NaturalPlayerEyesWeight);
					}
					else
					{
						if (Bits.IsSet(flags, Busy))
							w = ps.Get(PSE.BusyOtherEyesWeight);
						else
							w = ps.Get(PSE.NaturalOtherEyesWeight);
					}

					w += p.Mood.GazeEnergy * ps.Get(PSE.OtherEyesExcitementWeight);

					targets_.SetWeightIfZero(
						p, BP.Eyes, w, "random person");
				}
			}

			return Continue;
		}

		protected override int DoCheckEmergency()
		{
			var ps = person_.Personality;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				if (p.Mood.State == Mood.OrgasmState)
				{
					if (!ps.GetBool(PSE.AvoidGazeDuringSexOthers))
					{
						person_.Gaze.Clear();

						targets_.SetWeightIfZero(
							p, BP.Eyes,
							ps.Get(PSE.OtherEyesOrgasmWeight), "orgasming");

						return Exclusive;
					}
				}
			}

			return Continue;
		}

		public override string ToString()
		{
			return "others";
		}
	}
}
