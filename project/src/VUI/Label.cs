using UnityEngine;
using UnityEngine.UI;

namespace VUI
{
	class Label : Panel
	{
		public override string TypeName { get { return "Label"; } }

		public const int AlignLeft = 0x01;
		public const int AlignCenter = 0x02;
		public const int AlignRight = 0x04;
		public const int AlignTop = 0x08;
		public const int AlignVCenter = 0x10;
		public const int AlignBottom = 0x20;
		public const int AlignDefault = AlignLeft | AlignVCenter;

		public const int Wrap = 0;
		public const int Overflow = 1;
		public const int Clip = 2;
		public const int ClipEllipsis = 3;

		private string text_;
		private int align_;
		private Text textObject_ = null;
		private Transform ellipsis_ = null;
		private Text ellipsisText_ = null;
		private UnityEngine.UI.Image ellipsisBackground_ = null;
		private int wrap_ = Overflow;
		private bool autoTooltip_ = false;

		public Label(
			string t = "",
			int align = AlignDefault,
			FontStyle fs = FontStyle.Normal,
			int wrapMode = Overflow)
		{
			text_ = t;
			align_ = align;
			FontStyle = fs;
			wrap_ = wrapMode;
			WantsFocus = false;
		}

		public Label(string t, FontStyle fs, int wrapMode=Overflow)
			: this(t, AlignDefault, fs, wrapMode)
		{
		}

