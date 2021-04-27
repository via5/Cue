using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VUI
{
	class MouseHandler : MonoBehaviour, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler
	{
		public const int Continue = 0;
		public const int StopPropagation = 1;

		public delegate void Callback(PointerEventData data);
		public event Callback Clicked, Down, Up;

		public void OnPointerClick(PointerEventData data)
		{
			Clicked?.Invoke(data);
		}

		public void OnPointerDown(PointerEventData data)
		{
			Down?.Invoke(data);
		}

		public void OnPointerUp(PointerEventData data)
		{
			Up?.Invoke(data);
		}
	}

	class Overlay : Widget
	{
		public override string TypeName { get { return "Overlay"; } }

		private Image graphics_ = null;

		public Overlay(Rectangle b)
		{
			Bounds = b;
		}

		protected override void DoCreate()
		{
			base.DoCreate();

			graphics_ = MainObject.AddComponent<Image>();
			graphics_.color = new Color(0, 0, 0, 0.7f);
			graphics_.raycastTarget = true;

			MainObject.AddComponent<MouseHandler>();
		}

		public new void Destroy()
		{
			base.Destroy();
		}
	}


	class RootPanel : Panel
	{
		public override string TypeName { get { return "RootPanel"; } }

		private readonly Root root_;

		public RootPanel(Root r)
		{
			root_ = r;
			Margins = new Insets(5);
		}

		protected override void NeedsLayoutImpl(string why)
		{
			root_.SetDirty(why);
		}

		public override Root GetRoot()
		{
			return root_;
		}

		public new void Destroy()
		{
			base.Destroy();
		}
	}


	interface IRootSupport
	{
		MVRScriptUI ScriptUI { get; }
		Canvas Canvas { get; }
		Transform RootParent { get; }
		void Destroy();
	}


	class Root
	{
		public const int FocusDefault = 0x0;
		public const int FocusKeepPopup = 0x01;


		private IRootSupport support_ = null;
		static private TextGenerator tg_ = new TextGenerator();
		static private TextGenerationSettings ts_ = new TextGenerationSettings();

		private Rectangle bounds_;
		private Insets margins_ = new Insets(5);
		private RootPanel content_;
		private RootPanel floating_;
		private Overlay overlay_ = null;
		private readonly TooltipManager tooltips_;
		private float topOffset_ = 0;
		private bool dirty_ = true;
		private Canvas canvas_;

		private UIPopup openedPopup_ = null;
		private Widget focused_ = null;

		private TimerManager ownTm_ = null;

		public static Point NoMousePos
		{
			get
			{
				return new Point(float.MaxValue, float.MaxValue);
			}
		}

		public Root(IRootSupport support)
		{
			if (TimerManager.Instance == null)
				ownTm_ = new TimerManager();

			content_ = new RootPanel(this);
			floating_ = new RootPanel(this);
			tooltips_ = new TooltipManager(this);

			AttachTo(support);
		}

		public IRootSupport RootParent
		{
			get { return support_; }
		}

		public Transform WidgetParentTransform
		{
			get { return support_.RootParent; }
		}

		public void SetOpenedPopup(UIPopup p)
		{
			openedPopup_ = p;
		}

		public void SetFocus(Widget w, int flags = FocusDefault)
		{
			if (focused_ == w)
				return;

			focused_ = w;

			// used by the filter textbox in the combobox so clicking it doesn't
			// close the combobox
			if (!Bits.IsSet(flags, FocusKeepPopup) && openedPopup_ != null)
			{
				if (openedPopup_.visible)
					openedPopup_.Toggle();

				openedPopup_ = null;
			}
		}

		public void AttachTo(IRootSupport support)
		{
			support_ = support;

			if (support_.ScriptUI == null)
			{
				var rt = support_.RootParent.GetComponent<RectTransform>();

				topOffset_ = rt.offsetMin.y - rt.offsetMax.y;
				bounds_ = Rectangle.FromPoints(
					0, 0, rt.rect.width, rt.rect.height);
			}
			else
			{
				var scrollView = support_.ScriptUI.GetComponentInChildren<ScrollRect>();
				var scrollViewRT = scrollView.GetComponent<RectTransform>();
				topOffset_ = scrollViewRT.offsetMin.y - scrollViewRT.offsetMax.y;

				bounds_ = Rectangle.FromPoints(
					1, 1, scrollViewRT.rect.width - 3, scrollViewRT.rect.height - 3);

				Style.SetupRoot(support_.ScriptUI);
			}

			content_.Bounds = new Rectangle(bounds_);
			floating_.Bounds = new Rectangle(bounds_);


			var text = support_.RootParent.root.GetComponentInChildren<Text>();
			if (text == null)
			{
				Glue.LogError("no text in attach");
			}
			else
			{
				tg_ = text.cachedTextGenerator;
				ts_ = text.GetGenerationSettings(new Vector2());
			}
		}

		public void Destroy()
		{
			if (support_.ScriptUI != null)
				Style.RevertRoot(support_.ScriptUI);

			content_?.Destroy();
			floating_?.Destroy();
			overlay_?.Destroy();
			tooltips_?.Destroy();
			support_?.Destroy();

			openedPopup_ = null;
			focused_ = null;
		}

		public Panel ContentPanel
		{
			get { return content_; }
		}

		public Panel FloatingPanel
		{
			get { return floating_; }
		}

		public TooltipManager Tooltips
		{
			get { return tooltips_; }
		}

		public Rectangle Bounds
		{
			get { return bounds_; }
		}

		public bool Visible
		{
			get { return support_.RootParent.gameObject.activeInHierarchy; }
			set { support_.RootParent.gameObject.SetActive(value); }
		}

		public void Update(bool forceLayout=false)
		{
			if (ownTm_ != null)
			{
				ownTm_.TickTimers(Time.deltaTime);
				ownTm_.CheckTimers();
			}

			if (dirty_ || forceLayout)
			{
				var start = Time.realtimeSinceStartup;

				content_.DoLayout();
				content_.Create();
				content_.UpdateBounds();

				floating_.DoLayout();
				floating_.Create();
				floating_.UpdateBounds();

				var t = Time.realtimeSinceStartup - start;

				Glue.LogVerbose("layout: " + t.ToString("0.000") + "s");

				dirty_ = false;
			}
		}

		public void SetDirty(string why)
		{
			if (!dirty_)
			{
				Glue.LogVerbose("needs layout: " + why);
				dirty_ = true;
			}
		}

		public bool OverlayVisible
		{
			set
			{
				if (value)
					ShowOverlay();
				else
					HideOverlay();
			}
		}

		public Point MousePosition
		{
			get
			{
				if (canvas_ == null)
				{
					canvas_ = support_.Canvas;
					if (canvas_ == null)
						return NoMousePos;
				}


				var mp = Input.mousePosition;

				Vector2 pp;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(
					canvas_.transform as RectTransform, mp,
					canvas_.worldCamera, out pp);

				pp.x = bounds_.Left + bounds_.Width / 2 + pp.x;
				pp.y = bounds_.Top + (bounds_.Height - pp.y + topOffset_);

				return new Point(pp.x, pp.y);
			}
		}

		private void ShowOverlay()
		{
			if (overlay_ == null)
			{
				overlay_ = new Overlay(bounds_);
				floating_.Add(overlay_);
				overlay_.Create();
				overlay_.UpdateBounds();
			}

			overlay_.Visible = true;
			overlay_.DoLayout();
			overlay_.BringToTop();
		}

		private void HideOverlay()
		{
			if (overlay_ != null)
				overlay_.Visible = false;
		}

		public static float TextLength(Font font, int fontSize, string s)
		{
			var ts = ts_;
			ts.font = font ?? Style.Theme.DefaultFont;
			ts.fontSize = (fontSize < 0 ? Style.Theme.DefaultFontSize : fontSize);

			return tg_.GetPreferredWidth(s, ts);
		}

		public static Size TextSize(Font font, int fontSize, string s)
		{
			var ts = ts_;
			ts.font = font ?? Style.Theme.DefaultFont;
			ts.fontSize = (fontSize < 0 ? Style.Theme.DefaultFontSize : fontSize);
			ts.horizontalOverflow = HorizontalWrapMode.Overflow;
			ts.verticalOverflow = VerticalWrapMode.Overflow;

			var size = Size.Zero;

			foreach (var line in s.Split('\n'))
			{
				size.Width = Math.Max(size.Width, tg_.GetPreferredWidth(line, ts));
				size.Height += tg_.GetPreferredHeight(line, ts);
			}

			return size;
		}

		public static Size FitText(Font font, int fontSize, string s, Size maxSize)
		{
			var ts = ts_;
			ts.font = font ?? Style.Theme.DefaultFont;
			ts.fontSize = (fontSize < 0 ? Style.Theme.DefaultFontSize : fontSize);

			var extents = new Vector2();

			if (maxSize.Width == Widget.DontCare)
			{
				extents.x = 10000;
				ts.horizontalOverflow = HorizontalWrapMode.Overflow;
			}
			else
			{
				extents.x = maxSize.Width;
				ts.horizontalOverflow = HorizontalWrapMode.Wrap;
			}

			if (maxSize.Height == Widget.DontCare)
			{
				extents.y = 10000;
				ts.verticalOverflow = VerticalWrapMode.Overflow;
			}
			else
			{
				extents.y = maxSize.Height;
				ts.verticalOverflow = VerticalWrapMode.Truncate;
			}

			ts.generationExtents = extents;
			ts.generateOutOfBounds = false;
			ts.resizeTextForBestFit = false;

			var size = Size.Zero;

			foreach (var line in s.Split('\n'))
			{
				size.Width = Math.Max(size.Width, tg_.GetPreferredWidth(line, ts));
				size.Height += tg_.GetPreferredHeight(line, ts);
			}

			if (maxSize.Width != Widget.DontCare)
				size.Width = Math.Min(size.Width, maxSize.Width);

			if (maxSize.Height != Widget.DontCare)
				size.Height = Math.Min(size.Height, maxSize.Height);

			return size;
		}
	}
}
