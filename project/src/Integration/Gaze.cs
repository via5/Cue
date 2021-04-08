using System;
using UnityEngine;

namespace Cue
{
	interface IEyes
	{
		int LookAt { get; set; }
		Vector3 Target { get; set; }
	}

	class VamEyes : IEyes
	{
		private Person person_ = null;
		private Rigidbody eyes_ = null;
		private JSONStorableStringChooser lookMode_ = null;
		private JSONStorableFloat leftRightAngle_ = null;
		private JSONStorableFloat upDownAngle_ = null;

		public VamEyes(Person p)
		{
			person_ = p;
		}

		public int LookAt
		{
			get
			{
				Get();
				if (lookMode_ == null)
					return GazeSettings.LookAtDisabled;

				string s = lookMode_.val;

				if (s == "Player")
					return GazeSettings.LookAtPlayer;
				else if (s == "Target")
					return GazeSettings.LookAtTarget;
				else
					return GazeSettings.LookAtDisabled;
			}

			set
			{
				Get();
				if (lookMode_ == null)
					return;

				switch (value)
				{
					case GazeSettings.LookAtDisabled:
					{
						lookMode_.val = "None";
						break;
					}

					case GazeSettings.LookAtTarget:
					{
						lookMode_.val = "Target";
						break;
					}
					case GazeSettings.LookAtPlayer:
					{
						lookMode_.val = "Player";
						break;
					}
				}
			}
		}

		public Vector3 Target
		{
			get
			{
				Get();
				if (eyes_ == null)
					return Vector3.Zero;

				return Vector3.FromUnity(eyes_.position);
			}

			set
			{
				Get();
				if (eyes_ == null)
					return;

				eyes_.position = Vector3.ToUnity(value);
			}
		}

		private void Get()
		{
			var a = ((W.VamAtom)person_.Atom).Atom;

			if (eyes_ == null)
			{
				eyes_ = FindRigidbody("eyeTargetControl");
				if (eyes_ == null)
					Cue.LogError("atom " + a.uid + " has no eyeTargetControl");
			}

			var eyesStorable = a.GetStorableByID("Eyes");
			if (eyesStorable != null)
			{
				lookMode_ = eyesStorable.GetStringChooserJSONParam("lookMode");
				if (lookMode_ == null)
					Cue.LogError("atom " + a.uid + " has no lookMode");

				leftRightAngle_ = eyesStorable.GetFloatJSONParam("leftRightAngleAdjust");
				if (leftRightAngle_ == null)
					Cue.LogError("atom " + a.uid + " has no leftRightAngleAdjust");

				upDownAngle_ = eyesStorable.GetFloatJSONParam("upDownAngleAdjust");
				if (upDownAngle_ == null)
					Cue.LogError("atom " + a.uid + " has no upDownAngleAdjust");
			}
		}

		private Rigidbody FindRigidbody(string name)
		{
			var a = ((W.VamAtom)person_.Atom).Atom;

			foreach (var rb in a.rigidbodies)
			{
				if (rb.name == name)
					return rb.GetComponent<Rigidbody>();
			}

			return null;
		}
	}


	class GazeSettings
	{
		public const int LookAtDisabled = 0;
		public const int LookAtTarget = 1;
		public const int LookAtPlayer = 2;
	}


	interface IGazer
	{
		int LookAt { get; set; }
		Vector3 Target { get; set; }
	}


	class MacGruberGaze : IGazer
	{
		private Person person_ = null;
		private int lookat_ = GazeSettings.LookAtDisabled;
		private JSONStorableBool toggle_ = null;
		private JSONStorableBool lookatTarget_ = null;

		public MacGruberGaze(Person p)
		{
			person_ = p;
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
				Set();
			}
		}

		// no-op
		public Vector3 Target
		{
			get { return Vector3.Zero; }
			set { }
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
