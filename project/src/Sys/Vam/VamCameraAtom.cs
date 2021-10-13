using UnityEngine;

namespace Cue.Sys.Vam
{
	class VamCameraEyes : VamBodyPart
	{
		public VamCameraEyes()
			: base(null, BP.Eyes)
		{
		}

		public override Vector3 ControlPosition
		{
			get { return Position; }
			set { }
		}

		public override Quaternion ControlRotation
		{
			get { return Rotation; }
			set { }
		}

		public override Vector3 Position
		{
			get { return Cue.Instance.VamSys.CameraPosition; }
		}

		public override Quaternion Rotation
		{
			get { return Quaternion.Zero; }
		}

		public override string ToString()
		{
			return $"camera eyes";
		}
	}


	class VamCameraHead : VamBodyPart
	{
		public VamCameraHead()
			: base(null, BP.Head)
		{
		}

		public override Vector3 ControlPosition
		{
			get { return Position; }
			set { }
		}

		public override Quaternion ControlRotation
		{
			get { return Rotation; }
			set { }
		}

		public override Vector3 Position
		{
			get { return Cue.Instance.VamSys.CameraPosition; }
		}

		public override Quaternion Rotation
		{
			get { return Quaternion.Zero; }
		}

		public override string ToString()
		{
			return $"camera head";
		}
	}


	class VamCameraHand : VamBodyPart
	{
		private Transform vrHand_, desktopHand_;
		private Rigidbody desktopRb_;

		public VamCameraHand(int type, Transform vrHand, Transform desktopHand)
			: base(null, type)
		{
			vrHand_ = vrHand;
			desktopHand_ = desktopHand;
			desktopRb_ = desktopHand_?.GetComponent<Rigidbody>();
		}

		public override Transform Transform
		{
			get
			{
				if (Cue.Instance.VamSys.IsVR)
					return vrHand_;
				else
					return desktopHand_;
			}
		}

		public override Rigidbody Rigidbody
		{
			get
			{
				if (Cue.Instance.VamSys.IsVR)
					return null; //; todo
				else
					return desktopRb_;
			}
		}

		public override FreeControllerV3 Controller
		{
			get
			{
				return null;
			}
		}

		public override Vector3 ControlPosition
		{
			get { return Position; }
			set { }
		}

		public override Quaternion ControlRotation
		{
			get { return Rotation; }
			set { }
		}

		public override Vector3 Position
		{
			get
			{
				if (Transform == null)
					return Vector3.Zero;
				else
					return U.FromUnity(Transform.position);
			}
		}

		public override Quaternion Rotation
		{
			get
			{
				if (Transform == null)
					return Quaternion.Zero;
				else
					return U.FromUnity(Transform.rotation);
			}
		}

		public override string ToString()
		{
			return $"cameraHand {Transform?.name}";
		}
	}


	class VamCameraBody : VamBasicBody
	{
		private IBodyPart[] parts_;

		public VamCameraBody(VamCameraAtom a)
		{
			parts_ = new IBodyPart[BP.Count];

			parts_[BP.Head] = new VamCameraHead();
			parts_[BP.Eyes] = new VamCameraEyes();
			parts_[BP.LeftHand] = new VamCameraHand(
				BP.LeftHand,
				SuperController.singleton.leftHand,
				null);

			parts_[BP.RightHand] = new VamCameraHand(
				BP.RightHand,
				SuperController.singleton.rightHand,
				SuperController.singleton.mouseGrab);
		}

		public override bool Exists
		{
			get { return false; }
		}

		public override float Scale
		{
			get { return 1; }
		}

		public override float Sweat
		{
			get { return 0; }
			set { }
		}

		public override float Flush
		{
			get { return 0; }
			set { }
		}

		public override bool Strapon
		{
			get { return false; }
			set { }
		}

		public override IBodyPart[] GetBodyParts()
		{
			return parts_;
		}

		public override Hand GetLeftHand()
		{
			return new Hand();
		}

		public override Hand GetRightHand()
		{
			return new Hand();
		}

		public override int BodyPartForCollider(Collider c)
		{
			var ho = c.GetComponentInParent<MeshVR.Hands.HandOutput>();
			if (ho == null)
				return -1;

			if (ho.hand == MeshVR.Hands.HandOutput.Hand.Left)
				return BP.LeftHand;
			else if (ho.hand == MeshVR.Hands.HandOutput.Hand.Right)
				return BP.RightHand;
			else
				return -1;
		}
	}


	class VamCameraAtom : IAtom
	{
		private VamCameraBody body_;
		private VamHair hair_;

		public VamCameraAtom()
		{
			body_ = new VamCameraBody(this);
			hair_ = new VamHair(null);
		}

		public string ID
		{
			get { return "Camera"; }
		}

		public bool IsPerson
		{
			get { return true; }
		}

		public bool IsMale
		{
			get { return true; }
		}

		public bool HasPenis
		{
			get { return false; }
		}

		public bool Teleporting
		{
			get { return false; }
		}

		public bool Possessed
		{
			get
			{
				foreach (var p in Cue.Instance.ActivePersons)
				{
					if (p.Atom == this)
						continue;

					if (p.Possessed)
						return false;
				}

				return true;
			}
		}

		public bool Selected
		{
			get { return false; }
		}

		public IBody Body { get { return body_; } }
		public IHair Hair { get { return hair_; } }

		public bool Visible { get { return true; } set { } }
		public bool Collisions { get; set; }
		public bool Physics { get; set; }
		public bool Hidden { get; set; }
		public float Scale { get; set; }
		public Vector3 Position { get; set; }
		public Quaternion Rotation { get; set; }
		public bool NavEnabled { get; set; }
		public bool NavPaused { get; set; }
		public int NavState { get; }

		public void Destroy()
		{
		}

		public IMorph GetMorph(string id)
		{
			return null;
		}

		public void Init()
		{
		}

		public void NavStop(string why)
		{
		}

		public void NavTo(Vector3 v, float bearing, float stoppingDistance)
		{
		}

		public void OnPluginState(bool b)
		{
		}

		public void SetDefaultControls(string why)
		{
		}

		public void SetParentLink(IBodyPart bp)
		{
		}

		public void SetBodyDamping(int e)
		{
		}

		public void TeleportTo(Vector3 p, float bearing)
		{
		}

		public void Update(float s)
		{
		}

		public void LateUpdate(float s)
		{
		}

		public bool HasCollider(Collider c)
		{
			return
				c.transform.IsChildOf(SuperController.singleton.leftHand) ||
				c.transform.IsChildOf(SuperController.singleton.rightHand);
		}
	}
}
