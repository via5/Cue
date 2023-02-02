using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VUI
{
	abstract class Widget : IDisposable, IWidget
	{
		public virtual string TypeName { get { return "Widget"; } }

		public delegate void Callback();
		public event Callback Created;

		public const float DontCare = -1;

		private Widget parent_ = null;
		private string name_ = "";
		private readonly List<Widget> children_ = new List<Widget>();
		private Layout layout_ = null;
		private Rectangle bounds_ = new Rectangle();
		private Size minSize_ = new Size(DontCare, DontCare);
		private Size maxSize_ = new Size(DontCare, DontCare);
		private bool fixedBounds_ = false;

		private GameObject mainObject_ = null;
		private GameObject widgetObject_ = null;
		private GameObject graphicsObject_ = null;
		private WidgetBorderGraphics borderGraphics_ = null;

		private bool render_ = true;
		private bool visible_ = true;
		private bool enabled_ = true;
		private Insets margins_ = new Insets();
		private Insets borders_ = new Insets();
		private Insets padding_ = new Insets();
		private Color borderColor_ = Style.Theme.BorderColor;
		private Font font_ = null;
		private FontStyle fontStyle_ = FontStyle.Normal;
		private int fontSize_ = -1;
		private Color textColor_ = Style.Theme.TextColor;
		private readonly Tooltip tooltip_;
		private Events events_ = new Events();
		private bool wantsFocus_ = true;

		private bool dirty_ = true;


		public Widget(string name = "")
		{
			name_ = name;
			tooltip_ = new Tooltip();
		}

		public virtual void Dispose()
		{
			Destroy();
		}

		protected virtual void Destroy()
		{
			foreach (var c in children_)
				c.Destroy();

			if (mainObject_ != null)
			{
				UnityEngine.Object.Destroy(mainObject_);
				mainObject_ = null;
				widgetObject_ = null;
				graphicsObject_ = null;
				borderGraphics_ = null;
			}
		}

		public static string S(string s, params object[] ps)
		{
			return Glue.GetString(s, ps);
		}

		public Layout Layout
		{
			get
			{
				return layout_;
			}

			set
			{
				layout_ = value;

				if (layout_ != null)
					layout_.Parent = this;

				NeedsLayout("layout changed");
			}
		}

		public Font Font
		{
			get
			{
				return font_;
			}

			set
			{
				if (font_ != value)
				{
					font_ = value;
					NeedsLayout("font changed");
				}
			}
		}

		public FontStyle FontStyle
		{
			get
			{
				return fontStyle_;
			}

			set
			{
				if (fontStyle_ != value)
				{
					fontStyle_ = value;
					NeedsLayout("font style changed");
				}
			}
		}

		public int FontSize
		{
			get
			{
				return fontSize_;
			}

			set
			{
				if (fontSize_ != value)
				{
					fontSize_ = value;
					NeedsLayout("font size changed");
				}
			}
		}

		public Color TextColor
		{
			get
			{
				return textColor_;
			}

			set
			{
				if (textColor_ != value)
				{
					textColor_ = value;
					Polish();
				}
			}
		}

		public GameObject MainObject
		{
			get { return mainObject_; }
		}

		public GameObject WidgetObject
		{
			get { return widgetObject_; }
		}

		public Events Events
		{
			get { return events_; }
		}

		public bool WantsFocus
		{
			get { return wantsFocus_; }
			set { wantsFocus_ = value; }
		}

		public bool Render
		{
			get
			{
				return render_;
			}

			set
			{
				if (render_ != value)
				{
					render_ = value;
					UpdateRenderState();
				}
			}
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
					UpdateActiveState();

					if (!visible_)
						NeedsLayout("visibility changed to hidden", true);
				}
			}
		}

		public virtual Widget WidgetAt(Point p)
		{
			if (!IsVisibleOnScreen())
				return null;

			if (AbsoluteClientBounds.Contains(p))
			{
				for (int i = 0; i < children_.Count; ++i)
				{
					var w = children_[i].WidgetAt(p);
					if (w != null)
						return w;
				}

				if (!IsTransparent())
					return this;
			}

			return null;
		}

		public bool HasParent(Widget w)
		{
			if (w == null)
				return false;

			if (w == this)
				return true;

			return Parent?.HasParent(w) ?? false;
		}

		protected virtual bool IsTransparent()
		{
			return false;
		}

		protected virtual void UpdateActiveState()
		{
			if (mainObject_ != null)
				mainObject_.SetActive(visible_);

			if (render_ && visible_)
			{
				var dirtyChild = AnyDirtyChild();
				if (dirtyChild != null)
				{
					NeedsLayout(
						"visibility changed, dirty child:\n" +
						dirtyChild.DebugLine);
				}
			}
		}

		private Widget AnyDirtyChild()
		{
			if (!visible_)
				return null;

			if (dirty_)
				return this;

			foreach (var c in children_)
			{
				var w = c.AnyDirtyChild();
				if (w != null)
					return w;
			}

			return null;
		}

		public bool IsVisibleOnScreen()
		{
			if (mainObject_ == null)
				return render_ && visible_;
			else
				return mainObject_.activeInHierarchy;
		}

		public bool Enabled
		{
			get
			{
				if (!enabled_)
					return false;

				if (parent_ != null)
					return parent_.Enabled;

				return true;
			}

			set
			{
				if (enabled_ != value)
				{
					enabled_ = value;

					if (widgetObject_ != null)
					{
						DoSetEnabled(enabled_);
						PolishRecursive();
					}
				}
			}
		}

		private void PolishRecursive()
		{
			if (WidgetObject != null)
				Polish();

			foreach (var c in children_)
				c.PolishRecursive();
		}

		public void Polish()
		{
			if (WidgetObject != null)
				DoPolish();
		}

		protected virtual void DoPolish()
		{
			// no-op
		}

		public bool StrictlyEnabled
		{
			get
			{
				return enabled_;
			}
		}

		public Insets Margins
		{
			get
			{
				return margins_;
			}

			set
			{
				if (margins_ != value)
				{
					margins_ = value;
					NeedsLayout("margins changed");
				}
			}
		}

		public Insets Borders
		{
			get
			{
				return borders_;
			}

			set
			{
				if (borders_ != value)
				{
					borders_ = value;

					if (borderGraphics_ != null)
						borderGraphics_.Borders = value;

					NeedsLayout("borders changed");
				}
			}
		}

		public Insets Padding
		{
			get
			{
				return padding_;
			}

			set
			{
				if (padding_ != value)
				{
					padding_ = value;
					NeedsLayout("padding changed");
				}
			}
		}

		public Insets Insets
		{
			get { return margins_ + borders_ + padding_; }
		}

		public Color BorderColor
		{
			get
			{
				return borderColor_;
			}

			set
			{
				borderColor_ = value;

				if (borderGraphics_ != null)
					borderGraphics_.Color = value;
			}
		}

		public Tooltip Tooltip
		{
			get { return tooltip_; }
		}

		public Rectangle Bounds
		{
			get { return new Rectangle(bounds_); }
		}

		public bool FixedBounds()
		{
			if (fixedBounds_)
				return true;

			return parent_?.FixedBounds() ?? false;
		}

		public bool StrictlyFixedBounds
		{
			get { return fixedBounds_; }
		}

		public Rectangle AbsoluteClientBounds
		{
			get
			{
				var r = new Rectangle(Bounds);

				r.Deflate(Margins);
				r.Deflate(Borders);
				r.Deflate(Padding);

				return r;
			}
		}

		public Rectangle ClientBounds
		{
			get
			{
				var r = new Rectangle(0, 0, Bounds.Size);

				r.Deflate(Margins);
				r.Deflate(Borders);
				r.Deflate(Padding);

				return r;
			}
		}

		public Rectangle RelativeBounds
		{
			get
			{
				var r = new Rectangle(Bounds);

				if (parent_ != null)
					r.Translate(-parent_.Bounds.TopLeft);

				return r;
			}
		}

		public void SetBounds(Rectangle r, bool isFixed = false)
		{
			bounds_ = r;
			fixedBounds_ = isFixed;
		}

		public List<Widget> Children
		{
			get { return new List<Widget>(children_); }
		}

		public Widget Parent
		{
			get { return parent_; }
		}

		public Size GetRealPreferredSize(float maxWidth, float maxHeight)
		{
			var s = new Size();

			if (layout_ != null)
				s = layout_.GetPreferredSize(maxWidth, maxHeight);

			s = Size.Max(s, DoGetPreferredSize(maxWidth, maxHeight));
			s = Size.Max(s, GetRealMinimumSize());

			if (maxSize_.Width >= 0)
				s.Width = Math.Min(s.Width, maxSize_.Width);

			if (maxSize_.Height >= 0)
				s.Height = Math.Min(s.Height, maxSize_.Height);

			s += Margins.Size + Borders.Size + Padding.Size;

			if (maxWidth != DontCare)
				s.Width = Math.Min(maxWidth, s.Width);

			if (maxHeight != DontCare)
				s.Height = Math.Min(maxHeight, s.Height);

			return s;
		}

		public Size GetRealMinimumSize()
		{
			return Size.Max(DoGetMinimumSize(), minSize_);
		}

		public Size MinimumSize
		{
			get
			{
				return minSize_;
			}

			set
			{
				if (minSize_ != value)
				{
					NeedsLayout("min size changed");
					minSize_ = value;
				}
			}
		}

		public Size MaximumSize
		{
			get
			{
				return maxSize_;
			}

			set
			{
				if (maxSize_ != value)
				{
					maxSize_ = value;
					NeedsLayout("max size changed");
				}
			}
		}

		public string Name
		{
			get
			{
				return name_;
			}

			set
			{
				name_ = value;
			}
		}

		public virtual Root GetRoot()
		{
			if (parent_ != null)
				return parent_.GetRoot();

			return null;
		}

		public void Focus()
		{
			DoFocus();
		}

		protected virtual void DoFocus()
		{
			// no-op
		}

		public override string ToString()
		{
			if (string.IsNullOrEmpty(name_))
				return $"{TypeName}";
			else
				return $"{TypeName}.{name_}";
		}

		public virtual string DebugLine
		{
			get
			{
				var list = new List<string>();

				list.Add(TypeName);
				list.Add(name_);
				list.Add("b=" + Bounds.ToString());
				list.Add("rb=" + RelativeBounds.ToString());
				list.Add("ps=" + GetRealPreferredSize(DontCare, DontCare).ToString());
				list.Add("ly=" + (Layout?.TypeName ?? "none"));
				list.Add("r=" + render_.ToString());
				list.Add("v=" + visible_.ToString());
				list.Add("d=" + dirty_.ToString());

				return string.Join(" ", list.ToArray());
			}
		}


		public void AddGeneric(IWidget w, LayoutData d = null)
		{
			Add((Widget)w, d);
		}

		public T Add<T>(T w, LayoutData d = null)
			where T : Widget
		{
			if (w.parent_ != null)
				Glue.LogWarningST("widget already has a parent");

			w.parent_ = this;
			children_.Add(w);
			layout_?.Add(w, d);
			NeedsLayout("widget added (" + w.TypeName + ")");
			return w;
		}

		public void Remove(Widget w)
		{
			if (!children_.Remove(w))
			{
				Glue.LogError(
					"can't remove widget '" + w.Name + "' from " +
					"'" + Name + "', not found");

				return;
			}

			layout_.Remove(w);
			w.parent_ = null;

			NeedsLayout("widget removed (" + w.TypeName + ")");

			w.Destroy();
		}

		public void Remove()
		{
			if (parent_ == null)
			{
				Glue.LogError("can't remove '" + Name + ", no parent");
				return;
			}

			parent_.Remove(this);
		}

		public void RemoveAllChildren()
		{
			while (children_.Count > 0)
				Remove(children_[0]);
		}

		public void BringToTop()
		{
			if (widgetObject_ != null)
				Utilities.BringToTop(widgetObject_);
		}

		public void DoLayout()
		{
			DoLayoutImpl();
			Create();
			UpdateBounds();
		}

		protected void DoLayoutImpl()
		{
			layout_?.DoLayout();

			foreach (var w in children_)
			{
				if (w.IsVisibleOnScreen())
					w.DoLayoutImpl();
			}

			SetDirty(false);
		}

		public virtual void Create()
		{
			bool created = false;

			if (mainObject_ == null)
			{
				created = true;

				mainObject_ = new GameObject(ToString());
				mainObject_.AddComponent<RectTransform>();
				mainObject_.AddComponent<LayoutElement>();
				mainObject_.AddComponent<MouseCallbacks>().Widget = this;

				if (parent_?.MainObject == null)
					mainObject_.transform.SetParent(GetRoot().WidgetParentTransform, false);
				else
					mainObject_.transform.SetParent(parent_.MainObject.transform, false);

				widgetObject_ = CreateGameObject();
				widgetObject_.AddComponent<MouseCallbacks>().Widget = this;
				widgetObject_.transform.SetParent(mainObject_.transform, false);

				DoCreate();
				DoSetEnabled(enabled_);

				graphicsObject_ = new GameObject("WidgetBorders");
				graphicsObject_.transform.SetParent(mainObject_.transform, false);

				borderGraphics_ = graphicsObject_.AddComponent<WidgetBorderGraphics>();
				borderGraphics_.Borders = borders_;
				borderGraphics_.Color = borderColor_;

				UpdateActiveState();
				SetBackground();
			}

			foreach (var w in children_)
				w.Create();

			UpdateRenderState();

			if (created)
				Created?.Invoke();
		}

		private void UpdateRenderState()
		{
			SetRender(render_);
		}

		private void SetRender(bool b)
		{
			if (widgetObject_ == null || !visible_)
				return;

			if (!borders_.Empty)
				borderGraphics_?.gameObject?.SetActive(b);

			DoSetRender(b);

			foreach (var c in children_)
				c.SetRender(b && c.render_);
		}

		protected virtual void DoSetRender(bool b)
		{
		}

		private void SetMainObjectBounds()
		{
			var r = RelativeBounds;
			Utilities.SetRectTransform(mainObject_, r);

			var layoutElement = mainObject_.GetComponent<LayoutElement>();
			layoutElement.minWidth = r.Width;
			layoutElement.preferredWidth = r.Width;
			layoutElement.flexibleWidth = r.Width;
			layoutElement.minHeight = r.Height;
			layoutElement.preferredHeight = r.Height;
			layoutElement.flexibleHeight = r.Height;
			layoutElement.ignoreLayout = true;
		}

		private void SetBackground()
		{
		}

		private void SetBorderBounds()
		{
			var r = new Rectangle(0, 0, Bounds.Size);
			r.Deflate(Margins);

			Utilities.SetRectTransform(borderGraphics_, r);
		}

		protected virtual void SetWidgetObjectBounds()
		{
			Utilities.SetRectTransform(widgetObject_, ClientBounds);
		}

		public virtual void UpdateBounds()
		{
			SetBackground();

			SetMainObjectBounds();
			SetBorderBounds();
			SetWidgetObjectBounds();

			foreach (var w in children_)
				w.UpdateBounds();

			UpdateActiveState();
		}

		public void NeedsLayout(string why, bool force = false)
		{
			if (parent_ != null && parent_.Layout is AbsoluteLayout)
				return;

			if (force || IsVisibleOnScreen())
				NeedsLayoutImpl(TypeName + ": " + why);
			else
				SetDirty(true, TypeName + ": " + why);
		}

		protected virtual void SetDirty(bool b, string why = "")
		{
			dirty_ = b;

			if (why != "")
				Glue.LogVerbose("SetDirty: " + why);
		}

		protected virtual void NeedsLayoutImpl(string why)
		{
			if (parent_ != null)
				parent_.NeedsLayoutImpl(why);
		}

		public void Dump()
		{
			Glue.LogVerbose(DumpString());
		}

		public string DumpString()
		{
			var lines = new List<string>();

			var p = Parent;
			while (p != null)
			{
				lines.Insert(0, p.DebugLine);
				p = p.Parent;
			}

			for (int i = 0; i < lines.Count; ++i)
				lines[i] = new string(' ', i * 2) + lines[i];

			int indent = lines.Count;

			lines.Add(new string(' ', indent * 2)  + DebugLine + "    *** <-");

			DumpChildren(lines, indent + 1);

			return string.Join("\n", lines.ToArray());
		}

		private void DumpChildren(List<string> lines, int indent)
		{
			foreach (var w in children_)
			{
				lines.Add(new string(' ', indent * 2) + w.DebugLine);
				w.DumpChildren(lines, indent + 1);
			}
		}


		public float TextLength(string s)
		{
			return Root.TextLength(Font, FontSize, FontStyle, s);
		}

		public Size TextSize(string s)
		{
			return Root.TextSize(Font, FontSize, FontStyle, s);
		}

		public Size FitText(string s, Size maxSize)
		{
			return Root.FitText(Font, FontSize, FontStyle, s, maxSize);
		}


		protected virtual GameObject CreateGameObject()
		{
			var o = new GameObject("Widget");
			o.AddComponent<RectTransform>();
			o.AddComponent<LayoutElement>();
			return o;
		}

		protected virtual void DoCreate()
		{
			// no-op
		}

		protected virtual void DoSetEnabled(bool b)
		{
			// no-op
		}

		protected virtual Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(DontCare, DontCare);
		}

		protected virtual Size DoGetMinimumSize()
		{
			return new Size(DontCare, DontCare);
		}


		public void OnFocusInternal(Widget w)
		{
			events_.FireFocus(w);
		}

		public void OnBlurInternal(Widget w)
		{
			events_.FireBlur(w);
		}

		public void OnPointerEnterInternal(PointerEventData d)
		{
			GetRoot()?.WidgetEntered(this);
			events_.FirePointerEnter(this, d);
		}

		public void OnPointerExitInternal(PointerEventData d)
		{
			GetRoot()?.WidgetExited(this);
			events_.FirePointerExit(this, d);
		}

		public virtual void OnPointerDownInternal(PointerEventData d, bool setFocus=true)
		{
			if (setFocus)
			{
				GetRoot()?.PointerDown(this);

				if (WantsFocus)
				{
					GetRoot().SetFocus(this);
					setFocus = false;
				}
			}

			bool bubble = events_.FirePointerDown(this, d);
			if (bubble && parent_ != null)
				parent_.OnPointerDownInternal(d, setFocus);
		}

		public void OnPointerUpInternal(PointerEventData d)
		{
			bool bubble = events_.FirePointerUp(this, d);
			if (bubble && parent_ != null)
				parent_.OnPointerUpInternal(d);
		}

		public void OnPointerClickInternal(PointerEventData d)
		{
			bool bubble = events_.FirePointerClick(this, d);
			if (bubble && parent_ != null)
				parent_.OnPointerClickInternal(d);
		}

		public void OnPointerMoveInternal()
		{
			bool bubble = events_.FirePointerMove(this, null);
			if (bubble && parent_ != null)
				parent_.OnPointerMoveInternal();
		}

		public void OnBeginDragInternal(PointerEventData d)
		{
			events_.FireDragStart(this, d);
		}

		public void OnDragInternal(PointerEventData d)
		{
			events_.FireDrag(this, d);
		}

		public void OnEndDragInternal(PointerEventData d)
		{
			events_.FireDragEnd(this, d);
		}

		public void OnWheelInternal(PointerEventData d)
		{
			bool bubble = events_.FireWheel(this, d);
			if (bubble && parent_ != null)
				parent_.OnWheelInternal(d);
		}
	}
}
