using System;
using UnityEngine.UI;

namespace VUI
{
	public class Tooltip
	{
		private string text_ = "";
		private int fontSize_ = -1;
		private Func<string> textFunc_ = null;

		public Tooltip(string text = "")
		{
			text_ = text;
		}

		public bool HasValue
		{
			get { return textFunc_ != null || !string.IsNullOrEmpty(text_); }
		}

		public Func<string> TextFunc
		{
			get { return textFunc_; }
			set { textFunc_ = value; }
		}

		public string Text
		{
			get { return text_; }
			set { text_ = value; }
		}

		public int FontSize
		{
			get { return fontSize_; }
			set { fontSize_ = value; }
		}

		public string GetText()
		{
			if (textFunc_ != null)
				return textFunc_();
			else
				return text_;
		}
	}


	public class TooltipWidget : Panel
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
			label_.WrapMode = VUI.Label.Wrap;
			label_.Alignment = VUI.Label.AlignLeft | VUI.Label.AlignTop;

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

		public Size Set(string text, int fontSize)
		{
			label_.Text = text;
			label_.FontSize = fontSize;
			label_.Polish();

			return label_.FitText(
				label_.Text, new Size(
					Style.Metrics.MaxTooltipWidth, Widget.DontCare));
		}
	}


	public class TooltipManager
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

			if (w.Tooltip.HasValue)
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

			var text = w.Tooltip.GetText();
			if (text == "")
			{
				// some tooltips have a TextFunc, but it returns an empty
				// string
				return;
			}

			active_ = w;

			// size of text
			var size = widget_.Set(text, w.Tooltip.FontSize);

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

				if (p.X < 0)
					p.X = 0;
			}

			if (p.Y + size.Height >= av.Height)
			{
				// tooltip would extend past the bottom edge; make sure it's
				// above the mouse cursor
				p.Y = mp.Y - size.Height;

				if (p.Y < 0)
					p.Y = 0;
			}

			widget_.SetBounds(new Rectangle(p.X, p.Y, size));
			widget_.Visible = true;
			widget_.BringToTop();
			widget_.DoLayout();
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
