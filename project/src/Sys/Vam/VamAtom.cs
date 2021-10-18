using UnityEngine;

namespace Cue.Sys.Vam
{
	class VamCorruptionDetector
	{
		public delegate void Handler();
		public event Handler Corrupted;

		private const float UICheckInterval = 1;

		private Atom atom_;
		private UnityEngine.UI.Slider slider_ = null;
		private float elapsed_ = UICheckInterval;
		private float last_ = 0;

		public VamCorruptionDetector(Atom a, Handler h = null)
		{
			atom_ = a;

			if (h != null)
				Corrupted += h;
		}

		public void Update(float s)
		{
			if (slider_ == null)
			{
				elapsed_ += s;
				if (elapsed_ >= UICheckInterval)
				{
					elapsed_ = 0;

					var aui = atom_.UITransform
						?.GetComponentInChildren<AtomUI>(includeInactive: true);

					slider_ = aui?.resetPhysicsProgressSlider;
				}

				if (slider_ == null)
					return;

				last_ = slider_.value;
			}

			if (slider_.value > 0 && slider_.value != last_)
				Corrupted?.Invoke();
		}
	}


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
		private BoolParameter blink_;
		private VamCorruptionDetector cd_;

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
			blink_ = new BoolParameter(this, "EyelidControl", "blinkEnabled");
			cd_ = new VamCorruptionDetector(atom, OnCorruption);
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

		public bool IsMale
		{
			get
			{
				if (char_ == null)
				{
					log_.Error($"VamAtom.Sex: atom {ID} is not a person");
					return true;
				}

				return char_.isMale;
			}
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

		public bool Grabbed
		{
			get { return atom_.mainController.isGrabbing; }
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
			SetStrongerDamping("hipControl", e, true);
			SetStrongerDamping("chestControl", e, true);
			SetStrongerDamping("headControl", e, true);
			SetStrongerDamping("lThighControl", e, true);
			SetStrongerDamping("rThighControl", e, true);
			SetStrongerDamping("lFootControl", e, false);
			SetStrongerDamping("rFootControl", e, false);
			SetStrongerDamping("lKneeControl", e, false);
			SetStrongerDamping("rKneeControl", e, false);
		}

		public void SetBlink(bool b)
		{
			blink_.Value = b;
		}

		private void SetStrongerDamping(string cn, int e, bool lowerForSex)
		{
			float pos = 160;
			float rot = 25;

			if (e == BodyDamping.Sex || lowerForSex)
			{
				pos = 35;
				rot = 5;
			}

			var c = U.FindController(atom_, cn);
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
			var fc = U.FindController(atom_, id);

			fc.currentPositionState = (b ?
				FreeControllerV3.PositionState.Off :
				FreeControllerV3.PositionState.On);

			fc.currentRotationState = (b ?
				FreeControllerV3.RotationState.Off :
				FreeControllerV3.RotationState.On);
		}

		public void SetControlsForMoving(bool b)
		{
			SetControllerForMoving("chestControl", true);
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
			body_?.OnPluginState(b);
			hair_?.OnPluginState(b);
		}

		public void Update(float s)
		{
			nav_.Update(s);
			hair_?.Update(s);
			cd_.Update(s);
		}

		public void LateUpdate(float s)
		{
			body_?.LateUpdate(s);
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

			head_ = U.FindController(atom_, "headControl");
		}

		private void OnCorruption()
		{
			Cue.LogError(
				"cue: VaM detected corruption, disabling plugin");

			var p = Cue.Instance.FindPerson(ID);
			if (p != null)
				p.Animator.DumpAllForces();

			Cue.Instance.DisablePlugin();
		}
	}
}
