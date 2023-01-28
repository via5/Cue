using UnityEngine;

namespace VUI
{
	class Button : Widget
	{
		public override string TypeName { get { return "Button"; } }

		public struct Polishing
		{
			public Color disabledTextColor;
			public Color backgroundColor, disabledBackgroundColor;
			public Color highlightBackgroundColor;

			public static Polishing Default
			{
				get
				{
					var p = new Polishing();

					p.disabledTextColor = Style.Theme.DisabledTextColor;
					p.backgroundColor = Style.Theme.ButtonBackgroundColor;
					p.disabledBackgroundColor = Style.Theme.DisabledButtonBackgroundColor;
					p.highlightBackgroundColor = Style.Theme.HighlightBackgroundColor;

					return p;
				}
			}
		}


		public event Callback Clicked;

		private string text_ = "";
		private UIDynamicButton button_ = null;
		private int align_ = Label.AlignCenter | Label.AlignVCenter;
		private Polishing polishing_ = Polishing.Default;

		public Button(string t = "", Callback clicked = null)
		{
			text_ = t;

			if (clicked != null)
				Clicked += clicked;

			Events.PointerDown += OnPointerDown;
			Events.PointerClick += OnPointerClick;
		}

		public string Text
		{
			get
			{
				return text_;
			}

			set
			{
				if (text_ != value)
				{
					text_ = value;

					if (button_ != null)
					{
						button_.buttonText.text = value;
						NeedsLayout("text changed");
					}
				}
			}
		}

		public int Alignment
		{
			get
			{
				return align_;
			}

			set
			{
				align_ = value;

				if (button_ != null)
					button_.buttonText.alignment = Label.ToTextAnchor(align_);
			}
		}

		public Color BackgroundColor
		{
			get
			{
				return polishing_.backgroundColor;
			}

			set
			{
				polishing_.backgroundColor = value;
				Polish();
			}
		}

		public Color HighlightBackgroundColor
		{
			get
			{
				return polishing_.highlightBackgroundColor;
			}

			set
			{
				polishing_.highlightBackgroundColor = value;
				Polish();
			}
		}

		public void Click()
		{
			if (button_ != null)
				button_.button.onClick?.Invoke();
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Glue.PluginManager.configurableButtonPrefab).gameObject;
		}

		protected override void DoCreate()
		{
			button_ = WidgetObject.GetComponent<UIDynamicButton>();
			button_.buttonText.text = text_;
			button_.buttonText.alignment = Label.ToTextAnchor(align_);

			Style.Setup(this, polishing_);
		}

		protected override void DoSetEnabled(bool b)
		{
			button_.button.interactable = b;
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			Style.Polish(this, polishing_);
		}

		public override void UpdateBounds()
		{
			base.UpdateBounds();

			// padding must go inside the button so it's applied to the text
			// instead of around the button itself

			// remove padding around the button itself
			var rt = button_.GetComponent<RectTransform>();

			rt.offsetMin = new Vector2(
				rt.offsetMin.x - 2 - Padding.Left,
				rt.offsetMin.y - 1 - Padding.Bottom);

			rt.offsetMax = new Vector2(
				rt.offsetMax.x + 2 - Padding.Right,
				rt.offsetMax.y - Padding.Top);

			// add padding around the text instead
			rt = button_.buttonText.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(Padding.Left, Padding.Bottom);
			rt.offsetMax = new Vector2(Padding.Right, Padding.Top);
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			var s = Root.FitText(
				Font, FontSize, text_, new Size(maxWidth, maxHeight));

			s += Style.Metrics.ButtonPadding;

			return Size.Max(s, Style.Metrics.ButtonMinimumSize);
		}

		protected override Size DoGetMinimumSize()
		{
			var s = Root.TextSize(Font, FontSize, text_);
			return Size.Max(s, Style.Metrics.ButtonMinimumSize);
		}

		protected override void DoSetRender(bool b)
		{
			if (button_ != null)
				button_.gameObject.SetActive(b);
		}

		private void OnPointerDown(PointerEvent e)
		{
			e.Bubble = false;
		}

		private void OnPointerClick(PointerEvent e)
		{
			if (e.Button == PointerEvent.LeftButton)
			{
				Clicked?.Invoke();
				button_.button.OnDeselect(new UnityEngine.EventSystems.BaseEventData(null));
			}

			e.Bubble = false;
		}

		public override string ToString()
		{
			return $"{base.ToString()} '{text_}'";
		}
	}


	class ToolButton : VUI.Button
	{
		public override string TypeName { get { return "ToolButton"; } }

		public ToolButton(string text = "", Callback clicked = null, string tooltip = "")
			: base(text, clicked)
		{
			if (tooltip != "")
				Tooltip.Text = tooltip;
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(Root.TextLength(Font, FontSize, Text) + 20, 40);
		}

		protected override Size DoGetMinimumSize()
		{
			return new Size(50, DontCare);
		}
	}


	class CustomButton : Button
	{
		public override string TypeName { get { return "CustomButton"; } }

		public CustomButton(string text = "", Callback clicked = null)
			: base(text, clicked)
		{
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(0, 0);
		}

		protected override Size DoGetMinimumSize()
		{
			return new Size(DontCare, DontCare);
		}
	}
}
