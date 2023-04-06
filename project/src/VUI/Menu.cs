using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VUI
{
	public interface IMenuItem
	{
		Widget Widget { get; }
		Menu Parent { get; set; }
	}

	public abstract class BasicMenuItem : IMenuItem
	{
		private Menu parent_ = null;
		public abstract Widget Widget { get; }

		public Menu Parent
		{
			get { return parent_; }
			set { parent_ = value; }
		}
	}

	public class ButtonMenuItem : BasicMenuItem
	{
		private readonly Button button_;

		public ButtonMenuItem(string text, string tooltip = null)
		{
			button_ = new Button(text);
			button_.BackgroundColor = new Color(0, 0, 0, 0);
			button_.Padding = new Insets(10, 5, 5, 5);
			button_.Alignment = Align.VCenterLeft;
			button_.Clicked += () => Parent?.ItemActivatedInternal(this);

			if (tooltip != null)
				button_.Tooltip.Text = tooltip;
		}

		public Button Button
		{
			get { return button_; }
		}

		public override Widget Widget
		{
			get { return button_; }
		}
	}


	public class CheckBoxMenuItem : BasicMenuItem
	{
		private readonly CheckBox cb_;

		public CheckBoxMenuItem(
			string text, CheckBox.ChangedCallback changed,
			bool initial = false, string tooltip = null)
		{
			cb_ = new CheckBox(text, changed, initial);
			cb_.Padding = new Insets(10, 5, 5, 5);
			cb_.Changed += (b) => Parent?.ItemActivatedInternal(this);

			if (tooltip != null)
				cb_.Tooltip.Text = tooltip;
		}

		public CheckBox CheckBox
		{
			get { return cb_; }
		}

		public override Widget Widget
		{
			get { return cb_; }
		}
	}


	public class RadioMenuItem : BasicMenuItem
	{
		private readonly RadioButton radio_;

		public RadioMenuItem(
			string text, RadioButton.ChangedCallback changed,
			bool initial = false, RadioButton.Group group = null,
			string tooltip = null)
		{
			radio_ = new RadioButton(text, changed, initial, group);
			radio_.Padding = new Insets(10, 5, 5, 5);
			radio_.Changed += (b) => Parent?.ItemActivatedInternal(this);

			if (tooltip != null)
				radio_.Tooltip.Text = tooltip;
		}

		public RadioButton RadioButton
		{
			get { return radio_; }
		}

		public override Widget Widget
		{
			get { return radio_; }
		}
	}


	public class Menu : Panel
	{
		public delegate void Handler(IMenuItem item);
		public event Handler Activated;

		private readonly List<IMenuItem> items_ = new List<IMenuItem>();

		public Menu()
		{
			Layout = new VerticalFlow(10);
			Padding = new Insets(5);
		}

		public ButtonMenuItem AddMenuItem(string text)
		{
			return AddMenuItem(new ButtonMenuItem(text));
		}

		public T AddMenuItem<T>(T item) where T : IMenuItem
		{
			item.Parent = this;
			items_.Add(item);
			Add(item.Widget);
			return item;
		}

		public void AddSeparator()
		{
			var p = Add(new Panel());
			p.Margins = new Insets(5, 0, 5, 0);
			p.Borders = new Insets(0, 1, 0, 0);
		}

		public void Clear()
		{
			foreach (var i in items_)
				i.Parent = null;

			DestroyAllChildren();
		}

		public virtual void ItemActivatedInternal(IMenuItem item)
		{
			Activated?.Invoke(item);
		}
	}


	public class ContextMenu : Menu, IPopup
	{
		public ContextMenu()
		{
			Borders = new Insets(1);
			BackgroundColor = Style.Theme.BackgroundColor;
			Clickthrough = false;
			MinimumSize = Style.Metrics.ContextMenuMinimumSize;
		}

		public void RunMenu(Root root, Point p)
		{
			root.FloatingPanel.Add(this);

			var s = GetRealPreferredSize(
				root.FloatingPanel.Bounds.Width,
				root.FloatingPanel.Bounds.Height);

			var r = Bounds;
			r.Left = p.X;
			r.Top = p.Y;
			r.Right = r.Left + s.Width;
			r.Bottom = r.Top + s.Height;

			SetBounds(r);
			DoLayout();
			BringToTop();

			GetRoot().SetOpenedPopup(this);
		}

		public void ClosePopupInternal()
		{
			GetRoot()?.FloatingPanel?.Remove(this);
		}

		public bool PopupContainsWidgetInternal(Widget w)
		{
			return w.HasParent(this);
		}

		public Widget PopupWidgetAtInternal(Point p)
		{
			return WidgetAtInternal(p);
		}

		public override void ItemActivatedInternal(IMenuItem item)
		{
			ClosePopupInternal();
			base.ItemActivatedInternal(item);
		}
	}


	class ToggledPanel
	{
		public delegate void ToggledHandler(bool b);
		public event ToggledHandler Toggled;

		private readonly Size Size = new Size(500, 800);

		public delegate void Handler();
		public event Handler RightClick;

		private readonly Button button_;
		private readonly Panel panel_;
		private bool firstShow_ = true;
		private bool autoSize_ = false;
		private bool disableOverlay_ = true;
		private Color oldBackground_ = new Color(0, 0, 0, 0);

		public ToggledPanel(string buttonText, bool toolButton = false, bool autoSize = false)
		{
			if (toolButton)
				button_ = new ToolButton(buttonText, Toggle);
			else
				button_ = new Button(buttonText, Toggle);

			autoSize_ = autoSize;

			button_.Events.PointerClick += ToggleClick;
			button_.Events.PointerDown += OnPointerDown;

			panel_ = new Panel();
			panel_.Layout = new BorderLayout();
			panel_.BackgroundColor = Style.Theme.BackgroundColor;
			panel_.BorderColor = Style.Theme.BorderColor;
			panel_.Borders = new Insets(1);
			panel_.Clickthrough = false;
			panel_.Visible = false;
			panel_.SetDropShadow(Style.Theme.DropShadowColor, Style.Metrics.DropShadowDistance);

			if (!autoSize_)
				panel_.MinimumSize = Size;
		}

		public bool DisableOverlay
		{
			get { return disableOverlay_; }
			set { disableOverlay_ = value; }
		}

		public Button Button
		{
			get { return button_; }
		}

		public Panel Panel
		{
			get { return panel_; }
		}

		public void Toggle()
		{
			if (panel_.Visible)
				Hide();
			else
				Show();
		}

		public void Show()
		{
			if (firstShow_)
			{
				firstShow_ = false;

				var root = button_.GetRoot();
				root.FloatingPanel.Add(panel_);
				Glue.PluginManager.StartCoroutine(CoDoShow());
			}
			else
			{
				DoShow();
			}
		}

		private IEnumerator CoDoShow()
		{
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			DoShow();
		}

		private void DoShow()
		{
			Toggled?.Invoke(!panel_.Visible);

			Size size;

			var root = button_.GetRoot();

			if (autoSize_)
			{
				var s = panel_.GetRealPreferredSize(
					root.FloatingPanel.Bounds.Width,
					root.FloatingPanel.Bounds.Height);

				var r = panel_.Bounds;
				r.Width = s.Width;
				r.Height = s.Height;

				panel_.SetBounds(r);
				size = r.Size;
			}
			else
			{
				size = Size;
			}

			var bounds = Rectangle.FromSize(
				root.FloatingPanel.Bounds.Left + button_.Bounds.Left,
				root.FloatingPanel.Bounds.Top + button_.Bounds.Bottom,
				size.Width, size.Height);

			if (bounds.Right >= root.FloatingPanel.Bounds.Right)
				bounds.Translate(-(bounds.Right - root.FloatingPanel.AbsoluteClientBounds.Right), 0);

			panel_.SetBounds(bounds);

			panel_.Visible = true;
			panel_.BringToTop();
			panel_.GetRoot().FocusChanged += OnFocusChanged;

			if (disableOverlay_)
			{
				panel_.GetRoot().FloatingPanel.BackgroundColor =
					Style.Theme.ActiveOverlayColor;

				panel_.GetRoot().FloatingPanel.Clickthrough = false;
			}

			panel_.DoLayout();

			oldBackground_ = button_.BackgroundColor;
			button_.BackgroundColor = Style.Theme.HighlightBackgroundColor;
		}

		public void Hide()
		{
			panel_.Visible = false;

			panel_.GetRoot().FocusChanged -= OnFocusChanged;

			if (disableOverlay_)
			{
				panel_.GetRoot().FloatingPanel.BackgroundColor =
					new UnityEngine.Color(0, 0, 0, 0);

				panel_.GetRoot().FloatingPanel.Clickthrough = true;
			}

			button_.BackgroundColor = oldBackground_;
			Toggled?.Invoke(panel_.Visible);
		}

		private void OnFocusChanged(Widget blurred, Widget focused)
		{
			if (!focused.HasParent(panel_) && focused != button_)
				Hide();
		}

		private void ToggleClick(PointerEvent e)
		{
			if (e.Button == PointerEvent.RightButton)
				RightClick?.Invoke();

			e.Bubble = false;
		}

		private void OnPointerDown(PointerEvent e)
		{
		}
	}


	class MenuButton : ToggledPanel
	{
		public new delegate void Handler();
		public event Handler AboutToOpen;

		private Menu menu_;
		private bool closeOnActivated_ = false;

		public MenuButton(string buttonText, Menu menu)
			: this(buttonText, false, true, menu)
		{
		}

		public MenuButton(string buttonText, bool toolButton = false, bool autoSize = true, Menu menu = null)
			: base(buttonText, toolButton, autoSize)
		{
			DisableOverlay = false;
			Panel.Layout = new BorderLayout();

			if (menu != null)
				Menu = menu;

			Toggled += (b) =>
			{
				if (b)
					AboutToOpen?.Invoke();
			};
		}

		public Menu Menu
		{
			get
			{
				return menu_;
			}

			set
			{
				if (menu_ != null)
					menu_.Activated -= OnItemActivated;

				menu_ = value;

				if (menu_ != null)
					menu_.Activated += OnItemActivated;

				Panel.RemoveAllChildren();
				Panel.Add(menu_, BorderLayout.Center);
			}
		}

		public bool CloseOnMenuActivated
		{
			get { return closeOnActivated_; }
			set { closeOnActivated_ = value; }
		}

		private void OnItemActivated(IMenuItem item)
		{
			if (closeOnActivated_)
				Hide();
		}
	}
}
