﻿using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class RigidbodyBodyPart : VamBodyPart
	{
		private Rigidbody rb_;
		private FreeControllerV3 fc_ = null;
		private Collider[] colliders_;

		public RigidbodyBodyPart(
			VamAtom a, int type, Rigidbody rb, FreeControllerV3 fc,
			string[] colliders)
				: base(a, type)
		{
			rb_ = rb;
			fc_ = fc;

			var cs = new List<Collider>();
			foreach (var cn in colliders)
			{
				var c = U.FindCollider(a.Atom, cn);
				if (c == null)
				{
					Cue.LogError($"collider {cn} not found");
					continue;
				}

				cs.Add(c);
			}

			colliders_ = cs.ToArray();
		}

		public override Transform Transform
		{
			get { return rb_.transform; }
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
			get { return U.FromUnity(rb_.position); }
		}

		public override Quaternion Rotation
		{
			get { return U.FromUnity(rb_.rotation); }
		}

		protected override Collider[] GetColliders()
		{
			return colliders_;
		}

		public override void AddRelativeForce(Vector3 v)
		{
			rb_.AddRelativeForce(U.ToUnity(v));
		}

		public override void AddRelativeTorque(Vector3 v)
		{
			rb_.AddRelativeTorque(U.ToUnity(v));
		}

		public override string ToString()
		{
			return $"rb {rb_.name}";
		}
	}
}