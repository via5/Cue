using UnityEngine;

namespace Cue.Sys.Vam
{
	class VamCameraEyes : VamBodyPart
	{
		public VamCameraEyes()
			: base(null, BodyParts.Eyes)
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
			return $"camera";
		}
	}


	class VamTransformBodyPart : VamBodyPart
	{
		private Transform t_;

		public VamTransformBodyPart(int type, Transform t)
			: base(null, type)
		{
			t_ = t;
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
			get { return U.FromUnity(t_.position); }
		}

		public override Quaternion Rotation
		{
			get { return U.FromUnity(t_.rotation); }
		}
	}


	class VamCameraBody : VamBasicBody
	{
		private IBodyPart[] parts_;

		public VamCameraBody(VamCameraAtom a)
		{
			parts_ = new IBodyPart[BodyParts.Count];

			parts_[BodyParts.Eyes] = new VamCameraEyes();
			parts_[BodyParts.LeftHand] = new VamTransformBodyPart(
				BodyParts.LeftHand, SuperController.singleton.leftHand);
			parts_[BodyParts.RightHand] = new VamTransformBodyPart(
				BodyParts.RightHand, SuperController.singleton.rightHand);
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
				return BodyParts.LeftHand;
			else if (ho.hand == MeshVR.Hands.HandOutput.Hand.Right)
				return BodyParts.RightHand;
			else
				return -1;
		}
	}


	class VamCameraAtom : IAtom
	{
		private VamCameraBody body_;
		private VamClothing clothing_;
		private VamHair hair_;

		public VamCameraAtom()
		{
			body_ = new VamCameraBody(this);
			clothing_ = new VamClothing(null);
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

		public int Sex
		{
			get { return Sexes.Male; }
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

		public IClothing Clothing { get { return clothing_; } }
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

		public void TeleportTo(Vector3 p, float bearing)
		{
		}

		public void Update(float s)
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
