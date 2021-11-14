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

		private List<TriggerInfo> triggerCache_ = null;
		private List<string> foundOtherCache_ = null;

		protected TriggerBodyPart(VamAtom a, int type)
			: base(a, type)
		{
		}

		public TriggerBodyPart(
			VamAtom a, int type, CollisionTriggerEventHandler h,
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

		public override bool CanTrigger
		{
			get { return true; }
		}

		public override bool ContainsTransform(Transform t)
		{
			return (t_ == t);
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

			if (triggerCache_ != null)
				triggerCache_.Clear();

			if (foundOtherCache_ != null)
				foundOtherCache_.Clear();

			var found = new bool[Cue.Instance.AllPersons.Count, BP.Count];

			foreach (var kv in h_.collidingWithDictionary)
			{
				if (!kv.Value || kv.Key == null)
					continue;

				if (!ValidTrigger(kv.Key))
					continue;

				if (triggerCache_ == null)
					triggerCache_ = new List<TriggerInfo>();

				var bp = Cue.Instance.VamSys.BodyPartForTransform(kv.Key.transform);

				if (bp == null)
				{
					bool skip = false;

					if (foundOtherCache_ == null)
						foundOtherCache_ = new List<string>();
					else if (foundOtherCache_.Contains(kv.Key.name))
						skip = true;
					else
						foundOtherCache_.Add(kv.Key.name);

					if (!skip)
						triggerCache_.Add(TriggerInfo.None);
				}
				else
				{
					var p = Cue.Instance.PersonForAtom(bp.Atom);
					var personIndex = p.PersonIndex;

					if (!found[personIndex, bp.Type])
					{
						if (!ValidCollision(p, bp.Type))
							continue;

						found[p.PersonIndex, bp.Type] = true;
						triggerCache_.Add(new TriggerInfo(p.PersonIndex, bp.Type, 1.0f));
					}
				}
			}

			if (triggerCache_ == null)
				triggers_ = null;
			else
				triggers_ = triggerCache_.ToArray();
		}

		private bool ValidCollision(Person p, int bp)
		{
			// self collision
			if (p.VamAtom == VamAtom)
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

		public override string ToString()
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
