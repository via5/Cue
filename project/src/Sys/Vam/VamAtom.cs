using System.Collections.Generic;
using UnityEngine;

namespace Cue.W
{
	class RigidbodyBodyPart : IBodyPart
	{
		private VamAtom atom_;
		private int type_;
		private Rigidbody rb_;

		public RigidbodyBodyPart(VamAtom a, int type, Rigidbody rb)
		{
			atom_ = a;
			type_ = type;
			rb_ = rb;
		}

		public int Type
		{
			get { return type_; }
		}

		public bool Triggering
		{
			get { return false; }
		}

		public Vector3 Position
		{
			get { return W.VamU.FromUnity(rb_.position); }
		}

		public Vector3 Direction
		{
			get { return W.VamU.FromUnity(rb_.rotation.eulerAngles); }
		}
	}


	class ColliderBodyPart : IBodyPart
	{
		private VamAtom atom_;
		private int type_;
		private Collider c_;

		public ColliderBodyPart(VamAtom a, int type, Collider c)
		{
			atom_ = a;
			type_ = type;
			c_ = c;
		}

		public int Type
		{
			get { return type_; }
		}

		public bool Triggering
		{
			get { return false; }
		}

		public Vector3 Position
		{
			get
			{
				return W.VamU.FromUnity(c_.bounds.center);
			}
		}

		public Vector3 Direction
		{
			get { return W.VamU.FromUnity(c_.transform.rotation.eulerAngles); }
		}
	}


	class TriggerBodyPart : IBodyPart
	{
		private VamAtom atom_;
		private int type_;
		private CollisionTriggerEventHandler h_;
		private Trigger trigger_;
		private Rigidbody rb_;

		public TriggerBodyPart(VamAtom a, int type, CollisionTriggerEventHandler h)
		{
			atom_ = a;
			type_ = type;
			h_ = h;
			trigger_ = h.collisionTrigger.trigger;
			rb_ = h.thisRigidbody;
		}

		public int Type
		{
			get { return type_; }
		}

		public bool Triggering
		{
			get { return trigger_.active; }
		}

		public Vector3 Position
		{
			get
			{
				if (rb_ == null)
					return Vector3.Zero;
				else
					return W.VamU.FromUnity(rb_.position);
			}
		}

		public Vector3 Direction
		{
			get
			{
				if (rb_ == null)
					return Vector3.Zero;
				else
					return W.VamU.FromUnity(rb_.rotation.eulerAngles);
			}
		}
	}


	class VamAtom : IAtom
	{
		private readonly Atom atom_;
		private Logger log_;
		private VamActionParameter setOnlyKeyJointsOn_;
		private VamAtomNav nav_;
		private FreeControllerV3 head_ = null;
		private DAZCharacter char_ = null;
		private VamClothing clothing_ = null;

		public VamAtom(Atom atom)
		{
			atom_ = atom;
			log_ = new Logger(Logger.Sys, this, "VamAtom");
			setOnlyKeyJointsOn_ = new VamActionParameter(
				atom_, "AllJointsControl", "SetOnlyKeyJointsOn");
			nav_ = new VamAtomNav(this);

			char_ = atom_.GetComponentInChildren<DAZCharacter>();
			if (char_ != null)
				clothing_ = new VamClothing(this);
		}

		public void Init()
		{
		}

		public string ID
		{
			get { return atom_.uid; }
		}

		public bool IsPerson
		{
			get { return atom_.type == "Person"; }
		}

		public int Sex
		{
			get
			{
				if (char_ == null)
				{
					log_.Error($"VamAtom.Sex: atom {ID} is not a person");
					return Sexes.Male;
				}

				if (char_.isMale)
					return Sexes.Male;
				else
					return Sexes.Female;
			}
		}

		public bool Selected
		{
			get
			{
				return (SuperController.singleton.GetSelectedAtom() == atom_);
			}
		}

		public IClothing Clothing
		{
			get { return clothing_; }
		}

		public bool Teleporting
		{
			get { return nav_.Teleporting; }
		}

		public Vector3 Position
		{
			get { return W.VamU.FromUnity(atom_.mainController.transform.position); }
			set { atom_.mainController.MoveControl(W.VamU.ToUnity(value)); }
		}

		public Vector3 Direction
		{
			get
			{
				var v =
					atom_.mainController.transform.rotation *
					UnityEngine.Vector3.forward;

				return W.VamU.FromUnity(v);
			}

			set
			{
				var r = Quaternion.LookRotation(W.VamU.ToUnity(value));
				atom_.mainController.RotateTo(r);
			}
		}

		public float Bearing
		{
			get { return Vector3.Angle(Vector3.Zero, Direction); }
			set { Direction = Vector3.Rotate(0, value, 0); }
		}

		public Atom Atom
		{
			get { return atom_; }
		}

		public bool Possessed
		{
			get
			{
				GetHead();
				return head_.possessed;
			}
		}

