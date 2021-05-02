using UnityEngine;

namespace Cue.W
{
	class VamButton
	{
		private Vector3 downPos_ = Vector3.Zero;
		private bool down_ = false;
		private bool clicked_ = false;
		private readonly int b_;

		public VamButton(int b)
		{
			b_ = b;
		}

		public bool Clicked
		{
			get { return clicked_; }
		}

		public void Update()
		{
			clicked_ = false;

			if (down_)
			{
				if (Input.GetMouseButtonUp(b_))
				{
					var cp = CameraPosition;
					var d = Vector3.Distance(downPos_, cp);

					if (d < 0.02f)
						clicked_ = true;

					down_ = false;
				}
			}
			else
			{
				if (Input.GetMouseButtonDown(b_))
				{
					down_ = true;
					downPos_ = CameraPosition;
				}
			}
		}

		private Vector3 CameraPosition
		{
			get
			{
				return W.VamU.FromUnity(
					SuperController.singleton.MonitorCenterCamera
						.transform.position);
			}
		}
	}


	class VamInput : IInput
	{
		private VamSys sys_;
		private Ray ray_ = new Ray();
		private VamButton left_ = new VamButton(0);
		private VamButton right_ = new VamButton(1);
		private VamButton middle_ = new VamButton(2);
		private bool uiPointerShow_ = false;

		public VamInput(VamSys sys)
		{
			sys_ = sys;
		}

		public void Update()
		{
			left_.Update();
			right_.Update();
			middle_.Update();
		}

		public bool HardReset
		{
			get
			{
				var shift =
					Input.GetKey(KeyCode.LeftShift) ||
					Input.GetKey(KeyCode.RightShift);

				return shift && Input.GetKeyDown(KeyCode.F5);
			}
		}

		public bool ReloadPlugin
		{
			get
			{
				return Input.GetKeyDown(KeyCode.F5);
			}
		}

		public bool ToggleMenu
		{
			get
			{
				if (sys_.IsVR)
					return SuperController.singleton.GetLeftSelect();
				else
					return false;
			}
		}

		public bool Select
		{
			get
			{
				if (sys_.IsVR)
					return SuperController.singleton.GetRightSelect();
				else
					return left_.Clicked;
			}
		}

		public bool LeftAction
		{
			get
			{
				if (sys_.IsVR)
					return SuperController.singleton.GetLeftGrab();
				else
					return right_.Clicked;
			}
		}

		public bool RightAction
		{
			get
			{
				if (sys_.IsVR)
					return SuperController.singleton.GetRightGrab();
				else
					return right_.Clicked;
			}
		}

		public bool ToggleControls
		{
			get
			{
				if (sys_.IsVR)
				{
					var d =
						SuperController.singleton.GetLeftUIPointerShow() ||
						SuperController.singleton.GetRightUIPointerShow();

					if (!uiPointerShow_ && d)
					{
						uiPointerShow_ = true;
						return true;
					}
					else
					{
						uiPointerShow_ = false;
					}

					return false;
				}
				else
				{
					return middle_.Clicked;
				}
			}
		}

		public bool ShowLeftMenu
		{
			get
			{
				if (sys_.IsVR)
					return SuperController.singleton.GetLeftUIPointerShow();
				else
					return true;
			}
		}

		public bool ShowRightMenu
		{
			get
			{
				if (sys_.IsVR)
					return SuperController.singleton.GetRightUIPointerShow();
				else
					return true;
			}
		}

		public HoveredInfo GetLeftHovered()
		{
			if (sys_.IsVR)
			{
				if (!GetLeftVRRay())
					return HoveredInfo.None;
			}
			else
			{
				if (!GetMouseRay())
					return HoveredInfo.None;
			}

			return GetHovered();
		}

		public HoveredInfo GetRightHovered()
		{
			if (sys_.IsVR)
			{
				if (!GetRightVRRay())
					return HoveredInfo.None;
			}
			else
			{
				if (!GetMouseRay())
					return HoveredInfo.None;
			}

			return GetHovered();
		}

		private HoveredInfo GetHovered()
		{
			if (HitUI())
				return HoveredInfo.None;

			var h = HitPerson();
			if (h.hit)
				return h;

			h = HitObject();
			if (h.hit)
				return h;

			return HitScene();
		}

