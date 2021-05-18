using System.Collections.Generic;
using UnityEngine;

namespace Cue.W
{
	class VamBody : IBody
	{
		private VamAtom atom_;
		private VamFloatParameter gloss_ = null;

		public VamBody(VamAtom a)
		{
			atom_ = a;
			gloss_ = new VamFloatParameter(a, "skin", "Gloss");
			if (!gloss_.Check(true))
				atom_.Log.Error("no skin gloss parameter");
		}

		public List<IBodyPart> GetBodyParts()
		{
			var list = new List<IBodyPart>();

			list.Add(GetRigidbody(BodyParts.Head, "headControl", "head"));

			list.Add(GetTrigger(BodyParts.Lips, "", "LipTrigger"));
			list.Add(GetTrigger(BodyParts.Mouth, "", "MouthTrigger"));
			list.Add(GetTrigger(BodyParts.LeftBreast, "lNippleControl", "lNippleTrigger", ""));
			list.Add(GetTrigger(BodyParts.RightBreast, "rNippleControl", "rNippleTrigger", ""));
			list.Add(GetTrigger(BodyParts.Labia, "", "LabiaTrigger", ""));
			list.Add(GetTrigger(BodyParts.Vagina, "", "VaginaTrigger", ""));
			list.Add(GetTrigger(BodyParts.DeepVagina, "", "DeepVaginaTrigger", ""));
			list.Add(GetTrigger(BodyParts.DeeperVagina, "", "DeeperVaginaTrigger", ""));
			list.Add(null);  // anus

			list.Add(GetRigidbody(BodyParts.Chest, "chestControl", "chest"));
			list.Add(GetRigidbody(BodyParts.Belly, "", "abdomen2"));
			list.Add(GetRigidbody(BodyParts.Hips, "hipControl", "abdomen"));
			list.Add(GetCollider(BodyParts.LeftGlute, "", "LGlute1Joint", ""));
			list.Add(GetCollider(BodyParts.RightGlute, "", "RGlute1Joint", ""));

			list.Add(GetCollider(BodyParts.LeftShoulder, "lArmControl", "lShldr"));
			list.Add(GetCollider(BodyParts.LeftArm, "lElbowControl", "StandardColliderslShldr/_Collider1"));
			list.Add(GetCollider(BodyParts.LeftForearm, "lElbowControl", "lForeArm/_Collider2"));
			list.Add(GetRigidbody(BodyParts.LeftHand, "lHandControl", "lHand"));

			list.Add(GetCollider(BodyParts.RightShoulder, "rArmControl", "rShldr"));
			list.Add(GetCollider(BodyParts.RightArm, "rElbowControl", "StandardCollidersrShldr/_Collider1"));
			list.Add(GetCollider(BodyParts.RightForearm, "rElbowControl", "rForeArm/_Collider2"));
			list.Add(GetRigidbody(BodyParts.RightHand, "rHandControl", "rHand"));

			list.Add(GetCollider(BodyParts.LeftThigh, "lKneeControl", "lThigh12Joint", "StandardColliderslThigh/_Collider6"));
			list.Add(GetCollider(BodyParts.LeftShin, "lKneeControl", "lShin8Joint", "StandardColliderslShin/_Collider2"));
			list.Add(GetRigidbody(BodyParts.LeftFoot, "lFootControl", "lFoot"));

			list.Add(GetCollider(BodyParts.RightThigh, "rKneeControl", "rThigh12Joint", "StandardCollidersrThigh/_Collider6"));
			list.Add(GetCollider(BodyParts.RightShin, "rKneeControl", "rShin8Joint", "StandardCollidersrShin/_Collider2"));
			list.Add(GetRigidbody(BodyParts.RightFoot, "rFootControl", "rFoot"));

			list.Add(new EyesBodyPart(atom_));

			if (atom_.Sex == Sexes.Male)
				list.Add(GetRigidbody(BodyParts.Genitals, "penisBaseControl", "", "Gen1"));
			else
				list.Add(GetTrigger(BodyParts.Genitals, "", "LabiaTrigger", ""));

			if (atom_.Sex == Sexes.Male)
				list.Add(GetRigidbody(BodyParts.Pectorals, "chestControl", "chest"));
			else
				list.Add(null);

			return list;
		}

		public float Sweat
		{
			set
			{
				var p = gloss_.Parameter;
				if (p != null)
				{
					float def = p.defaultVal;
					float range = p.max - def;

					p.val = def + value * range;
				}
			}
		}

		private string MakeName(string nameFemale, string nameMale)
		{
			if (atom_.Sex == Sexes.Female)
				return nameFemale;

			if (nameMale == "")
				return "";
			else if (nameMale == "same")
				return nameFemale;
			else
				return nameMale;
		}

		private IBodyPart GetRigidbody(int id, string controller, string nameFemale, string nameMale = "same")
		{
			string name = MakeName(nameFemale, nameMale);
			if (name == "")
				return null;

			var rb = Cue.Instance.VamSys.FindRigidbody(atom_.Atom, name);
			if (rb == null)
				Cue.LogError($"rb {name} not found in {atom_.ID}");

			FreeControllerV3 fc = null;
			if (controller != "")
			{
				fc = Cue.Instance.VamSys.FindController(atom_.Atom, controller);
				if (fc == null)
					Cue.LogError($"rb {name} has no controller {controller} in {atom_.ID}");
			}

			return new RigidbodyBodyPart(atom_, id, rb, fc);
		}

