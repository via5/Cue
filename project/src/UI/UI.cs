﻿namespace Cue
{
	class UI
	{
		private W.ISys sys_;
		private Menu leftMenu_ = null;
		private Menu rightMenu_ = null;
		private Menu desktopMenu_ = null;
		private Controls controls_ = null;
		private bool vr_ = false;

		public UI(W.ISys sys)
		{
			sys_ = sys;
			vr_ = sys_.IsVR;
			leftMenu_ = new Menu();
			rightMenu_ = new Menu();
			desktopMenu_ = new Menu();
			controls_ = new Controls();
		}

		public Controls Controls
		{
			get { return controls_; }
		}

		public void CheckInput()
		{
			var vr = sys_.IsVR;
			if (vr_ != vr)
			{
				vr_ = vr;
				DestroyUI();
				CreateUI();
			}

			if (sys_.IsPlayMode)
			{
				if (vr_)
					CheckVRInput();
				else
					CheckDesktopInput();
			}
		}

		public void OnPluginState(bool b)
		{
			if (b)
				CreateUI();
			else
				DestroyUI();
		}

		private void CreateUI()
		{
			controls_.Create();

			if (vr_)
			{
				leftMenu_?.Create(vr_, true);
				rightMenu_?.Create(vr_, false);
			}
			else
			{
				desktopMenu_?.Create(false, false);
				desktopMenu_.Visible = true;
			}
		}

		private void DestroyUI()
		{
			controls_?.Destroy();
			leftMenu_?.Destroy();
			rightMenu_?.Destroy();
			desktopMenu_?.Destroy();
		}

		private void CheckVRInput()
		{
			var lh = sys_.Input.GetLeftHovered();
			var rh = sys_.Input.GetRightHovered();

			if (sys_.Input.ShowLeftMenu)
			{
				controls_.HoverTargetVisible = true;
				controls_.HoverTargetPosition = rh.pos;
			}
			else if (sys_.Input.ShowRightMenu)
			{
				controls_.HoverTargetVisible = true;
				controls_.HoverTargetPosition = lh.pos;
			}
			else
			{
				controls_.HoverTargetVisible = false;
			}


			if (lh.o != null)
			{
				leftMenu_.Visible = sys_.Input.ShowLeftMenu;
				leftMenu_.Selected = lh.o as Person;

				rightMenu_.Visible = false;
				rightMenu_.Selected = null;

				if (sys_.Input.RightAction)
				{
					Cue.LogInfo($"right action {lh.o} {rh.o}");

					if (rh.o != null)
					{
						Cue.LogInfo($"interacting {lh.o} with {rh.o}");
						lh.o.InteractWith(rh.o);
					}
					else if (rh.hit)
					{
						Cue.LogInfo($"hit for {lh.o} on {rh.pos}");
						lh.o.MoveToManual(
							rh.pos,
							Vector3.Bearing(rh.pos - lh.o.Position));
					}
					else
					{
						Cue.LogInfo($"nothing");
					}
				}
			}
			else if (rh.o != null)
			{
				leftMenu_.Visible = false;
				leftMenu_.Selected = null;

				rightMenu_.Visible = sys_.Input.ShowRightMenu;
				rightMenu_.Selected = rh.o as Person;

				if (sys_.Input.LeftAction)
				{
					Cue.LogInfo($"left action {lh.o} {rh.o}");

					if (lh.o != null)
					{
						Cue.LogInfo($"interacting {rh.o} with {lh.o}");
						rh.o.InteractWith(lh.o);
					}
					else if (lh.hit)
					{
						Cue.LogInfo($"hit for {rh.o} on {lh.pos}");
						rh.o.MoveToManual(
							lh.pos,
							Vector3.Bearing(lh.pos - rh.o.Position));
					}
					else
					{
						Cue.LogInfo($"nothing");
					}
				}
			}
			else
			{
				leftMenu_.Visible = false;
				leftMenu_.Selected = null;

				rightMenu_.Visible = false;
				rightMenu_.Selected = null;
			}
		}

		private void CheckDesktopInput()
		{
			var h = sys_.Input.GetMouseHovered();

			if (h.hit)
			{
				controls_.HoverTargetVisible = true;
				controls_.HoverTargetPosition = h.pos;
			}
			else
			{
				controls_.HoverTargetVisible = false;
			}

			if (sys_.Input.Select)
			{
				desktopMenu_.Selected = h.o as Person;
			}

			desktopMenu_.Hovered = h.o;
			controls_.Hovered = h.o;

			if (sys_.Input.Action)
			{
				var src = desktopMenu_.Selected;
				var dest = desktopMenu_.Hovered;

				if (src != null && dest != null && src != dest)
				{
					src.InteractWith(dest);
				}
				else if (h.hit)
				{
					src.MoveToManual(
						h.pos,
						Vector3.Bearing(h.pos - src.Position));
				}
			}

			if (sys_.Input.ToggleControls)
				controls_.Visible = !controls_.Visible;
		}

		public void Update(float s)
		{
			controls_?.Update();
			leftMenu_?.Update();
			rightMenu_?.Update();
			desktopMenu_?.Update();
		}
	}
}