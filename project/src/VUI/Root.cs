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
				var t = Time.realtimeSinceStartup - start;

				Glue.LogVerbose($"layout {Name}: {t:0.000:}s");

				dirty_ = false;
			}
		}

		public void ForceRelayout(string why)
		{
			SetDirty(true, why);
		}

		protected override void NeedsLayoutImpl(string why)
		{
			if (!dirty_)
			{
				Glue.LogVerbose($"{Name} needs layout: {why}");
				dirty_ = true;
			}
		}

		public override void OnPointerDownInternal(PointerEventData d, bool source)
		{
			GetRoot().SetFocus(this);
			base.OnPointerDownInternal(d, source);
		}

		protected override void SetDirty(bool b, string why = "")
		{
			base.SetDirty(b, why);
			dirty_ = b;
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


	class BadInit : Exception { }


	public class Root
	{
		public const int FocusDefault = 0x0;
		public const int FocusKeepPopup = 0x01;

		private IRootSupport support_ = null;
		private bool ready_ = false;

		static private TextGenerator tg_ = null;
		static private TextGenerationSettings ts_ = new TextGenerationSettings();

		public delegate void FocusHandler(Widget blurred, Widget focused);
		public event FocusHandler FocusChanged;

		private Rectangle bounds_;
		private Insets margins_ = new Insets(5);
		private RootPanel content_;
		private RootPanel floating_;
		private Overlay overlay_ = null;
		private TooltipManager tooltips_;
		private bool visible_ = true;
		private GameObject rootObject_ = null;

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

		public Root(MVRScript s, string prefix="")
		{
			CheckInit(s, prefix);
			Create(new ScriptUIRootSupport(s));
		}

		public Root(MVRScriptUI sui, string prefix="")
		{
			CheckInit(null, prefix);
			Create(new ScriptUIRootSupport(sui));
		}

		public Root(IRootSupport support, string prefix="")
		{
			CheckInit(null, prefix);
			Create(support);
		}

		private void CheckInit(MVRScript s, string prefix)
		{
			if (Glue.Initialized)
				return;

			if (s == null)
			{
				SuperController.LogError(
					"vui: must call Init() manually when Root is not created " +
					"from a MVRScript or a MVRScriptUI");

				throw new BadInit();
			}
			else
			{
				Init(s, prefix);
			}
		}

		private void Create(IRootSupport support)
		{
			if (TimerManager.Instance == null)
				ownTm_ = new TimerManager();

			content_ = new RootPanel(this, "content");
			floating_ = new RootPanel(this, "floating");
			tooltips_ = new TooltipManager(this);

			content_.Clickthrough = false;

			AttachTo(support);
		}

		public static void Init(MVRScript script, string prefix="")
		{
			if (string.IsNullOrEmpty(prefix))
				prefix = script.name;

			Init(prefix , () => script.manager, null, null, null, null, null);
		}

		public static void Init(
			string prefix,
			Glue.PluginManagerDelegate getPluginManager,
			Glue.StringDelegate getString = null,
			Glue.LogDelegate logVerbose = null,
			Glue.LogDelegate logInfo = null,
			Glue.LogDelegate logWarning = null,
			Glue.LogDelegate logError = null)
		{
			Glue.InitInternal(
				prefix, getPluginManager, getString,
				logVerbose, logInfo, logWarning, logError);

			BasicRootSupport.Cleanup();
		}

		public IRootSupport RootSupport
		{
			get { return support_; }
		}

		public Transform WidgetParentTransform
		{
			get { return rootObject_.transform; }
		}

		public void SetOpenedPopup(UIPopup p)
		{
			openedPopup_ = p;
		}

		public void SetFocus(Widget w, int flags = FocusDefault)
		{
			if (focused_ == w)
				return;

			Widget oldFocus = focused_;

			if (focused_ != null)
				focused_.OnBlurInternal(w);

			focused_ = w;

			if (focused_ != null)
				focused_.OnFocusInternal(oldFocus);

			// used by the filter textbox in the combobox so clicking it doesn't
			// close the combobox
			if (!Bits.IsSet(flags, FocusKeepPopup) && openedPopup_ != null)
			{
				if (openedPopup_.visible)
					openedPopup_.Toggle();

				openedPopup_ = null;
			}

			FocusChanged?.Invoke(oldFocus, focused_);
		}

		public void AttachTo(IRootSupport support)
		{
			Glue.LogVerbose($"root: attaching to {support}");

			support_ = support;

			if (support_.Init())
			{
				Glue.LogVerbose("root: support is already ready");
				OnSupportReady();
			}
			else
			{
				Glue.LogVerbose("root: support is not ready, creating timer");
				checkReadyTimer_ = TimerManager.Instance.CreateTimer(
					0.5f, CheckSupportReady, Timer.Repeat);
			}
		}

		private void CheckSupportReady()
		{
			if (support_.Init())
			{
				Glue.LogVerbose("root: support is finally ready");
				checkReadyTimer_.Destroy();
				checkReadyTimer_ = null;
				OnSupportReady();
				Glue.LogVerbose("root: all done");
			}
		}

		private void OnSupportReady()
		{
			ready_ = true;

			rootObject_ = new GameObject("RootObject");
			rootObject_.transform.SetParent(support_.RootParent, false);

			var rt = rootObject_.AddComponent<RectTransform>();
			Utilities.SetRectTransform(rootObject_, support_.Bounds);
			rootObject_.AddComponent<LayoutElement>().ignoreLayout = true;

			SupportBoundsChanged();

			var text = SuperController.singleton.GetComponentInChildren<Text>();
			if (text == null)
			{
				Glue.LogError($"no text component in supercontroller");
			}
			else
			{
				tg_ = text.cachedTextGenerator;
				ts_ = text.GetGenerationSettings(new Vector2());
			}

			Update(true);
		}

		public void Destroy()
		{
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
			get
			{
				return visible_;
			}

			set
			{
				if (visible_ != value)
				{
					visible_ = value;

					support_.SetActive(visible_);
					rootObject_?.SetActive(visible_);

					if (visible_)
					{
						content_.NeedsLayout("root visibility changed");
						floating_.NeedsLayout("root visibility changed");
					}
				}
			}
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
			try
			{
				support_?.Update(Time.deltaTime);

				if (ownTm_ != null)
				{
					ownTm_.TickTimers(Time.deltaTime);
					ownTm_.CheckTimers();
				}

				if (ready_)
				{
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
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
			}
		}

		public void SupportBoundsChanged()
		{
			bounds_ = new Rectangle(0, 0, support_.Bounds.Size);
			Glue.LogVerbose($"root: bounds {bounds_}");

			content_.SetBounds(bounds_);
			floating_.SetBounds(bounds_);

			ForceRelayout("support bounds changed");
		}

		public void ForceRelayout(string why = "")
		{
			content_.ForceRelayout(why);
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
			return support_.ToLocal(v);
		}

		public Point MousePosition
		{
			get
			{
				return ToLocal(Input.mousePosition);
			}
		}

		public Widget WidgetAt(Point p)
		{
			var w = FloatingPanel.WidgetAt(p);
			if (w != null)
				return w;

			w = ContentPanel.WidgetAt(p);
			if (w != null)
				return w;

			return null;
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

		static private TextGenerator GetTG()
		{
			if (tg_ == null)
				tg_ = new TextGenerator();

			return tg_;
		}

		static private TextGenerationSettings GetTS()
		{
			return ts_;
		}

		public static float TextLength(Font font, int fontSize, FontStyle fontStyle, string s)
		{
			var ts = GetTS();
			ts.font = font ?? Style.Theme.DefaultFont;
			ts.fontSize = (fontSize < 0 ? Style.Theme.DefaultFontSize : fontSize);
			ts.fontStyle = fontStyle;
			ts.horizontalOverflow = HorizontalWrapMode.Overflow;
			ts.verticalOverflow = VerticalWrapMode.Overflow;
			ts.generateOutOfBounds = false;
			ts.resizeTextForBestFit = false;

			return GetTG().GetPreferredWidth(s, ts);
		}

		public static Size TextSize(Font font, int fontSize, FontStyle fontStyle, string s)
		{
			var ts = GetTS();
			ts.font = font ?? Style.Theme.DefaultFont;
			ts.fontSize = (fontSize < 0 ? Style.Theme.DefaultFontSize : fontSize);
			ts.fontStyle = fontStyle;
			ts.horizontalOverflow = HorizontalWrapMode.Overflow;
			ts.verticalOverflow = VerticalWrapMode.Overflow;

			var size = Size.Zero;

			foreach (var line in s.Split('\n'))
			{
				// todo: line spacing, but this is broken, really
				if (size.Height > 0)
					size.Height += 1;

				size.Width = Math.Max(size.Width, GetTG().GetPreferredWidth(line, ts));

				if (line == "")
					size.Height += GetTG().GetPreferredHeight("W", ts);
				else
					size.Height += GetTG().GetPreferredHeight(line, ts);
			}

			return size;
		}

		public static Size FitText(Font font, int fontSize, FontStyle fontStyle, string s, Size maxSize)
		{
			var ts = GetTS();
			ts.font = font ?? Style.Theme.DefaultFont;
			ts.fontSize = (fontSize < 0 ? Style.Theme.DefaultFontSize : fontSize);
			ts.fontStyle = fontStyle;

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

			ts.textAnchor = TextAnchor.UpperLeft;
			ts.pivot = Vector2.zero;
			ts.richText = false;
			ts.generationExtents = extents;
			ts.generateOutOfBounds = false;
			ts.resizeTextForBestFit = false;

			var size = Size.Zero;

			foreach (var line in s.Split('\n'))
			{
				// todo: line spacing, but this is broken, really
				if (size.Height > 0)
					size.Height += 1;

				size.Width = Math.Max(size.Width, GetTG().GetPreferredWidth(line, ts));

				if (line == "")
					size.Height += GetTG().GetPreferredHeight("W", ts);
				else
					size.Height += GetTG().GetPreferredHeight(line, ts);
			}

			if (maxSize.Width != Widget.DontCare)
				size.Width = Math.Min(size.Width, maxSize.Width);

			if (maxSize.Height != Widget.DontCare)
				size.Height = Math.Min(size.Height, maxSize.Height);

			return size;
		}
	}
}
