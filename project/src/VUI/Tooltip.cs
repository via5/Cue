using UnityEngine.UI;

namespace VUI
{
	class Tooltip
	{
		private string text_ = "";

		public Tooltip(string text = "")
		{
			text_ = text;
		}

		public string Text
		{
			get { return text_; }
			set { text_ = value; }
		}
	}


	class TooltipWidget : Panel
	{
		public override string TypeName { get { return "TooltipWidget"; } }

		private readonly Label label_;

		public TooltipWidget()
		{
			Layout = new BorderLayout();

			Borders = new Insets(1);
			BackgroundColor = Style.Theme.BackgroundColor;
			Padding = new Insets(5);
			Visible = false;

			label_ = new Label();
			Add(label_, BorderLayout.Center);

			Tooltip.Text = "";
			label_.Tooltip.Text = "";
		}

		public override void Create()
		{
			base.Create();

			foreach (var c in MainObject.GetComponentsInChildren<Graphic>())
				c.raycastTarget = false;
		}

		public new void Destroy()
		{
			base.Destroy();
		}

		public string Text
		{
			get { return label_.Text; }
			set { label_.Text = value; }
		}
	}


	class TooltipManager
	{
		private readonly Root root_;
		private readonly TooltipWidget widget_ = new TooltipWidget();
		private Timer timer_ = null;
		private Widget active_ = null;

		public TooltipManager(Root root)
		{
			root_ = root;
			root_.FloatingPanel.Add(widget_);
		}

		public void Destroy()
		{
			Hide();
			widget_.Destroy();
		}

		public void WidgetEntered(Widget w)
		{
			if (active_ == w)
				return;

			if (w.Tooltip.Text != "")
			{
				Hide();
				active_ = w;

				timer_ = TimerManager.Instance.CreateTimer(
					Style.Metrics.TooltipDelay, () =>
					{
						timer_ = null;
						Show(active_);
					});
			}
		}

		public void WidgetExited(Widget w)
		{
			if (active_ == w)
				Hide();
		}

		public void Show(Widget w)
		{
			// current mouse position
			var mp = root_.MousePosition;
			if (mp == Root.NoMousePos)
				return;

			active_ = w;
			widget_.Text = w.Tooltip.Text;

			// size of text
			var size = Root.FitText(null, -1, widget_.Text, new Size(
				Style.Metrics.MaxTooltipWidth, Widget.DontCare));

			// widget is size of text plus its insets
			size += widget_.Insets.Size;

			// preferred position is just below the cursor
			var p = new Point(mp.X, mp.Y + Style.Metrics.CursorHeight);

			// available rectangle, offset from the edges
			var av = root_.Bounds.DeflateCopy(
				Style.Metrics.TooltipBorderOffset);


			if (p.X + size.Width >= av.Width)
			{
				// tooltip would extend past the right edge
				p.X = av.Width - size.Width;
			}

			if (p.Y + size.Height >= av.Height)
			{
				// tooltip would extend past the bottom edge; make sure it's
				// above the mouse cursor
				p.Y = mp.Y - size.Height;
			}

			widget_.Bounds = new Rectangle(p.X, p.Y, size);
			widget_.Visible = true;
			widget_.BringToTop();
			widget_.DoLayout();
			widget_.UpdateBounds();
		}

		public void Hide()
		{
			if (timer_ != null)
			{
				timer_.Destroy();
				timer_ = null;
			}

			widget_.Visible = false;
			active_ = null;
		}
	}
}
