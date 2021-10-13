using System;
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
		public virtual bool Exists { get { return true; } }

		public virtual Transform Transform { get { return null; } }
		public virtual Rigidbody Rigidbody { get { return null; } }
		public virtual FreeControllerV3 Controller { get { return null; } }

		public virtual bool CanTrigger { get { return false; } }
		public virtual TriggerInfo[] GetTriggers() { return null; }

		public virtual bool CanGrab { get { return false; } }
		public virtual bool Grabbed { get { return false; } }

		public abstract Vector3 ControlPosition { get; set; }
		public abstract Quaternion ControlRotation { get; set; }
		public abstract Vector3 Position { get; }
		public abstract Quaternion Rotation { get; }

		public virtual bool Linked
		{
			get
			{
				return
					Controller?.linkToRB != null &&
					Controller.currentPositionState == FreeControllerV3.PositionState.ParentLink &&
					Controller.currentRotationState == FreeControllerV3.RotationState.ParentLink;
			}
		}

		public virtual void LinkTo(IBodyPart other)
		{
			if (Controller == null)
			{
				Cue.LogError($"cannot link {this} to {other}, no controller");
				return;
			}

			if (other == null)
			{
				SetOn(Controller);
			}
			else
			{
				var o = other as VamBodyPart;
				if (o?.Rigidbody == null)
				{
					Cue.LogError($"cannot link {this} to {other}, not an rb");
				}
				else
				{
					if (o.Controller?.linkToRB == Rigidbody)
					{
						Cue.LogError(
							$"cannot link {this} to {other}, would be " +
							$"reciprocal");
					}
					else
					{
						SetParentLink(Controller, o.Rigidbody);
					}
				}
			}
		}

		public virtual bool IsLinkedTo(IBodyPart other)
		{
			var o = other as VamBodyPart;
			if (o == null)
				return false;

			if (Controller == null || o.Rigidbody == null)
				return false;

			return (Controller.linkToRB == o.Rigidbody);
		}

		private void SetParentLink(FreeControllerV3 fc, Rigidbody rb)
		{
			fc.linkToRB = rb;
			fc.currentPositionState = FreeControllerV3.PositionState.ParentLink;
			fc.currentRotationState = FreeControllerV3.RotationState.ParentLink;
		}

		private void SetOn(FreeControllerV3 fc)
		{
			fc.linkToRB = null;
			fc.currentPositionState = FreeControllerV3.PositionState.On;
			fc.currentRotationState = FreeControllerV3.RotationState.On;
		}

		public virtual float DistanceToSurface(IBodyPart other)
		{
			return Vector3.Distance(other.Position, Position);
		}

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
				var c = Cue.Instance.VamSys.FindCollider(a.Atom, cn);
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

		public override float DistanceToSurface(IBodyPart other)
		{
			float closest = float.MaxValue;
			var op = U.ToUnity(other.Position);

			for (int i = 0; i < colliders_.Length; ++i)
			{
				var p = colliders_[i].ClosestPoint(op);
				var d = Vector3.Distance(other.Position, U.FromUnity(p));
				closest = Math.Min(closest, d);
			}

			return closest;
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
		private Rigidbody rb_;

		public ColliderBodyPart(
			VamAtom a, int type, Collider c, FreeControllerV3 fc,
			Rigidbody closestRb)
				: base(a, type)
		{
			c_ = c;
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

		public override float DistanceToSurface(IBodyPart other)
		{
			var p = c_.ClosestPoint(U.ToUnity(other.Position));
			return Vector3.Distance(other.Position, U.FromUnity(p));
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


	class TriggerBodyPart : VamBodyPart
	{
		private CollisionTriggerEventHandler h_;
		private Trigger trigger_ = null;
		private Rigidbody rb_ = null;
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
			rb_ = h?.thisRigidbody;
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
			var rb = Cue.Instance.VamSys.FindRigidbody(atom_.Atom, "hip");
			if (rb == null)
				Cue.LogError($"{atom_.ID}: trigger {h_.name}: no hip");
			else
				ignoreStop_ = rb.transform;

			var list = new List<Transform>();
			for (int i = 0; i < ignoreTransforms.Length; ++i)
			{
				rb = Cue.Instance.VamSys.FindRigidbody(
					atom_.Atom, ignoreTransforms[i]);

				if (rb != null)
				{
					list.Add(rb.transform);
				}
				else
				{
					var t = Cue.Instance.VamSys.FindChildRecursive(
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
			get { return rb_; }
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
				if (rb_ == null)
					return Vector3.Zero;
				else
					return U.FromUnity(rb_.position);
			}

			set { Cue.LogError("cannot move triggers"); }
		}

		public override Quaternion ControlRotation
		{
			get
			{
				if (rb_ == null)
					return Quaternion.Zero;
				else
					return U.FromUnity(rb_.rotation);
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


	class EyesBodyPart : VamBodyPart
	{
		private Transform lEye_ = null;
		private Transform rEye_ = null;
		private Rigidbody head_;

		public EyesBodyPart(VamAtom a)
			: base(a, BP.Eyes)
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


	class VamStraponBodyPart : TriggerBodyPart
	{
		private IObject dildo_ = null;
		private Collider anchor_ = null;

		private float postCreateElapsed_ = 0;
		private bool postCreate_ = false;

		public VamStraponBodyPart(VamAtom a)
			: base(a, BP.Penis)
		{
			Get();
		}

		private Logger Log
		{
			get { return atom_.Log; }
		}

		private string DildoID
		{
			get { return $"Dildo#{atom_.ID}"; }
		}

		private string StraponID
		{
			get { return $"Strapon#{atom_.ID}"; }
		}

		private void Get()
		{
			var anchorName = "pelvisF4/pelvisF4Joint";
			anchor_ = Cue.Instance.VamSys.FindCollider(atom_.Atom, anchorName);
			if (anchor_ == null)
			{
				Cue.LogError($"collider {anchorName} not found in {atom_.ID}");
				return;
			}

			var d = SuperController.singleton.GetAtomByUid(DildoID);
			if (d != null)
			{
				Log.Info("dildo already exists, taking");
				SetDildo(new BasicObject(-1, new VamAtom(d)));
			}
		}

		public void Set(bool b)
		{
			if (Exists == b)
				return;

			if (b)
			{
				if (dildo_ == null)
					Create();
				else
					dildo_.Visible = true;
			}
			else
			{
				if (dildo_ != null)
					dildo_.Visible = false;
			}
		}

		private void Create()
		{
			Log.Info("creating strapon");
			AddDildo();
		}

		private void SetClothingActive(bool b)
		{
			// todo: this assumes clothing item, doesn't attempt to get the
			//       object first, would need a generic way to figure this out
			//       instead of using GetAtomByUid() in Get()

			var oc = Resources.Objects.Get("strapon");

			if (oc == null)
			{
				Log.Error("no strapon object creator");
				return;
			}

			if (b)
			{
				oc.Create(atom_, StraponID, (o) =>
				{
					Log.Error("strapon created");
				});
			}
			else
			{
				oc.Destroy(atom_, StraponID);
			}
		}

		private void AddDildo()
		{
			Log.Info("creating dildo");

			var oc = Resources.Objects.Get("dildo");
			if (oc == null)
			{
				Log.Error("no dildo object creator");
				return;
			}

			oc.Create(atom_, DildoID, (o) =>
			{
				if (o == null)
				{
					Log.Error("failed to create dildo");
					return;
				}

				SetDildo(o);
			});
		}

		private void SetDildo(IObject a)
		{
			if (a == null)
			{
				Log.Info($"removing dildo");
				dildo_ = null;
				Init(null, null, null, null);
				SetEnabled(false);
			}
			else
			{
				Log.Info($"setting dildo to {a.ID}");
				dildo_ = a;
				dildo_.Atom.Collisions = false;

				postCreate_ = true;
				postCreateElapsed_ = 0;

				DoInit();
				SetEnabled(true);
			}
		}

		private void SetEnabled(bool b)
		{
			Enabled = b;
			SetClothingActive(b);
		}

		public void LateUpdate(float s)
		{
			if (dildo_ == null || anchor_ == null)
				return;

			try
			{
				if (!dildo_.Visible && Enabled)
				{
					Log.Info($"dildo {dildo_.ID} turned off");
					SetEnabled(false);
				}
				else if (dildo_.Visible && !Enabled)
				{
					Log.Info($"dildo {dildo_.ID} turned on");
					SetEnabled(true);
				}

				if (!Enabled)
					return;

				var labia = (atom_.Body as VamBody)?.GetPart(BP.Labia);

				var q = Quaternion.Zero;
				if (labia != null)
					q = labia.Rotation;

				var v = U.FromUnity(anchor_.transform.position);
				v.Y += 0.01f;
				v.Z -= 0.01f;

				dildo_.Position = v;
				dildo_.Rotation = q;

				if (postCreate_)
				{
					postCreateElapsed_ += s;
					if (postCreateElapsed_ > 2)
					{
						postCreate_ = false;
						dildo_.Atom.Collisions = true;
					}
				}
			}
			catch (Exception)
			{
				// dildo can get deleted at any time
				Log.Error($"looks like dildo got deleted");
				SetDildo(null);
			}
		}

		public override string ToString()
		{
			if (!Exists)
				return "";

			return $"dildo (" + base.ToString() + ")";
		}

		private void DoInit()
		{
			var d = (dildo_.Atom as VamAtom).Atom;

			var ct = d.GetComponentInChildren<CollisionTrigger>();
			if (ct == null)
			{
				Cue.LogError($"{d.uid} has no collision trigger");
				return;
			}

			var oldTriggerEnabled = ct.triggerEnabled;
			ct.triggerEnabled = true;

			var h = d.GetComponentInChildren<CollisionTriggerEventHandler>();
			if (h == null)
			{
				Cue.LogError($"{d.uid} has no collision trigger handler");
				ct.triggerEnabled = oldTriggerEnabled;
				return;
			}

			Init(h, d.mainController, d.transform, null);
		}
	}
}
