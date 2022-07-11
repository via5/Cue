using UnityEngine;

namespace Cue.Sys.Vam
{
	class VamCameraEyes : VamBodyPart
	{
		public VamCameraEyes(VamCameraAtom a)
			: base(a, BP.Eyes)
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
			get { return Quaternion.Identity; }
		}

		protected override bool DoContainsTransform(Transform t, bool debug)
		{
			return false;
		}

		public string ToDetailedString()
		{
			return $"camera eyes";
		}
	}


	class VamCameraHead : VamBodyPart
	{
		public VamCameraHead(VamCameraAtom a)
			: base(a, BP.Head)
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
			get { return Quaternion.Identity; }
		}

		protected override bool DoContainsTransform(Transform t, bool debug)
		{
			return false;
		}

		public string ToDetailedString()
		{
			return $"camera head";
		}
	}


	class VamCameraHand : VamBodyPart
	{
		private Transform vrHand_, desktopHand_;
		private Rigidbody desktopRb_;
		private MeshVR.Hands.HandOutput.Hand handOutputType_;

		public VamCameraHand(
			VamCameraAtom a, BodyPartType bodyPart, MeshVR.Hands.HandOutput.Hand handOutputType,
			Transform vrHand, Transform desktopHand)
				: base(a, bodyPart)
		{
			vrHand_ = vrHand;
			desktopHand_ = desktopHand;
			desktopRb_ = desktopHand_?.GetComponent<Rigidbody>();
			handOutputType_ = handOutputType;
		}

		protected override bool DoContainsTransform(Transform t, bool debug)
		{
			if (t == desktopHand_)
				return true;

			if (Cue.Instance.VamSys.IsVRHand(t, Type))
				return true;

			var ho = t.GetComponentInParent<MeshVR.Hands.HandOutput>();
			if (ho != null)
			{
				if (ho.hand == handOutputType_)
				{
					Log.Info($"{t}");
					return true;
				}
			}

			return false;
		}

		private Transform ActiveTransform
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
				if (ActiveTransform == null)
				{
					return Vector3.MaxValue;
				}
				else if (!Cue.Instance.VamSys.IsVR)
				{
					// on desktop, Transform is mouseGrab, which doesn't make
					// much sense for this and can make random body parts end
					// up being marked as groped depending on where the last
					// click was with the mouse; returning MaxValue will make
					// all distance checks fail
					return Vector3.MaxValue;
				}
				else
				{
					return U.FromUnity(ActiveTransform.position);
				}
			}
		}

		public override Quaternion Rotation
		{
			get
			{
				if (ActiveTransform == null)
					return Quaternion.Identity;
				else if (!Cue.Instance.VamSys.IsVR)  // see Position
					return Quaternion.Identity;
				else
					return U.FromUnity(ActiveTransform.rotation);
			}
		}

		public override string ToString()
		{
			return $"cameraHand {ActiveTransform?.name}";
		}
	}


	class VamCameraBody : VamBasicBody
	{
		private IBodyPart[] parts_;
		private VamCameraHand left_, right_;

		public VamCameraBody(VamCameraAtom a)
			: base(null)
		{
			parts_ = new IBodyPart[BP.Count];

			left_ = new VamCameraHand(
				a, BP.LeftHand,
				MeshVR.Hands.HandOutput.Hand.Left,
				SuperController.singleton.leftHand,
				null);

			right_ = new VamCameraHand(
				a, BP.RightHand,
				MeshVR.Hands.HandOutput.Hand.Right,
				SuperController.singleton.rightHand,
				SuperController.singleton.mouseGrab);

			parts_[BP.Head.Int] = new VamCameraHead(a);
			parts_[BP.Eyes.Int] = new VamCameraEyes(a);
			parts_[BP.LeftHand.Int] = left_;
			parts_[BP.RightHand.Int] = right_;

			foreach (BodyPartType i in BodyPartType.Values)
			{
				if (parts_[i.Int] == null)
					parts_[i.Int] = new NullBodyPart(Atom, i);
			}
		}

		public void Init()
		{
			foreach (var p in parts_)
				(p as VamBodyPart).Init();
		}

		public override bool Exists
		{
			get { return false; }
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

		public override VamBodyPart BodyPartForTransform(Transform t, Transform stop, bool debug)
		{
			// see VamSys.BodyPartForTransform()

			if (left_.ContainsTransform(t, debug))
				return left_;
			else if (right_.ContainsTransform(t, debug))
				return right_;
			else
				return null;
		}
	}


	class VamCameraAtom : VamBasicAtom
	{
		private VamCameraBody body_;
		private VamHair hair_;

		public VamCameraAtom()
		{
			body_ = new VamCameraBody(this);
			hair_ = new VamHair(null);
		}

		public override string Warning
		{
			get { return ""; }
		}

		public override string ID
		{
			get { return "Camera"; }
		}

		public override bool IsPerson
		{
			get { return true; }
		}

		public override bool IsMale
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

		public override bool Possessed
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

		public override bool Selected
		{
			get { return false; }
		}

		public override bool Grabbed
		{
			get { return false; }
		}

		public override IBody Body { get { return body_; } }
		public override IHair Hair { get { return hair_; } }

		public override bool Visible { get { return true; } set { } }
		public override bool Collisions { get; set; }
		public override bool Physics { get; set; }
		public override bool Hidden { get; set; }
		public override float Scale { get; set; }
		public override bool AutoBlink { get; set; }
		public override Vector3 Position { get; set; }
		public override Quaternion Rotation { get; set; }
		public bool NavEnabled { get; set; }
		public bool NavPaused { get; set; }
		public int NavState { get; }
		public override Atom Atom { get { return null; } }

		public override VamBodyPart RealBodyPart(VamBodyPart bp)
		{
			return Cue.Instance.Player.Body.Get(bp.Type).Sys as VamBodyPart;
		}

		public override void Destroy()
		{
		}

		public override IMorph GetMorph(string id)
		{
			return null;
		}

		public override void Init()
		{
			body_.Init();
		}

		public void NavStop(string why)
		{
		}

		public void NavTo(Vector3 v, float bearing, float stoppingDistance)
		{
		}

		public override void OnPluginState(bool b)
		{
		}

		public override void SetDefaultControls(string why)
		{
		}

		public override void SetParentLink(IBodyPart bp)
		{
		}

		public override void SetBodyDamping(int e)
		{
		}

		public override void SetCollidersForKiss(bool b, IAtom other)
		{
		}

		public void TeleportTo(Vector3 p, float bearing)
		{
		}

		public override void Update(float s)
		{
		}

		public override void LateUpdate(float s)
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
