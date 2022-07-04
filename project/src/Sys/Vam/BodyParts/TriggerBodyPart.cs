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
		private bool enabled_ = false;

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
			get { return (h_ != null && enabled_); }
		}

		protected bool Enabled
		{
			get { return enabled_; }
			set { enabled_ = value; }
		}

		protected void Init(
			CollisionTriggerEventHandler h,
			FreeControllerV3 fc, Transform tr)
		{
			h_ = h;
			trigger_ = h?.collisionTrigger?.trigger;
			fc_ = fc;
			t_ = tr;

			if (h_ == null)
			{
				enabled_ = false;
			}
			else
			{
				enabled_ = true;
			}
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

		protected override void UpdateTriggers()
		{
			base.UpdateTriggers();

			if (!trigger_.active)
				return;

			foreach (var kv in h_.collidingWithDictionary)
			{
				if (!kv.Value || kv.Key == null)
					continue;

				var bp = Cue.Instance.VamSys.BodyPartForTransform(kv.Key.transform) as VamBodyPart;

				if (bp == null)
				{
					var a = U.AtomForCollider(kv.Key);
					AddExternalCollision(a, 1.0f);
				}
				else
				{
					if (VamBodyPart.IgnoreTrigger(
							bp.Atom as VamAtom, bp, Atom as VamAtom, this))
					{
						continue;
					}

					var p = Cue.Instance.PersonForAtom(bp.Atom);
					if (p == null)
						return;

					AddPersonCollision(p.PersonIndex, bp.Type, 1.0f);
				}
			}
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
