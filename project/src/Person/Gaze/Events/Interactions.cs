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


		private bool headEmergencyStarted_ = false;
		private bool headReleased_ = false;
		private float timeSinceHeadReleased_ = 0;


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

			SetLastResult("unknown");

			return r;
		}

		protected override bool DoHasEmergency(float s)
		{
			if (headEmergencyStarted_)
			{
				if (headReleased_)
				{
					timeSinceHeadReleased_ += s;
					if (timeSinceHeadReleased_ >= 5)
					{
						headEmergencyStarted_ = false;
						SetLastResult("head released, done");
					}
					else
					{
						SetLastResult("head released, waiting");
					}
				}
				else if (!CheckHead())
				{
					headReleased_ = true;
					timeSinceHeadReleased_ = 0;
					SetLastResult("head just released");
				}
				else
				{
					SetLastResult("head touching");
				}
			}
			else
			{
				if (CheckHead())
				{
					headEmergencyStarted_ = true;
					headReleased_ = false;
					timeSinceHeadReleased_ = 0;
					Logger.Global.Info("head touched by player, emergency started");
					SetLastResult("head touched by player, emergency started");
					return true;
				}
				else
				{
					SetLastResult("head not touching");
				}
			}

			return false;
		}

		private bool CheckHead()
		{
			var player = Cue.Instance.Player;
			if (player == person_)
				return false;

			if (person_.Status.HeadTouchedBy(player) || player.Status.HeadTouchedBy(person_))
				return true;

			return false;
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
					targets_.SetReluctant(t, true, g_.AvoidWeight(t), "avoid in ps");
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

		private float GetHeadTouchedWeight(Person touched, Person touching, bool lookingAtTouched)
		{
			var ps = person_.Personality;

			if (lookingAtTouched)
			{
				if (touched == person_)
				{
					// shouldn't happen, can't look at yourself
					return 0;
				}
				else
				{
					if (touching == person_)
					{
						if (touched.IsPlayer)
							return ps.Get(PS.TouchingPlayerHeadEyesWeight);
						else
							return ps.Get(PS.TouchingOtherHeadEyesWeight);
					}
					else
					{
						if (touched.IsPlayer)
							return ps.Get(PS.PlayerHeadTouchedByOtherEyesWeight);
						else
							return ps.Get(PS.OtherHeadTouchedByOtherEyesWeight);
					}
				}
			}
			else
			{
				if (touching == person_)
				{
					// shouldn't happen, can't look at yourself
					return 0;
				}
				else
				{
					if (touched == person_)
					{
						if (touching.IsPlayer)
							return ps.Get(PS.HeadTouchedByPlayerEyesWeight);
						else
							return ps.Get(PS.HeadTouchedByOtherEyesWeight);
					}
					else
					{
						if (touching.IsPlayer)
							return ps.Get(PS.OtherHeadTouchedByPlayerEyesWeight);
						else
							return ps.Get(PS.OtherHeadTouchedByOtherEyesWeight);
					}
				}
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
				// check if head being groped
				if (person_.Status.HeadTouchedBy(t))
				{
					eyes.Set(GetHeadTouchedWeight(person_, t, false), $"head groped");
				}
				else if (t.Status.HeadTouchedBy(person_))
				{
					eyes.Set(GetHeadTouchedWeight(t, person_, true), $"groping head");
				}

				// check if breasts being groped
				if (person_.Status.GropedBy(t, SS.Breasts))
				{
					eyes.Set(ps.Get(PS.GropedEyesWeight), $"chest groped");
					ownChest.Set(ps.Get(PS.GropedTargetWeight), $"chest groped");
				}
				else if (t.Status.GropedBy(person_, SS.Breasts))
				{
					eyes.Set(ps.Get(PS.GropedEyesWeight), $"groping chest");
					otherChest.Set(ps.Get(PS.GropedTargetWeight), $"groping chest");
				}

				// check if genitals being groped
				if (person_.Status.GropedBy(t, SS.Genitals))
				{
					eyes.Set(ps.Get(PS.GropedEyesWeight), $"genitals groped");
					ownGenitals.Set(ps.Get(PS.GropedTargetWeight), $"genitals groped");
				}
				else if (t.Status.GropedBy(person_, SS.Genitals))
				{
					eyes.Set(ps.Get(PS.GropedEyesWeight), $"groping genitals");
					otherGenitals.Set(ps.Get(PS.GropedTargetWeight), $"groping genitals");
				}
			}

			if (eyes.set || ownChest.set || otherChest.set || ownGenitals.set || otherGenitals.set)
			{
				// this character is being interacted with

				if (g_.ShouldAvoidDuringSex(t))
				{
					targets_.SetReluctant(t, true, g_.AvoidWeight(t), "avoid during sex");
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
				// check if head being groped
				if (target.Status.HeadTouchedBy(source))
				{
					targetEyes.Set(
						GetHeadTouchedWeight(target, source, true),
						$"head groped by {source.ID}");

					sourceEyes.Set(
						GetHeadTouchedWeight(target, source, false),
						$"groping head {target.ID}");
				}

				// check if breasts being groped
				if (target.Status.GropedBy(source, SS.Breasts))
				{
					targetEyes.Set(
						ps.Get(PS.OtherGropedEyesWeight),
						$"breasts groped by {source.ID}");

					sourceEyes.Set(
						ps.Get(PS.OtherGropedSourceEyesWeight),
						$"breasts groped by {source.ID}");

					targetChest.Set(
						ps.Get(PS.OtherGropedTargetWeight),
						$"breasts groped by {source.ID}");
				}

				// check if genitals being groped
				if (target.Status.GropedBy(source, SS.Genitals))
				{
					targetEyes.Set(
						ps.Get(PS.OtherGropedEyesWeight),
						$"genitals groped by {source.ID}");

					sourceEyes.Set(
						ps.Get(PS.OtherGropedSourceEyesWeight),
						$"genitals groped by {source.ID}");

					targetGenitals.Set(
						ps.Get(PS.OtherGropedTargetWeight),
						$"genitals groped by {source.ID}");
				}
			}

			if (sourceEyes.set || sourceChest.set || sourceGenitals.set ||
				targetEyes.set || targetChest.set || targetGenitals.set)
			{
				// this character is being interacted with by someone else

				if (g_.ShouldAvoidUninvolvedHavingSex(source))
				{
					targets_.SetReluctant(
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
					targets_.SetReluctant(
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
