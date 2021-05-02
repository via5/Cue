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
		private float elapsed_ = 0;

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
			get { return wasKissing_; }
		}

		public float Elapsed
		{
			get { return elapsed_; }
		}

		public Person Target
		{
			get
			{
				if (!Active)
					return null;

				var tid = atom_.GetValue();
				if (tid == "")
					return null;

				return Cue.Instance.FindPerson(tid);
			}
		}

		public void Update(float s)
		{
			var k = kissingRunning_.GetValue();
			if (wasKissing_ != k)
				SetActive(k);

			if (k)
				elapsed_ += s;
		}

		public void Stop()
		{
			activate_.SetValue(false);
		}

		public void Start(Person target)
		{
			if (Active)
			{
				Cue.LogError($"Clockwise {person_}: can't start, already active");
				return;
			}

			DoKiss(target, true);
		}

		public void StartReciprocal(Person target)
		{
			var t = target.Kisser as ClockwiseSilverKiss;
			if (t == null)
			{
				Cue.LogError($"Clockwise {person_}: can't kiss, {target} is not using clockwise");
				return;
			}

			if (Active)
			{
				Cue.LogError($"Clockwise {person_}: can't start reciprocal, already active");
				return;
			}

			if (t.Active)
			{
				Cue.LogError($"Clockwise {t.person_}: can't start reciprocal, already active");
				return;
			}

			Cue.LogInfo($"Clockwise {person_}: starting reciprocal with {target}");

			bool thisPos = true;
			bool targetPos = false;

			if (person_ == Cue.Instance.Player)
			{
				thisPos = false;
				targetPos = true;
			}

			DoKiss(target, thisPos);
			t.DoKiss(person_, targetPos);
		}

		private void DoKiss(Person target, bool pos)
		{
			// force reset
			atom_.SetValue("");
			target_.SetValue("");
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

			elapsed_ = 0;
		}

		private void SetActive(bool b)
		{
			if (b)
			{
				Cue.LogInfo($"Clockwise {person_}: kiss got activated");

				var atom = atom_.GetValue();
				Cue.LogInfo($"Clockwise {person_}: target is '{atom}'");

				if (atom != "")
				{
					var target = Cue.Instance.FindPerson(atom);
					if (target == null)
					{
						Cue.LogInfo($"Clockwise {person_}: person '{atom}' not found");
					}
					else
					{
						Cue.LogInfo($"Clockwise {person_}: now kissing {target}");
						person_.LookAt(target, false);
					}
				}
			}
			else
			{
				Cue.LogInfo($"Clockwise {person_}: kiss stopped");
				person_.LookAtDefault();
			}

			wasKissing_ = b;
		}

		public override string ToString()
		{
			return
				$"Clockwise: " +
				$"running={kissingRunning_.GetValue()} " +
				$"active={activate_.GetValue()}";
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
