using UnityEngine;

namespace Cue.Sys.Vam
{
	class EyesBodyPart : VamBodyPart
	{
		private Transform lEye_ = null;
		private Transform rEye_ = null;
		private Rigidbody head_;

		public EyesBodyPart(VamAtom a)
			: base(a, BP.Eyes)
		{
			foreach (var t in a.Atom.GetComponentsInChildren<DAZBone>())
			{
				if (t.name == "lEye")
					lEye_ = t.transform;
				else if (t.name == "rEye")
					rEye_ = t.transform;

				if (lEye_ != null && rEye_ != null)
					break;
			}

			if (lEye_ == null)
				Cue.LogError($"{a.ID} has no left eye");

			if (rEye_ == null)
				Cue.LogError($"{a.ID} has no right eye");

			head_ = U.FindRigidbody(atom_.Atom, "head");
			if (head_ == null)
				Cue.LogError($"{a.ID} has no head");
		}

		public override Transform Transform
		{
			get { return lEye_; }
		}

		public override Rigidbody Rigidbody
		{
			get { return head_; }
		}

		public override bool CanGrab { get { return false; } }
		public override bool Grabbed { get { return false; } }

		public override Vector3 ControlPosition
		{
			get
			{
				if (atom_.Possessed)
					return Cue.Instance.Sys.CameraPosition;
				else if (lEye_ != null && rEye_ != null)
					return U.FromUnity((lEye_.position + rEye_.position) / 2);
				else if (head_ != null)
					return U.FromUnity(head_.transform.position) + new Vector3(0, 0.05f, 0);
				else
					return Vector3.Zero;
			}

			set { Cue.LogError("cannot move eyes"); }
		}

		public override Quaternion ControlRotation
		{
			get { return U.FromUnity(head_.rotation); }
			set { Cue.LogError("cannot rotate eyes"); }
		}

		public override Vector3 Position
		{
			get { return ControlPosition; }
		}

		public override Quaternion Rotation
		{
			get { return ControlRotation; }
		}

		public override string ToString()
		{
			return $"eyes {Position}";
		}
	}
}
