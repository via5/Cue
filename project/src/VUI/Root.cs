using System;
using System.Collections.Generic;
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
			SetBounds(b);
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
		private bool dirty_ = true;

		public RootPanel(Root r, string name)
			: base(name)
		{
			root_ = r;
			Margins = new Insets(5);
		}

		public void Update(bool forceLayout)
		{
			if (dirty_ || forceLayout)
			{
				var start = Time.realtimeSinceStartup;

				DoLayout();
				Create();
				UpdateBounds();

				var t = Time.realtimeSinceStartup - start;

				Glue.LogVerbose($"layout {Name}: {t:0.000:}s");

				dirty_ = false;
			}
		}

		protected override void NeedsLayoutImpl(string why)
		{
			if (!dirty_)
			{
				Glue.LogVerbose($"{Name} needs layout: {why}");
				dirty_ = true;
			}
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


	class ScriptUIRootSupport : IRootSupport
	{
		private MVRScriptUI sui_;

		public ScriptUIRootSupport(MVRScript s)
			: this(s.UITransform.GetComponentInChildren<MVRScriptUI>())
		{
		}

		public ScriptUIRootSupport(MVRScriptUI sui)
		{
			sui_ = sui;
		}

		public MVRScriptUI ScriptUI
		{
			get { return sui_; }
		}

		public Canvas Canvas
		{
			get
			{
				return sui_.GetComponentInChildren<Image>()?.canvas;
			}
		}

		public Transform RootParent
		{
			get
			{
				return sui_.fullWidthUIContent;
			}
		}

		public void Destroy()
		{
			// no-op
		}
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
		private Canvas canvas_;

		private UIPopup openedPopup_ = null;
		private Widget focused_ = null;
		private Point lastMouse_ = new Point(-10000, -10000);
		private List<Widget> track_ = new List<Widget>();

		private TimerManager ownTm_ = null;
		private Timer checkReadyTimer_ = null;

		public static Point NoMousePos
		{
			get
			{
				return new Point(float.MaxValue, float.MaxValue);
			}
		}

		public Root(MVRScript s)
			: this(new ScriptUIRootSupport(s))
		{
		}

		public Root(MVRScriptUI sui)
			: this(new ScriptUIRootSupport(sui))
		{
		}

		public Root(IRootSupport support)
		{
			if (TimerManager.Instance == null)
				ownTm_ = new TimerManager();

			content_ = new RootPanel(this, "content");
			floating_ = new RootPanel(this, "floating");
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

			CheckSupportBounds();

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

		private void CheckSupportBounds()
		{
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

				if (scrollViewRT.rect.width <= 0 || scrollViewRT.rect.height <= 0)
				{
					Glue.LogVerbose("vui: scriptui not ready, starting timer");

					checkReadyTimer_ = TimerManager.Instance.CreateTimer(
						0.5f, CheckScriptUIReady, Timer.Repeat);
				}

				topOffset_ = scrollViewRT.offsetMin.y - scrollViewRT.offsetMax.y;

				bounds_ = Rectangle.FromPoints(
					1, 1, scrollViewRT.rect.width - 3, scrollViewRT.rect.height - 3);

				Style.SetupRoot(support_.ScriptUI);
			}

			content_.SetBounds(bounds_);
			floating_.SetBounds(bounds_);
		}

		private void CheckScriptUIReady()
		{
			var scrollView = support_.ScriptUI.GetComponentInChildren<ScrollRect>();
			var scrollViewRT = scrollView.GetComponent<RectTransform>();

			if (scrollViewRT.rect.width <= 0 || scrollViewRT.rect.height <= 0)
			{
				// still not ready
				return;
			}

			checkReadyTimer_?.Destroy();
			checkReadyTimer_ = null;

			Glue.LogVerbose("vui: scriptui ready, relayout");
			CheckSupportBounds();
			Update(true);
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

		public void TrackPointer(Widget w, bool b)
		{
			if (b)
				track_.Add(w);
			else
				track_.Remove(w);
		}

		public void Update(bool forceLayout=false)
		{
			if (ownTm_ != null)
			{
				ownTm_.TickTimers(Time.deltaTime);
				ownTm_.CheckTimers();
			}

			content_.Update(forceLayout);
			floating_.Update(forceLayout);

			var mp = MousePosition;

			if (track_.Count > 0 && mp != lastMouse_)
			{
				for (int i = 0; i < track_.Count; ++i)
				{
					var r = track_[i].AbsoluteClientBounds;
					if (r.Contains(mp))
						track_[i].OnPointerMoveInternal();
				}
			}

			lastMouse_ = mp;
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

		public Point ToLocal(Vector2 v)
		{
			if (canvas_ == null)
			{
				canvas_ = support_.Canvas;
				if (canvas_ == null)
					return NoMousePos;
			}

			Vector2 pp;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				canvas_.transform as RectTransform, v,
				canvas_.worldCamera, out pp);

			pp.x = bounds_.Left + bounds_.Width / 2 + pp.x;
			pp.y = bounds_.Top + (bounds_.Height - pp.y + topOffset_);

			return new Point(pp.x, pp.y);
		}

		public Point MousePosition
		{
			get
			{
				return ToLocal(Input.mousePosition);
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

		public void WidgetEntered(Widget w)
		{
			Tooltips.WidgetEntered(w);
		}

		public void WidgetExited(Widget w)
		{
			Tooltips.WidgetExited(w);
		}

		public void PointerDown(Widget w)
		{
			Tooltips.Hide();
		}

		public static float TextLength(Font font, int fontSize, string s)
		{
			var ts = ts_;
			ts.font = font ?? Style.Theme.DefaultFont;
			ts.fontSize = (fontSize < 0 ? Style.Theme.DefaultFontSize : fontSize);
			ts.horizontalOverflow = HorizontalWrapMode.Overflow;
			ts.verticalOverflow = VerticalWrapMode.Overflow;
			ts.generateOutOfBounds = false;
			ts.resizeTextForBestFit = false;

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
				// todo: line spacing, but this is broken, really
				if (size.Height > 0)
					size.Height += 1;

				size.Width = Math.Max(size.Width, tg_.GetPreferredWidth(line, ts));

				if (line == "")
					size.Height += tg_.GetPreferredHeight("W", ts);
				else
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
				// todo: line spacing, but this is broken, really
				if (size.Height > 0)
					size.Height += 1;

				size.Width = Math.Max(size.Width, tg_.GetPreferredWidth(line, ts));

				if (line == "")
					size.Height += tg_.GetPreferredHeight("W", ts);
				else
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
