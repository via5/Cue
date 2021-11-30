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
		private CircularIndex<UIActions.IItem> widgetSel_;
		private int state_ = Hidden;
		private VUI.VRHandRootSupport vrSupport_ = null;

		public VRMenu(bool debugDesktop)
		{
			sys_ = Cue.Instance.Sys;
			widgetSel_ = new CircularIndex<UIActions.IItem>(Items);

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
			SetWidget(widgetSel_.Index);
			PersonChanged();
		}

		public override void CheckInput()
		{
			if (sys_.IsPlayMode || UI.VRMenuDebug)
			{
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
			if (UI.VRMenuAlwaysVisible)
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
