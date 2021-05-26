using System.Collections.Generic;

namespace Cue
{
	class Gaze
	{
		private Person person_;
		private IEyes eyes_;
		private IGazer gazer_;
		private GazeTargets targets_;
		private GazeTargetPicker pick_;
		private bool[] avoid_ = new bool[0];
		private string lastString_ = "";

		public Gaze(Person p)
		{
			person_ = p;
			eyes_ = Integration.CreateEyes(p);
			gazer_ = Integration.CreateGazer(p);
			targets_ = new GazeTargets(p);
			pick_ = new GazeTargetPicker(p);
		}

		public IEyes Eyes { get { return eyes_; } }
		public IGazer Gazer { get { return gazer_; } }
		public GazeTargets Targets { get { return targets_; } }
		public string LastString { get { return lastString_; } }


		public void Init()
		{
			targets_.Init();
			avoid_ = new bool[Cue.Instance.AllObjects.Count];
			pick_.SetTargets(targets_.All);
		}

		public void Clear()
		{
			for (int i = 0; i < avoid_.Length; ++i)
				avoid_[i] = false;

			targets_.Clear();
		}

		public bool ShouldAvoid(IObject o)
		{
			return avoid_[o.ObjectIndex];
		}

		public void SetShouldAvoid(IObject o, bool b)
		{
			avoid_[o.ObjectIndex] = b;
		}

		public List<Pair<IObject, bool>> GetAllAvoidForDebug()
		{
			var list = new List<Pair<IObject, bool>>();

			for (int i = 0; i < avoid_.Length; ++i)
				list.Add(new Pair<IObject, bool>(Cue.Instance.AllObjects[i], avoid_[i]));

			return list;
		}

		public GazeTargetPicker Picker
		{
			get { return pick_; }
		}

		public void Update(float s)
		{
			UpdateTargets();

			if (pick_.Update(s))
				gazer_.Duration = person_.Personality.GazeDuration;

			if (pick_.HasTarget)
				eyes_.LookAt(pick_.Position);
			// else ?

			eyes_.Update(s);
			gazer_.Update(s);
		}

		public void UpdateTargets()
		{
			var ps = person_.Personality;

			Clear();
			lastString_ = "";

			// exclusive
			if (person_.Body.Get(BodyParts.Head).Grabbed)
			{
				lastString_ = $"head grabbed by player";
				targets_.SetWeight(Cue.Instance.Player, BodyParts.Eyes, 1);
				gazer_.Enabled = false;
				return;
			}

			// exclusive
			if (person_.Kisser.Active)
			{
				var t = person_.Kisser.Target;

				if (t != null)
				{
					if (ps.AvoidGazeInsidePersonalSpace)
					{
						lastString_ = $"kissing {t.ID}, avoid in ps";
						targets_.SetRandomWeight(1);
						SetShouldAvoid(t, true);
					}
					else
					{
						lastString_ = $"kissing {t.ID}, looking at eyes";
						targets_.SetWeight(t, BodyParts.Eyes, 1);
					}

					gazer_.Enabled = false;
					return;
				}
			}

			// exclusive
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

				gazer_.Enabled = true;
				return;
			}


			// non exclusive

			bool gazerEnabled = true;
			bool clearRandom = false;
			bool busy = false;

			if (person_.Blowjob.Active)
			{
				var t = person_.Blowjob.Target;

				if (t != null)
				{
					if (ps.AvoidGazeInsidePersonalSpace)
					{
						lastString_ += $"bj {t.ID}, avoid in ps/";
						SetShouldAvoid(t, true);
					}
					else
					{
						lastString_ += $"bj {t.ID}/";
						clearRandom = true;
						targets_.SetWeight(t, BodyParts.Eyes, ps.BlowjobEyesWeight);
						targets_.SetWeight(t, BodyParts.Genitals, ps.BlowjobGenitalsWeight);
					}

					gazerEnabled = false;
					busy = true;
				}
			}


			if (person_.Handjob.Active)
			{
				var t = person_.Handjob.Target;

				if (t != null)
				{
					if (ps.AvoidGazeInsidePersonalSpace)
					{
						lastString_ += $"hj {t.ID}, avoid in ps/";
						SetShouldAvoid(t, true);
					}
					else
					{
						lastString_ += $"hj {t.ID}/";
						clearRandom = true;
						targets_.SetWeight(t, BodyParts.Eyes, ps.HandjobEyesWeight);
						targets_.SetWeight(t, BodyParts.Genitals, ps.HandjobGenitalsWeight);
					}

					gazerEnabled = false;
					busy = true;
				}
			}


			for (int i=0; i<Cue.Instance.Persons.Count; ++i)
			{
				var t = Cue.Instance.Persons[i];
				if (t == person_)
					continue;

				if (person_.Body.InsidePersonalSpace(t))
				{
					lastString_ += $"{t.ID} in ps, ";

					if (ps.AvoidGazeInsidePersonalSpace)
					{
						lastString_ += $"avoid in ps/";
						SetShouldAvoid(t, true);
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
							eyes = ps.PenetrationEyesWeight;
							otherChest = ps.PenetrationChestWeight;
							genitals = ps.PenetrationGenitalsWeight;
						}
						else
						{
							if (person_.Body.GropedBy(t, BodyParts.BreastParts))
							{
								lastString_ += $"grope chest, ";
								eyes = ps.GropedEyesWeight;
								selfChest = ps.GropedChestWeight;
							}

							if (person_.Body.GropedBy(t, BodyParts.GenitalParts))
							{
								lastString_ += $"grope gen, ";
								eyes = ps.GropedEyesWeight;
								genitals = ps.GropedGenitalsWeight;
							}
						}

						if (eyes != 0 || selfChest != 0 || otherChest != 0 || genitals != 0)
						{
							busy = true;

							if (person_.Personality.AvoidGazeDuringSex)
							{
								lastString_ += $"avoid in ps/";
								SetShouldAvoid(t, true);
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
					targets_.SetWeight(t, BodyParts.Eyes, ps.OtherSexEyesWeight);
				}
			}

			if (!clearRandom)
			{
				// always at least a small change
				targets_.SetRandomWeight(ps.NaturalRandomWeight);
			}

			if (!busy)
				lastString_ += "not busy";

			for (int i = 0; i < Cue.Instance.Persons.Count; ++i)
			{
				if (i == person_.PersonIndex)
					continue;

				targets_.SetWeightIfZero(
					Cue.Instance.Persons[i], BodyParts.Eyes,
					busy ? ps.BusyOtherEyesWeight : ps.NaturalOtherEyesWeight);
			}

			gazer_.Enabled = gazerEnabled;
		}


		public override string ToString()
		{
			return pick_.ToString();
		}
	}
}