		public Label(string t, int wrapMode)
			: this(t, AlignDefault, FontStyle.Normal, wrapMode)
		{
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
					string oldText = text_;
					text_ = value;

					if (NeedsLayoutForTextChanged(oldText, text_))
						NeedsLayout($"text changed");

					if (textObject_ != null)
					{
						textObject_.text = value;
						UpdateClip();
					}
				}
			}
		}

		private bool NeedsLayoutForTextChanged(string oldText, string newText)
		{
			// optimization to avoid a relayout every time the text changes; a
			// relayout occurs when:
			//
			//  1) the label is visible,
			//  2) the bounds of the label are not fixed (like in a grid layout
			//     with uniform sizes),
			//  3) the text is too long
			//  4) the text length is different or the font isn't monospace
			//
			// for 3), another optimization is for the ellipsis clip: if the
			// ellipsis is already present, the text is already too long, so
			// don't bother checking it and assume the label can't grow anymore

			// not visible
			if (!IsVisibleOnScreen())
				return false;

			// can't grow anymore
			if (FixedBounds())
				return false;

			if (Font == VUI.Style.Theme.MonospaceFont)
			{
				if (oldText.Length == newText.Length)
					return false;
			}

			// text already too long
			if (wrap_ == ClipEllipsis && EllipsisVisible())
				return false;

			// check if text is longer than current bounds
			return TextTooLong();
		}

		private bool EllipsisVisible()
		{
			return (ellipsis_?.gameObject?.activeInHierarchy ?? false);
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
				NeedsLayout("alignment changed");
			}
		}

		public int WrapMode
		{
			get
			{
				return wrap_;
			}

			set
			{
				wrap_ = value;
				NeedsLayout("wrap changed");
			}
		}

		public bool AutoTooltip
		{
			get
			{
				return autoTooltip_;
			}

			set
			{
				autoTooltip_ = value;
				UpdateClip();
			}
		}

		protected override void DoCreate()
		{
			base.DoCreate();

			textObject_ = WidgetObject.AddComponent<Text>();
			textObject_.text = text_;
			textObject_.horizontalOverflow = GetHorizontalOverflow();
			textObject_.maskable = true;

			// needed for tooltips
			textObject_.raycastTarget = true;

			Style.Setup(this);
		}

		private HorizontalWrapMode GetHorizontalOverflow()
		{
			if (wrap_ == Overflow)
				return HorizontalWrapMode.Overflow;
			else
				return HorizontalWrapMode.Wrap;
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			Style.Polish(this);
		}

		public override void UpdateBounds()
		{
			base.UpdateBounds();
			textObject_.alignment = ToTextAnchor(align_);
			UpdateClip();
		}

		private void UpdateClip()
		{
			if (textObject_ == null)
				return;

			if (!IsVisibleOnScreen())
				return;

			switch (wrap_)
			{
				case Wrap:
				case Overflow:
				{
					break;
				}

				case Clip:
				{
					break;
				}

				case ClipEllipsis:
				{
					if (TextTooLong())
					{
						var ellipsisSize = TextSize("...");
						ellipsisSize.Width += 5;

						if (ellipsis_ == null)
							CreateEllipsis();

						var r = Rectangle.FromSize(
							ClientBounds.Left + ClientBounds.Width - ellipsisSize.Width,
							ClientBounds.Top + ClientBounds.Height - ellipsisSize.Height - 5,
							ellipsisSize.Width,
							ellipsisSize.Height);

						ellipsis_.gameObject.SetActive(true);

						if (BackgroundColor.a == 0)
							ellipsisBackground_.color = Style.Theme.BackgroundColor;
						else
							ellipsisBackground_.color = BackgroundColor;

						Utilities.SetRectTransform(ellipsis_, r);

						if (autoTooltip_)
							Tooltip.Text = text_;
					}
					else
					{
						if (autoTooltip_)
							Tooltip.Text = "";

						if (ellipsis_ != null)
							ellipsis_.gameObject.SetActive(false);
					}

					break;
				}
			}
		}

		private void CreateEllipsis()
		{
			var go = new GameObject("ellipsisParent");
			go.AddComponent<RectTransform>();
			go.AddComponent<LayoutElement>();

			{
				var b = new GameObject("background");
				ellipsisBackground_ = b.AddComponent<UnityEngine.UI.Image>();

				var rt = b.GetComponent<RectTransform>();
				if (rt == null)
					rt = b.AddComponent<RectTransform>();

				rt.offsetMin = new Vector2(0, 0);
				rt.offsetMax = new Vector2(0, 0);
				rt.anchorMin = new Vector2(0, 0);
				rt.anchorMax = new Vector2(1, 1);

				b.transform.SetParent(go.transform, false);
			}

			{
				var e = new GameObject("ellipsis");

				var rt = e.GetComponent<RectTransform>();
				if (rt == null)
					rt = e.AddComponent<RectTransform>();

				rt.offsetMin = new Vector2(0, 0);
				rt.offsetMax = new Vector2(0, 0);
				rt.anchorMin = new Vector2(0, 0);
				rt.anchorMax = new Vector2(1, 1);

				ellipsisText_ = e.AddComponent<Text>();
				ellipsisText_.text = "...";
				ellipsisText_.raycastTarget = false;
				ellipsisText_.alignment = TextAnchor.MiddleCenter;

				e.transform.SetParent(go.transform, false);
			}

			go.SetActive(true);
			go.transform.SetParent(MainObject.transform, false);

			ellipsis_ = go.transform;

			Polish();
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			if (wrap_ == Wrap)
				return FitText(text_, new Size(maxWidth, maxHeight));
			else
				return FitText(text_, new Size(DontCare, maxHeight));
		}

		protected override Size DoGetMinimumSize()
		{
			return TextSize(text_) + new Size(0, 5);
		}

		protected override void DoSetRender(bool b)
		{
			base.DoSetRender(b);

			UpdateClip();

			if (textObject_ != null)
				textObject_.gameObject.SetActive(b);

			if (ellipsis_ != null)
				ellipsis_.gameObject.SetActive(b);
		}

		protected override void UpdateActiveState()
		{
			base.UpdateActiveState();
			UpdateClip();
		}

		private bool TextTooLong()
		{
			var tl = Root.FitText(Font, FontSize, FontStyle, text_, ClientBounds.Size, true);
			return (tl.Width > ClientBounds.Width) || (tl.Height > ClientBounds.Height);
		}

		public static TextAnchor ToTextAnchor(int a)
		{
			if (a == (AlignLeft | AlignTop))
				return TextAnchor.UpperLeft;
			else if (a == (AlignLeft | AlignVCenter))
				return TextAnchor.MiddleLeft;
			else if (a == (AlignLeft | AlignBottom))
				return TextAnchor.LowerLeft;
			else if (a == (AlignCenter | AlignTop))
				return TextAnchor.UpperCenter;
			else if (a == (AlignCenter | AlignVCenter))
				return TextAnchor.MiddleCenter;
			else if (a == (AlignCenter | AlignBottom))
				return TextAnchor.LowerCenter;
			else if (a == (AlignRight | AlignTop))
				return TextAnchor.UpperRight;
			else if (a == (AlignRight | AlignVCenter))
				return TextAnchor.MiddleRight;
			else if (a == (AlignRight | AlignBottom))
				return TextAnchor.LowerRight;
			else
				return TextAnchor.MiddleLeft;
		}

		public override string DebugLine
		{
			get
			{
				return base.DebugLine + " '" + text_ + "'";
			}
		}
	}
}
