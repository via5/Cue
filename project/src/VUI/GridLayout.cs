using System;
using System.Collections.Generic;

namespace VUI
{
	class GridLayout : Layout
	{
		public override string TypeName { get { return "gl"; } }


		public class Data : LayoutData
		{
			public int col, row;

			public Data(int col, int row)
			{
				this.col = col;
				this.row = row;
			}
		}


		class RowData<T>
			where T : new()
		{
			private readonly List<T> cells_ = new List<T>();

			public int Count
			{
				get { return cells_.Count; }
			}

			public T Cell(int i)
			{
				return cells_[i];
			}

			public void Set(int i, T t)
			{
				cells_[i] = t;
			}

			public void Extend(int cols)
			{
				while (cells_.Count < cols)
					cells_.Add(new T());
			}

			public void Extend(int cols, T value)
			{
				while (cells_.Count < cols)
					cells_.Add(value);
			}
		}


		class CellData<T>
			where T : new()
		{
			private readonly List<RowData<T>> data_ = new List<RowData<T>>();

			public CellData()
			{
			}

			public CellData(int cols, int rows)
			{
				Extend(cols, rows);
			}

			public int RowCount
			{
				get { return data_.Count; }
			}

			public int ColumnCount
			{
				get
				{
					if (data_.Count == 0)
						return 0;
					else
						return data_[0].Count;
				}
			}

			public RowData<T> Row(int row)
			{
				return data_[row];
			}

			public T Cell(int col, int row)
			{
				return Row(row).Cell(col);
			}

			public void Set(int col, int row, T t)
			{
				Row(row).Set(col, t);
			}

			public void Extend(int cols, int rows)
			{
				cols = Math.Max(cols, ColumnCount);

				while (data_.Count < rows)
					data_.Add(new RowData<T>());

				foreach (var row in data_)
					row.Extend(cols);
			}
		}

		struct SizesData
		{
			public Size ps;
			public List<float> widths;
			public CellData<Size> sizes;
			public float tallest;

			public SizesData(int cols, int rows)
			{
				ps = new Size();
				widths = new List<float>();
				for (int i = 0; i < cols; ++i)
					widths.Add(0);
				sizes = new CellData<Size>(cols, rows);
				tallest = 0;
			}
		}


		private readonly CellData<List<Widget>> widgets_ =
			new CellData<List<Widget>>();

		private readonly RowData<bool> horStretch_ = new RowData<bool>();
		private readonly RowData<bool> verStretch_ = new RowData<bool>();


		private float hspacing_ = 0;
		private float vspacing_ = 0;
		private bool uniformWidth_ = false;
		private bool uniformHeight_ = true;
		private bool hfill_ = false;
		private int nextCol_ = 0;
		private int nextRow_ = 0;

		public GridLayout()
		{
		}

		public GridLayout(int cols, int spacing = 0)
			: this(cols, spacing, spacing)
		{
		}

		public GridLayout(int cols, int horSpacing, int verSpacing)
		{
			widgets_.Extend(cols, 1);
			horStretch_.Extend(cols, true);
			HorizontalSpacing = horSpacing;
			VerticalSpacing = verSpacing;
		}

		public static Data P(int col, int row)
		{
			return new Data(col, row);
		}

		public override float Spacing
		{
			get
			{
				return base.Spacing;
			}

			set
			{
				hspacing_ = value;
				vspacing_ = value;
			}
		}


		public float HorizontalSpacing
		{
			get { return hspacing_; }
			set { hspacing_ = value; }
		}

		public float VerticalSpacing
		{
			get { return vspacing_; }
			set { vspacing_ = value; }
		}

		public bool UniformHeight
		{
			get { return uniformHeight_; }
			set { uniformHeight_ = value; }
		}

		public bool UniformWidth
		{
			get { return uniformWidth_; }
			set { uniformWidth_ = value; }
		}

		public bool HorizontalFill
		{
			get { return hfill_; }
			set { hfill_ = value; }
		}

		public List<bool> HorizontalStretch
		{
			get
			{
				var list = new List<bool>();

				for (int i = 0; i < horStretch_.Count; ++i)
					list.Add(horStretch_.Cell(i));

				return list;
			}

			set
			{
				horStretch_.Extend(value.Count);
				for (int i = 0; i < value.Count; ++i)
					horStretch_.Set(i, value[i]);
			}
		}

