using System;
using System.Collections.Generic;

namespace VUI
{
	abstract class FlowLayout : Layout
	{
		public const int AlignDefault = Align.Left | Align.Top;

		private int align_;
		private bool expand_;

		public FlowLayout(int spacing, int align, bool expand)
		{
			Spacing = spacing;
			expand_ = expand;
			align_ = MakeAlign(align);
		}

		public bool Expand
		{
			get { return expand_; }
			set { expand_ = value; }
		}

		public int Alignment
		{
			get { return align_; }
			set { align_ = MakeAlign(value); }
		}

		protected int MakeAlign(int a)
		{
			if ((a & (Align.Top | Align.VCenter | Align.Bottom)) == 0)
				a |= Align.Top;

			if ((a & (Align.Left | Align.Center | Align.Right)) == 0)
				a |= Align.Left;

			return a;
		}
	}


	class HorizontalFlow : FlowLayout
	{
		public override string TypeName { get { return "horflow"; } }

		public HorizontalFlow(int spacing = 0, int align = AlignDefault)
			: base(spacing, align, false)
		{
		}

		public HorizontalFlow(int spacing, int align, bool expand)
			: base(spacing, align, expand)
		{
		}

		protected override void LayoutImpl()
		{
			var av = Parent.AbsoluteClientBounds;
			var r = av;

			var bounds = new List<Rectangle?>();
			float totalWidth = 0;

			foreach (var w in Children)
			{
				if (!w.Visible)
				{
					bounds.Add(null);
					continue;
				}

				if (totalWidth > 0)
					totalWidth += Spacing;

				var wr = new Rectangle(
					r.TopLeft, w.GetRealPreferredSize(r.Width, r.Height));

				if (Expand)
				{
					wr.Height = r.Height;
				}
				else if (wr.Height < r.Height)
				{
					if (Bits.IsSet(Alignment, Align.VCenter))
					{
						wr.MoveTo(wr.Left, r.Top + (r.Height / 2) - (wr.Height / 2));
					}
					else if (Bits.IsSet(Alignment, Align.Bottom))
					{
						wr.MoveTo(wr.Left, r.Bottom - wr.Height);
					}
					else // AlignTop
					{
						// no-op
					}
				}

				bounds.Add(wr);
				totalWidth += wr.Width;
				r.Left += wr.Width + Spacing;
			}

			if (totalWidth > av.Width)
			{
				var excess = totalWidth - av.Width;
				bool zeroOnly = true;

				for (int j = 0; j < 2; ++j)
				{
					float offset = 0;

					for (int i = 0; i < bounds.Count; ++i)
					{
						if (bounds[i] == null)
							continue;

						var b = bounds[i].Value;

						b.Translate(-offset, 0);

						if (excess > 0.1f)
						{
							var ms = Children[i].GetRealMinimumSize();

							if (!zeroOnly || ms.Width == 0)
							{
								if (b.Width > ms.Width)
								{
									var d = Math.Min(b.Width - ms.Width, excess);

									b.Width -= d;
									excess -= d;
									offset += d;
									totalWidth -= d;
								}
							}
						}

						bounds[i] = b;
					}

					if (excess <= 0.1f)
						break;

					zeroOnly = false;
				}
			}

			if (Bits.IsSet(Alignment, Align.Center))
			{
				float offset = (av.Width / 2) - (totalWidth / 2);
				for (int i = 0; i < bounds.Count; ++i)
				{
					if (bounds[i] != null)
						bounds[i] = bounds[i].Value.TranslateCopy(offset, 0);
				}
			}
			else if (Bits.IsSet(Alignment, Align.Right))
			{
				float offset = av.Width - totalWidth;
				for (int i = 0; i < bounds.Count; ++i)
				{
					if (bounds[i] != null)
						bounds[i] = bounds[i].Value.TranslateCopy(offset, 0);
				}
			}
			else // left
			{
				// no-op
			}

			for (int i = 0; i < Children.Count; ++i)
			{
				if (bounds[i] != null)
					Children[i].SetBounds(bounds[i].Value);
			}
		}

		protected override Size DoGetPreferredSize(float maxWidth, float maxHeight)
		{
			return GetSize(maxWidth, maxHeight, true);
		}

		protected override Size DoGetMinimumSize()
		{
			return GetSize(-1, -1, false);
		}

