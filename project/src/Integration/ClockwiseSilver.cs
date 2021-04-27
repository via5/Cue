namespace Cue
{
	class ClockwiseSilverKiss : IKisser
	{
		private Person person_;
		private W.VamBoolParameter kissing_ = null;
		private bool wasKissing_ = false;
		private int oldGaze_ = -1;

		public ClockwiseSilverKiss(Person p)
		{
			person_ = p;
			kissing_ = new W.VamBoolParameter(
				p, "ClockwiseSilver.Kiss", "Is Kissing");
		}

		public void Update(float s)
		{
			var k = kissing_.GetValue();
			if (wasKissing_ != k)
				SetActive(k);
		}

		private void SetActive(bool b)
		{
			if (b)
			{
				oldGaze_ = person_.Gaze.LookAt;
				person_.Gaze.LookAt = GazeSettings.LookAtDisabled;
			}
			else
			{
				if (oldGaze_ != -1)
					person_.Gaze.LookAt = oldGaze_;
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