		public List<bool> VerticalStretch
		{
			get
			{
				var list = new List<bool>();

				for (int i = 0; i < verStretch_.Count; ++i)
					list.Add(verStretch_.Cell(i));

				return list;
			}

			set
			{
				verStretch_.Extend(value.Count);
				for (int i = 0; i < value.Count; ++i)
					verStretch_.Set(i, value[i]);
			}
		}

		protected override void AddImpl(Widget w, LayoutData data)
		{
			var d = data as Data;
			if (d == null)
			{
				d = new Data(nextCol_, nextRow_);

				++nextCol_;
				if (nextCol_ >= widgets_.ColumnCount)
				{
					nextCol_ = 0;
					++nextRow_;
				}
			}

			if (d.row < 0)
			{
				Glue.LogError("gridlayout: bad row");
				return;
			}

			if (d.col < 0)
			{
				Glue.LogError("gridlayout: bad col");
				return;
			}

			widgets_.Extend(d.col + 1, d.row + 1);
			widgets_.Cell(d.col, d.row).Add(w);
			horStretch_.Extend(d.col + 1, true);
			verStretch_.Extend(d.row + 1, true);
		}

		protected override void LayoutImpl()
		{
			var r = new Rectangle(Parent.AbsoluteClientBounds);
			var d = GetCellSizes(r.Width, r.Height, true);


			var extraWidth = new List<float>();

			{
				int stretchCols = 0;
				for (int colIndex = 0; colIndex < horStretch_.Count; ++colIndex)
				{
					if (horStretch_.Cell(colIndex))
						++stretchCols;
				}

				var totalExtraWidth = Math.Max(0, r.Width - d.ps.Width);

				if (stretchCols == 0)
				{
					for (int colIndex = 0; colIndex < horStretch_.Count; ++colIndex)
						extraWidth.Add(0);
				}
				else
				{
					var addWidthPerCell = totalExtraWidth / stretchCols;

					for (int colIndex = 0; colIndex < horStretch_.Count; ++colIndex)
					{
						if (horStretch_.Cell(colIndex))
							extraWidth.Add(addWidthPerCell);
						else
							extraWidth.Add(0);
					}
				}
			}

			var extraHeight = new List<float>();

			{
				int stretchRows = 0;
				for (int rowIndex = 0; rowIndex < verStretch_.Count; ++rowIndex)
				{
					if (verStretch_.Cell(rowIndex))
						++stretchRows;
				}

				var totalExtraHeight = Math.Max(0, r.Height - d.ps.Height);

				if (stretchRows == 0)
				{
					for (int rowIndex = 0; rowIndex < verStretch_.Count; ++rowIndex)
						extraHeight.Add(0);
				}
				else
				{
					var addHeightPerCell = totalExtraHeight / stretchRows;

					for (int rowIndex = 0; rowIndex < verStretch_.Count; ++rowIndex)
					{
						if (verStretch_.Cell(rowIndex))
							extraHeight.Add(addHeightPerCell);
						else
							extraHeight.Add(0);
					}
				}
			}


			float x = r.Left;
			float y = r.Top;

			var rowWidths = new List<float>();
			for (int colIndex = 0; colIndex < widgets_.ColumnCount; ++colIndex)
				rowWidths.Add(0);

			for (int rowIndex = 0; rowIndex < widgets_.RowCount; ++rowIndex)
			{
				for (int colIndex = 0; colIndex < widgets_.ColumnCount; ++colIndex)
				{
					var ws = widgets_.Cell(colIndex, rowIndex);
					var ps = d.sizes.Cell(colIndex, rowIndex);
					var uniformWidth = d.widths[colIndex];

					float ww = 0;
					if (uniformWidth_)
						ww = uniformWidth + extraWidth[colIndex];
					else
						ww = ps.Width + extraWidth[colIndex];

					if (x + ww > r.Right)
						ww = r.Right - x;

					rowWidths[colIndex] = Math.Max(rowWidths[colIndex], ww);
				}
			}


			for (int rowIndex = 0; rowIndex < widgets_.RowCount; ++rowIndex)
			{
				float tallestInRow = 0;

				for (int colIndex = 0; colIndex < widgets_.ColumnCount; ++colIndex)
				{
					if (colIndex > 0)
						x += HorizontalSpacing;

					var ws = widgets_.Cell(colIndex, rowIndex);
					var ps = d.sizes.Cell(colIndex, rowIndex);
					var uniformWidth = d.widths[colIndex];

					float ww = 0;
					if (uniformWidth_)
						ww = uniformWidth + extraWidth[colIndex];
					else if (hfill_)
						ww = rowWidths[colIndex];
					else
						ww = ps.Width + extraWidth[colIndex];

					float wh = 0;
					if (uniformHeight_)
						wh = d.tallest + extraHeight[rowIndex];
					else
						wh = ps.Height + extraHeight[rowIndex];

					if (x + ww > r.Right)
						ww = r.Right - x;

					if (y + wh > r.Bottom)
						wh = r.Bottom - y;

					var wr = Rectangle.FromSize(x, y, ww, wh);

					foreach (var w in ws)
					{
						if (!w.Visible)
							continue;

						// a widget's size can never change again when uniform,
						// unless the grid itself changes size
						w.SetBounds(wr, uniformWidth_ && uniformHeight_);

						if (Parent.Name == "bleh")
							Glue.LogInfo($"row={rowIndex} col={colIndex} {w} {wr}");
					}

					x += uniformWidth + extraWidth[colIndex];
					tallestInRow = Math.Max(tallestInRow, wr.Height);
				}

				x = r.Left;

				if (uniformHeight_)
					y += d.tallest + extraHeight[rowIndex] + VerticalSpacing;
				else
					y += tallestInRow + VerticalSpacing;
			}
		}

