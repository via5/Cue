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
			log_ = new Logger(Logger.Sys, () => atom_.uid);
			setOnlyKeyJointsOn_ = new VamActionParameter(
				atom_, "AllJointsControl", "SetOnlyKeyJointsOn");
			nav_ = new VamAtomNav(this);

			char_ = atom_.GetComponentInChildren<DAZCharacter>();
			if (char_ != null)
				clothing_ = new VamClothing(this);
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

			GetRigidbody(list, BodyPartTypes.Head, "head");
			GetTrigger(list, BodyPartTypes.Lips, "LipTrigger");
			GetTrigger(list, BodyPartTypes.Mouth, "MouthTrigger");
			GetTrigger(list, BodyPartTypes.LeftBreast, "lNippleTrigger");
			GetTrigger(list, BodyPartTypes.RightBreast, "rNippleTrigger");
			GetTrigger(list, BodyPartTypes.Labia, "LabiaTrigger");
			GetTrigger(list, BodyPartTypes.Vagina, "VaginaTrigger");
			GetTrigger(list, BodyPartTypes.DeepVagina, "DeepVaginaTrigger");
			GetTrigger(list, BodyPartTypes.DeeperVagina, "DeeperVaginaTrigger");

			return list;
		}

		private void GetRigidbody(List<IBodyPart> list, int id, string name)
		{
			var rb = Cue.Instance.VamSys.FindRigidbody(atom_, name);
			if (rb == null)
				return;

			list.Add(new RigidbodyBodyPart(this, id, rb));
		}

		private void GetTrigger(List<IBodyPart> list, int id, string name)
		{
			var o = Cue.Instance.VamSys.FindChildRecursive(atom_.transform, name);
			if (o == null)
				return;

			var t = o.GetComponentInChildren<CollisionTriggerEventHandler>();
			if (t == null)
				return;

			if (t.thisRigidbody == null)
				return;

			list.Add(new TriggerBodyPart(this, id, t));
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
