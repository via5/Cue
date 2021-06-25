using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue
{
	class VamEyesBehaviour : MonoBehaviour
	{
		private Vector3 pos_ = new Vector3();
		private bool hasPos_ = false;

		public void SetPosition(Vector3 v)
		{
			pos_ = v;
			hasPos_ = true;
			transform.position = W.VamU.ToUnity(v);
		}

		public void LateUpdate()
		{
			if (hasPos_)
			{
				transform.position = W.VamU.ToUnity(pos_);
				hasPos_ = false;
			}
		}
	}

	class VamEyes : IEyes
	{
		private Person person_;
		private W.VamStringChooserParameter lookMode_;
		private W.VamFloatParameter leftRightAngle_;
		private W.VamFloatParameter upDownAngle_;
		private W.VamBoolParameter blink_;
		private Rigidbody eyes_;
		private VamEyesBehaviour eyesImpl_ = null;
		private Vector3 pos_ = Vector3.Zero;
		private bool update_ = false;

		public VamEyes(Person p)
		{
			person_ = p;
			lookMode_ = new W.VamStringChooserParameter(p, "Eyes", "lookMode");
			leftRightAngle_ = new W.VamFloatParameter(p, "Eyes", "leftRightAngleAdjust");
			upDownAngle_ = new W.VamFloatParameter(p, "Eyes", "upDownAngleAdjust");
			blink_ = new W.VamBoolParameter(p, "EyelidControl", "blinkEnabled");

			eyes_ = Cue.Instance.VamSys?.FindRigidbody(person_, "eyeTargetControl");

			if (eyes_ == null)
			{
				Cue.LogError("atom " + p.ID + " has no eyeTargetControl");
			}
			else
			{
				foreach (var c in eyes_.gameObject.GetComponents<Component>())
				{
					if (c != null && c.ToString().Contains("Cue.VamEyesBehaviour"))
						UnityEngine.Object.Destroy(c);
				}

				eyesImpl_ = eyes_.gameObject.AddComponent<VamEyesBehaviour>();
			}
		}

		public bool Blink
		{
			get { return blink_.Value; }
			set { blink_.Value = value; }
		}

		public Vector3 Position
		{
			get { return W.VamU.FromUnity(eyes_.position); }
		}

		public void LookAt(Vector3 p)
		{
			pos_ = p;
			lookMode_.Value = "Target";
			update_ = true;
			eyesImpl_?.SetPosition(p);
		}

		public void LookAtNothing()
		{
			lookMode_.Value = "None";
			update_ = false;
		}

		public void Update(float s)
		{
			if (update_)
				eyesImpl_?.SetPosition(pos_);
		}

		public override string ToString()
		{
			string s = $"vam: blink={blink_} mode={lookMode_} ";

			if (update_)
				s += $"pos={pos_}";
			else
				s += $"pos=N/A";

			return s;
		}
	}


	class VamSpeaker : ISpeaker
	{
		private Person person_;
		private W.VamStringParameter text_;
		private string lastText_ = "";

		public VamSpeaker(Person p)
		{
			person_ = p;
			text_ = new W.VamStringParameter(p, "SpeechBubble", "bubbleText");
		}

		public void Say(string s)
		{
			text_.Value = s;
			lastText_ = s;
		}

		public override string ToString()
		{
			string s = "Vam: lastText=";

			if (lastText_ == "")
				s += "(none)";
			else
				s += lastText_;

			return s;
		}
	}
}
