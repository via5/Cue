namespace Cue
{
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
			: base(p, I.GazeInteractions)
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
}
