using System.Collections.Generic;

namespace Cue
{
	class VRMenu : BasicMenu
	{
		private const float Width = 300;
		private const float MinDesktopHeight = 375;
		private const float MinVRHeight = 350;

		private const int Hidden = 0;
		private const int Left = 1;
		private const int Right = 2;

		private readonly Sys.ISys sys_;
		private float minHeight_;
		private VUI.Label name_ = null;
		private VUI.Panel buttons_ = null;
		private CircularIndex<UIActions.IItem> widgetSel_;
		private int state_ = Hidden;
		private VUI.VRHandRootSupport vrSupport_ = null;
		private float menuDelay_ = 0;

		public VRMenu(bool debugDesktop)
		{
			sys_ = Cue.Instance.Sys;

			VUI.Root root;

			if (debugDesktop)
			{
				minHeight_ = MinDesktopHeight;
				root = new VUI.Root(
					new VUI.OverlayRootSupport(10, Width, MinDesktopHeight),
					"cue.vrui.debug");
			}
			else
			{
				minHeight_ = MinVRHeight;

				vrSupport_ = new VUI.VRHandRootSupport(
					VUI.VRHandRootSupport.LeftHand,
					new UnityEngine.Vector3(0, 0.1f, 0),
					new UnityEngine.Vector2(0, 0),
					new UnityEngine.Vector2(Width, MinVRHeight));

				root = new VUI.Root(vrSupport_, "cue.vrui");
			}

			var ly = new VUI.VerticalFlow();
			var p = new VUI.Panel(ly);

			name_ = p.Add(new VUI.Label(
				"", VUI.Align.VCenterCenter, UnityEngine.FontStyle.Bold));

			p.Add(new VUI.Spacer(10));

			buttons_ = new VUI.Panel(new VUI.VerticalFlow());
			p.Add(buttons_);

			if (UI.VRMenuDebug)
			{
				var gl = new VUI.GridLayout(2, 2);
				gl.HorizontalStretch = new List<bool> { true, false };
				var dp = new VUI.Panel(gl);
				dp.Add(new VUI.ToolButton("Reload", () => Cue.Instance.ReloadPlugin()));
				dp.Add(new VUI.ToolButton("UI", () => Cue.Instance.OpenScriptUI()));
				p.Add(dp);
			}

			root.ContentPanel.Layout = new VUI.BorderLayout();
			root.ContentPanel.Add(p, VUI.BorderLayout.Center);

			p.Events.Wheel += (w) =>
			{
				if (w.Delta.Y > 0)
					PreviousWidget();
				else if (w.Delta.Y < 0)
					NextWidget();

				w.Bubble = true;
			};

			SetRoot(root);
			Rebuild();

			UpdateVisibility();
			SetWidget(widgetSel_.Index);
			PersonChanged();

			Cue.Instance.Options.CustomMenuItems.Changed += Rebuild;
		}

		public override void Destroy()
		{
			base.Destroy();
			Cue.Instance.Options.CustomMenuItems.Changed -= Rebuild;
		}

		private void Rebuild()
		{
			widgetSel_ = new CircularIndex<UIActions.IItem>(Items);
			buttons_.RemoveAllChildren();

			foreach (var i in Items)
				buttons_.Add(i.Panel);

			float height =
				minHeight_ +
				Cue.Instance.Options.CustomMenuItems.Items.Length *
				VUI.Style.Metrics.ButtonMinimumSize.Height;

			Root.RootSupport.SetSize(new UnityEngine.Vector3(Width, height));
			Root.SupportBoundsChanged();
		}

		private bool CheckMenuDelay(float s)
		{
			menuDelay_ += s;

			if (menuDelay_ >= Cue.Instance.Options.MenuDelay)
				return true;

			return false;
		}

		public override void CheckInput(float s)
		{
			if (sys_.IsPlayMode || UI.VRMenuDebug)
			{
				if (sys_.Input.ShowLeftMenu)
				{
					if (CheckMenuDelay(s))
						ShowLeft();
				}
				else if (sys_.Input.ShowRightMenu)
				{
					if (CheckMenuDelay(s))
						ShowRight();
				}
				else
				{
					Hide();
					menuDelay_ = 0;
				}
			}
			else
			{
				Hide();
				menuDelay_ = 0;
			}


			if (state_ != Hidden)
			{
				CheckItemInput();
				CheckPersonInput();

				if (Cue.Instance.Sys.Input.MenuSelect)
					widgetSel_.Value?.Activate();
			}
		}

		private void CheckItemInput()
		{
			if (Cue.Instance.Sys.Input.MenuDown)
				NextWidget();
			else if (Cue.Instance.Sys.Input.MenuUp)
				PreviousWidget();
		}

		private void CheckPersonInput()
		{
			if (Cue.Instance.Sys.Input.MenuRight)
				NextPerson();
			else if (Cue.Instance.Sys.Input.MenuLeft)
				PreviousPerson();
		}

		public void NextWidget()
		{
			widgetSel_.Next(+1);
			SetWidget(widgetSel_.Index);
		}

		public void PreviousWidget()
		{
			widgetSel_.Next(-1);
			SetWidget(widgetSel_.Index);
		}

		public void SetWidget(int index)
		{
			for (int i = 0; i < Items.Count; ++i)
				Items[i].Selected = (i == index);
		}

		public void ShowLeft()
		{
			if (Cue.Instance.Options.LeftMenu)
			{
				state_ = Left;
				UpdateVisibility();
			}
		}

		public void ShowRight()
		{
			if (Cue.Instance.Options.RightMenu)
			{
				state_ = Right;
				UpdateVisibility();
			}
		}

		public void Hide()
		{
			state_ = Hidden;
			UpdateVisibility();
		}

		private void UpdateVisibility()
		{
			if (UI.VRMenuAlwaysVisible)
				state_ = Left;

			switch (state_)
			{
				case Hidden:
				{
					Visible = false;
					Cue.Instance.Sys.SetMenuVisible(false);
					break;
				}

				case Left:
				{
					Visible = true;

					if (vrSupport_ != null)
						vrSupport_.Attach(VUI.VRHandRootSupport.LeftHand);

					if (!UI.VRMenuAlwaysVisible)
						Cue.Instance.Sys.SetMenuVisible(true);

					break;
				}

				case Right:
				{
					Visible = true;

					if (vrSupport_ != null)
						vrSupport_.Attach(VUI.VRHandRootSupport.RightHand);

					if (!UI.VRMenuAlwaysVisible)
						Cue.Instance.Sys.SetMenuVisible(true);

					break;
				}
			}
		}

		protected override void PersonChanged()
		{
			name_.Text = SelectedPerson?.ID ?? "";
		}
	}
}