		private Size GetSize(float maxWidth, float maxHeight, bool preferred)
		{
			float totalWidth = 0;
			float tallest = 0;

			for (int i = 0; i < Children.Count; ++i)
			{
				var w = Children[i];
				if (!w.Visible)
					continue;

				if (i > 0)
					totalWidth += Spacing;

				Size ps;

				if (preferred)
					ps = w.GetRealPreferredSize(maxWidth, maxHeight);
				else
					ps = w.GetRealMinimumSize();

				totalWidth += ps.Width;
				tallest = Math.Max(tallest, ps.Height);
			}

			return new Size(totalWidth, tallest);
		}

	}


	class VerticalFlow : FlowLayout
	{
		public override string TypeName { get { return "verflow"; } }

		public VerticalFlow(int spacing = 0, bool expand = true, int align = AlignDefault)
			: base(spacing, align, expand)
		{
		}

		protected override void LayoutImpl()
		{
			var av = Parent.AbsoluteClientBounds;
			var r = av;

			var bounds = new List<Rectangle?>();
			float totalHeight = 0;

			foreach (var w in Children)
			{
				if (!w.Visible)
				{
					bounds.Add(null);
					continue;
				}

				if (totalHeight > 0)
					totalHeight += Spacing;

				var wr = new Rectangle(
					r.TopLeft, w.GetRealPreferredSize(r.Width, r.Height));

				if (Expand)
				{
					wr.Width = r.Width;
				}
				else if (wr.Width < r.Width)
				{
					if (Bits.IsSet(Alignment, Align.Center))
					{
						wr.MoveTo(r.Left + (r.Width / 2) - (wr.Width / 2), wr.Top);
					}
					else if (Bits.IsSet(Alignment, Align.Right))
					{
						wr.MoveTo(r.Right - wr.Width, wr.Top);
					}
					else // AlignLeft
					{
						// no-op
					}
				}

				bounds.Add(wr);
				totalHeight += wr.Height;
				r.Top += wr.Height + Spacing;
			}

			if (totalHeight > av.Height)
			{
				var excess = totalHeight - av.Height;
				bool zeroOnly = true;

				for (int j = 0; j < 2; ++j)
				{
					float offset = 0;

					for (int i = 0; i < bounds.Count; ++i)
					{
						if (bounds[i] == null)
							continue;

						var b = bounds[i].Value;

						b.Translate(0, -offset);

						if (excess > 0.1f)
						{
							var ms = Children[i].GetRealMinimumSize();

							if (!zeroOnly || ms.Height == 0)
							{
								if (b.Height > ms.Height)
								{
									var d = Math.Min(b.Height - ms.Height, excess);

									b.Height -= d;
									excess -= d;
									offset += d;
									totalHeight -= d;
								}
							}
						}

						bounds[i] = b;
					}

					if (excess <= 0.1f)
						break;

					zeroOnly = false;
				}
			}

			if (Bits.IsSet(Alignment, Align.VCenter))
			{
				float offset = (av.Height / 2) - (totalHeight / 2);
				for (int i = 0; i < bounds.Count; ++i)
				{
					if (bounds[i] != null)
						bounds[i] = bounds[i].Value.TranslateCopy(0, offset);
				}
			}
			else if (Bits.IsSet(Alignment, Align.Bottom))
			{
				float offset = av.Height - totalHeight;
				for (int i = 0; i < bounds.Count; ++i)
				{
					if (bounds[i] != null)
						bounds[i] = bounds[i].Value.TranslateCopy(0, offset);
				}
			}
			else // top
			{
				// no-op
			}

			for (int i = 0; i < Children.Count; ++i)
			{
				if (bounds[i] != null)
					Children[i].SetBounds(bounds[i].Value);
			}
		}

		protected override Size DoGetPreferredSize(float maxWidth, float maxHeight)
		{
			return GetSize(maxWidth, maxHeight, true);
		}

		protected override Size DoGetMinimumSize()
		{
			return GetSize(-1, -1, false);
		}

		private Size GetSize(float maxWidth, float maxHeight, bool preferred)
		{
			float totalHeight = 0;
			float widest = 0;

			for (int i = 0; i < Children.Count; ++i)
			{
				var w = Children[i];
				if (!w.Visible)
					continue;

				if (i > 0)
					totalHeight += Spacing;

				Size ps;

				if (preferred)
					ps = w.GetRealPreferredSize(maxWidth, maxHeight);
				else
					ps = w.GetRealMinimumSize();

				totalHeight += ps.Height;
				widest = Math.Max(widest, ps.Width);
			}

			return new Size(widest, totalHeight);
		}
	}
}
