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
			transform.position = Sys.Vam.U.ToUnity(v);
		}

		public void LateUpdate()
		{
			if (hasPos_)
			{
				transform.position = Sys.Vam.U.ToUnity(pos_);
				hasPos_ = false;
			}
		}
	}

	class VamEyes : IEyes
	{
		private Person person_;
		private Sys.Vam.StringChooserParameter lookMode_;
		private Sys.Vam.FloatParameter leftRightAngle_;
		private Sys.Vam.FloatParameter upDownAngle_;
		private Sys.Vam.BoolParameter blink_;
		private Rigidbody eyes_;
		private VamEyesBehaviour eyesImpl_ = null;
		private Vector3 pos_ = Vector3.Zero;
		private float minDistance_ = 0.5f;
		private bool update_ = false;

		public VamEyes(Person p)
		{
			person_ = p;
			lookMode_ = new Sys.Vam.StringChooserParameter(p, "Eyes", "lookMode");
			leftRightAngle_ = new Sys.Vam.FloatParameter(p, "Eyes", "leftRightAngleAdjust");
			upDownAngle_ = new Sys.Vam.FloatParameter(p, "Eyes", "upDownAngleAdjust");
			blink_ = new Sys.Vam.BoolParameter(p, "EyelidControl", "blinkEnabled");

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

		public Vector3 TargetPosition
		{
			get
			{
				if (eyes_ == null)
					return Vector3.Zero;
				else
					return pos_;
			}
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
				eyesImpl_?.SetPosition(AdjustedPosition());
		}

		private Vector3 AdjustedPosition()
		{
			var pos = pos_;

			var head = person_.Body.Get(BP.Head).Position;
			var d = Vector3.Distance(head, pos);

			if (d < minDistance_)
			{
				var add = minDistance_ - d;
				var dir = (pos - head).Normalized;

				pos += (dir * add);
			}

			return pos;
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
		private Sys.Vam.StringParameter text_;
		private string lastText_ = "";

		public VamSpeaker(Person p)
		{
			person_ = p;
			text_ = new Sys.Vam.StringParameter(p, "SpeechBubble", "bubbleText");
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
