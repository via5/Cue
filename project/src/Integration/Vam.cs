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
		private IObject object_ = null;
		private bool camera_ = false;

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
			get { return blink_.GetValue(); }
			set { blink_.SetValue(value); }
		}

		public void LookAt(IObject o)
		{
			object_ = o;
			camera_ = false;
			lookMode_.SetValue("Target");
			eyesImpl_.SetPosition(object_.EyeInterest);
		}

		public void LookAt(Vector3 p)
		{
			object_ = null;
			camera_ = false;
			lookMode_.SetValue("Target");
			eyesImpl_.SetPosition(p);
		}

		public void LookInFront()
		{
			object_ = null;
			camera_ = false;

			eyesImpl_.SetPosition(
				person_.Body.Head?.Position ?? Vector3.Zero +
				Vector3.Rotate(new Vector3(0, 0, 1), person_.Bearing));

			lookMode_.SetValue("None");
		}

		public void LookAtNothing()
		{
			object_ = null;
			camera_ = false;
			lookMode_.SetValue("None");
		}

		public void LookAtCamera()
		{
			LookAt(Cue.Instance.Sys.Camera);
			camera_ = true;
		}

		public void Update(float s)
		{
			if (object_ != null)
				eyesImpl_.SetPosition(object_.EyeInterest);
			else if (camera_)
				eyesImpl_.SetPosition(Cue.Instance.Sys.Camera);
		}

		public override string ToString()
		{
			return $"vam: blink={blink_.GetValue()} mode={lookMode_.GetValue()}";
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
			text_.SetValue(s);
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
