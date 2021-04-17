using System;

namespace Cue
{
	class ClockwiseSilverKiss : IKisser
	{
		private Person person_;
		private JSONStorableBool kissing_ = null;
		private bool wasKissing_ = false;
		private int oldGaze_ = -1;

		public ClockwiseSilverKiss(Person p)
		{
			person_ = p;
		}

		public void Update(float s)
		{
			GetParameters();
			if (kissing_ == null)
				return;

			var k = kissing_.val;
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

		private void GetParameters()
		{
			if (kissing_ != null)
				return;

			kissing_ = ((W.VamSys)Cue.Instance.Sys)
				.GetBoolParameter(person_, "ClockwiseSilver.Kiss", "Is Kissing");
		}
	}


	class ClockwiseSilverHandjob : IHandjob
	{
		private Person person_;
		private JSONStorableBool active_ = null;
		private JSONStorableStringChooser male_ = null;
		private JSONStorableStringChooser hand_ = null;
		private Person target_ = null;

		public ClockwiseSilverHandjob(Person p)
		{
			person_ = p;
		}

		public bool Active
		{
			get
			{
				GetParameters();
				if (active_ == null)
					return false;

				try
				{
					return active_.val;
				}
				catch (Exception e)
				{
					active_ = null;
					Cue.LogError("ClockwiseSilverHandjob: can't get active, " + e.Message);
					return false;
				}
			}

			set
			{
				GetParameters();
				if (active_ == null)
					return;

				try
				{
					active_.val = value;
				}
				catch (Exception e)
				{
					active_ = null;
					Cue.LogError("ClockwiseSilverHandjob: can't set active, " + e.Message);
				}
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

		private void SetTarget()
		{
			GetParameters();

			try
			{
				if (male_ != null && target_ != null)
					male_.val = target_.Atom.ID;
			}
			catch (Exception e)
			{
				male_ = null;
				Cue.LogError("ClockwiseSilverHandjob: can't set male, " + e.Message);
			}

			try
			{
				// todo
				if (hand_ != null)
					hand_.val = "Right";
			}
			catch (Exception e)
			{
				hand_ = null;
				Cue.LogError("ClockwiseSilverHandjob: can't set hand, " + e.Message);
			}
		}

		private void GetParameters()
		{
			if (active_ == null)
			{
				active_ = ((W.VamSys)Cue.Instance.Sys).GetBoolParameter(
					person_, "ClockwiseSilver.HJ", "isActive");
			}

			if (male_ == null)
			{
				male_ = ((W.VamSys)Cue.Instance.Sys).GetStringChooserParameter(
					person_, "ClockwiseSilver.HJ", "Atom");
			}

			if (hand_ == null)
			{
				hand_ = ((W.VamSys)Cue.Instance.Sys).GetStringChooserParameter(
					person_, "ClockwiseSilver.HJ", "handedness");
			}
		}
	}
}
