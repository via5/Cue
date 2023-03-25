using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

namespace VUI
{
	class ScrollBarHandle : Button
	{
		public delegate void Handler();
		public event Handler DragStarted, Moved, DragEnded;

		private Point dragStart_;
		private Rectangle initialBounds_;
		private bool dragging_ = false;

		public ScrollBarHandle()
		{
			Events.DragStart += OnDragStart;
			Events.Drag += OnDrag;
			Events.DragEnd += OnDragEnd;
		}

		public void OnDragStart(DragEvent e)
		{
			dragging_ = true;
			dragStart_ = e.Pointer;
			initialBounds_ = AbsoluteClientBounds;

			SetCapture();
			DragStarted?.Invoke();
		}

		public void OnDrag(DragEvent e)
		{
			if (!dragging_)
				return;

			var p = e.Pointer;
			var delta = p - dragStart_;

			if (Math.Abs(delta.X) > Style.Metrics.ScrollBarMaxDragDistance)
				delta.Y = 0;

			var r = Rectangle.FromSize(
				initialBounds_.Left,
				initialBounds_.Top + (delta.Y),
				initialBounds_.Width,
				initialBounds_.Height);

			var box = Parent.AbsoluteClientBounds;

			if (r.Top < box.Top)
				r.MoveTo(r.Left, box.Top);

			if (r.Bottom > box.Bottom)
				r.MoveTo(r.Left, box.Bottom - r.Height);

			SetBounds(r);
			UpdateBounds();

			Moved?.Invoke();
		}

		public void OnDragEnd(DragEvent e)
		{
			bool wasDragging_ = dragging_;

			dragging_ = false;
			ReleaseCapture();

			if (wasDragging_)
				DragEnded?.Invoke();
		}
	}


	class ScrollBar : Panel
	{
		public delegate void Handler();
		public delegate void ValueHandler(float v);

		public event Handler DragStarted, DragEnded;
		public event ValueHandler ValueChanged;

		private ScrollBarHandle handle_ = new ScrollBarHandle();
		private float range_ = 0;
		private float value_ = 0;

		public ScrollBar()
		{
			Margins = new Insets(0, 1, 1, 1);
			Layout = new AbsoluteLayout();
			Clickthrough = false;
			BackgroundColor = Style.Theme.ScrollBarBackgroundColor;

			Add(handle_);

			Events.PointerDown += OnPointerDown;
			handle_.Moved += OnHandleMoved;
			handle_.DragStarted += () => DragStarted?.Invoke();
			handle_.DragEnded += () => DragEnded?.Invoke();
		}

		public float Range
		{
			get
			{
				return range_;
			}

			set
			{
				if (range_ != value)
				{
					range_ = value;

					if (WidgetObject != null)
						UpdateBounds();
				}
			}
		}

		public float Value
		{
			get
			{
				return value_;
			}

			set
			{
				if (value_ != value)
				{
					value_ = value;

					if (WidgetObject != null)
						UpdateBounds();

					ValueChanged?.Invoke(value_);
				}
			}
		}

		protected override void DoSetEnabled(bool b)
		{
			base.DoSetEnabled(b);
			handle_.Visible = b;
		}

		protected override Size DoGetMinimumSize()
		{
			return new Size(Style.Metrics.ScrollBarWidth, DontCare);
		}

		protected override void BeforeUpdateBounds()
		{
			var r = AbsoluteClientBounds;
			var h = Math.Max(r.Height - range_, 50);

			var cb = ClientBounds;
			var avh = cb.Height - handle_.ClientBounds.Height;
			var p = range_ == 0 ? 0 : (value_ / range_);
			r.Top += Borders.Top + p * avh;
			r.Bottom = r.Top + h;

			handle_.SetBounds(r);
			DoLayoutImpl();
		}

		private void OnHandleMoved()
		{
			var r = AbsoluteClientBounds;
			var hr = handle_.RelativeBounds;
			var top = hr.Top - Borders.Top;
			var h = r.Height - hr.Height;
			var p = (top / h);

			value_ = p * range_;
			ValueChanged?.Invoke(value_);
		}

