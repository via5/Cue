using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class RigidbodyBodyPart : VamBodyPart
	{
		private Rigidbody[] rbs_;
		private Rigidbody forForce_;
		private FreeControllerV3 fc_ = null;

		public RigidbodyBodyPart(
			VamAtom a, int type, Rigidbody[] rbs, FreeControllerV3 fc,
			string[] colliders, Rigidbody forForce)
				: base(a, type, colliders)
		{
			Cue.Assert(rbs != null, $"null rbs in {a.ID} {BP.ToString(Type)}");
			foreach (var rb in rbs)
				Cue.Assert(rb != null, $"null rb in {a.ID} {BP.ToString(Type)}");

			rbs_ = rbs;
			fc_ = fc;
			forForce_ = forForce;

			if (forForce_ == null && rbs_ != null && rbs_.Length > 0)
				forForce_ = rbs_[0];
		}

		public override Rigidbody Rigidbody
		{
			get { return rbs_?[0]; }
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
			get { return U.FromUnity(Rigidbody.position); }
		}

		public override Quaternion Rotation
		{
			get { return U.FromUnity(Rigidbody.rotation); }
		}

		public override bool ContainsTransform(Transform t, bool debug)
		{
			if (rbs_ == null)
			{
				if (debug)
					Log.Error($"{t.name} not found, rbs is null");

				return false;
			}
			else
			{
				if (debug)
					Log.Error($"checking in {rbs_.Length} rbs");
			}

			for (int i = 0; i < rbs_.Length; ++i)
			{
				if (rbs_[i].transform == t)
				{
					if (debug)
						Log.Error($"found {t.name}");

					return true;
				}
				else
				{
					if (debug)
						Log.Error($"{t.name} is not {rbs_[i].transform.name}");
				}
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
			if (forForce_ != null)
				forForce_.AddRelativeForce(U.ToUnity(v));
		}

		public override void AddRelativeTorque(Vector3 v)
		{
			if (forForce_ != null)
				forForce_.AddRelativeTorque(U.ToUnity(v));
		}

		public override void AddForce(Vector3 v)
		{
			if (forForce_ != null)
				forForce_.AddForce(U.ToUnity(v));
		}

		public override void AddTorque(Vector3 v)
		{
			if (forForce_ != null)
				forForce_.AddTorque(U.ToUnity(v));
		}

		public override string ToString()
		{
			string s = $"rb {(Rigidbody == null ? "" : Rigidbody.name)}";

			if (forForce_ != null)
				s += $" (fr={forForce_.name})";

			return s;
		}
	}
}