		public List<IBodyPart> GetBodyParts()
		{
			var list = new List<IBodyPart>();

			list.Add(GetRigidbody(BodyParts.Head, "head"));

			list.Add(GetTrigger(BodyParts.Lips, "LipTrigger"));
			list.Add(GetTrigger(BodyParts.Mouth, "MouthTrigger"));
			list.Add(GetTrigger(BodyParts.LeftBreast, "lNippleTrigger", ""));
			list.Add(GetTrigger(BodyParts.RightBreast, "rNippleTrigger", ""));
			list.Add(GetTrigger(BodyParts.Labia, "LabiaTrigger", ""));
			list.Add(GetTrigger(BodyParts.Vagina, "VaginaTrigger", ""));
			list.Add(GetTrigger(BodyParts.DeepVagina, "DeepVaginaTrigger", ""));
			list.Add(GetTrigger(BodyParts.DeeperVagina, "DeeperVaginaTrigger", ""));
			list.Add(null);  // anus

			list.Add(GetRigidbody(BodyParts.Chest, "chest"));
			list.Add(GetRigidbody(BodyParts.Belly, "abdomen2"));
			list.Add(GetRigidbody(BodyParts.Hips, "abdomen"));
			list.Add(GetCollider(BodyParts.LeftGlute, "LGlute1Joint", ""));
			list.Add(GetCollider(BodyParts.RightGlute, "RGlute1Joint", ""));

			list.Add(GetCollider(BodyParts.LeftShoulder, "lShldr"));
			list.Add(GetCollider(BodyParts.LeftArm, "StandardColliderslShldr/_Collider1"));
			list.Add(GetCollider(BodyParts.LeftForearm, "lForeArm/_Collider2"));
			list.Add(GetRigidbody(BodyParts.LeftHand, "lHand"));

			list.Add(GetCollider(BodyParts.RightShoulder, "rShldr"));
			list.Add(GetCollider(BodyParts.RightArm, "StandardCollidersrShldr/_Collider1"));
			list.Add(GetCollider(BodyParts.RightForearm, "rForeArm/_Collider2"));
			list.Add(GetRigidbody(BodyParts.RightHand, "rHand"));

			list.Add(GetCollider(BodyParts.LeftThigh, "lThigh12Joint", "StandardColliderslThigh/_Collider6"));
			list.Add(GetCollider(BodyParts.LeftShin, "lShin8Joint", "StandardColliderslShin/_Collider2"));
			list.Add(GetRigidbody(BodyParts.LeftFoot, "lFoot"));

			list.Add(GetCollider(BodyParts.RightThigh, "rThigh12Joint", "StandardCollidersrThigh/_Collider6"));
			list.Add(GetCollider(BodyParts.RightShin, "rShin8Joint", "StandardCollidersrShin/_Collider2"));
			list.Add(GetRigidbody(BodyParts.RightFoot, "rFoot"));

			return list;
		}

		private string MakeName(string nameFemale, string nameMale)
		{
			if (Sex == Sexes.Female)
				return nameFemale;

			if (nameMale == "")
				return "";
			else if (nameMale == "same")
				return nameFemale;
			else
				return nameMale;
		}

		private IBodyPart GetRigidbody(int id, string nameFemale, string nameMale = "same")
		{
			string name = MakeName(nameFemale, nameMale);
			if (name == "")
				return null;

			var rb = Cue.Instance.VamSys.FindRigidbody(atom_, name);
			if (rb == null)
				Cue.LogError($"rb {name} not found in {atom_.uid}");

			return new RigidbodyBodyPart(this, id, rb);
		}

		private IBodyPart GetTrigger(int id, string nameFemale, string nameMale = "same")
		{
			string name = MakeName(nameFemale, nameMale);
			if (name == "")
				return null;

			var o = Cue.Instance.VamSys.FindChildRecursive(atom_.transform, name);
			if (o == null)
			{
				Cue.LogError($"trigger {name} not found in {atom_.uid}");
				return null;
			}

			var t = o.GetComponentInChildren<CollisionTriggerEventHandler>();
			if (t == null)
			{
				Cue.LogError($"trigger {name} has no event handler in {atom_.uid}");
				return null;
			}

			if (t.thisRigidbody == null)
			{
				Cue.LogError($"trigger {name} has no rb in {atom_.uid}");
				return null;
			}

			return new TriggerBodyPart(this, id, t);
		}

		private IBodyPart GetCollider(int id, string nameFemale, string nameMale = "same")
		{
			string name = MakeName(nameFemale, nameMale);
			if (name == "")
				return null;

			var c = Cue.Instance.VamSys.FindCollider(atom_, name);
			if (c == null)
			{
				Cue.LogError($"collider {name} not found in {atom_.uid}");
				return null;
			}

			return new ColliderBodyPart(this, id, c);
		}

		public void SetDefaultControls(string why)
		{
			log_.Info($"{ID}: setting default controls ({why})");
			setOnlyKeyJointsOn_.Fire();
		}

		public DAZMorph FindMorph(string id)
		{
			return Cue.Instance.VamSys.FindMorph(atom_, id);
		}

		public void OnPluginState(bool b)
		{
			// this would prevent both grabbing with a possessed hand and
			// grabbing	from a distance with the pointer, there doesn't seem to
			// be a way to only disable the latter
			//
			//foreach (var rb in atom_.rigidbodies)
			//{
			//	var fc = rb.GetComponent<FreeControllerV3>();
			//	if (fc != null)
			//		fc.interactableInPlayMode = !b;
			//}
			//
			//atom_.mainController.interactableInPlayMode = !b;

			clothing_?.OnPluginState(b);
		}

		public void Update(float s)
		{
			nav_.Update(s);
		}

		public void TeleportTo(Vector3 v, float bearing)
		{
			nav_.TeleportTo(v, bearing);
		}

		public VamAtomNav VamAtomNav
		{
			get { return nav_; }
		}

		public bool NavEnabled
		{
			get { return nav_.Enabled; }
			set { nav_.Enabled = value; }
		}

		public bool NavPaused
		{
			get { return nav_.Paused; }
			set { nav_.Paused = value; }
		}

		public void NavTo(Vector3 v, float bearing, float stoppingDistance)
		{
			nav_.MoveTo(v, bearing, stoppingDistance);
		}

		public void NavStop(string why)
		{
			nav_.Stop(why);
		}

		public int NavState
		{
			get { return nav_.State; }
		}

		private void GetHead()
		{
			if (head_ != null)
				return;

			var vsys = ((W.VamSys)Cue.Instance.Sys);
			head_ = vsys.FindController(atom_, "headControl");
		}
	}
}
