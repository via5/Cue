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

		private UnityEngine.UI.Image graphics_ = null;

		public Overlay(Rectangle b)
		{
			SetBounds(b);
		}

		protected override void DoCreate()
		{
			base.DoCreate();

			graphics_ = MainObject.AddComponent<UnityEngine.UI.Image>();
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
				if (root_.RootSupport.RootParent.gameObject.activeInHierarchy)
				{
					var start = Time.realtimeSinceStartup;
					DoLayout();
					var t = Time.realtimeSinceStartup - start;

					Log.Verbose($"layout {Name}: {t:0.000:}s");

					dirty_ = false;
				}
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
				Log.Verbose($"{Name} needs layout: {why}");
				dirty_ = true;
			}
		}

		public override void OnPointerDownInternal(PointerEventData d, bool source)
		{
			Focus();
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
		static private TextGenerationSettings defaultTs_ = new TextGenerationSettings();

		public delegate void FocusHandler(Widget blurred, Widget focused);
		public event FocusHandler FocusChanged;

		private readonly Logger log_;
		private Rectangle bounds_;
		private Insets margins_ = new Insets(5);
		private RootPanel content_;
		private RootPanel floating_;
		private Overlay overlay_ = null;
		private TooltipManager tooltips_;
		private bool visible_ = true;
		private GameObject rootObject_ = null;

		private Widget openedPopupWidget_ = null;
		private UIPopup openedPopup_ = null;
		private Widget focused_ = null;
		private Widget captured_ = null;
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

		private Root(string prefix)
		{
			log_ = new Logger("vui.root." + prefix);
		}

		public Root(MVRScript s, string prefix="")
			: this(prefix)
		{
			CheckInit(s, prefix);
			Create(new ScriptUIRootSupport(s));
		}

		public Root(MVRScriptUI sui, string prefix="")
			: this(prefix)
		{
			CheckInit(null, prefix);
			Create(new ScriptUIRootSupport(sui));
		}

		public Root(IRootSupport support, string prefix="")
			: this(prefix)
		{
			CheckInit(null, prefix);
			Create(support);
		}

		public Logger Log
		{
			get { return log_; }
		}

		private void CheckInit(MVRScript s, string prefix)
		{
			if (Glue.Initialized)
				return;

			if (s == null)
			{
				SuperController.LogMessage(
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

			Init(prefix , () => script.manager, null, null, null, null, null, null);
		}

		public static void Init(
			string prefix,
			Glue.PluginManagerDelegate getPluginManager,
			Glue.StringDelegate getString = null,
			Glue.LogDelegate logVerbose = null,
			Glue.LogDelegate logInfo = null,
			Glue.LogDelegate logWarning = null,
			Glue.LogDelegate logError = null,
			Glue.IconProviderDelegate icons = null)
		{
			Glue.InitInternal(
				prefix, getPluginManager, getString,
				logVerbose, logInfo, logWarning, logError, icons);

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

		public void SetOpenedPopup(Widget w, UIPopup p)
		{
			if (w == null || openedPopupWidget_ == null || openedPopupWidget_ == w)
			{
				openedPopupWidget_ = w;
				openedPopup_ = p;
			}
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

				SetOpenedPopup(openedPopupWidget_, null);
			}

			FocusChanged?.Invoke(oldFocus, focused_);
		}

		public void SetCapture(Widget w)
		{
			if (captured_ == null)
				captured_ = w;
		}

		public void ReleaseCapture(Widget w)
		{
			if (captured_ == w)
			{
				Widget old = captured_;

				captured_ = null;
				UpdateTracking(true);

				var h = WidgetAt(MousePosition);

				if (old != null)
				{
					if (h == null || !h.HasParent(old))
						old.OnPointerExitInternalSynth();
				}

				if (h != null)
					h.OnPointerEnterInternalSynth();
			}
		}

		public Widget Captured
		{
			get { return captured_; }
		}

		public void AttachTo(IRootSupport support)
		{
			Log.Verbose($"root: attaching to {support}");

			support_ = support;

			if (support_.Init(this))
			{
				Log.Verbose("root: support is already ready");
				OnSupportReady();
			}
			else
			{
				Log.Verbose("root: support is not ready, creating timer");
				checkReadyTimer_ = TimerManager.Instance.CreateTimer(
					0.5f, CheckSupportReady, Timer.Repeat);
			}
		}

		private void CheckSupportReady()
		{
			if (support_.Init(this))
			{
				Log.Verbose("root: support is finally ready");
				checkReadyTimer_.Destroy();
				checkReadyTimer_ = null;
				OnSupportReady();
				Log.Verbose("root: all done");
			}
		}

		private void OnSupportReady()
		{
			ready_ = true;

			rootObject_ = new GameObject("RootObject");
			rootObject_.transform.SetParent(support_.RootParent, false);

			var rt = rootObject_.AddComponent<RectTransform>();
			Utilities.SetRectTransform(rootObject_.GetComponent<RectTransform>(), support_.Bounds);
			rootObject_.AddComponent<LayoutElement>().ignoreLayout = true;

			SupportBoundsChanged();

			var text = SuperController.singleton.GetComponentInChildren<Text>();
			if (text == null)
			{
				Log.Error($"no text component in supercontroller");
			}
			else
			{
				tg_ = text.cachedTextGenerator;
				defaultTs_ = text.GetGenerationSettings(new Vector2());
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

			SetOpenedPopup(null, null);
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
						floating_?.NeedsLayout("root visibility changed");
					}
				}
			}
		}

		public Widget Focused
		{
			get { return focused_; }
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
					floating_?.Update(forceLayout);

					UpdateTracking();
				}
			}
			catch (Exception e)
			{
				Log.ErrorST(e.ToString());
			}
		}

		private void UpdateTracking(bool force=false)
		{
			var mp = MousePosition;

			if (track_.Count > 0 && (mp != lastMouse_ || force))
			{
				for (int i = 0; i < track_.Count; ++i)
				{
					if (captured_ == null || captured_ == track_[i])
					{
						var r = track_[i].AbsoluteClientBounds;
						if (r.Contains(mp))
							track_[i].OnPointerMoveInternal();
					}
				}
			}

			lastMouse_ = mp;
		}

		public void SupportBoundsChanged()
		{
			bounds_ = new Rectangle(0, 0, support_.Bounds.Size);
			Log.Verbose($"root: bounds {bounds_}");

			content_.SetBounds(bounds_);
			floating_?.SetBounds(bounds_);

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

		public Rectangle ToLocal(Rectangle v)
		{
			var tl = support_.ToLocal(new Vector2(v.Left, v.Top));
			var br = support_.ToLocal(new Vector2(v.Right, v.Bottom));

			return new Rectangle(tl, br);
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
			if (openedPopup_ != null)
			{
				if (openedPopup_.visible)
				{
					var r = Utilities.RectTransformBounds(
						this, openedPopup_.popupPanel);

					if (r.Contains(p))
						return openedPopupWidget_;
				}
			}

			var w = FloatingPanel.WidgetAtInternal(p);
			if (w != null)
				return w;

			w = ContentPanel.WidgetAtInternal(p);
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

		static public TextGenerator GetTG()
		{
			if (tg_ == null)
				tg_ = new TextGenerator();

			return tg_;
		}

		static public TextGenerationSettings GetTS()
		{
			return defaultTs_;
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

		public static Size TextSize(
			Font font, int fontSize, FontStyle fontStyle, string s,
			Size maxSize, bool vertOverflow = false)
		{
			// much of this is the same as ClipTextEllipsis() below

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

				if (vertOverflow)
					ts.verticalOverflow = VerticalWrapMode.Overflow;
				else
					ts.verticalOverflow = VerticalWrapMode.Truncate;
			}

			ts.textAnchor = TextAnchor.UpperLeft;
			ts.pivot = Vector2.zero;
			ts.richText = false;
			ts.generationExtents = extents;
			ts.generateOutOfBounds = false;
			ts.resizeTextForBestFit = false;

			var size = Size.Zero;
			var lines = s.Split('\n');

			foreach (var line in lines)
			{
				// todo: line spacing, but this is broken, really
				if (size.Height > 0)
					size.Height += 1;

				size.Width = Math.Max(size.Width, GetWidth(line, ts));

				if (line == "")
					size.Height += GetHeight("W", ts);
				else
					size.Height += GetHeight(line, ts);
			}

			if (maxSize.Width != Widget.DontCare)
				size.Width = Math.Min(size.Width, maxSize.Width);

			if (!vertOverflow)
			{
				if (maxSize.Height != Widget.DontCare)
					size.Height = Math.Min(size.Height, maxSize.Height);
			}

			return size;
		}

		public static Size FitText(
			Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor,
			string s, Size maxSize, bool vertOverflow = false)
		{
			// much of this is the same as ClipTextEllipsis() below

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

				if (vertOverflow)
					ts.verticalOverflow = VerticalWrapMode.Overflow;
				else
					ts.verticalOverflow = VerticalWrapMode.Truncate;
			}

			ts.textAnchor = anchor;
			ts.alignByGeometry = false;
			ts.pivot = Vector2.zero;
			ts.richText = false;
			ts.generationExtents = extents;
			ts.generateOutOfBounds = false;
			ts.resizeTextForBestFit = false;
			ts.updateBounds = true;

			var size = Size.Zero;
			var tg = GetTG();

			float lineHeight = tg.GetPreferredHeight("W", ts);
			tg.PopulateWithErrors(s, ts, null);

			int lineCount = Math.Max(tg.lineCount, 1);

			if (tg.verts != null)
			{
				for (int i = 0; i < tg.verts.Count; ++i)
					size.Width = Math.Max(size.Width, tg.verts[i].position.x);
			}

			size.Height =  lineCount * lineHeight + (lineCount - 1) * ts.lineSpacing;

			if (maxSize.Width != Widget.DontCare)
				size.Width = Math.Min(size.Width, maxSize.Width);

			if (!vertOverflow)
			{
				if (maxSize.Height != Widget.DontCare)
					size.Height = Math.Min(size.Height, maxSize.Height);
			}

			return size;
		}

		public static string ClipTextEllipsis(
			Font font, int fontSize, FontStyle fontStyle, TextAnchor anchor,
			string s, Size maxSize, bool vertOverflow = false)
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

				if (vertOverflow)
					ts.verticalOverflow = VerticalWrapMode.Overflow;
				else
					ts.verticalOverflow = VerticalWrapMode.Truncate;
			}

			ts.textAnchor = anchor;
			ts.alignByGeometry = false;
			ts.pivot = Vector2.zero;
			ts.richText = false;
			ts.generationExtents = extents;
			ts.generateOutOfBounds = false;
			ts.resizeTextForBestFit = false;
			ts.updateBounds = true;

			var size = Size.Zero;
			var tg = GetTG();

			float lineHeight = tg.GetPreferredHeight("W", ts);
			float ellipsisWidth = tg.GetPreferredHeight("...", ts);

			tg.PopulateWithErrors(s, ts, null);

			// number of lines that fit in the given rectangle
			int lineCount = Math.Max(tg.lineCount, 1);

			// calculate the width of the generated text by finding the
			// right-most vertex; rectExtents seems to always contain whatever
			// was given in generationExents and so is useless
			if (tg.verts != null)
			{
				for (int i = 0; i < tg.verts.Count; ++i)
					size.Width = Math.Max(size.Width, tg.verts[i].position.x);
			}

			// height of the generated text, simpler than going through verts
			size.Height = lineCount * lineHeight + (lineCount - 1) * ts.lineSpacing;

			// clamp size to max
			if (maxSize.Width != Widget.DontCare)
				size.Width = Math.Min(size.Width, maxSize.Width);

			if (!vertOverflow)
			{
				if (maxSize.Height != Widget.DontCare)
					size.Height = Math.Min(size.Height, maxSize.Height);
			}

			var chars = tg.GetCharactersArray();
			var lines = tg.GetLinesArray();
			var verts = tg.verts;

			if (lines.Length > 0 && verts.Count > 0)
			{
				var ss = "";

				// all lines but the last one can be rendered in full
				for (int i = 0; i < (lines.Length - 1); ++i)
				{
					var beginChar = lines[i].startCharIdx;
					var endChar = lines[i + 1].startCharIdx;

					ss += s.Substring(beginChar, endChar - beginChar);
				}

				float lastLineWidth = 0;

				// get all the characters that fit in the last line
				{
					var lastLine = lines[lines.Length - 1];
					int beginChar = lastLine.startCharIdx;
					int endChar = -1;

					for (int j = beginChar; j < s.Length; ++j)
					{
						// vertex index of this character
						var vi = j * 4;

						// if the vertex index is larger than the count, it means
						// this character was not rendered and so the string must
						// cut there
						//
						// for whatever reason, it looks like there's always one
						// character too many
						if (vi >= (verts.Count - 4))
						{
							endChar = j;
							break;
						}

						// get the right-most vertex to find the width of this
						// line
						for (int k = 0; k < 4; ++k)
							lastLineWidth = Math.Max(lastLineWidth, verts[vi + k].position.x);
					}

					if (endChar == -1)
						endChar = s.Length;

					ss += s.Substring(beginChar, endChar - beginChar);
				}

				if (ss != s)
				{
					// if the string is different, it was cut and needs an
					// ellipsis

					// the last line doesn't necessarily end right at the edge
					// of the rectangle, unity word wraps correctly and could
					// have a line that ends short because it has a space
					// followed by a very long word
					//
					// if the ellipsis fits on the last line, just append it;
					// if not, cut the last three characters to make space
					//
					// always trim the end so the ellipsis doesn't look like
					// it's in the middle of nowhere

					if (lastLineWidth + ellipsisWidth < maxSize.Width)
						s = ss.TrimEnd() + "...";
					else if (ss.Length >= 3)
						s = ss.Substring(0, ss.Length - 3).TrimEnd() + "...";
					else
						s = "...";
				}
			}

			return s;
		}

		private static float GetWidth(string line, TextGenerationSettings ts)
		{
			return GetTG().GetPreferredWidth(line, ts);
		}

		private static float GetHeight(string line, TextGenerationSettings ts)
		{
			return GetTG().GetPreferredHeight(line, ts);
		}
	}
}