		protected override Size DoGetPreferredSize(float maxWidth, float maxHeight)
		{
			return GetCellSizes(maxWidth, maxHeight, true).ps;
		}

		protected override Size DoGetMinimumSize()
		{
			return GetCellSizes(-1, -1, false).ps;
		}

		private SizesData GetCellSizes(
			float maxWidth, float maxHeight, bool preferred)
		{
			var d = new SizesData(widgets_.ColumnCount, widgets_.RowCount);

			float maxCellHeight = maxHeight;
			if (uniformHeight_ && maxHeight != DontCare)
				maxCellHeight = (maxHeight - (VerticalSpacing  * (widgets_.RowCount - 1))) / widgets_.RowCount;

			for (int rowIndex = 0; rowIndex < widgets_.RowCount; ++rowIndex)
			{
				var row = widgets_.Row(rowIndex);

				float width = 0;
				float tallestInRow = 0;

				for (int colIndex = 0; colIndex < row.Count; ++colIndex)
				{
					var cell = row.Cell(colIndex);
					var cellPs = new Size(Widget.DontCare, Widget.DontCare);

					foreach (var w in cell)
					{
						if (!w.Visible)
							continue;

						Size ps;

						if (preferred)
							ps = w.GetRealPreferredSize(maxWidth, maxHeight);
						else
							ps = w.GetRealMinimumSize();

						cellPs.Width = Math.Max(cellPs.Width, ps.Width);
						cellPs.Height = Math.Max(cellPs.Height, ps.Height);

						if (maxCellHeight != DontCare)
							cellPs.Height = Math.Min(cellPs.Height, maxCellHeight);
					}

					d.sizes.Set(colIndex, rowIndex, cellPs);

					if (uniformWidth_)
					{
						var avWidth = maxWidth - (HorizontalSpacing * (widgets_.ColumnCount - 1));
						d.widths[colIndex] = avWidth / widgets_.ColumnCount;
					}
					else
					{
						d.widths[colIndex] = Math.Max(d.widths[colIndex], cellPs.Width);
					}

					if (colIndex > 0)
						width += HorizontalSpacing;

					width += d.widths[colIndex];
					tallestInRow = Math.Max(tallestInRow, cellPs.Height);
				}

				if (rowIndex > 0)
					d.ps.Height += vspacing_;

				d.ps.Width = Math.Max(d.ps.Width, width);
				d.ps.Height += tallestInRow;
				d.tallest = Math.Max(d.tallest, tallestInRow);
			}

			if (uniformHeight_)
			{
				d.ps.Height = 0;

				for (int i = 0; i < widgets_.RowCount; ++i)
				{
					if (i > 0)
						d.ps.Height += vspacing_;

					d.ps.Height += d.tallest;
				}
			}

			return d;
		}
	}
}
