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
			Get();
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

		private void Get()
		{
			if (kissing_ != null)
				return;

			kissing_ = ((W.VamSys)Cue.Instance.Sys)
				.GetBoolParameter(person_, "ClockwiseSilver.Kiss", "Is Kissing");
		}
	}
}
