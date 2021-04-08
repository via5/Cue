using System;

namespace Cue
{
	interface IBreather
	{
		float Intensity { get; set; }
		float Speed { get; set; }
	}


	class MacGruberBreather : IBreather
	{
		private Person person_ = null;
		private JSONStorableFloat intensity_ = null;

		public MacGruberBreather(Person p)
		{
			person_ = p;
		}

		public float Intensity
		{
			get
			{
				GetParameters();
				if (intensity_ == null)
					return 0;

				try
				{
					return intensity_.val;
				}
				catch (Exception e)
				{
					intensity_ = null;
					Cue.LogError("MacGruberBreather: can't get, " + e.Message);
					return 0;
				}
			}

			set
			{
				GetParameters();
				if (intensity_ == null)
					return;

				try
				{
					intensity_.val = value;
				}
				catch (Exception e)
				{
					intensity_ = null;

					Cue.LogError(
						"MacGruberBreather: " +
						"can't set to " + value.ToString() + ", " + e.Message);
				}
			}
		}

		// not supported
		public float Speed
		{
			get { return 0; }
			set { }
		}

		private void GetParameters()
		{
			if (intensity_ != null)
				return;

			var vsys = ((W.VamSys)Cue.Instance.Sys);

			intensity_ = vsys.GetFloatParameter(
				person_, "MacGruber.Gaze", "enabled");
		}
	}
}
