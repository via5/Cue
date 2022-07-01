using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class TriggerBodyPart : VamBodyPart
	{
		private CollisionTriggerEventHandler h_;
		private Trigger trigger_ = null;
		private FreeControllerV3 fc_ = null;
		private Transform t_ = null;
		private Transform ignoreStop_ = null;
		private Transform[] ignoreTransforms_ = new Transform[0];
		private bool enabled_ = false;

		private List<Atom> foundOtherCache_ = null;

		protected TriggerBodyPart(VamAtom a, BodyPartType type)
			: base(a, type)
		{
		}

		public TriggerBodyPart(
			VamAtom a, BodyPartType type, CollisionTriggerEventHandler h,
			FreeControllerV3 fc, Transform tr,
			string[] ignoreTransforms, string[] colliders)
				: base(a, type, colliders)
		{
			Init(h, fc, tr, ignoreTransforms);
		}

		public override bool Exists
		{
			get { return (h_ != null && enabled_); }
		}

		protected bool Enabled
		{
			get { return enabled_; }
			set { enabled_ = value; }
		}

		protected void Init(
			CollisionTriggerEventHandler h,
			FreeControllerV3 fc, Transform tr, string[] ignoreTransforms)
		{
			h_ = h;
			trigger_ = h?.collisionTrigger?.trigger;
			fc_ = fc;
			t_ = tr;
			ignoreTransforms_ = new Transform[0];

			if (h_ == null)
			{
				enabled_ = false;
			}
			else
			{
				enabled_ = true;

				if (ignoreTransforms != null)
					FindIgnoreTransforms(ignoreTransforms);
			}
		}

		private void FindIgnoreTransforms(string[] ignoreTransforms)
		{
			var rb = U.FindRigidbody(VamAtom.Atom, "hip");
			if (rb == null)
				Log.Error($"{Atom.ID}: trigger {h_.name}: no hip");
			else
				ignoreStop_ = rb.transform;

			var list = new List<Transform>();
			for (int i = 0; i < ignoreTransforms.Length; ++i)
			{
				rb = U.FindRigidbody(VamAtom.Atom, ignoreTransforms[i]);

				if (rb != null)
				{
					list.Add(rb.transform);
				}
				else
				{
					var t = U.FindChildRecursive(
						VamAtom.Atom, ignoreTransforms[i])?.transform;

					if (t != null)
					{
						list.Add(t);
					}
					else
					{
						Log.Error(
							$"{Atom.ID}: trigger {h_.name}: " +
							$"no ignore {ignoreTransforms[i]}");
					}
				}
			}

			if (list.Count > 0)
				ignoreTransforms_ = list.ToArray();
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
					return Quaternion.Zero;
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
