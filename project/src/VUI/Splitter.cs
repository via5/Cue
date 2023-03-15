using UnityEngine;

namespace VUI
{
	class SplitterHandle : Panel
	{
		public delegate void MovedHandler(float x);
		public event MovedHandler Moved;

		private readonly Splitter sp_;
		private Point dragStart_;
		private float initialPos_ = 0;
		private bool dragging_ = false;
		private bool on_ = false;

		public SplitterHandle(Splitter sp)
		{
			sp_ = sp;

			Clickthrough = false;
			BackgroundColor = Style.Theme.SplitterHandleBackgroundColor;

			Events.PointerDown += OnPointerDown;
			Events.PointerUp += OnPointerUp;
			Events.DragStart += OnDragStart;
			Events.Drag += OnDrag;
			Events.DragEnd += OnDragEnd;
			Events.PointerEnter += OnPointerEnter;
			Events.PointerExit += OnPointerExit;
		}

		private void OnPointerEnter(PointerEvent e)
		{
			on_ = true;

			Glue.IconProvider.ResizeWE?.GetTexture((t) =>
			{
				if (on_)
				{
					Cursor.SetCursor(
						t as Texture2D,
						new Vector2(t.width / 2, t.height / 2), CursorMode.ForceSoftware);
				}
			});
		}

		private void OnPointerExit(PointerEvent e)
		{
			on_ = false;
			Cursor.SetCursor(null, new Vector2(0, 0), CursorMode.Auto);
		}

		public void OnPointerDown(PointerEvent e)
		{
			dragStart_ = e.Pointer;
			SetCapture();
		}

		public void OnPointerUp(PointerEvent e)
		{
			ReleaseCapture();
		}

		public void OnDragStart(DragEvent e)
		{
			dragging_ = true;
			initialPos_ = sp_.HandlePosition;
		}

		public void OnDrag(DragEvent e)
		{
			if (!dragging_)
				return;

			var p = e.Pointer;
			var delta = p - dragStart_;

			Moved?.Invoke(initialPos_ + delta.X);
		}

		public void OnDragEnd(DragEvent e)
		{
			dragging_ = false;
			ReleaseCapture();
		}
	}


	class Splitter : Panel
	{
		public delegate void Handler();
		public event Handler Moved;

		public const int AbsolutePosition = 0;
		public const int MinimumFirst = 1;
		public const int Centered = 2;
		public const int MinimumSecond = 3;

		private Widget first_ = null;
		private Widget second_  = null;
		private readonly SplitterHandle handle_;
		private int initHandle_ = Centered;
		private float handlePos_ = -1;
		private bool handleInited_ = false;

		public Splitter()
			: this(null, null, 0)
		{
		}

		public Splitter(Widget first, Widget second, int initialHandleMode, float handlePos = -1)
		{
			Layout = new AbsoluteLayout();
			handle_ = new SplitterHandle(this);
			handle_.Moved += OnHandleMoved;

			first_ = first;
			second_ = second;
			initHandle_ = initialHandleMode;
			handlePos_ = handlePos;

			Rebuild();
		}

		public Widget First
		{
			get { return first_; }
			set { first_ = value; Rebuild(); }
		}

		public Widget Second
		{
			get { return second_; }
			set { second_ = value; Rebuild(); }
		}

		public float HandlePosition
		{
			get { return handlePos_; }
			set { SetHandlePosition(value); }
		}

		public int InitialHandleMode
		{
			get { return initHandle_; }
			set { initHandle_ = value; }
		}

		private void Rebuild()
		{
			RemoveAllChildren();

			if (first_ != null)
				Add(first_);

			if (second_ != null)
				Add(second_);

			Add(handle_);
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
			if (first_ == null || second_ == null)
				return Size.Zero;

			Size firstPs, secondPs;

			if (preferred)
			{
				firstPs = first_.GetRealPreferredSize(maxWidth, maxHeight);
				secondPs = second_.GetRealPreferredSize(maxWidth, maxHeight);
			}
			else
			{
				firstPs = first_.GetRealMinimumSize();
				secondPs = second_.GetRealMinimumSize();
			}

			var ps = Size.Zero;

			ps.Width =
				Mathf.Max(firstPs.Width, handlePos_) +
				Style.Metrics.SplitterHandleSize +
				secondPs.Width;

			ps.Height = Mathf.Max(firstPs.Height, secondPs.Height);

			return ps;
		}

		protected override void AfterUpdateBounds()
		{
			if (first_ == null || second_ == null)
				return;

			float minFirst = -1;
			float minSecond = -1;
			var r = AbsoluteClientBounds;

			if (!handleInited_)
			{
				handleInited_ = true;

				switch (initHandle_)
				{
					case AbsolutePosition:
					{
						// no-op
						break;
					}

					case MinimumFirst:
					{
						minFirst = first_.GetRealMinimumSize().Width;
						handlePos_ = minFirst;
						break;
					}

					case MinimumSecond:
					{
						minSecond = second_.GetRealMinimumSize().Width;
						handlePos_ = r.Width - minSecond;
						break;
					}

					case Centered:  // fall-through
					default:
					{
						handlePos_ = r.Width / 2;
						break;
					}
				}
			}

			handlePos_ = AdjustedPos(handlePos_, minFirst, minSecond);

			var leftRect = r;
			leftRect.Right = leftRect.Left + handlePos_;

			var handleRect = r;
			handleRect.Left = leftRect.Right;
			handleRect.Right = handleRect.Left + Style.Metrics.SplitterHandleSize;

			var rightRect = r;
			rightRect.Left = handleRect.Right;

			first_.SetBounds(leftRect);
			second_.SetBounds(rightRect);
			handle_.SetBounds(handleRect);

			first_.DoLayout();
			second_.DoLayout();
			handle_.DoLayout();
		}

		private void OnHandleMoved(float x)
		{
			SetHandlePosition(x);
			Moved?.Invoke();
		}

		private void SetHandlePosition(float x)
		{
			var a = AdjustedPos(x);

			if (Mathf.Abs(handlePos_ - a) > 0.1f)
			{
				handlePos_ = a;
				UpdateBounds();
			}
		}

		private float AdjustedPos(float x, float minFirst = -1, float minSecond = -1)
		{
			if (first_ != null && minFirst < 0)
				minFirst = first_.GetRealMinimumSize().Width;

			if (second_ != null && minSecond < 0)
				minSecond = second_.GetRealMinimumSize().Width;

			var r = ClientBounds;

			if (x - r.Left < minFirst)
				x = r.Left + minFirst;

			if (r.Right - x < minSecond)
				x = r.Right - minSecond;

			return x;
		}
	}
}