		private IBodyPart GetTrigger(int id, string controller, string nameFemale, string nameMale = "same")
		{
			string name = MakeName(nameFemale, nameMale);
			if (name == "")
				return null;

			var o = Cue.Instance.VamSys.FindChildRecursive(atom_.Atom.transform, name);
			if (o == null)
			{
				Cue.LogError($"trigger {name} not found in {atom_.ID}");
				return null;
			}

			var t = o.GetComponentInChildren<CollisionTriggerEventHandler>();
			if (t == null)
			{
				Cue.LogError($"trigger {name} has no event handler in {atom_.ID}");
				return null;
			}

			if (t.thisRigidbody == null)
			{
				Cue.LogError($"trigger {name} has no rb in {atom_.ID}");
				return null;
			}

			FreeControllerV3 fc = null;
			if (controller != "")
			{
				fc = Cue.Instance.VamSys.FindController(atom_.Atom, controller);
				if (fc == null)
					Cue.LogError($"trigger {name} has no controller {controller} in {atom_.ID}");
			}

			return new TriggerBodyPart(atom_, id, t, fc);
		}

		private IBodyPart GetCollider(int id, string controller, string nameFemale, string nameMale = "same")
		{
			string name = MakeName(nameFemale, nameMale);
			if (name == "")
				return null;

			var c = Cue.Instance.VamSys.FindCollider(atom_.Atom, name);
			if (c == null)
			{
				Cue.LogError($"collider {name} not found in {atom_.ID}");
				return null;
			}

			FreeControllerV3 fc = null;
			if (controller != "")
			{
				fc = Cue.Instance.VamSys.FindController(atom_.Atom, controller);
				if (fc == null)
					Cue.LogError($"collider {name} has no controller {controller} in {atom_.ID}");
			}

			return new ColliderBodyPart(atom_, id, c, fc);
		}
	}


	abstract class VamBodyPart : IBodyPart
	{
		protected VamAtom atom_;
		private int type_;

		protected VamBodyPart(VamAtom a, int t)
		{
			atom_ = a;
			type_ = t;
		}

		public int Type
		{
			get { return type_; }
		}

		public abstract Transform Transform { get; }
		public abstract bool CanTrigger { get; }
		public abstract float Trigger { get; }
		public abstract bool CanGrab { get; }
		public abstract bool Grabbed { get; }
		public abstract Vector3 Position { get; }
		public abstract Vector3 Direction { get; }
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

		public override bool CanTrigger
		{
			get { return false; }
		}

		public override float Trigger
		{
			get { return 0; }
		}

		public override bool CanGrab
		{
			get { return (fc_ != null); }
		}

		public override bool Grabbed
		{
			get { return fc_?.isGrabbing ?? false; }
		}

		public override Vector3 Position
		{
			get { return W.VamU.FromUnity(rb_.position); }
		}

		public override Vector3 Direction
		{
			get { return W.VamU.Direction(rb_.rotation); }
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

		public override bool CanTrigger
		{
			get { return false; }
		}

		public override float Trigger
		{
			get { return 0; }
		}

		public override bool CanGrab
		{
			get { return (fc_ != null); }
		}

		public override bool Grabbed
		{
			get { return (fc_?.isGrabbing ?? false); }
		}

		public override Vector3 Position
		{
			get
			{
				return W.VamU.FromUnity(c_.bounds.center);
			}
		}

		public override Vector3 Direction
		{
			get { return W.VamU.Direction(c_.transform.rotation); }
		}

		public override string ToString()
		{
			return $"collider {c_.name}";
		}
	}


	class TriggerBodyPart : VamBodyPart
	{
		private CollisionTriggerEventHandler h_;
		private Trigger trigger_;
		private Rigidbody rb_;
		private FreeControllerV3 fc_;

		public TriggerBodyPart(VamAtom a, int type, CollisionTriggerEventHandler h, FreeControllerV3 fc)
			: base(a, type)
		{
			h_ = h;
			trigger_ = h.collisionTrigger.trigger;
			rb_ = h.thisRigidbody;
			fc_ = fc;
		}

		public override Transform Transform
		{
			get { return rb_.transform; }
		}

		public override bool CanTrigger
		{
			get { return true; }
		}

		public override float Trigger
		{
			get { return trigger_.active ? 1 : 0; }
		}

		public override bool CanGrab
		{
			get { return (fc_ != null); }
		}

		public override bool Grabbed
		{
			get { return fc_?.isGrabbing ?? false; }
		}

		public override Vector3 Position
		{
			get
			{
				if (rb_ == null)
					return Vector3.Zero;
				else
					return W.VamU.FromUnity(rb_.position);
			}
		}

		public override Vector3 Direction
		{
			get
			{
				if (rb_ == null)
					return Vector3.Zero;
				else
					return W.VamU.Direction(rb_.rotation);
			}
		}

		public override string ToString()
		{
			return $"trigger {trigger_.displayName}";
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

		public override bool CanTrigger { get { return false; } }
		public override float Trigger { get { return 0; } }
		public override bool CanGrab { get { return false; } }
		public override bool Grabbed { get { return false; } }

		public override Vector3 Position
		{
			get
			{
				if (atom_.Possessed)
					return Cue.Instance.Sys.Camera;
				else if (lEye_ != null && rEye_ != null)
					return VamU.FromUnity((lEye_.position + rEye_.position) / 2);
				else if (head_ != null)
					return VamU.FromUnity(head_.transform.position) + new Vector3(0, 0.05f, 0);
				else
					return Vector3.Zero;
			}
		}

		public override Vector3 Direction
		{
			get
			{
				if (head_ == null)
					return Vector3.Zero;
				else
					return W.VamU.Direction(head_.rotation);
			}
		}

		public override string ToString()
		{
			return $"eyes {Position}";
		}
	}

}
