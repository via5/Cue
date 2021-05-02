namespace Cue
{
	class Hud
	{
		private VUI.Root root_ = null;
		private VUI.Label sel_ = null;
		private VUI.Label hovered_ = null;
		private bool visible_ = false;

		public bool Visible
		{
			get
			{
				return visible_;
			}

			set
			{
				visible_ = value;
				if (root_ != null)
					root_.Visible = value;
			}
		}

		public void Create(bool vr)
		{
			if (vr)
			{
				root_ = Cue.Instance.Sys.CreateHud(
					new Vector3(0, 0, 2),
					new Point(-1000, 200),
					new Size(3000, 1000));
			}
			else
			{
				root_ = Cue.Instance.Sys.Create2D(20, new Size(1000, 100));
			}

			root_.ContentPanel.Layout = new VUI.BorderLayout();

			var p = new VUI.Panel(new VUI.VerticalFlow());
			sel_ = p.Add(new VUI.Label());
			hovered_ = p.Add(new VUI.Label());
			root_.ContentPanel.Add(p, VUI.BorderLayout.Center);

			root_.Visible = visible_;
		}

		public void Destroy()
		{
			if (root_ != null)
			{
				root_.Destroy();
				root_ = null;
			}
		}

		public void Update()
		{
			if (visible_)
			{
				//sel_.Text = "Sel: " + (Cue.Instance.Selected == null ? "" : Cue.Instance.Selected.ToString());
				//hovered_.Text = "Hovered: " + (Cue.Instance.Hovered == null ? "" : Cue.Instance.Hovered.ToString());
				//
				//root_.Update();
			}
		}
	}
}
