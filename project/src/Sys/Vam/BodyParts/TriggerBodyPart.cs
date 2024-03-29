﻿using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class TriggerBodyPart : VamBodyPart
	{
		private CollisionTriggerEventHandler h_;
		private CollisionTriggerEventHandler[] hs_ = null;
		private Trigger trigger_ = null;
		private FreeControllerV3 fc_ = null;
		private Transform t_ = null;

		protected TriggerBodyPart(VamAtom a, BodyPartType type, string[] ignoreBodyParts)
			: base(a, type, (Collider[])null, ignoreBodyParts)
		{
		}

		public TriggerBodyPart(
			VamAtom a, BodyPartType type, CollisionTriggerEventHandler h,
			FreeControllerV3 fc, Transform tr,
			string[] ignoreBodyParts, string[] colliders)
				: base(a, type, colliders, ignoreBodyParts)
		{
			Init(h, fc, tr);
		}

		public override bool Exists
		{
			get { return (h_ != null); }
		}

		protected void Init(
			CollisionTriggerEventHandler h,
			FreeControllerV3 fc, Transform tr)
		{
			h_ = h;
			trigger_ = h?.collisionTrigger?.trigger;
			fc_ = fc;
			t_ = tr;

			if (h_ != null)
				hs_ = new CollisionTriggerEventHandler[] { h_ };
		}

		public override Rigidbody Rigidbody
		{
			get { return h_?.thisRigidbody; }
		}

		public override FreeControllerV3 Controller
		{
			get { return fc_; }
		}

		protected override bool DoContainsTransform(Transform t, bool debug)
		{
			if (t_ == null)
			{
				if (debug)
					Log.Error($"{t.name} not found, null transform");

				return false;
			}

			if (t_ == t)
			{
				if (debug)
					Log.Error($"found {t.name}");

				return true;
			}
			else
			{
				if (debug)
					Log.Error($"{t.name} is not {t_.name}");

				return false;
			}
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
			get
			{
				if (h_?.thisRigidbody == null)
					return Vector3.Zero;
				else
					return U.FromUnity(h_.thisRigidbody.position);
			}

			set { Log.Error("cannot move triggers"); }
		}

		public override Quaternion ControlRotation
		{
			get
			{
				if (h_?.thisRigidbody == null)
					return Quaternion.Identity;
				else
					return U.FromUnity(h_.thisRigidbody.rotation);
			}

			set { Log.Error("cannot rotate triggers"); }
		}

		public override Vector3 Position
		{
			get { return ControlPosition; }
		}

		public override Quaternion Rotation
		{
			get { return ControlRotation; }
		}

		protected override CollisionTriggerEventHandler[] GetTriggerHandlers()
		{
			return hs_;
		}

		public string ToDetailedString()
		{
			string s = "";

			if (fc_?.containingAtom != null && VamAtom?.Atom != fc_.containingAtom)
				s += fc_.containingAtom.uid;

			if (trigger_ != null && trigger_.displayName != "")
			{
				if (s != "")
					s += ".";

				s += trigger_.displayName;
			}

			return $"trigger {s}";
		}
	}
}
