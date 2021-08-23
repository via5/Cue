using System;

namespace Cue.Sys.Vam
{
	class VamAtom : IAtom
	{
		private readonly Atom atom_;
		private Logger log_;
		private ActionParameter setOnlyKeyJointsOn_;
		private VamAtomNav nav_;
		private FreeControllerV3 head_ = null;
		private DAZCharacter char_ = null;
		private VamBody body_ = null;
		private VamHair hair_ = null;

		private BoolParameter collisions_;
		private BoolParameter physics_;
		private FloatParameter scale_;

		public VamAtom(Atom atom)
		{
			atom_ = atom;
			log_ = new Logger(Logger.Sys, this, "VamAtom");
			setOnlyKeyJointsOn_ = new ActionParameter(
				atom_, "AllJointsControl", "SetOnlyKeyJointsOn");
			nav_ = new VamAtomNav(this);

			char_ = atom_.GetComponentInChildren<DAZCharacter>();
			if (char_ != null)
			{
				body_ = new VamBody(this);
				hair_ = new VamHair(this);
			}

			collisions_ = new BoolParameter(this, "AtomControl", "collisionEnabled");
			physics_ = new BoolParameter(this, "control", "physicsEnabled");
			scale_ = new FloatParameter(this, "scale", "scale");
		}

		public void Init()
		{
			VamFixes.Run(atom_);
		}

		public void Destroy()
		{
			atom_.SetUID(atom_.uid + $"_cue_destroy");
			SuperController.singleton.RemoveAtom(atom_);
		}

		public Logger Log
		{
			get { return log_; }
		}

		public string ID
		{
			get { return atom_.uid; }
		}

		public bool Visible
		{
			get { return atom_.on; }
			set { atom_.SetOn(value); }
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

		public bool HasPenis
		{
			get { return Sex == Sexes.Male; }
		}

		public bool Selected
		{
			get
			{
				return (SuperController.singleton.GetSelectedAtom() == atom_);
			}
		}

		public IBody Body
		{
			get { return body_; }
		}

		public IHair Hair
		{
			get { return hair_; }
		}

		public bool Teleporting
		{
			get { return nav_.Teleporting; }
		}

		public Vector3 Position
		{
			get { return U.FromUnity(atom_.mainController.transform.position); }
			set { atom_.mainController.MoveControl(U.ToUnity(value)); }
		}

		public Quaternion Rotation
		{
			get { return U.FromUnity(atom_.mainController.transform.rotation); }
			set { atom_.mainController.transform.rotation = U.ToUnity(value); }
		}

		public bool Collisions
		{
			get { return collisions_.Value; }
			set { collisions_.Value = value; }
		}

		public bool Physics
		{
			get { return physics_.Value; }
			set { physics_.Value = value; }
		}

		public bool Hidden
		{
			get { return atom_.hidden; }
			set { atom_.hidden = value; }
		}

		public float Scale
		{
			get { return scale_.Value; }
			set { scale_.Value = value; }
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
			// this breaks possession, it stays enabled but control is lost
			if (!Possessed)
			{
				log_.Info($"{ID}: setting default controls ({why})");
				setOnlyKeyJointsOn_.Fire();
			}
		}

		public void SetParentLink(IBodyPart bp)
		{
			var v = bp as VamBodyPart;
			if (v == null)
			{
				log_.Error($"SetParentLink: {bp} not a VamBodyPart");
				return;
			}

			var rb = v.Rigidbody;
			if (rb == null)
			{
				log_.Error($"SetParentLink: {bp} has no associated rigidbody");
				return;
			}

			atom_.mainController.SelectLinkToRigidbody(rb);
		}

		public void SetBodyDamping(int e)
		{
			float pos = 160;
			float rot = 25;

			float hipsPos = pos;
			float hipsRot = rot;

			if (e == BodyDamping.Sex)
			{
				hipsPos = 35;
				hipsRot = 5;
			}

			SetStrongerDamping("hipControl", hipsPos, hipsRot);
			SetStrongerDamping("chestControl", pos, rot);
			SetStrongerDamping("headControl", pos, rot);
			SetStrongerDamping("lThighControl", pos, rot);
			SetStrongerDamping("rThighControl", pos, rot);
			SetStrongerDamping("lFootControl", pos, rot);
			SetStrongerDamping("rFootControl", pos, rot);
			SetStrongerDamping("lKneeControl", pos, rot);
			SetStrongerDamping("rKneeControl", pos, rot);
		}

		private void SetStrongerDamping(string cn, float pos, float rot)
		{
			var c = Cue.Instance.VamSys.FindController(atom_, cn);
			if (c == null)
			{
				log_.Error($"SetStrongerDamping: controller '{cn}' not found");
				return;
			}

			c.RBHoldPositionDamper = pos;
			c.RBHoldRotationDamper = rot;
		}

		void SetControllerForMoving(string id, bool b)
		{
			var fc = Cue.Instance.VamSys.FindController(atom_, id);

			fc.currentPositionState = (b ?
				FreeControllerV3.PositionState.Off :
				FreeControllerV3.PositionState.On);

			fc.currentRotationState = (b ?
				FreeControllerV3.RotationState.Off :
				FreeControllerV3.RotationState.On);
		}

		public void SetControlsForMoving(bool b)
		{
			SetControllerForMoving("chestControl", b);
			SetControllerForMoving("hipControl", b);
			SetControllerForMoving("lFootControl", b);
			SetControllerForMoving("rFootControl", b);
		}

		public IMorph GetMorph(string name)
		{
			return new VamMorph(this, name);
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

			body_?.OnPluginState(b);
			hair_?.OnPluginState(b);
		}

		public void Update(float s)
		{
			nav_.Update(s);
			hair_?.Update(s);
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

			head_ = Cue.Instance.VamSys.FindController(atom_, "headControl");
		}
	}
}
