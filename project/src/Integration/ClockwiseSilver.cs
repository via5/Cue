namespace Cue
{
	class ClockwiseSilverKiss : IKisser
	{
		private Person person_;
		private W.VamBoolParameter kissingRunning_ = null;
		private W.VamBoolParameter activate_ = null;
		private W.VamStringChooserParameter atom_ = null;
		private W.VamStringChooserParameter target_ = null;
		private W.VamBoolParameter trackPos_ = null;
		private W.VamBoolParameter trackRot_ = null;
		private W.VamFloatParameter headAngleX_ = null;
		private W.VamFloatParameter headAngleY_ = null;
		private W.VamFloatParameter headAngleZ_ = null;
		private bool wasKissing_ = false;

		public ClockwiseSilverKiss(Person p)
		{
			person_ = p;

			kissingRunning_ = new W.VamBoolParameter(
				p, "ClockwiseSilver.Kiss", "Is Kissing");

			activate_ = new W.VamBoolParameter(
				p, "ClockwiseSilver.Kiss", "isActive");

			atom_ = new W.VamStringChooserParameter(
				p, "ClockwiseSilver.Kiss", "atom");

			target_ = new W.VamStringChooserParameter(
				p, "ClockwiseSilver.Kiss", "kissTargetJSON");

			trackPos_ = new W.VamBoolParameter(
				p, "ClockwiseSilver.Kiss", "trackPosition");

			trackRot_ = new W.VamBoolParameter(
				p, "ClockwiseSilver.Kiss", "trackRotation");

			headAngleX_ = new W.VamFloatParameter(
				p, "ClockwiseSilver.Kiss", "Head Angle X");

			headAngleY_ = new W.VamFloatParameter(
				p, "ClockwiseSilver.Kiss", "Head Angle Y");

			headAngleZ_ = new W.VamFloatParameter(
				p, "ClockwiseSilver.Kiss", "Head Angle Z");

			activate_.SetValue(false);
		}

		public bool Active
		{
			get { return kissingRunning_.GetValue(); }
		}

		public void Update(float s)
		{
			var k = kissingRunning_.GetValue();
			if (wasKissing_ != k)
				SetActive(k);

			if (wasKissing_)
			{
				var tid = atom_.GetValue();
				if (tid != "")
				{
					var t = Cue.Instance.FindPerson(tid);
					if (t != null && t.Atom.Triggers.Lip != null)
					{
						var tp = t.Atom.Triggers.Lip.Position;
						if (Vector3.Distance(person_.Atom.Triggers.Lip.Position, tp) >= 0.05f)
						{
							Stop();

							var tk = t.Kisser as ClockwiseSilverKiss;
							if (tk != null)
								tk.Stop();
						}
					}
				}
			}
		}

		public void Stop()
		{
			activate_.SetValue(false);
		}

		public void Kiss(Person target)
		{
			DoKiss(target, true);
		}

		public void KissReciprocal(Person target)
		{
			var t = target.Kisser as ClockwiseSilverKiss;
			if (t == null)
			{
				Cue.LogError("Clockwise: can't kiss, target is not using clockwise");
				return;
			}

			DoKiss(target, true);
			t.DoKiss(person_, false);
		}

		private void DoKiss(Person target, bool pos)
		{
			atom_.SetValue(target.ID);
			target_.SetValue("LipTrigger");
			activate_.SetValue(true);
			person_.LookAt(target, false);
			target.LookAt(person_, false);
			trackPos_.SetValue(pos);
			trackRot_.SetValue(true);
			wasKissing_ = true;

			headAngleX_.SetValue(-10);
			headAngleZ_.SetValue(-40);
		}

		private void SetActive(bool b)
		{
			if (b)
			{
				Cue.LogInfo("Clockwise: kiss got activated");

				var atom = atom_.GetValue();
				Cue.LogInfo($"Clockwise: atom is '{atom}'");

				if (atom != "")
				{
					var target = Cue.Instance.FindPerson(atom);
					if (target == null)
					{
						Cue.LogInfo($"Clockwise: person '{atom}' not found");
					}
					else
					{
						Cue.LogInfo($"Clockwise: now kissing {target}");
						person_.LookAt(target, false);
					}
				}
			}
			else
			{
				Cue.LogInfo("Clockwise: kiss stopped");
				person_.LookAt(Cue.Instance.Player);
			}

			wasKissing_ = b;
		}

		public override string ToString()
		{
			return $"Clockwise: active={wasKissing_}";
		}
	}


	class ClockwiseSilverHandjob : IHandjob
	{
		private Person person_;
		private W.VamBoolParameter active_ = null;
		private W.VamStringChooserParameter male_ = null;
		private W.VamStringChooserParameter hand_ = null;
		private Person target_ = null;
		private bool wasActive_ = false;

		public ClockwiseSilverHandjob(Person p)
		{
			person_ = p;
			active_ = new W.VamBoolParameter(p, "ClockwiseSilver.HJ", "isActive");
			male_ = new W.VamStringChooserParameter(p, "ClockwiseSilver.HJ", "Atom");
			hand_ = new W.VamStringChooserParameter(p, "ClockwiseSilver.HJ", "handedness");
			Active = false;
		}

		public bool Active
		{
			get
			{
				wasActive_ = active_.GetValue();
				return wasActive_;
			}

			set
			{
				wasActive_ = value;
				active_.SetValue(value);
			}
		}

		public Person Target
		{
			get
			{
				return target_;
			}

			set
			{
				target_ = value;
				SetTarget();
			}
		}

		public override string ToString()
		{
			string s = $"Clockwise: active={wasActive_} target=";

			if (target_ == null)
				s += "(none)";
			else
				s += target_.ID;

			return s;
		}

		private void SetTarget()
		{
			if (target_ != null)
				male_.SetValue(target_.ID);

			// todo
			hand_.SetValue("Right");
		}
	}
}
