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
		private TriggerInfo[] triggers_ = null;
		private bool enabled_ = false;

		protected TriggerBodyPart(VamAtom a, int type)
			: base(a, type)
		{
		}

		public TriggerBodyPart(
			VamAtom a, int type, CollisionTriggerEventHandler h,
			FreeControllerV3 fc, Transform tr, string[] ignoreTransforms)
				: base(a, type)
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
			var rb = U.FindRigidbody(atom_.Atom, "hip");
			if (rb == null)
				Cue.LogError($"{atom_.ID}: trigger {h_.name}: no hip");
			else
				ignoreStop_ = rb.transform;

			var list = new List<Transform>();
			for (int i = 0; i < ignoreTransforms.Length; ++i)
			{
				rb = U.FindRigidbody(atom_.Atom, ignoreTransforms[i]);

				if (rb != null)
				{
					list.Add(rb.transform);
				}
				else
				{
					var t = U.FindChildRecursive(
						atom_.Atom, ignoreTransforms[i])?.transform;

					if (t != null)
					{
						list.Add(t);
					}
					else
					{
						Cue.LogError(
							$"{atom_.ID}: trigger {h_.name}: " +
							$"no ignore {ignoreTransforms[i]}");
					}
				}
			}

			if (list.Count > 0)
				ignoreTransforms_ = list.ToArray();
		}

		public override Transform Transform
		{
			get { return t_; }
		}

		public override Rigidbody Rigidbody
		{
			get { return h_.thisRigidbody; }
		}

		public override FreeControllerV3 Controller
		{
			get { return fc_; }
		}

		public override bool CanTrigger
		{
			get { return true; }
		}

		public override TriggerInfo[] GetTriggers()
		{
			if (!Exists)
				return null;

			UpdateTriggers();
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

			var found = new bool[Cue.Instance.AllPersons.Count, BP.Count];
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
				if (bp == BP.Penis)
				{
					// probably the dildo touching genitals, ignore
					return false;
				}
				else
				{
					if (Type == BP.Penis)
					{
						if (bp == BP.Hips)
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
				var pn = p.Body.Get(BP.Penis).VamSys as TriggerBodyPart;
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
				if (h_.thisRigidbody == null)
					return Vector3.Zero;
				else
					return U.FromUnity(h_.thisRigidbody.position);
			}

			set { Cue.LogError("cannot move triggers"); }
		}

		public override Quaternion ControlRotation
		{
			get
			{
				if (h_.thisRigidbody == null)
					return Quaternion.Zero;
				else
					return U.FromUnity(h_.thisRigidbody.rotation);
			}

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
}
