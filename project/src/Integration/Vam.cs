using System;
using UnityEngine;

namespace Cue
{
	class VamEyes
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
			var vsys = ((W.VamSys)Cue.Instance.Sys);
			var a = ((W.VamAtom)person_.Atom).Atom;

			if (eyes_ == null)
			{
				eyes_ = vsys.FindRigidbody(person_, "eyeTargetControl");
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
	}


	class VamSpeaker : ISpeaker
	{
		private Person person_ = null;
		private JSONStorableString text_ = null;

		public VamSpeaker(Person p)
		{
			person_ = p;
		}

		public void Say(string s)
		{
			GetParameters();
			if (text_ == null)
				return;

			try
			{
				text_.val = s;
			}
			catch (Exception e)
			{
				Cue.LogError(
					person_.ToString() + " can't speak, " + e.Message + " " +
					"(while trying to say '" + s + "'");
			}
		}

		private void GetParameters()
		{
			if (text_ != null)
				return;

			var vsys = ((W.VamSys)Cue.Instance.Sys);

			text_ = vsys.GetStringParameter(
				person_, "SpeechBubble", "bubbleText");
		}
	}
}