		private HoveredInfo HitScene()
		{
			// todo: this probably doesn't apply to all CUAs, but it works for
			// now
			var layer =
				Bits.Bit(9);

			RaycastHit hit;
			bool b = Physics.Raycast(ray_, out hit, float.MaxValue, layer);

			if (!b)
				return HoveredInfo.None;

			return new HoveredInfo(null, W.VamU.FromUnity(hit.point), true);
		}

		private bool GetMouseRay()
		{
			ray_ = SuperController.singleton.MonitorCenterCamera
				.ScreenPointToRay(Input.mousePosition);

			return true;
		}

		private bool GetLeftVRRay()
		{
			ray_.origin = SuperController.singleton.viveObjectLeft.position;
			ray_.direction = SuperController.singleton.viveObjectLeft.forward;
			return true;
		}

		private bool GetRightVRRay()
		{
			ray_.origin = SuperController.singleton.viveObjectRight.position;
			ray_.direction = SuperController.singleton.viveObjectRight.forward;
			return true;
		}

		private bool HitUI()
		{
			// todo
			return false;
		}

		private HoveredInfo HitObject()
		{
			RaycastHit hit;

			bool b = Physics.Raycast(
				ray_, out hit, float.MaxValue, Bits.Bit(VamGraphic.Layer));

			if (!b)
				return HoveredInfo.None;

			// todo
			var cs = Cue.Instance.Controls.All;
			for (int i = 0; i < cs.Count; ++i)
			{
				var g = cs[i].Graphic as VamBoxGraphic;
				if (g.Transform == hit.transform)
				{
					return new HoveredInfo(
						cs[i].Object, W.VamU.FromUnity(hit.point), true);
				}
			}

			return HoveredInfo.None;
		}

		private HoveredInfo HitPerson()
		{
			HoveredInfo h;
			Atom a;
			if (!HitAtom(out h, out a))
				return HoveredInfo.None;

			var ps = Cue.Instance.Persons;

			for (int i = 0; i < ps.Count; ++i)
			{
				if (((W.VamAtom)ps[i].Atom).Atom == a)
					return new HoveredInfo(ps[i], h.pos, true);
			}

			return HoveredInfo.None;
		}

		private bool HitAtom(out HoveredInfo hi, out Atom a)
		{
			// todo: ignore stuff like eye targets, there's a couple ways:
			//
			//  1) check the transform name after hitting, but this will ignore
			//     atoms that are behind the eye target, because it'll always be
			//     hit first, and HitAtom() will immediately return
			//
			//  2) use RaycastAll to fix the above, but they need to be sorted
			//     manually, unity doesn't do it, and it's more expensive
			//
			//  3) temporarily change the layer of all these objects to one
			//     that's ignored and restore after; not sure about the cost of
			//     changing layers on the fly, it changes internal vam stuff,
			//     and it would need to keep a list of transforms somewhere,
			//     but it sounds like the best way

			int layer =
				Bits.Bit(8) |
				Bits.Bit(26) |
				Bits.Bit(29);

			RaycastHit hit;
			bool b = Physics.Raycast(ray_, out hit, float.MaxValue, layer);

			if (!b)
			{
				hi = HoveredInfo.None;
				a = null;
				return false;
			}

			var fc = hit.transform.GetComponent<FreeControllerV3>();
			if (fc != null)
			{
				hi = new HoveredInfo(null, W.VamU.FromUnity(hit.point), true);
				a = fc.containingAtom;
				return true;
			}

			var bone = hit.transform.GetComponent<DAZBone>();
			if (bone != null)
			{
				hi = new HoveredInfo(null, W.VamU.FromUnity(hit.point), true);
				a = bone.containingAtom;
				return true;
			}

			var rb = hit.transform.GetComponent<Rigidbody>();
			var p = rb.transform;

			while (p != null)
			{
				var ra = p.GetComponent<Atom>();
				if (ra != null)
				{
					hi = new HoveredInfo(null, W.VamU.FromUnity(hit.point), true);
					a = ra;
					return true;
				}

				p = p.parent;
			}

			hi = HoveredInfo.None;
			a = null;

			return false;
		}
	}
}
