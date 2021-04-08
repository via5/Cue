using System;

namespace Cue
{
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


	class MacGruberGaze : IGazer
	{
		private Person person_ = null;
		private int lookat_ = GazeSettings.LookAtDisabled;
		private JSONStorableBool toggle_ = null;
		private JSONStorableBool lookatTarget_ = null;
		private VamEyes eyes_ = null;

		public MacGruberGaze(Person p)
		{
			person_ = p;
			eyes_ = new VamEyes(p);
		}

		public int LookAt
		{
			get
			{
				return lookat_;
			}

			set
			{
				lookat_ = value;
				eyes_.LookAt = value;
				Set();
			}
		}

		public Vector3 Target
		{
			get { return eyes_.Target; }
			set { eyes_.Target = value; }
		}

		private void Set()
		{
			GetParameters();
			if (toggle_ == null || lookatTarget_ == null)
				return;

			try
			{
				switch (lookat_)
				{
					case GazeSettings.LookAtDisabled:
					{
						toggle_.val = false;
						break;
					}

					case GazeSettings.LookAtTarget:
					{
						toggle_.val = true;
						lookatTarget_.val = true;
						break;
					}

					case GazeSettings.LookAtPlayer:
					{
						toggle_.val = true;
						lookatTarget_.val = false;
						break;
					}
				}
			}
			catch (Exception e)
			{
				toggle_ = null;
				lookatTarget_ = null;
				Cue.LogError("MacGruberGaze: can't set, " + e.Message);
			}
		}

		private void GetParameters()
		{
			if (toggle_ != null && lookatTarget_ != null)
				return;

			var vsys = ((W.VamSys)Cue.Instance.Sys);

			toggle_ = vsys.GetBoolParameter(
				person_, "MacGruber.Gaze", "enabled");

			lookatTarget_ = vsys.GetBoolParameter(
				person_, "MacGruber.Gaze", "LookAt EyeTarget");
		}
	}
}
