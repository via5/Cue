using System.Collections.Generic;
using UnityEngine;

namespace Cue.W
{
	class VamAtom : IAtom
	{
		private readonly Atom atom_;
		private Logger log_;
		private VamActionParameter setOnlyKeyJointsOn_;
		private VamAtomNav nav_;
		private FreeControllerV3 head_ = null;
		private DAZCharacter char_ = null;
		private VamClothing clothing_ = null;
		private VamBody body_ = null;

		public VamAtom(Atom atom)
		{
			atom_ = atom;
			log_ = new Logger(Logger.Sys, this, "VamAtom");
			setOnlyKeyJointsOn_ = new VamActionParameter(
				atom_, "AllJointsControl", "SetOnlyKeyJointsOn");
			nav_ = new VamAtomNav(this);

			char_ = atom_.GetComponentInChildren<DAZCharacter>();
			if (char_ != null)
			{
				clothing_ = new VamClothing(this);
				body_ = new VamBody(this);
			}
		}

		public void Init()
		{
		}

		public Logger Log
		{
			get { return log_; }
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

		public IBody Body
		{
			get { return body_; }
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
