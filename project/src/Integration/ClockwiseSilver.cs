namespace Cue
{
	class ClockwiseSilverKiss : IKisser
	{
		private Person person_;
		private W.VamBoolParameter kissingRunning_ = null;
		private W.VamBoolParameter active_ = null;
		private W.VamStringChooserParameter atom_ = null;
		private W.VamStringChooserParameter target_ = null;
		private bool wasKissing_ = false;

		public ClockwiseSilverKiss(Person p)
		{
			person_ = p;

			kissingRunning_ = new W.VamBoolParameter(
				p, "ClockwiseSilver.Kiss", "Is Kissing");

			active_ = new W.VamBoolParameter(
				p, "ClockwiseSilver.Kiss", "isActive");

			atom_ = new W.VamStringChooserParameter(
				p, "ClockwiseSilver.Kiss", "atom");

			target_ = new W.VamStringChooserParameter(
				p, "ClockwiseSilver.Kiss", "kissTargetJSON");
		}

		public void Update(float s)
		{
			var k = kissingRunning_.GetValue();
			if (wasKissing_ != k)
				SetActive(k);
		}

		public void Kiss(Person target)
		{
			atom_.SetValue(target.ID);
			target_.SetValue("LipTrigger");
			active_.SetValue(true);
			person_.Gaze.LookAt(target);
			target.Gaze.LookAt(person_);
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
						person_.Gaze.LookAt(target);
					}
				}
			}
			else
			{
				Cue.LogInfo("Clockwise: kiss stopped");
				person_.Gaze.LookAt(Cue.Instance.Player);
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
