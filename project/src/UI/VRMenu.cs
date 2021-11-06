using System.Collections.Generic;

namespace Cue
{
	class VRMenu : BasicMenu
	{
		private const int Hidden = 0;
		private const int Left = 1;
		private const int Right = 2;

		private readonly Sys.ISys sys_;
		private VUI.Label name_ = null;
		private int widgetSel_ = -1;
		private int state_ = Hidden;
		private VUI.VRHandRootSupport vrSupport_ = null;

		public VRMenu(bool debugDesktop)
		{
			sys_ = Cue.Instance.Sys;

			VUI.Root root;

			if (debugDesktop)
			{
				root = new VUI.Root(new VUI.OverlayRootSupport(10, 300, 400));
			}
			else
			{
				vrSupport_ = new VUI.VRHandRootSupport(
					VUI.VRHandRootSupport.LeftHand,
					new UnityEngine.Vector3(0, 0.1f, 0),
					new UnityEngine.Vector2(0, 0),
					new UnityEngine.Vector2(300, 350));

				root = new VUI.Root(vrSupport_);
			}

			var ly = new VUI.VerticalFlow();
			var p = new VUI.Panel(ly);

			name_ = p.Add(new VUI.Label(
				"", VUI.Label.AlignCenter | VUI.Label.AlignVCenter,
				UnityEngine.FontStyle.Bold));

			p.Add(new VUI.Spacer(10));

			foreach (var i in Items)
				p.Add(i.Panel);

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

				return true;
			};

			SetRoot(root);

			UpdateVisibility();
			PersonChanged();
		}

		public override void CheckInput()
		{
			if (sys_.IsPlayMode)
			{
				var lh = sys_.Input.GetLeftHovered();
				var rh = sys_.Input.GetRightHovered();

				if (sys_.Input.ShowLeftMenu)
					ShowLeft();
				else if (sys_.Input.ShowRightMenu)
					ShowRight();
				else
					Hide();
			}
			else
			{
				Hide();
			}


			CheckItemInput();
			CheckPersonInput();

			if (Cue.Instance.Sys.Input.MenuSelect)
			{
				if (widgetSel_ >= 0 && widgetSel_ < Items.Count)
					Items[widgetSel_].Activate();
			}
		}

		private void CheckItemInput()
		{
			if (Cue.Instance.Sys.Input.MenuDown)
				NextWidget();
			else if (Cue.Instance.Sys.Input.MenuUp)
				PreviousWidget();
		}

		public void NextWidget()
		{
			ChangeWidget(+1);
		}

		public void PreviousWidget()
		{
			ChangeWidget(-1);
		}

		public void ChangeWidget(int dir)
		{
			var old = widgetSel_;

			widgetSel_ += dir;
			if (widgetSel_ >= Items.Count)
				widgetSel_ = 0;
			else if (widgetSel_ < 0)
				widgetSel_ = Items.Count - 1;

			if (widgetSel_ == -1)
				widgetSel_ = 0;

			if (widgetSel_ != old)
			{
				for (int i = 0; i < Items.Count; ++i)
					Items[i].Selected = (i == widgetSel_);
			}
		}

		private void CheckPersonInput()
		{
			if (Cue.Instance.Sys.Input.MenuRight)
				NextPerson();
			else if (Cue.Instance.Sys.Input.MenuLeft)
				PreviousPerson();
		}

		public void ShowLeft()
		{
			state_ = Left;
			UpdateVisibility();
		}

		public void ShowRight()
		{
			state_ = Right;
			UpdateVisibility();
		}

		public void Hide()
		{
			state_ = Hidden;
			UpdateVisibility();
		}

		private void UpdateVisibility()
		{
			if (UI.VRMenuDebug)
				state_ = Left;

			switch (state_)
			{
				case Hidden:
				{
					Visible = false;
					break;
				}

				case Left:
				{
					Visible = true;

					if (vrSupport_ != null)
						vrSupport_.Attach(VUI.VRHandRootSupport.LeftHand);

					break;
				}

				case Right:
				{
					Visible = true;

					if (vrSupport_ != null)
						vrSupport_.Attach(VUI.VRHandRootSupport.RightHand);

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