		private void OnPointerDown(PointerEvent e)
		{
			if (!Enabled)
				return;

			var r = AbsoluteClientBounds;
			var p = e.Pointer - r.TopLeft;
			var y = r.Top + p.Y - handle_.ClientBounds.Height / 2;

			if (y < r.Top)
				y = r.Top;
			else if (y + handle_.ClientBounds.Height > ClientBounds.Height)
				y = r.Bottom - handle_.ClientBounds.Height;

			var cb = handle_.Bounds;
			var h = cb.Height;
			cb.Top = y;
			cb.Bottom = y + h;

			handle_.SetBounds(cb);
			DoLayoutImpl();
			OnHandleMoved();
			base.UpdateBounds();

			var d = e.EventData as PointerEventData;
			SuperController.singleton.StartCoroutine(StartDrag(d));

			e.Bubble = false;
		}

		private IEnumerator StartDrag(PointerEventData d)
		{
			yield return new WaitForEndOfFrame();

			var o = handle_.WidgetObject.gameObject;

			d.pointerPress = o;
			d.pointerDrag = o;
			d.rawPointerPress = o;
			d.pointerEnter = o;
			d.selectedObject = o;
			d.hovered.Clear();

			List<RaycastResult> rc = new List<RaycastResult>();
			EventSystem.current.RaycastAll(d, rc);

			foreach (var r in rc)
			{
				d.hovered.Add(r.gameObject);

				if (r.gameObject == o)
				{
					d.pointerCurrentRaycast = r;
					d.pointerPressRaycast = r;
					break;
				}
			}

			ExecuteEvents.Execute(
				handle_.WidgetObject.gameObject, d, ExecuteEvents.pointerEnterHandler);

			ExecuteEvents.Execute(
				handle_.WidgetObject.gameObject, d, ExecuteEvents.pointerDownHandler);
		}
	}


	class FixedScrolledPanel : Panel
	{
		public delegate void Handler(int v);
		public event Handler Scrolled;

		private readonly ScrollBar sb_;
		private readonly Panel panel_;

		private float rowSize_ = 1;
		private int rows_ = 1;
		private int top_ = 0;

		public FixedScrolledPanel()
		{
			sb_ = new ScrollBar();
			sb_.MinimumSize = new Size(Style.Metrics.ScrollBarWidth, 0);
			sb_.ValueChanged += OnScrollbar;
			sb_.DragEnded += OnScrollbarDragEnded;

			panel_ = new Panel();

			Layout = new BorderLayout();
			MinimumSize = new Size(300, Widget.DontCare);
			Margins = new Insets(0);
			Padding = new Insets(5, 0, 0, 0);
			Borders = new Insets(0);

			Clickthrough = false;
			Events.Wheel += OnWheel;

			Add(panel_, BorderLayout.Center);
			Add(sb_, BorderLayout.Right);
		}

		public Panel ContentPanel
		{
			get { return panel_; }
		}

		public ScrollBar VerticalScrollbar
		{
			get { return sb_; }
		}

		public void Set(int rows, float rowSize, float scrollPos = 0)
		{
			top_ = 0;
			rows_ = rows;
			rowSize_ = rowSize;

			if (panel_.ClientBounds.Height <= 0)
			{
				sb_.Range = 0;
			}
			else
			{
				if (rows_ <= 0)
				{
					sb_.Range = 0;
					sb_.Enabled = false;
				}
				else
				{
					sb_.Range = (rows_) * rowSize_ - 1;
					sb_.Enabled = true;
				}
			}

			sb_.Value = scrollPos;
			OnScrollbar(scrollPos);
		}

		private void OnWheel(WheelEvent e)
		{
			int newTop = Utilities.Clamp(top_ + (int)-e.Delta.Y, 0, rows_);
			float v = newTop * rowSize_;

			if (e.Delta.Y < 0)
			{
				// down
				if (newTop == rows_)
					v = sb_.Range;
			}

			sb_.Value = v;
			GetRoot()?.Tooltips.Hide();

			e.Bubble = false;
		}

		private void OnScrollbar(float v)
		{
			int y = Math.Max(0, (int)Math.Round(v / rowSize_));

			if (top_ != y)
			{
				top_ = y;
				Scrolled?.Invoke(top_);
			}
		}

		private void OnScrollbarDragEnded()
		{
			if (top_ >= rows_)
				sb_.Value = sb_.Range;
			else
				sb_.Value = top_ * rowSize_;
		}
	}
}
