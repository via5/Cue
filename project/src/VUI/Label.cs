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

		public const int Wrap = 0;
		public const int Overflow = 1;
		public const int Clip = 2;
		public const int ClipEllipsis = 3;

		private string text_;
		private int align_;
		private Text textObject_ = null;
		private Text ellipsis_ = null;
		private int wrap_ = Overflow;
		private bool autoTooltip_ = false;

		public Label(string t = "", int align = AlignLeft | AlignVCenter, FontStyle fs = FontStyle.Normal)
		{
			text_ = t;
			align_ = align;
			FontStyle = fs;
		}

		public Label(string t, FontStyle fs)
			: this(t)
		{
			FontStyle = fs;
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

					if (NeedsLayoutForTextChanged())
						NeedsLayout($"text changed");

					if (textObject_ != null)
					{
						textObject_.text = value;
						UpdateClip();
					}
				}
			}
		}

		private bool NeedsLayoutForTextChanged()
		{
			// optimization to avoid a relayout every time the text changes; a
			// relayout occurs when:
			//
			//  1) the label is visible,
			//  2) the bounds of the label are not fixed (like in a grid layout
			//     with uniform sizes),
			//  3) the text is too long
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
			if (wrap_ == Wrap)
				return HorizontalWrapMode.Wrap;
			else
				return HorizontalWrapMode.Overflow;
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

		private Rect MakeClipRect()
		{
			var root = GetRoot();
			if (root == null)
				return Rect.zero;

			var rb = root.RootSupport.Bounds;

			var ar = AbsoluteClientBounds;

			return new Rect(
				ar.Left - rb.Width / 2 - 2,
				rb.Height - ar.Top - ar.Height,
				ar.Width, ar.Height + 3);
		}

		private void UpdateClip()
		{
			if (textObject_ == null)
				return;

			switch (wrap_)
			{
				case Wrap:
				case Overflow:
				{
					textObject_.SetClipRect(Rect.zero, false);
					break;
				}

				case Clip:
				{
					textObject_.SetClipRect(MakeClipRect(), true);
					break;
				}

				case ClipEllipsis:
				{
					if (TextTooLong())
					{
						var ellipsisSize = Root.TextSize(Font, FontSize, "...");

						var cr = MakeClipRect();
						cr.width -= (ellipsisSize.Width + 5);

						textObject_.SetClipRect(cr, true);

						if (ellipsis_ == null)
							CreateEllipsis();

						var r = Rectangle.FromSize(
							RelativeBounds.Width - ellipsisSize.Width, 0,
							ellipsisSize.Width, ellipsisSize.Height);

						ellipsis_.gameObject.SetActive(true);
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

						textObject_.SetClipRect(Rect.zero, false);
					}

					break;
				}
			}
		}

		private void CreateEllipsis()
		{
			var go = new GameObject("ellipsis");
			go.AddComponent<RectTransform>();
			go.AddComponent<LayoutElement>();
			ellipsis_ = go.AddComponent<Text>();
			go.SetActive(true);
			go.transform.SetParent(MainObject.transform, false);
			ellipsis_.text = "...";
			ellipsis_.raycastTarget = false;

			Polish();
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			if (wrap_ == Wrap)
			{
				return Root.FitText(
					Font, FontSize, text_, new Size(maxWidth, maxHeight));
			}
			else
			{
				return Root.FitText(
					Font, FontSize, text_, new Size(DontCare, maxHeight));
			}
		}

		protected override Size DoGetMinimumSize()
		{
			return Root.TextSize(Font, FontSize, text_) + new Size(0, 5);
		}

		protected override void DoSetRender(bool b)
		{
			if (textObject_ != null)
				textObject_.gameObject.SetActive(b);

			if (ellipsis_ != null)
				ellipsis_.gameObject.SetActive(b);
		}

		private bool TextTooLong()
		{
			// todo: wrap mode
			var tl = Root.TextLength(Font, FontSize, text_);
			return (tl > Bounds.Width);
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
