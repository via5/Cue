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

			intensity_ = Cue.Instance.VamSys?.GetFloatParameter(
				person_, "MacGruber.Gaze", "enabled");
		}
	}


	abstract class BasicGazer : IGazer
	{
		protected Person person_ = null;

		public abstract int LookAt { get; set; }
		public abstract Vector3 Target { get; set; }
		public abstract void Update(float s);

		protected BasicGazer(Person p)
		{
			person_ = p;
		}

		public void LookInFront()
		{
			LookAt = GazeSettings.LookAtTarget;

			Target =
				person_.HeadPosition +
				Vector3.Rotate(new Vector3(0, 0, 1), person_.Bearing);
		}
	}


	class MacGruberGaze : BasicGazer
	{
		private int lookat_ = GazeSettings.LookAtDisabled;
		private JSONStorableBool toggle_ = null;
		private JSONStorableBool lookatTarget_ = null;
		private VamEyes eyes_ = null;

		public MacGruberGaze(Person p)
			: base(p)
		{
			eyes_ = new VamEyes(p);
		}

		public override int LookAt
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

		public override Vector3 Target
		{
			get { return eyes_.Target; }
			set { eyes_.Target = value; }
		}

		public override void Update(float s)
		{
			eyes_.Update(s);
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

			toggle_ = Cue.Instance.VamSys?.GetBoolParameter(
				person_, "MacGruber.Gaze", "enabled");

			lookatTarget_ = Cue.Instance.VamSys?.GetBoolParameter(
				person_, "MacGruber.Gaze", "LookAt EyeTarget");
		}
	}
}
