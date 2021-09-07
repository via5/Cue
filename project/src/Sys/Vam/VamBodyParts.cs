using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	abstract class VamBodyPart : IBodyPart
	{
		protected VamAtom atom_;
		private int type_;

		protected VamBodyPart(VamAtom a, int t)
		{
			atom_ = a;
			type_ = t;
		}

		public int Type { get { return type_; } }

		public virtual Transform Transform { get { return null; } }
		public virtual Rigidbody Rigidbody { get { return null; } }

		public virtual bool CanTrigger { get { return false; } }
		public virtual TriggerInfo[] GetTriggers() { return null; }

		public virtual bool CanGrab { get { return false; } }
		public virtual bool Grabbed { get { return false; } }

		public abstract Vector3 ControlPosition { get; set; }
		public abstract Quaternion ControlRotation { get; set; }
		public abstract Vector3 Position { get; }
		public abstract Quaternion Rotation { get; }

		public virtual void AddRelativeForce(Vector3 v)
		{
			// no-op
		}

		public virtual void AddRelativeTorque(Vector3 v)
		{
			// no-op
		}
	}


	class RigidbodyBodyPart : VamBodyPart
	{
		private Rigidbody rb_;
		private FreeControllerV3 fc_ = null;

		public RigidbodyBodyPart(VamAtom a, int type, Rigidbody rb, FreeControllerV3 fc)
			: base(a, type)
		{
			rb_ = rb;
			fc_ = fc;
		}

		public override Transform Transform
		{
			get { return rb_.transform; }
		}

		public override Rigidbody Rigidbody
		{
			get { return rb_; }
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


	class ColliderBodyPart : VamBodyPart
	{
		private Collider c_;
		private FreeControllerV3 fc_;

		public ColliderBodyPart(VamAtom a, int type, Collider c, FreeControllerV3 fc)
			: base(a, type)
		{
			c_ = c;
			fc_ = fc;
		}

		public override Transform Transform
		{
			get { return c_.transform; }
		}

		public override Rigidbody Rigidbody
		{
			get { return null; }
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

			return "collider " + s;
		}
	}


	class TriggerBodyPart : VamBodyPart
	{
		private const float TriggerCheckDelay = 1;

		private CollisionTriggerEventHandler h_;
		private Trigger trigger_;
		private Rigidbody rb_;
		private FreeControllerV3 fc_;
		private Transform t_;
		private Transform ignoreStop_ = null;
		private Transform[] ignoreTransforms_ = new Transform[0];
		private TriggerInfo[] triggers_ = null;
		private float lastTriggerCheck_ = 0;

		public TriggerBodyPart(
			VamAtom a, int type, CollisionTriggerEventHandler h,
			FreeControllerV3 fc, Transform tr, string[] ignoreTransforms)
				: base(a, type)
		{
			h_ = h;
			trigger_ = h.collisionTrigger.trigger;
			rb_ = h.thisRigidbody;
			fc_ = fc;
			t_ = tr;

			if (rb_ == null)
				Cue.LogError($"{a.ID}: trigger {h.name}: no rb");

			if (ignoreTransforms != null)
			{
				var rb = Cue.Instance.VamSys.FindRigidbody(a.Atom, "hip");
				if (rb == null)
					Cue.LogError($"{a.ID}: trigger {h.name}: no hip");
				else
					ignoreStop_ = rb.transform;

				var list = new List<Transform>();
				for (int i = 0; i < ignoreTransforms.Length; ++i)
				{
					rb = Cue.Instance.VamSys.FindRigidbody(a.Atom, ignoreTransforms[i]);
					if (rb != null)
					{
						list.Add(rb.transform);
					}
					else
					{
						var t = Cue.Instance.VamSys.FindChildRecursive(
							a.Atom, ignoreTransforms[i])?.transform;

						if (t != null)
							list.Add(t);
						else
							Cue.LogError($"{a.ID}: trigger {h.name}: no ignore {ignoreTransforms[i]}");
					}
				}

				if (list.Count > 0)
					ignoreTransforms_ = list.ToArray();
			}
		}

		public override Transform Transform
		{
			get { return t_; }
		}

		public override Rigidbody Rigidbody
		{
			get { return rb_; }
		}

		public override bool CanTrigger
		{
			get { return true; }
		}

		public override TriggerInfo[] GetTriggers()
		{
			if (Time.realtimeSinceStartup >= (lastTriggerCheck_ + TriggerCheckDelay))
			{
				lastTriggerCheck_ = Time.realtimeSinceStartup;
				UpdateTriggers();
			}

			return triggers_;
		}

		private void UpdateTriggers()
		{
			if (!trigger_.active)
			{
				triggers_ = null;
				return;
			}

			List<TriggerInfo> list = null;

			var found = new bool[Cue.Instance.AllPersons.Count, BodyParts.Count];
			List<string> foundOther = null;

			foreach (var kv in h_.collidingWithDictionary)
			{
				if (!kv.Value || kv.Key == null)
					continue;

				if (!ValidTrigger(kv.Key))
					continue;

				if (list == null)
					list = new List<TriggerInfo>();

				var p = PersonForCollider(kv.Key);
				if (p == null)
				{
					bool skip = false;

					if (foundOther == null)
						foundOther = new List<string>();
					else if (foundOther.Contains(kv.Key.name))
						skip = true;
					else
						foundOther.Add(kv.Key.name);

					if (!skip)
						list.Add(new TriggerInfo(-1, -1, 1.0f));
				}
				else
				{
					var bp = ((VamBasicBody)p.Atom.Body).BodyPartForCollider(kv.Key);

					if (bp == -1)
					{
						//Cue.LogError($"no body part for {kv.Key.name} in {p.ID}");
					}
					else if (!found[p.PersonIndex, bp])
					{
						if (!ValidCollision(p, bp))
							continue;

						//Cue.LogInfo($"{kv.Key}");

						found[p.PersonIndex, bp] = true;
						list.Add(new TriggerInfo(p.PersonIndex, bp, 1.0f));
					}
				}
			}

			if (list == null)
				triggers_ = null;
			else
				triggers_ = list.ToArray();
		}

		private bool ValidCollision(Person p, int bp)
		{
			// self collision
			if (p.VamAtom == atom_)
			{
				if (bp == BodyParts.Penis)
				{
					// probably the dildo touching genitals, ignore
					return false;
				}
				else
				{
					if (Type == BodyParts.Penis)
					{
						if (bp == BodyParts.Hips)
						{
							// probably the dildo touching genitals, ignore
							return false;
						}
					}
				}
			}

			return true;
		}

		private Person PersonForCollider(Collider c)
		{
			var a = c.transform.GetComponentInParent<Atom>();
			if (a == null)
				return null;

			if (Cue.Instance.VamSys.IsVRHands(a))
			{
				foreach (var p in Cue.Instance.ActivePersons)
				{
					if (p.Atom == Cue.Instance.VamSys.CameraAtom)
						return p;
				}

				return null;
			}

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p.VamAtom?.Atom == a)
					return p;

				// todo, handles dildos separately because the trigger is not
				// part of the person itself, it's a different atom
				var pn = p.Body.Get(BodyParts.Penis).VamSys as TriggerBodyPart;
				if (pn != null && pn.Transform == a.transform)
					return p;
			}

			return null;
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
				if (rb_ == null)
					return Vector3.Zero;
				else
					return U.FromUnity(rb_.position);
			}

			set { Cue.LogError("cannot move triggers"); }
		}

		public override Quaternion ControlRotation
		{
			get { return U.FromUnity(rb_.rotation); }
			set { Cue.LogError("cannot rotate triggers"); }
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
			string s = "";

			if (fc_?.containingAtom != null && atom_?.Atom != fc_.containingAtom)
				s += fc_.containingAtom.uid;

			if (trigger_ != null && trigger_.displayName != "")
			{
				if (s != "")
					s += ".";

				s += trigger_.displayName;
			}

			return $"trigger {s}";
		}

		private bool ValidTrigger(Collider c)
		{
			var t = c.transform;

			while (t != null)
			{
				if (t == ignoreStop_)
					break;

				for (int i = 0; i < ignoreTransforms_.Length; ++i)
				{
					if (ignoreTransforms_[i] == t)
						return false;
				}

				t = t.parent;
			}

			return true;
		}
	}


	class EyesBodyPart : VamBodyPart
	{
		private Transform lEye_ = null;
		private Transform rEye_ = null;
		private Rigidbody head_;

		public EyesBodyPart(VamAtom a)
			: base(a, BodyParts.Eyes)
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

			head_ = Cue.Instance.VamSys.FindRigidbody(atom_.Atom, "head");
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
