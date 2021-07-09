﻿using System.Collections.Generic;

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
			avoid_ = new bool[Cue.Instance.Everything.Count];
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
				list.Add(new Pair<IObject, bool>(Cue.Instance.Everything[i], avoid_[i]));

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


			// do this even for exclusives
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
					if (ps.GetBool(PSE.AvoidGazeInsidePersonalSpace))
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

			bool gazerEnabled = !person_.Body.Get(BodyParts.Head).Busy;
			bool clearRandom = false;
			bool busy = false;

			if (person_.Blowjob.Active)
			{
				var t = person_.Blowjob.Target;

				if (t != null)
				{
					if (ps.GetBool(PSE.AvoidGazeInsidePersonalSpace))
					{
						lastString_ += $"bj {t.ID}, avoid in ps/";
						SetShouldAvoid(t, true);
					}
					else
					{
						lastString_ += $"bj {t.ID}/";
						clearRandom = true;
						targets_.SetWeight(t, BodyParts.Eyes, ps.Get(PSE.BlowjobEyesWeight));
						targets_.SetWeight(t, BodyParts.Genitals, ps.Get(PSE.BlowjobGenitalsWeight));
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
					if (ps.GetBool(PSE.AvoidGazeInsidePersonalSpace))
					{
						lastString_ += $"hj {t.ID}, avoid in ps/";
						SetShouldAvoid(t, true);
					}
					else
					{
						lastString_ += $"hj {t.ID}/";
						clearRandom = true;
						targets_.SetWeight(t, BodyParts.Eyes, ps.Get(PSE.HandjobEyesWeight));
						targets_.SetWeight(t, BodyParts.Genitals, ps.Get(PSE.HandjobGenitalsWeight));
					}

					busy = true;
				}
			}


			foreach (var t in Cue.Instance.ActivePersons)
			{
				if (t == person_)
					continue;

				if (t == Cue.Instance.Player && ShouldAvoidPlayer())
				{
					lastString_ += $"avoid player, ";
					SetShouldAvoid(t, true);
				}
				else if (person_.Body.InsidePersonalSpace(t))
				{
					lastString_ += $"{t.ID} in ps, ";

					if (ShouldAvoidInsidePersonalSpace())
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

							if (ShouldAvoidDuringSex())
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
					targets_.SetWeight(t, BodyParts.Eyes, ps.Get(PSE.OtherSexEyesWeight));
				}
			}

			if (!clearRandom || person_.Mood.Energy > ps.Get(PSE.MaxEnergyForRandomGaze))
			{
				// always at least a small change
				targets_.SetRandomWeight(ps.Get(PSE.NaturalRandomWeight));
			}

			if (!busy)
				lastString_ += "not busy";

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				if (p == Cue.Instance.Player && ps.GetBool(PSE.AvoidGazePlayer))
					continue;

				float w = busy ? ps.Get(PSE.BusyOtherEyesWeight) : ps.Get(PSE.NaturalOtherEyesWeight);
				w *= (p.Mood.RawExcitement + 1);

				targets_.SetWeightIfZero(p, BodyParts.Eyes, w);
			}

			gazer_.Enabled = gazerEnabled;
		}

		private bool ShouldAvoidPlayer()
		{
			var ps = person_.Personality;

			if (!ps.GetBool(PSE.AvoidGazePlayer))
				return false;

			return IsBored();
		}

		private bool IsBored()
		{
			var ps = person_.Personality;

			if (person_.Mood.RawExcitement >= ps.Get(PSE.MaxExcitementForAvoid))
				return false;

			if (person_.Mood.TimeSinceLastOrgasm < ps.Get(PSE.AvoidDelayAfterOrgasm))
				return false;

			return true;
		}

		private bool ShouldAvoidInsidePersonalSpace()
		{
			var ps = person_.Personality;

			if (!ps.GetBool(PSE.AvoidGazeInsidePersonalSpace))
				return false;

			return IsBored();
		}

		private bool ShouldAvoidDuringSex()
		{
			var ps = person_.Personality;

			if (!ps.GetBool(PSE.AvoidGazeDuringSex))
				return false;

			return IsBored();
		}


		public override string ToString()
		{
			return pick_.ToString();
		}
	}
}
