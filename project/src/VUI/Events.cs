using UnityEngine.EventSystems;
using UnityEngine;
using System;

namespace VUI
{
	class MouseCallbacks : MonoBehaviour,
		IPointerEnterHandler, IPointerExitHandler,
		IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
		IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		private Widget widget_ = null;

		public Widget Widget
		{
			get { return widget_; }
			set { widget_ = value; }
		}

		public void OnPointerEnter(PointerEventData d)
		{
			try
			{
				if (widget_ != null)
					widget_.OnPointerEnterInternal(d);
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
			}
		}

		public void OnPointerExit(PointerEventData d)
		{
			try
			{
				if (widget_ != null)
					widget_.OnPointerExitInternal(d);
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
			}
		}

		public void OnPointerDown(PointerEventData d)
		{
			try
			{
				if (widget_ != null)
					widget_.OnPointerDownInternal(d);
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
			}
		}

		public void OnPointerUp(PointerEventData d)
		{
			try
			{
				if (widget_ != null)
					widget_.OnPointerUpInternal(d);
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
			}
		}

		public void OnPointerClick(PointerEventData d)
		{
			try
			{
				if (widget_ != null)
					widget_.OnPointerClickInternal(d);
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
			}
		}

		public void OnScroll(PointerEventData d)
		{
			try
			{
				if (d.scrollDelta != Vector2.zero)
				{
					if (widget_ != null)
						widget_.OnWheelInternal(d);
				}
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
			}
		}

		public void OnBeginDrag(PointerEventData d)
		{
			try
			{
				if (widget_ != null)
					widget_.OnBeginDragInternal(d);
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
			}
		}

		public void OnDrag(PointerEventData d)
		{
			try
			{
				if (widget_ != null)
					widget_.OnDragInternal(d);
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
			}
		}

		public void OnEndDrag(PointerEventData d)
		{
			try
			{
				if (widget_ != null)
					widget_.OnEndDragInternal(d);
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
			}
		}
	}


	interface IWidget
	{
		void Remove();
	}


	interface IEvent
	{
		BaseEventData EventData { get; }
	}

	abstract class MouseEvent : IEvent
	{
		private readonly Widget w_;
		private readonly PointerEventData d_;

		protected MouseEvent(Widget w, PointerEventData d)
		{
			w_ = w;
			d_ = d;
		}

		public BaseEventData EventData
		{
			get { return d_; }
		}

		public Point Pointer
		{
			get
			{
				if (d_ == null)
					return w_?.GetRoot().MousePosition ?? Point.Zero;
				else
					return w_?.GetRoot()?.ToLocal(d_.position) ?? Point.Zero;
			}
		}
	}

	class WheelEvent : MouseEvent
	{
		private readonly Point d_;

		public WheelEvent(Widget w, PointerEventData d)
			: base(w, d)
		{
			d_ = new Point(d.scrollDelta.x / 100.0f, d.scrollDelta.y / 100.0f);
		}

		public Point Delta
		{
			get { return d_; }
		}
	}

	class DragEvent : MouseEvent
	{
		public DragEvent(Widget w, PointerEventData d)
			: base(w, d)
		{
		}
	}

	class PointerEvent : MouseEvent
	{
		public PointerEvent(Widget w, PointerEventData d)
			: base(w, d)
		{
		}
	}


	class Events
	{
		public delegate void DragHandler(DragEvent e);
		public event DragHandler DragStart, Drag, DragEnd;

		private void DoFireDrag(Widget w, PointerEventData d, DragHandler h)
		{
			if (h != null)
			{
				var e = new DragEvent(w, d);
				h.Invoke(e);
			}
		}

		public void FireDragStart(Widget w, PointerEventData d) { DoFireDrag(w, d, DragStart); }
		public void FireDrag(Widget w, PointerEventData d) { DoFireDrag(w, d, Drag); }
		public void FireDragEnd(Widget w, PointerEventData d) { DoFireDrag(w, d, DragEnd); }


		public delegate bool WheelHandler(WheelEvent e);
		public event WheelHandler Wheel;

		private bool? DoFireWheel(Widget w, PointerEventData d, WheelHandler h)
		{
			if (h != null)
			{
				var e = new WheelEvent(w, d);
				return h.Invoke(e);
			}

			return null;
		}

		public bool? FireWheel(Widget w, PointerEventData d) { return DoFireWheel(w, d, Wheel); }


		public delegate void PointerHandler(PointerEvent e);
		public event PointerHandler PointerEnter, PointerExit;

		private void DoFirePointer(Widget w, PointerEventData d, PointerHandler h)
		{
			if (h != null)
			{
				var e = new PointerEvent(w, d);
				h.Invoke(e);
			}
		}

		public void FirePointerEnter(Widget w, PointerEventData d) { DoFirePointer(w, d, PointerEnter); }
		public void FirePointerExit(Widget w, PointerEventData d) { DoFirePointer(w, d, PointerExit); }


		public delegate bool BubblePointerHandler(PointerEvent e);
		public event BubblePointerHandler PointerDown, PointerUp, PointerClick;
		public event BubblePointerHandler PointerMove;

		private bool? DoFireBubblePointer(Widget w, PointerEventData d, BubblePointerHandler h)
		{
			if (h != null)
			{
				var e = new PointerEvent(w, d);
				return h.Invoke(e);
			}

			return null;
		}

		public bool? FirePointerDown(Widget w, PointerEventData d) { return DoFireBubblePointer(w, d, PointerDown); }
		public bool? FirePointerUp(Widget w, PointerEventData d) { return DoFireBubblePointer(w, d, PointerUp); }
		public bool? FirePointerClick(Widget w, PointerEventData d) { return DoFireBubblePointer(w, d, PointerClick); }
		public bool? FirePointerMove(Widget w, PointerEventData d) { return DoFireBubblePointer(w, d, PointerMove); }
	}
}
