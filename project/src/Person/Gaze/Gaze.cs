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
			pick_.Update(s);

			if (pick_.HasTarget)
				eyes_.LookAt(pick_.Position);
			// else ?

			//gazer_.Duration = person_.Personality.GazeDuration;

			eyes_.Update(s);
			gazer_.Update(s);
		}

		public void UpdateTargets()
		{
			var ps = person_.Personality;

			Clear();

			// exclusive
			if (person_.Body.Get(BodyParts.Head).Grabbed)
			{
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
						targets_.SetRandomWeight(1);
						SetShouldAvoid(t, true);
					}
					else
					{
						targets_.SetExclusive(t, BodyParts.Eyes);
					}

					gazer_.Enabled = false;
					return;
				}
			}

			// exclusive
			if (person_.HasTarget)
			{
				if (person_.MoveTarget != null)
					targets_.SetObjectWeight(person_.MoveTarget, 1);
				else
					targets_.SetFrontWeight(1);

				gazer_.Enabled = true;
				return;
			}


			// non exclusive

			bool gazerEnabled = true;

			// always at least a small change
			targets_.SetRandomWeight(ps.NaturalRandomWeight);


			if (person_.Blowjob.Active)
			{
				var t = person_.Blowjob.Target;

				if (t != null)
				{
					if (ps.AvoidGazeInsidePersonalSpace)
					{
						SetShouldAvoid(t, true);
					}
					else
					{
						targets_.SetRandomWeight(0);
						targets_.SetWeight(t, BodyParts.Eyes, ps.BlowjobEyesWeight);
						targets_.SetWeight(t, BodyParts.Genitals, ps.BlowjobGenitalsWeight);
					}

					gazerEnabled = false;
				}
			}


			if (person_.Handjob.Active)
			{
				var t = person_.Handjob.Target;

				if (t != null)
				{
					if (ps.AvoidGazeInsidePersonalSpace)
					{
						SetShouldAvoid(t, true);
					}
					else
					{
						targets_.SetRandomWeight(0);
						targets_.SetWeight(t, BodyParts.Eyes, ps.HandjobEyesWeight);
						targets_.SetWeight(t, BodyParts.Genitals, ps.HandjobGenitalsWeight);
					}

					gazerEnabled = false;
				}
			}


			for (int i=0; i<Cue.Instance.Persons.Count; ++i)
			{
				var t = Cue.Instance.Persons[i];
				if (t == person_)
					continue;

				if (person_.Body.InsidePersonalSpace(t))
				{
					if (ps.AvoidGazeInsidePersonalSpace)
					{
						SetShouldAvoid(t, true);
					}
					else
					{
						float eyes = 0;
						float chest = 0;
						float genitals = 0;

						if (person_.Body.PenetratedBy(t))
						{
							eyes = ps.PenetrationEyesWeight;
							chest = ps.PenetrationChestWeight;
							genitals = ps.PenetrationGenitalsWeight;
						}
						else
						{
							if (person_.Body.GropedBy(t, BodyParts.Chest))
							{
								eyes = ps.GropedEyesWeight;
								chest = ps.GropedChestWeight;
							}

							if (person_.Body.GropedBy(t, BodyParts.Genitals))
							{
								eyes = ps.GropedEyesWeight;
								genitals = ps.GropedGenitalsWeight;
							}
						}

						if (eyes != 0 || chest != 0 || genitals != 0)
						{
							if (person_.Personality.AvoidGazeDuringSex)
							{
								SetShouldAvoid(t, true);
							}
							else
							{
								targets_.SetWeight(t, BodyParts.Eyes, eyes);
								targets_.SetWeight(person_, BodyParts.Chest, chest);
								targets_.SetWeight(person_, BodyParts.Genitals, genitals);

							}
						}
					}
				}
				else if (t.Body.Penetrated())
				{
					targets_.SetWeight(t, BodyParts.Eyes, ps.OtherSexEyesWeight);
				}
			}

			gazer_.Enabled = gazerEnabled;
		}


		public override string ToString()
		{
			return pick_.ToString();
		}
	}
}
