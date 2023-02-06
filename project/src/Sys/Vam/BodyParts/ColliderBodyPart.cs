using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class ColliderBodyPart : VamBodyPart
	{
		private FreeControllerV3 fc_;
		private Rigidbody rb2_, closestRb_;
		private Collider main_ = null;
		private CollisionTriggerEventHandler[] hs_ = null;

		public ColliderBodyPart(
			VamAtom a, BodyPartType type, Collider[] cs, FreeControllerV3 fc,
			Rigidbody rb, Rigidbody closestRb, string[] ignoreBodyParts,
			string[] triggers)
				: base(a, type, cs, ignoreBodyParts)
		{
			fc_ = fc;
			rb2_ = rb;
			closestRb_ = closestRb;

			if (cs != null && cs.Length > 0)
				main_ = cs[0];

			var hs = new List<CollisionTriggerEventHandler>();

			foreach (var tn in triggers)
			{
				var t = FindTrigger(tn);
				if (t != null)
					hs.Add(t);
			}

			if (hs.Count > 0)
				hs_ = hs.ToArray();
		}

		protected ColliderBodyPart(VamAtom a, BodyPartType type)
			: base(a, type)
		{
		}

		private CollisionTriggerEventHandler FindTrigger(string name)
		{
			var o = U.FindChildRecursive(VamAtom.Atom.transform, name);
			if (o == null)
			{
				Log.Error($"trigger {name} not found");
				return null;
			}

			var t = o.GetComponentInChildren<CollisionTriggerEventHandler>();
			if (t == null)
			{
				Log.Error($"trigger {name} has no event handler");
				return null;
			}

			if (t.thisRigidbody == null)
			{
				Log.Error($"trigger {name} has no rb");
				return null;
			}

			return t;
		}

		protected void Set(Collider[] cs, FreeControllerV3 fc, string[] ignoreBodyParts)
		{
			fc_ = fc;

			if (cs != null && cs.Length > 0)
				main_ = cs[0];

			Set(cs, ignoreBodyParts);
		}

		public override Rigidbody Rigidbody
		{
			get { return rb2_ ?? closestRb_; }
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

		private Collider MainCollider
		{
			get { return main_; }
		}

		public override Vector3 ControlPosition
		{
			get { return U.FromUnity(MainCollider.bounds.center); }
			set { Log.Error("cannot move colliders"); }
		}

		public override Quaternion ControlRotation
		{
			get { return U.FromUnity(MainCollider.transform.rotation); }
			set { Log.Error("cannot rotate colliders"); }
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

		protected override bool DoContainsTransform(Transform t, bool debug)
		{
			if (rb2_ != null)
			{
				if (rb2_.transform == t)
				{
					if (debug)
						Log.Error($"has rb, found {t.name}");

					return true;
				}
				else
				{
					if (debug)
						Log.Error($"has rb, {t.name} is not {rb2_.name}");
				}
			}

			return false;
		}

		public string ToDetailedString()
		{
			var ignore = new string[]
			{
				"AutoColliderFemaleAutoColliders",
				"AutoColliderMaleAutoColliders"
			};

			string s = "";

			if (rb2_ == null)
			{
				foreach (var i in ignore)
				{
					if (MainCollider.name.StartsWith(i))
					{
						s = MainCollider.name.Substring(i.Length);
						break;
					}
				}

				if (s == "")
					s = MainCollider.name;
			}
			else
			{
				s += rb2_.name;
			}

			if (fc_ != null)
				s = fc_.name + "." + s;

			return "collider " + s;
		}
	}
}
