using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class RigidbodyBodyPart : VamBodyPart
	{
		private Rigidbody[] rbs_;
		private FreeControllerV3 fc_ = null;

		public RigidbodyBodyPart(
			VamAtom a, int type, Rigidbody[] rbs, FreeControllerV3 fc,
			string[] colliders)
				: base(a, type, colliders)
		{
			Cue.Assert(rbs != null, $"null rbs in {a.ID} {BP.ToString(Type)}");
			foreach (var rb in rbs)
				Cue.Assert(rb != null, $"null rb in {a.ID} {BP.ToString(Type)}");

			rbs_ = rbs;
			fc_ = fc;
		}

		public override Rigidbody Rigidbody
		{
			get { return rbs_[0]; }
		}

		public override FreeControllerV3 Controller
		{
			get { return fc_; }
		}

		public override bool CanGrab
		{
			get { return (fc_ != null); }
		}

		public override bool Grabbed
		{
			get { return fc_?.isGrabbing ?? false; }
		}

		public override Vector3 ControlPosition
		{
			get { return U.FromUnity(fc_.transform.position); }
			set { fc_.transform.position = U.ToUnity(value); }
		}

		public override Quaternion ControlRotation
		{
			get { return U.FromUnity(fc_.transform.rotation); }
			set { fc_.transform.rotation = U.ToUnity(value); }
		}

		public override Vector3 Position
		{
			get { return U.FromUnity(rbs_[0].position); }
		}

		public override Quaternion Rotation
		{
			get { return U.FromUnity(rbs_[0].rotation); }
		}

		public override bool ContainsTransform(Transform t)
		{
			for (int i = 0; i < rbs_.Length; ++i)
			{
				if (rbs_[i].transform == t)
					return true;
			}

			return false;
		}

		public override bool CanApplyForce()
		{
			if (fc_ != null)
			{
				if (fc_.currentPositionState != FreeControllerV3.PositionState.On ||
					fc_.currentRotationState != FreeControllerV3.RotationState.On)
				{
					return false;
				}
			}

			return true;
		}

		public override void AddRelativeForce(Vector3 v)
		{
			rbs_[0].AddRelativeForce(U.ToUnity(v));
		}

		public override void AddRelativeTorque(Vector3 v)
		{
			rbs_[0].AddRelativeTorque(U.ToUnity(v));
		}

		public override void AddForce(Vector3 v)
		{
			rbs_[0].AddForce(U.ToUnity(v));
		}

		public override void AddTorque(Vector3 v)
		{
			rbs_[0].AddTorque(U.ToUnity(v));
		}

		public override string ToString()
		{
			return $"rb {(rbs_ == null ? "" : rbs_[0].name)}";
		}
	}
}
