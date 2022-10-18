using UnityEngine;

namespace Cue
{
	class VamEyesBehaviour : MonoBehaviour
	{
		private Vector3 pos_ = new Vector3();
		private bool hasPos_ = false;

		public Vector3 Position
		{
			get
			{
				return Sys.Vam.U.FromUnity(transform.position);
			}

			set
			{
				pos_ = value;
				hasPos_ = true;
				transform.position = Sys.Vam.U.ToUnity(value);
			}
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
		private bool updatePosition_ = false;

		private bool saccade_ = true;
		private Duration saccadeDuration_ = null;

		public VamEyes(Person p)
		{
			person_ = p;
			lookMode_ = new Sys.Vam.StringChooserParameter(p, "Eyes", "lookMode");
			leftRightAngle_ = new Sys.Vam.FloatParameter(p, "Eyes", "leftRightAngleAdjust");
			upDownAngle_ = new Sys.Vam.FloatParameter(p, "Eyes", "upDownAngleAdjust");
			blink_ = new Sys.Vam.BoolParameter(p, "EyelidControl", "blinkEnabled");

			eyes_ = Sys.Vam.U.FindRigidbody(person_, "eyeTargetControl");

			if (eyes_ != null)
			{
				eyes_.detectCollisions = false;

				foreach (var c in eyes_.gameObject.GetComponents<Component>())
				{
					if (c != null && c.ToString().Contains("Cue.VamEyesBehaviour"))
						UnityEngine.Object.Destroy(c);
				}

				eyesImpl_ = eyes_.gameObject.AddComponent<VamEyesBehaviour>();
			}

			ResetSaccade();

			person_.PersonalityChanged += OnPersonalityChanged;
		}

		public void OnPluginState(bool b)
		{
			if (eyes_ != null)
				eyes_.detectCollisions = !b;
		}

		public bool Blink
		{
			get { return blink_.Value; }
			set { blink_.Value = value; }
		}

		public bool Saccade
		{
			get
			{
				return saccade_;
			}

			set
			{
				if (saccade_ != value)
				{
					if (!value)
						ResetSaccade();

					saccade_ = value;
				}
			}
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
			updatePosition_ = true;
		}

		public void LookAtNothing()
		{
			lookMode_.Value = "None";
			updatePosition_ = false;
		}

		public void Update(float s)
		{
			if (updatePosition_)
				SetPosition(AdjustedPosition(), s);

			if (saccade_)
				UpdateSaccade(s);
		}

		private void ResetSaccade()
		{
			leftRightAngle_.Value = leftRightAngle_.DefaultValue;
			upDownAngle_.Value = upDownAngle_.DefaultValue;
		}

		private void UpdateSaccade(float s)
		{
			if (saccadeDuration_ == null)
				OnPersonalityChanged();

			saccadeDuration_.Update(s, person_.Mood.GazeEnergy);

			if (saccadeDuration_.Finished)
			{
				float range = person_.Personality.Get(PS.GazeSaccadeMovementRange);

				leftRightAngle_.Value = U.RandomFloat(-range, +range);
				upDownAngle_.Value = U.RandomFloat(-range, +range);
			}
		}

		private void OnPersonalityChanged()
		{
			Saccade = person_.Personality.GetBool(PS.GazeSaccade);
			Blink = person_.Personality.GetBool(PS.GazeBlink);
			person_.Gaze.AutoBlink = person_.Personality.GetBool(PS.GazeBlink);

			saccadeDuration_ = person_.Personality.GetDuration(
				PS.GazeSaccadeInterval);
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

		private void SetPosition(Vector3 pos, float s)
		{
			if (eyesImpl_ == null)
				return;

			var pos2 = Vector3.MoveTowards(
				eyesImpl_.Position, pos,
				person_.Personality.Get(PS.GazeEyeTargetMovementSpeed) * s);

			eyesImpl_.Position = pos2;
		}

		public override string ToString()
		{
			string s = $"vam: blink={blink_} mode={lookMode_} ";

			if (updatePosition_)
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
