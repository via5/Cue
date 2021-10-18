using UnityEngine;

namespace Cue.Sys.Vam
{
	class ColliderBodyPart : VamBodyPart
	{
		private Collider c_;
		private Collider[] colliders_;
		private FreeControllerV3 fc_;
		private Rigidbody rb_;

		public ColliderBodyPart(
			VamAtom a, int type, Collider c, FreeControllerV3 fc,
			Rigidbody closestRb)
				: base(a, type)
		{
			c_ = c;
			colliders_ = new Collider[] { c };
			fc_ = fc;
			rb_ = closestRb;
		}

		public override Transform Transform
		{
			get { return c_.transform; }
		}

		public override Rigidbody Rigidbody
		{
			get { return rb_; }
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
			get { return (fc_?.isGrabbing ?? false); }
		}

		public override Vector3 ControlPosition
		{
			get { return U.FromUnity(c_.bounds.center); }
			set { Cue.LogError("cannot move colliders"); }
		}

		public override Quaternion ControlRotation
		{
			get { return U.FromUnity(c_.transform.rotation); }
			set { Cue.LogError("cannot rotate colliders"); }
		}

		public override Vector3 Position
		{
			get { return ControlPosition; }
		}

		public override Quaternion Rotation
		{
			get { return ControlRotation; }
		}

		protected override Collider[] GetColliders()
		{
			return colliders_;
		}

		public override string ToString()
		{
			var ignore = new string[]
			{
				"AutoColliderFemaleAutoColliders",
				"AutoColliderMaleAutoColliders"
			};

			string s = "";

			foreach (var i in ignore)
			{
				if (c_.name.StartsWith(i))
				{
					s = c_.name.Substring(i.Length);
					break;
				}
			}

			if (s == "")
				s = c_.name;

			if (fc_ != null)
				s = fc_.name + "." + s;

			return "collider " + s;
		}
	}
}
