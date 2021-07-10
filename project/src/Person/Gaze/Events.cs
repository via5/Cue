using System.Collections.Generic;

namespace Cue
{
	interface IGazeEvent
	{
		int Check(int flags);
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
		protected string lastString_ = "";

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
				new GazeOrgasm(p),
				new GazeGrabbed(p),
				new GazeKissing(p),
				new GazeMoving(p),
				new GazeBJ(p),
				new GazeHJ(p),
				new GazeOthers(p),
				new GazeRandom(p),
				new GazeExcitedOthers(p)
			}.ToArray();
		}

		public int Check(int flags)
		{
			return DoCheck(flags);
		}

		protected abstract int DoCheck(int flags);
	}


	class GazeOrgasm : BasicGazeEvent
	{
		public GazeOrgasm(Person p)
			: base(p)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;

			if (person_.Mood.State == Mood.OrgasmState)
			{
				targets_.SetAboveWeight(
					person_.Mood.Energy * ps.Get(PSE.LookAboveMaxWeight));
			}
			else
			{
				targets_.SetAboveWeight(
					person_.Mood.Energy * ps.Get(PSE.LookAboveMaxWeightOrgasm));
			}

			return Continue;
		}
	}


	class GazeGrabbed : BasicGazeEvent
	{
		public GazeGrabbed(Person p)
			: base(p)
		{
		}

		protected override int DoCheck(int flags)
		{
			if (person_.Body.Get(BodyParts.Head).Grabbed)
			{
				lastString_ = $"head grabbed by player";
				targets_.SetWeight(Cue.Instance.Player, BodyParts.Eyes, 1);
				return Exclusive | NoGazer;
			}

			return Continue;
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
						lastString_ = $"kissing {t.ID}, avoid in ps";
						targets_.SetRandomWeight(1);
						g_.SetShouldAvoid(t, true);
					}
					else
					{
						lastString_ = $"kissing {t.ID}, looking at eyes";
						targets_.SetWeight(t, BodyParts.Eyes, 1);
					}

					return Exclusive | NoGazer;
				}
			}

			return Continue;
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
					lastString_ = $"moving to {person_.MoveTarget.ID}, looking at it";
					targets_.SetObjectWeight(person_.MoveTarget, 1);
				}
				else
				{
					lastString_ = $"moving to pos, looking in front";
					targets_.SetFrontWeight(1);
				}

				return Exclusive;
			}

			return Continue;
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
						lastString_ += $"bj {t.ID}, avoid in ps/";
						g_.SetShouldAvoid(t, true);
						return Continue | NoGazer | Busy;
					}
					else
					{
						lastString_ += $"bj {t.ID}/";
						targets_.SetWeight(t, BodyParts.Eyes, ps.Get(PSE.BlowjobEyesWeight));
						targets_.SetWeight(t, BodyParts.Genitals, ps.Get(PSE.BlowjobGenitalsWeight));

						return Continue | NoGazer | Busy | NoRandom;
					}
				}
			}

			return Continue;
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
				var t = person_.Handjob.Target;

				if (t != null)
				{
					if (ps.GetBool(PSE.AvoidGazeInsidePersonalSpace))
					{
						lastString_ += $"hj {t.ID}, avoid in ps/";
						g_.SetShouldAvoid(t, true);
						return Continue | Busy;
					}
					else
					{
						lastString_ += $"hj {t.ID}/";
						targets_.SetWeight(t, BodyParts.Eyes, ps.Get(PSE.HandjobEyesWeight));
						targets_.SetWeight(t, BodyParts.Genitals, ps.Get(PSE.HandjobGenitalsWeight));
						return Continue | Busy | NoRandom;
					}
				}
			}

			return Continue;
		}
	}


	class GazeOthers : BasicGazeEvent
	{
		public GazeOthers(Person p)
			: base(p)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;
			bool busy = false;

			foreach (var t in Cue.Instance.ActivePersons)
			{
				if (t == person_)
					continue;

				if (t == Cue.Instance.Player && g_.ShouldAvoidPlayer())
				{
					lastString_ += $"avoid player, ";
					g_.SetShouldAvoid(t, true);
				}
				else if (person_.Body.InsidePersonalSpace(t))
				{
					lastString_ += $"{t.ID} in ps, ";

					if (g_.ShouldAvoidInsidePersonalSpace())
					{
						lastString_ += $"avoid in ps/";
						g_.SetShouldAvoid(t, true);
					}
					else
					{
						float eyes = 0;
						float selfChest = 0;
						float otherChest = 0;
						float genitals = 0;

						if (person_.Body.PenetratedBy(t))
						{
							lastString_ += $"pen/";
							eyes = ps.Get(PSE.PenetrationEyesWeight);
							otherChest = ps.Get(PSE.PenetrationChestWeight);
							genitals = ps.Get(PSE.PenetrationGenitalsWeight);
						}
						else
						{
							if (person_.Body.GropedBy(t, BodyParts.Head))
							{
								lastString_ += $"grope head, ";
								eyes = ps.Get(PSE.GropedEyesWeight);
							}

							if (person_.Body.GropedBy(t, BodyParts.BreastParts))
							{
								lastString_ += $"grope chest, ";
								eyes = ps.Get(PSE.GropedEyesWeight);
								selfChest = ps.Get(PSE.GropedChestWeight);
							}

							if (person_.Body.GropedBy(t, BodyParts.GenitalParts))
							{
								lastString_ += $"grope gen, ";
								eyes = ps.Get(PSE.GropedEyesWeight);
								genitals = ps.Get(PSE.GropedGenitalsWeight);
							}
						}

						if (eyes != 0 || selfChest != 0 || otherChest != 0 || genitals != 0)
						{
							busy = true;

							if (g_.ShouldAvoidDuringSex())
							{
								lastString_ += $"avoid in ps/";
								g_.SetShouldAvoid(t, true);
							}
							else
							{
								lastString_ += $"ok";
								targets_.SetWeight(t, BodyParts.Eyes, eyes);
								targets_.SetWeight(person_, BodyParts.Chest, selfChest);
								targets_.SetWeight(t, BodyParts.Chest, otherChest);
								targets_.SetWeight(person_, BodyParts.Genitals, genitals);
							}
						}
					}
				}
				else if (t.Body.Penetrated())
				{
					lastString_ += $"{t.ID} other pen";
					targets_.SetWeight(t, BodyParts.Eyes, ps.Get(PSE.OtherSexEyesWeight));
				}
			}

			int r = Continue;
			if (busy)
				r |= Busy;

			return r;
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

			if (!Bits.IsSet(flags, NoRandom) || person_.Mood.Energy > ps.Get(PSE.MaxEnergyForRandomGaze))
			{
				// always at least a small change
				targets_.SetRandomWeight(ps.Get(PSE.NaturalRandomWeight));
			}

			return Continue;
		}
	}


	class GazeExcitedOthers : BasicGazeEvent
	{
		public GazeExcitedOthers(Person p)
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

				if (p == Cue.Instance.Player && ps.GetBool(PSE.AvoidGazePlayer))
					continue;

				float w;
				if (Bits.IsSet(flags, Busy))
					w = ps.Get(PSE.BusyOtherEyesWeight);
				else
					w = ps.Get(PSE.NaturalOtherEyesWeight);

				w *= (p.Mood.RawExcitement + 1);

				targets_.SetWeightIfZero(p, BodyParts.Eyes, w);
			}

			return Continue;
		}
	}
}
