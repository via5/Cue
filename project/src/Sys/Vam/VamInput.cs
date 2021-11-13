﻿using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cue.Sys.Vam
{
	interface IVRInput
	{
		bool IsAtomVRHands(Atom a);
		bool IsTransformVRHand(Transform t, bool left);

		Transform LeftController { get; }
		Transform RightController { get; }

		Vector2 LeftJoystick { get; }
		Vector2 RightJoystick { get; }
	}


	abstract class BasicVRInput : IVRInput
	{
		protected SuperController sc_ = SuperController.singleton;

		private bool got_ = false;
		private Atom cameraRig_ = null;
		private Transform leftHandAnchor_ = null;
		private Transform rightHandAnchor_ = null;

		public abstract Transform LeftController { get; }
		public abstract Transform RightController { get; }
		public abstract Vector2 LeftJoystick { get; }
		public abstract Vector2 RightJoystick { get; }

		public virtual bool IsAtomVRHands(Atom a)
		{
			Get();
			return (a == cameraRig_);
		}

		public virtual bool IsTransformVRHand(Transform t, bool left)
		{
			Get();

			if (left)
				return (t == leftHandAnchor_);
			else
				return (t == rightHandAnchor_);
		}

		private void Get()
		{
			if (got_)
				return;

			got_ = true;

			cameraRig_ = sc_.GetAtomByUid("[CameraRig]");
			if (cameraRig_ == null)
				Cue.LogError("no camera rig");

			leftHandAnchor_ = U.FindChildRecursive(cameraRig_, "LeftHandAnchor")?.transform;
			if (leftHandAnchor_ == null)
				Cue.LogError("camera rig has no left hand anchor");

			rightHandAnchor_ = U.FindChildRecursive(cameraRig_, "RightHandAnchor")?.transform;
			if (rightHandAnchor_ == null)
				Cue.LogError("camera rig has no right hand anchor");
		}
	}


	class SteamVRInput : BasicVRInput
	{
		public override Transform LeftController
		{
			get { return sc_.viveObjectLeft; }
		}

		public override Transform RightController
		{
			get { return sc_.viveObjectRight; }
		}

		public override Vector2 LeftJoystick
		{
			get
			{
				var v = sc_.GetFreeNavigateVector(sc_.freeModeMoveAction);
				return new Vector2(v.x, v.y);
			}
		}

		public override Vector2 RightJoystick
		{
			get
			{
				var v = sc_.GetFreeNavigateVector(sc_.freeModeMoveAction);
				return new Vector2(v.z, v.w);
			}
		}
	}



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
				return U.FromUnity(
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
					//Cue.LogInfo($"{name_} up");
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
						//Cue.LogInfo($"{name_} triggering");
						triggered_ = true;
						trigger_ = true;
					}
				}
			}
			else
			{
				if (getDown_())
				{
					//Cue.LogInfo($"{name_} down");
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



	class Latched
	{
		private readonly string name_;
		private float valueOn_;
		private float resetThreshold_;

		private float highest_ = 0;
		private bool wasOn_ = false;
		private bool onFrame_ = false;

		public Latched(string name, float valueOn, float resetThreshold)
		{
			name_ = name;
			valueOn_ = valueOn;
			resetThreshold_ = resetThreshold;
		}

		public bool OnFrame
		{
			get { return onFrame_; }
		}

		public void Update(float value)
		{
			onFrame_ = false;

			if (Math.Abs(value) < 0.01f)
			{
				highest_ = 0;
				wasOn_ = false;
				return;
			}

			if (valueOn_ < 0)
				UpdateNegative(value);
			else
				UpdatePositive(value);
		}

		private void UpdateNegative(float value)
		{
			if (wasOn_)
			{
				if (value < highest_)
				{
					highest_ = value;
				}
				else
				{
					var d = Math.Abs(highest_ - value);

					if (d > resetThreshold_)
						wasOn_ = false;
				}
			}
			else if (highest_ < 0)
			{
				if (Math.Abs(highest_ - value) < 0.05f)
				{
					onFrame_ = true;
					wasOn_ = true;
					highest_ = value;
				}
			}
			else
			{
				bool on = (value <= valueOn_);

				if (on)
				{
					onFrame_ = true;
					wasOn_ = true;
					highest_ = value;
				}
			}
		}

		private void UpdatePositive(float value)
		{
			if (wasOn_)
			{
				if (value > highest_)
				{
					highest_ = value;
				}
				else
				{
					var d = Math.Abs(highest_ - value);

					if (d > resetThreshold_)
						wasOn_ = false;
				}
			}
			else if (highest_ > 0)
			{
				if (Math.Abs(highest_ - value) < 0.05f)
				{
					onFrame_ = true;
					wasOn_ = true;
					highest_ = value;
				}
			}
			else
			{
				bool on = (value >= valueOn_);

				if (on)
				{
					onFrame_ = true;
					wasOn_ = true;
					highest_ = value;
				}
			}
		}
	}


	class VamInput : IInput
	{
		private SuperController sc_ = SuperController.singleton;
		private VamSys sys_;
		private Ray ray_ = new Ray();
		private VamButton left_ = new VamButton(0);
		private VamButton right_ = new VamButton(1);
		private VamButton middle_ = new VamButton(2);
		private VamDelayedAction leftAction_, rightAction_;
		private IVRInput vr_ = new SteamVRInput();

		private bool leftMenu_ = false;
		private bool leftMenuSticky_ = false;

		private bool rightMenu_ = false;
		private bool rightMenuSticky_ = false;

		private Latched leftMenuUp_ = new Latched("leftMenuUp", 0.5f, 0.2f);
		private Latched leftMenuDown_ = new Latched("leftMenuDown", -0.5f, 0.2f);
		private Latched leftMenuLeft_ = new Latched("leftMenuLeft", -0.8f, 0.3f);
		private Latched leftMenuRight_ = new Latched("leftMenuRight", 0.8f, 0.3f);

		private Latched rightMenuUp_ = new Latched("rightMenuUp", 0.5f, 0.2f);
		private Latched rightMenuDown_ = new Latched("rightMenuDown", -0.5f, 0.2f);
		private Latched rightMenuLeft_ = new Latched("rightMenuLeft", -0.8f, 0.3f);
		private Latched rightMenuRight_ = new Latched("rightMenuRight", 0.8f, 0.3f);

		public VamInput(VamSys sys)
		{
			sys_ = sys;

			leftAction_ = new VamDelayedAction(
				() => sc_.GetLeftGrab(),
				() => sc_.GetLeftGrabRelease(),
				"leftgrab");

			rightAction_ = new VamDelayedAction(
				() => sc_.GetRightGrab(),
				() => sc_.GetRightGrabRelease(),
				"rightgrab");
		}

		public void Update(float s)
		{
			left_.Update();
			right_.Update();
			middle_.Update();
			leftAction_.Update(s);
			rightAction_.Update(s);


			leftMenu_ = sc_.GetLeftUIPointerShow();

			if (leftMenuSticky_)
			{
				if (sc_.GetLeftGrabRelease())
					leftMenuSticky_ = false;
			}
			else if (leftMenu_)
			{
				if (sc_.GetLeftGrab())
					leftMenuSticky_ = true;
			}


			rightMenu_ = sc_.GetRightUIPointerShow();

			if (rightMenuSticky_)
			{
				if (sc_.GetRightGrabRelease())
					rightMenuSticky_ = false;
			}
			else if (rightMenu_)
			{
				if (sc_.GetRightGrab())
					rightMenuSticky_ = true;
			}


			MeshVR.GlobalSceneOptions.singleton.disableNavigation = DisableNav();

			leftMenuUp_.Update(vr_.LeftJoystick.y);
			leftMenuDown_.Update(vr_.LeftJoystick.y);
			leftMenuLeft_.Update(vr_.LeftJoystick.x);
			leftMenuRight_.Update(vr_.LeftJoystick.x);

			rightMenuUp_.Update(vr_.RightJoystick.y);
			rightMenuDown_.Update(vr_.RightJoystick.y);
			rightMenuLeft_.Update(vr_.RightJoystick.x);
			rightMenuRight_.Update(vr_.RightJoystick.x);
		}

		private bool DisableNav()
		{
			if (sc_.gameMode == SuperController.GameMode.Play || UI.VRMenuDebug)
				return (ShowLeftMenu || ShowRightMenu);

			return false;
		}

		public string DebugString()
		{
			return $"left:{vr_.LeftJoystick} right:{vr_.RightJoystick}";
		}

		public string VRInfo()
		{
			return $"isVR={Cue.Instance.Sys.IsVR} ovr={sc_.isOVR} openvr={sc_.isOpenVR}";
		}

		public IVRInput VRInput
		{
			get { return vr_; }
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
				return leftMenu_ || leftMenuSticky_;
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
				return rightMenu_ || rightMenuSticky_;
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

		public bool MenuUp
		{
			get { return leftMenuUp_.OnFrame || rightMenuUp_.OnFrame; }
		}

		public bool MenuDown
		{
			get { return leftMenuDown_.OnFrame || rightMenuDown_.OnFrame; }
		}

		public bool MenuLeft
		{
			get { return leftMenuLeft_.OnFrame || rightMenuLeft_.OnFrame; }
		}

		public bool MenuRight
		{
			get { return leftMenuRight_.OnFrame || rightMenuRight_.OnFrame; }
		}

		public bool MenuSelect
		{
			get
			{
				return
					sc_.GetLeftRemoteHoldGrab() ||
					sc_.GetRightRemoteHoldGrab();
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

			return new HoveredInfo(null, U.FromUnity(hit.point), true);
		}

		private bool GetMouseRay()
		{
			ray_ = sc_.MonitorCenterCamera.ScreenPointToRay(Input.mousePosition);
			return true;
		}

		private bool GetLeftVRRay()
		{
			ray_.origin = vr_.LeftController.position;
			ray_.direction = vr_.LeftController.forward;
			return true;
		}

		private bool GetRightVRRay()
		{
			ray_.origin = vr_.RightController.position;
			ray_.direction = vr_.RightController.forward;
			return true;
		}

		private bool HitUI()
		{
			// todo
			return false;
		}

		private HoveredInfo HitPerson()
		{
			HoveredInfo h;
			Atom a;
			if (!HitAtom(out h, out a))
				return HoveredInfo.None;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p.VamAtom != null && p.VamAtom.Atom == a)
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
				hi = new HoveredInfo(null, U.FromUnity(closestPoint), true);
				return true;
			}

			hi = HoveredInfo.None;
			return false;
		}
	}
}
