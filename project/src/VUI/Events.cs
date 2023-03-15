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

		public Logger Log
		{
			get { return widget_?.Log ?? Logger.Global; }
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
				Log.ErrorST(e.ToString());
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
				Log.ErrorST(e.ToString());
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
				Log.ErrorST(e.ToString());
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
				Log.ErrorST(e.ToString());
			}
		}

		public void OnPointerClick(PointerEventData d)
		{
			try
			{
				if (widget_ != null)
				{
					// vr has clickCount to 0
					if (d.clickCount == 0 || d.clickCount == 1)
						widget_.OnPointerClickInternal(d);
					else if (d.clickCount == 2)
						widget_.OnPointerDoubleClickInternal(d);
				}
			}
			catch (Exception e)
			{
				Log.ErrorST(e.ToString());
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
				Log.ErrorST(e.ToString());
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
				Log.ErrorST(e.ToString());
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
				Log.ErrorST(e.ToString());
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
				Log.ErrorST(e.ToString());
			}
		}
	}


	public interface IWidget
	{
		void Remove();
	}


	public interface IEvent
	{
		BaseEventData EventData { get; }
		bool Bubble { get; set; }
	}


	public abstract class BasicEvent : IEvent
	{
		private bool bubble_;

		protected BasicEvent(bool defBubble)
		{
			bubble_ = defBubble;
		}

		public bool Bubble
		{
			get { return bubble_; }
			set { bubble_ = value; }
		}

		public virtual BaseEventData EventData
		{
			get { return null; }
		}
	}


	public class FocusEvent : BasicEvent
	{
		private readonly Widget other_;

		public FocusEvent(Widget other)
			: base(false)
		{
			other_ = other;
		}

		public Widget Other
		{
			get { return other_; }
		}
	}

	public abstract class MouseEvent : BasicEvent
	{
		private readonly Widget w_;
		private readonly PointerEventData d_;

		protected MouseEvent(Widget w, PointerEventData d, bool defBubble)
			: base(defBubble)
		{
			w_ = w;
			d_ = d;
		}

		public override BaseEventData EventData
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

	public class WheelEvent : MouseEvent
	{
		private readonly Point d_;

		public WheelEvent(Widget w, PointerEventData d)
			: base(w, d, true)
		{
			d_ = new Point(d.scrollDelta.x / 100.0f, d.scrollDelta.y / 100.0f);
		}

		public Point Delta
		{
			get { return d_; }
		}
	}

	public class DragEvent : MouseEvent
	{
		public DragEvent(Widget w, PointerEventData d)
			: base(w, d, false)
		{
		}
	}

	public class PointerEvent : MouseEvent
	{
		public const int NoButton = -1;
		public const int LeftButton = 0;
		public const int RightButton = 1;
		public const int MiddleButton = 2;

		private int button_ = NoButton;

		public PointerEvent(Widget w, PointerEventData d, bool defBubble)
			: base(w, d, defBubble)
		{
			if (d != null)
				button_ = (int)d.button;
		}

		public int Button
		{
			get { return button_; }
		}
	}


	public class Events
	{
		public delegate void FocusHandler(FocusEvent e);
		public event FocusHandler Focus, Blur;

		private void DoFireFocus(Widget w, FocusHandler h)
		{
			if (h != null)
			{
				var e = new FocusEvent(w);
				h.Invoke(e);
			}
		}

		public void FireFocus(Widget w) { DoFireFocus(w, Focus); }
		public void FireBlur(Widget w) { DoFireFocus(w, Blur); }


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


		public delegate void WheelHandler(WheelEvent e);
		public event WheelHandler Wheel;

		private bool DoFireWheel(Widget w, PointerEventData d, WheelHandler h)
		{
			if (h != null)
			{
				var e = new WheelEvent(w, d);
				h.Invoke(e);
				return e.Bubble;
			}

			return true;
		}

		public bool FireWheel(Widget w, PointerEventData d) { return DoFireWheel(w, d, Wheel); }


		public delegate void PointerHandler(PointerEvent e);
		public event PointerHandler PointerEnter, PointerExit;

		private bool DoFirePointer(Widget w, PointerEventData d, PointerHandler h, bool defBubble = false)
		{
			if (h != null)
			{
				var e = new PointerEvent(w, d, defBubble);
				h.Invoke(e);
				return e.Bubble;
			}

			return defBubble;
		}

		public bool FirePointerEnter(Widget w, PointerEventData d, bool defBubble = false) { return DoFirePointer(w, d, PointerEnter, defBubble); }
		public bool FirePointerExit(Widget w, PointerEventData d) { return DoFirePointer(w, d, PointerExit); }


		public delegate void BubblePointerHandler(PointerEvent e);
		public event BubblePointerHandler PointerDown, PointerUp, PointerClick;
		public event BubblePointerHandler PointerDoubleClick, PointerMove;

		private bool DoFireBubblePointer(Widget w, PointerEventData d, BubblePointerHandler h)
		{
			if (h != null)
			{
				var e = new PointerEvent(w, d, true);
				h.Invoke(e);
				return e.Bubble;
			}

			return true;
		}

		public bool FirePointerDown(Widget w, PointerEventData d) { return DoFireBubblePointer(w, d, PointerDown); }
		public bool FirePointerUp(Widget w, PointerEventData d) { return DoFireBubblePointer(w, d, PointerUp); }
		public bool FirePointerClick(Widget w, PointerEventData d) { return DoFireBubblePointer(w, d, PointerClick); }
		public bool FirePointerDoubleClick(Widget w, PointerEventData d) { return DoFireBubblePointer(w, d, PointerDoubleClick); }
		public bool FirePointerMove(Widget w, PointerEventData d) { return DoFireBubblePointer(w, d, PointerMove); }
	}
}
