using UnityEngine;
using UnityEngine.UI;

namespace VUI
{
	class Label : Panel
	{
		public override string TypeName { get { return "Label"; } }

		public const int AlignDefault = Align.VCenterLeft;

		public const int Wrap = 0;
		public const int Overflow = 1;
		public const int Clip = 2;
		public const int ClipEllipsis = 3;

		private string text_;
		private int align_;
		private Text textObject_ = null;
		private Rectangle lastClip_ = Rectangle.Zero;
		private int wrap_ = Overflow;
		private bool autoTooltip_ = false;
		private bool hasEllipsis_ = false;

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
						NeedsLayout($"text changed from '{oldText}' to {value}'");

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
			if (wrap_ == ClipEllipsis && hasEllipsis_)
				return false;

			// check if text is longer than current bounds
			return TextTooLong();
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
			textObject_.resizeTextForBestFit = false;

			// needed for tooltips
			textObject_.raycastTarget = true;

			Style.Setup(this);
		}

		private HorizontalWrapMode GetHorizontalOverflow()
		{
			if (wrap_ == Wrap || wrap_ == ClipEllipsis)
				return HorizontalWrapMode.Wrap;
			else
				return HorizontalWrapMode.Overflow;
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			Style.Polish(this);
		}

		protected override void AfterUpdateBounds()
		{
			base.AfterUpdateBounds();
			textObject_.alignment = ToTextAnchor(align_);
			UpdateClip();
		}

		private Rectangle MakeClipRect()
		{
			var root = GetRoot();
			if (root == null)
				return Rectangle.Zero;

			var rb = root.RootSupport.Bounds;
			var ar = AbsoluteClientBounds;

			return Rectangle.FromSize(
				ar.Left - rb.Width / 2 - 2,
				rb.Bottom - ar.Top - ar.Height + root.RootSupport.TopOffset,
				ar.Width,
				ar.Height);
		}

		private void UpdateClip()
		{
			hasEllipsis_ = false;

			if (textObject_ == null)
				return;

			if (!IsVisibleOnScreen())
			{
				ClearClip();
				return;
			}

			switch (wrap_)
			{
				case Wrap:
				case Overflow:
				{
					ClearClip();
					break;
				}

				case Clip:
				{
					SetClip(MakeClipRect());
					break;
				}

				case ClipEllipsis:
				{
					if (TextTooLong())
					{
						var ss = Root.ClipTextEllipsis(
							Font, FontSize, FontStyle, ToTextAnchor(align_),
							text_, ClientBounds.Size, false);

						hasEllipsis_ = (ss != text_);
						textObject_.text = ss;

						if (autoTooltip_)
							Tooltip.Text = text_;
					}
					else
					{
						hasEllipsis_ = false;
						textObject_.text = text_;

						ClearClip();

						if (autoTooltip_)
							Tooltip.Text = "";
					}

					break;
				}
			}
		}

		private void ClearClip()
		{
			lastClip_ = Rectangle.Zero;
			textObject_.SetClipRect(Rect.zero, false);
		}

		private void SetClip(Rectangle r)
		{
			if ((int)r.Left != (int)lastClip_.Left ||
				(int)r.Top != (int)lastClip_.Top ||
				(int)r.Right != (int)lastClip_.Right ||
				(int)r.Bottom != (int)lastClip_.Bottom)
			{
				lastClip_ = r;
				textObject_.SetClipRect(r.ToRect(), true);
			}
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
		}

		protected override void UpdateActiveState()
		{
			base.UpdateActiveState();
			UpdateClip();
		}

		private bool TextTooLong()
		{
			var tl = TextSize(text_, ClientBounds.Size, true);
			return (tl.Width > ClientBounds.Width) || (tl.Height > ClientBounds.Height);
		}

		public static TextAnchor ToTextAnchor(int a)
		{
			if (a == (Align.Left | Align.Top))
				return TextAnchor.UpperLeft;
			else if (a == (Align.Left | Align.VCenter))
				return TextAnchor.MiddleLeft;
			else if (a == (Align.Left | Align.Bottom))
				return TextAnchor.LowerLeft;
			else if (a == (Align.Center | Align.Top))
				return TextAnchor.UpperCenter;
			else if (a == (Align.Center | Align.VCenter))
				return TextAnchor.MiddleCenter;
			else if (a == (Align.Center | Align.Bottom))
				return TextAnchor.LowerCenter;
			else if (a == (Align.Right | Align.Top))
				return TextAnchor.UpperRight;
			else if (a == (Align.Right | Align.VCenter))
				return TextAnchor.MiddleRight;
			else if (a == (Align.Right | Align.Bottom))
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

		public override string ToString()
		{
			return $"{base.ToString()} '{text_}'";
		}
	}
}
