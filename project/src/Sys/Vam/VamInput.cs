using UnityEngine;

namespace Cue.W
{
	class VamInput : IInput
	{
		private VamSys sys_;
		private Ray ray_ = new Ray();
		private bool controlsToggle_ = false;
		private Vector3 middlePos_ = Vector3.Zero;
		private bool middleDown_ = false;

		public VamInput(VamSys sys)
		{
			sys_ = sys;
		}

		public bool ReloadPlugin
		{
			get
			{
				return Input.GetKeyDown(KeyCode.F5);
			}
		}

		public bool MenuToggle
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
					return Input.GetMouseButtonUp(0);
			}
		}

		public bool Action
		{
			get
			{
				if (sys_.IsVR)
					return SuperController.singleton.GetRightGrab();
				else
					return Input.GetMouseButtonUp(1);
			}
		}

		public bool ShowControls
		{
			get
			{
				if (sys_.IsVR)
				{
					return
						SuperController.singleton.GetLeftUIPointerShow() ||
						SuperController.singleton.GetRightUIPointerShow();
				}
				else
				{
					if (middleDown_)
					{
						if (Input.GetMouseButtonUp(2))
						{
							var cp = W.VamU.FromUnity(
								SuperController.singleton.MonitorCenterCamera
									.transform.position);

							var d = Vector3.Distance(middlePos_, cp);
							if (d < 0.02f)
								controlsToggle_ = !controlsToggle_;

							middleDown_ = false;
						}
					}
					else
					{
						if (Input.GetMouseButtonDown(2))
						{
							var cp = W.VamU.FromUnity(
								SuperController.singleton.MonitorCenterCamera
									.transform.position);

							middleDown_ = true;
							middlePos_ = cp;
						}
					}

					return controlsToggle_;
				}
			}
		}

		public IObject GetHovered()
		{
			if (sys_.IsVR)
			{
				if (!GetVRRay())
					return null;
			}
			else
			{
				if (!GetMouseRay())
					return null;
			}

			if (HitUI())
				return null;

			var o = HitObject();
			if (o != null)
				return o;

			o = HitPerson();
			if (o != null)
				return o;

			return null;
		}

		private bool GetMouseRay()
		{
			ray_ = SuperController.singleton.MonitorCenterCamera
				.ScreenPointToRay(Input.mousePosition);

			return true;
		}

		private bool GetVRRay()
		{
			if (SuperController.singleton.GetLeftUIPointerShow())
			{
				ray_.origin = SuperController.singleton.viveObjectLeft.position;
				ray_.direction = SuperController.singleton.viveObjectLeft.forward;
				return true;
			}
			else if (SuperController.singleton.GetRightUIPointerShow())
			{
				ray_.origin = SuperController.singleton.viveObjectRight.position;
				ray_.direction = SuperController.singleton.viveObjectRight.forward;
				return true;
			}
			else
			{
				return false;
			}
		}

		private bool HitUI()
		{
			// todo
			return false;
		}

		private IObject HitObject()
		{
			RaycastHit hit;

			bool b = Physics.Raycast(
				ray_, out hit, float.MaxValue, 1 << VamBoxGraphic.Layer);

			if (!b)
				return null;

			// todo
			var cs = Cue.Instance.Controls.All;
			for (int i = 0; i < cs.Count; ++i)
			{
				var g = cs[i].Graphic as VamBoxGraphic;
				if (g.Transform == hit.transform)
					return cs[i].Object;
			}

			return null;
		}

		private IObject HitPerson()
		{
			var a = HitAtom();
			if (a == null)
				return null;

			var ps = Cue.Instance.Persons;

			for (int i = 0; i < ps.Count; ++i)
			{
				if (((W.VamAtom)ps[i].Atom).Atom == a)
					return ps[i];
			}

			return null;
		}

		private Atom HitAtom()
		{
			RaycastHit hit;
			bool b = Physics.Raycast(
				ray_, out hit, float.MaxValue, 0x24000100);

			if (!b)
				return null;

			var fc = hit.transform.GetComponent<FreeControllerV3>();

			if (fc != null)
				return fc.containingAtom;

			var bone = hit.transform.GetComponent<DAZBone>();
			if (bone != null)
				return bone.containingAtom;

			var rb = hit.transform.GetComponent<Rigidbody>();
			var p = rb.transform;

			while (p != null)
			{
				var a = p.GetComponent<Atom>();
				if (a != null)
					return a;

				p = p.parent;
			}

			return null;
		}
	}
}
