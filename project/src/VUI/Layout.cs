using System;
using System.Collections.Generic;

namespace VUI
{
	interface LayoutData
	{
	}

	abstract class Layout
	{
		public abstract string TypeName { get; }

		public const float DontCare = -1;

		private Widget parent_ = null;
		private readonly List<Widget> children_ = new List<Widget>();
		private float spacing_ = 0;

		public Widget Parent
		{
			get { return parent_; }
			set { parent_ = value; }
		}

		public List<Widget> Children
		{
			get { return children_; }
		}

		public virtual float Spacing
		{
			get { return spacing_; }
			set { spacing_ = value; }
		}

		public void Add(Widget w, LayoutData data = null)
		{
			if (Contains(w))
			{
				Glue.LogError("layout already has widget " + w.Name);
				return;
			}

			children_.Add(w);
			AddImpl(w, data);
		}

		public void Remove(Widget w)
		{
			if (!children_.Remove(w))
			{
				Glue.LogError(
					"can't remove '" + w.Name + "' from layout, not found");

				return;
			}

			RemoveImpl(w);
		}

		public Size GetPreferredSize(float maxWidth, float maxHeight)
		{
			return DoGetPreferredSize(maxWidth, maxHeight);
		}

		public void DoLayout()
		{
			LayoutImpl();
		}

		public bool Contains(Widget w)
		{
			return children_.Contains(w);
		}

		protected virtual void AddImpl(Widget w, LayoutData data)
		{
			// no-op
		}

		protected virtual void RemoveImpl(Widget w)
		{
			// no-op
		}

		protected virtual Size DoGetPreferredSize(float maxWidth, float maxHeight)
		{
			return new Size(Widget.DontCare, Widget.DontCare);
		}

		protected abstract void LayoutImpl();
	}


	class AbsoluteLayout : Layout
	{
		public override string TypeName { get { return "abs"; } }

		protected override void LayoutImpl()
		{
			// no-op
		}
	}


	abstract class FlowLayout : Layout
	{
		public const int AlignTop = 0x01;
		public const int AlignVCenter = 0x02;
		public const int AlignBottom = 0x04;

		public const int AlignLeft = 0x08;
		public const int AlignCenter = 0x10;
		public const int AlignRight = 0x20;

		private int align_;
		private bool expand_;

		public FlowLayout(int spacing, int align, bool expand)
		{
			Spacing = spacing;
			expand_ = expand;
			align_ = align;
		}

		public bool Expand
		{
			get { return expand_; }
			set { expand_ = value; }
		}

		public int Alignment
		{
			get { return align_; }
			set { align_ = value; }
		}
	}


	class HorizontalFlow : FlowLayout
	{
		public override string TypeName { get { return "horflow"; } }

		public HorizontalFlow(int spacing = 0, int align = AlignLeft|AlignVCenter)
			: base(spacing, align, false)
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
					if (Bits.IsSet(Alignment, AlignVCenter))
					{
						wr.MoveTo(wr.Left, r.Top + (r.Height / 2) - (wr.Height / 2));
					}
					else if (Bits.IsSet(Alignment, AlignBottom))
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

			if (Bits.IsSet(Alignment, AlignCenter))
			{
				float offset = (av.Width / 2) - (totalWidth / 2);
				for (int i = 0; i < bounds.Count; ++i)
				{
					if (bounds[i] != null)
						bounds[i] = bounds[i].Value.TranslateCopy(offset, 0);
				}
			}
			else if (Bits.IsSet(Alignment, AlignRight))
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
			float totalWidth = 0;
			float tallest = 0;

			for (int i=0; i<Children.Count; ++i)
			{
				var w = Children[i];
				if (!w.Visible)
					continue;

				if (i > 0)
					totalWidth += Spacing;

				var ps = w.GetRealPreferredSize(maxWidth, maxHeight);

				totalWidth += ps.Width;
				tallest = Math.Max(tallest, ps.Height);
			}

			return new Size(totalWidth, tallest);
		}
	}


	class VerticalFlow : FlowLayout
	{
		public override string TypeName { get { return "verflow"; } }

		public VerticalFlow(int spacing = 0, bool expand = true, int align = AlignLeft | AlignTop)
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
					if (Bits.IsSet(Alignment, AlignCenter))
					{
						wr.MoveTo(r.Left + (r.Width / 2) - (wr.Width / 2), wr.Top);
					}
					else if (Bits.IsSet(Alignment, AlignRight))
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

			if (Bits.IsSet(Alignment, AlignVCenter))
			{
				float offset = (av.Height / 2) - (totalHeight / 2);
				for (int i = 0; i < bounds.Count; ++i)
				{
					if (bounds[i] != null)
						bounds[i] = bounds[i].Value.TranslateCopy(0, offset);
				}
			}
			else if (Bits.IsSet(Alignment, AlignBottom))
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
			float totalHeight = 0;
			float widest = 0;

			for (int i = 0; i < Children.Count; ++i)
			{
				var w = Children[i];
				if (!w.Visible)
					continue;

				if (i > 0)
					totalHeight += Spacing;

				var ps = w.GetRealPreferredSize(maxWidth, maxHeight);

				totalHeight += ps.Height;
				widest = Math.Max(widest, ps.Width);
			}

			return new Size(widest, totalHeight);
		}
	}
}
