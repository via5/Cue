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
		private RectTransform buttonRT_ = null;
		private RectTransform buttonTextRT_ = null;
		private Icon icon_ = null;
		protected ImageObject image_ = null;
		private Texture tex_ = null;
		private Size iconSize_ = new Size(DontCare, DontCare);
		private int align_ = Label.AlignCenter | Label.AlignVCenter;
		private Polishing polishing_ = Polishing.Default;

		public Button(string t = "", Callback clicked = null, string tooltip = null)
		{
			text_ = t;

			if (clicked != null)
				Clicked += clicked;

			if (tooltip != null)
				Tooltip.Text = tooltip;

			Events.PointerDown += OnPointerDown;
			Events.PointerUp += OnPointerUp;
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
						SetTextOrIcon();
						NeedsLayout("text changed");
					}
				}
			}
		}

		public Icon Icon
		{
			get
			{
				return icon_;
			}

			set
			{
				if (icon_ != value)
				{
					icon_ = value;
					IconChanged();
				}
			}
		}

		public Size IconSize
		{
			get { return iconSize_; }
			set { iconSize_ = value; UpdateImage(); }
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

		public Color DisabledBackgroundColor
		{
			get
			{
				return polishing_.disabledBackgroundColor;
			}

			set
			{
				polishing_.disabledBackgroundColor = value;
				Polish();
			}
		}

		public void SetBorderless()
		{
			BackgroundColor = new Color(0, 0, 0, 0);
			DisabledBackgroundColor = new Color(0, 0, 0, 0);
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
			button_.button.onClick.AddListener(OnClicked);
			button_.buttonText.alignment = Label.ToTextAnchor(align_);

			buttonRT_ = button_.GetComponent<RectTransform>();
			buttonTextRT_ = button_.buttonText.GetComponent<RectTransform>();

			Style.Setup(this, polishing_);

			SetTextOrIcon();
		}

		protected override void DoSetEnabled(bool b)
		{
			button_.button.interactable = b;

			if (image_ != null)
				image_.SetEnabled(b);
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			Style.Polish(this, polishing_);
		}

		protected override void AfterUpdateBounds()
		{
			// padding must go inside the button so it's applied to the text
			// instead of around the button itself

			// remove padding around the button itself
			buttonRT_.offsetMin = new Vector2(
				buttonRT_.offsetMin.x - 2 - Padding.Left,
				buttonRT_.offsetMin.y - 1 - Padding.Bottom);

			buttonRT_.offsetMax = new Vector2(
				buttonRT_.offsetMax.x + 2 + Padding.Right,
				buttonRT_.offsetMax.y + 2 +  Padding.Top);

			// add padding around the text instead
			buttonTextRT_.offsetMin = new Vector2(Padding.Left, Padding.Bottom);
			buttonTextRT_.offsetMax = new Vector2(-Padding.Right, -Padding.Top);

			if (icon_ != null)
			{
				if (image_ == null)
					image_ = new ImageObject(this);

				image_.Texture = tex_;
				image_.SetEnabled(Enabled);
				UpdateImage();
			}
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			var s = FitText(text_, new Size(maxWidth, maxHeight));

			s += Style.Metrics.ButtonPadding;

			return Size.Max(s, Style.Metrics.ButtonMinimumSize);
		}

		protected override Size DoGetMinimumSize()
		{
			var s = TextSize(text_);
			return Size.Max(s, Style.Metrics.ButtonMinimumSize);
		}

		protected override void DoSetRender(bool b)
		{
			if (button_ != null)
				button_.gameObject.SetActive(b);

			if (image_ != null)
				image_.SetRender(b);
		}

		private void IconChanged()
		{
			if (icon_ != null)
				icon_.GetTexture(SetTexture);
		}

		private void SetTexture(Texture t)
		{
			tex_ = t;

			if (image_ != null)
			{
				UpdateImage();
				image_.Texture = t;
			}

			SetTextOrIcon();
			NeedsLayout("texture changed");
		}

		private void UpdateImage()
		{
			if (image_ != null)
				image_.Size = iconSize_;
		}

		private void SetTextOrIcon()
		{
			if (button_?.button == null)
				return;

			if (tex_ == null)
				button_.buttonText.text = text_;
			else
				button_.buttonText.text = "";
		}

		private void OnPointerDown(PointerEvent e)
		{
			e.Bubble = false;
		}

		private void OnPointerUp(PointerEvent e)
		{
			e.Bubble = false;
		}

		private void OnPointerClick(PointerEvent e)
		{
			e.Bubble = false;
		}

		private void OnClicked()
		{
			Clicked?.Invoke();
			button_.button.OnDeselect(new UnityEngine.EventSystems.BaseEventData(null));
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
			if (image_ == null)
			{
				return new Size(TextLength(Text) + 20, 40);
			}
			else
			{
				var s = Size.Zero;

				if (IconSize.Width == DontCare)
				{
					if (image_.Texture == null)
						s.Width = 20;
					else
						s.Width = image_.Texture.width;
				}
				else
				{
					s.Width = IconSize.Width;
				}

				if (IconSize.Height == DontCare)
				{
					if (image_.Texture == null)
						s.Height = 20;
					else
						s.Height = image_.Texture.height;
				}
				else
				{
					s.Height = IconSize.Height;
				}

				return s;
			}
		}

		protected override Size DoGetMinimumSize()
		{
			if (image_ == null || (IconSize.Width == DontCare && IconSize.Height == DontCare))
				return new Size(40, DontCare);
			else
				return new Size(IconSize.Width, IconSize.Height);
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
