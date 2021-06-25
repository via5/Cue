using System;
using UnityEngine;
using UnityEngine.EventSystems;

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


	class VamDelayedAction
	{
		public const float Delay = 3;

		private Func<bool> getDown_, getUp_;
		private string name_;
		private bool down_ = false;
		private bool trigger_ = false;
		private bool triggered_ = false;
		private float elapsed_ = 0;

		public VamDelayedAction(Func<bool> down, Func<bool> up, string name)
		{
			getDown_ = down;
			getUp_ = up;
			name_ = name;
		}

		public void Update(float s)
		{
			trigger_ = false;

			if (down_)
			{
				if (getUp_())
				{
					Cue.LogInfo($"{name_} up");
					down_ = false;
					trigger_ = false;
					triggered_ = false;
					elapsed_ = 0;
				}
				else
				{
					elapsed_ += s;

					if (elapsed_ > 2 && !triggered_)
					{
						Cue.LogInfo($"{name_} triggering");
						triggered_ = true;
						trigger_ = true;
					}
				}
			}
			else
			{
				if (getDown_())
				{
					Cue.LogInfo($"{name_} down");
					elapsed_ = 0;
					down_ = true;
				}
			}
		}

		public bool Trigger
		{
			get { return trigger_; }
		}
	}


	class VamInput : IInput
	{
		private VamSys sys_;
		private Ray ray_ = new Ray();
		private VamButton left_ = new VamButton(0);
		private VamButton right_ = new VamButton(1);
		private VamButton middle_ = new VamButton(2);
		private VamDelayedAction leftAction_, rightAction_;

		public VamInput(VamSys sys)
		{
			sys_ = sys;

			leftAction_ = new VamDelayedAction(
				() => SuperController.singleton.GetLeftGrab(),
				() => SuperController.singleton.GetLeftGrabRelease(),
				"leftgrab");

			rightAction_ = new VamDelayedAction(
				() => SuperController.singleton.GetRightGrab(),
				() => SuperController.singleton.GetRightGrabRelease(),
				"rightgrab");
		}

		public void Update(float s)
		{
			left_.Update();
			right_.Update();
			middle_.Update();
			leftAction_.Update(s);
			rightAction_.Update(s);
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

		public bool ShowLeftMenu
		{
			get
			{
				return SuperController.singleton.GetLeftUIPointerShow();
			}
		}

		public bool LeftAction
		{
			get
			{
				return leftAction_.Trigger;
			}
		}

		public bool ShowRightMenu
		{
			get
			{
				return SuperController.singleton.GetRightUIPointerShow();
			}
		}

		public bool RightAction
		{
			get
			{
				return rightAction_.Trigger;
			}
		}

		public bool Select
		{
			get
			{
				return left_.Clicked && !MouseOnUI();
			}
		}

		public bool Action
		{
			get
			{
				return right_.Clicked && !MouseOnUI();
			}
		}

		public bool ToggleControls
		{
			get
			{
				return middle_.Clicked;
			}
		}

		private bool MouseOnUI()
		{
			return EventSystem.current.IsPointerOverGameObject();
		}

		public HoveredInfo GetLeftHovered()
		{
			if (!GetLeftVRRay())
				return HoveredInfo.None;

			return GetHovered();
		}

		public HoveredInfo GetRightHovered()
		{
			if (!GetRightVRRay())
				return HoveredInfo.None;

			return GetHovered();
		}

		public HoveredInfo GetMouseHovered()
		{
			if (!GetMouseRay())
				return HoveredInfo.None;

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
			var cs = Cue.Instance.UI.Controls.All;
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

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p.VamAtom.Atom == a)
					return new HoveredInfo(p, h.pos, true);
			}

			return HoveredInfo.None;
		}

		private bool HitAtom(out HoveredInfo hi, out Atom a)
		{
			int layer =
				Bits.Bit(8) |
				Bits.Bit(26) |
				Bits.Bit(29);

			var hits = Physics.RaycastAll(ray_, float.MaxValue, layer);

			if (hits.Length == 0)
			{
				hi = HoveredInfo.None;
				a = null;
				return false;
			}

			UnityEngine.Vector3 closestPoint = UnityEngine.Vector3.zero;
			float closestDistance = float.MaxValue;
			a = null;

			for (int i = 0; i < hits.Length; ++i)
			{
				var hit = hits[i];

				var fc = hit.transform.GetComponent<FreeControllerV3>();
				if (fc != null)
				{
					if (fc.name != "eyeTargetControl")
					{
						if (hit.distance < closestDistance)
						{
							closestPoint = hit.point;
							closestDistance = hit.distance;
							a = fc.containingAtom;
						}
					}

					continue;
				}

				var bone = hit.transform.GetComponent<DAZBone>();
				if (bone != null)
				{
					if (hit.distance < closestDistance)
					{
						closestPoint = hit.point;
						closestDistance = hit.distance;
						a = bone.containingAtom;
					}

					continue;
				}

				var p = hit.transform;
				while (p != null)
				{
					var ra = p.GetComponent<Atom>();
					if (ra != null)
					{
						if (hit.distance < closestDistance)
						{
							closestPoint = hit.point;
							closestDistance = hit.distance;
							a = ra;
						}

						break;
					}

					p = p.parent;
				}
			}

			if (a != null)
			{
				hi = new HoveredInfo(null, W.VamU.FromUnity(closestPoint), true);
				return true;
			}

			hi = HoveredInfo.None;
			return false;
		}
	}
}
