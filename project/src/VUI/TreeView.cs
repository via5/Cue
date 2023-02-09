using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VUI
{
	class ScrollBarHandle : Button
	{
		public delegate void Handler();
		public event Handler Moved;

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
		}

		public void OnDrag(DragEvent e)
		{
			if (!dragging_)
				return;

			var p = e.Pointer;
			var delta = p - dragStart_;

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
			dragging_ = false;
			ReleaseCapture();
		}
	}


	class ScrollBar : Panel
	{
		public delegate void ValueHandler(float v);
		public event ValueHandler ValueChanged;

		private ScrollBarHandle handle_ = new ScrollBarHandle();
		private float range_ = 0;
		private float value_ = 0;

		public ScrollBar()
		{
			Borders = new Insets(1, 0, 0, 0);
			Layout = new AbsoluteLayout();
			Clickthrough = false;
			Add(handle_);

			Events.PointerDown += OnPointerDown;
			handle_.Moved += OnHandleMoved;
		}

		public float Range
		{
			get { return range_; }
			set { range_ = value; }
		}

		public float Value
		{
			get { return value_; }
			set { value_ = value; }
		}

		public override void UpdateBounds()
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

			base.UpdateBounds();
		}

		private void OnHandleMoved()
		{
			var r = ClientBounds;
			var hr = handle_.RelativeBounds;
			var top = hr.Top - Borders.Top;
			var h = r.Height - hr.Height;
			var p = (top / h);
			value_ = p * range_;
			ValueChanged?.Invoke(value_);
		}

		private void OnPointerDown(PointerEvent e)
		{
			var r = AbsoluteClientBounds;
			var p = e.Pointer - r.TopLeft;
			var y = r.Top + p.Y - handle_.ClientBounds.Height / 2;

			if (y < 0)
				y = 0;
			else if (y + handle_.ClientBounds.Height > ClientBounds.Height)
				y = ClientBounds.Height - handle_.ClientBounds.Height;

			var cb = handle_.AbsoluteClientBounds;
			var h = cb.Height;
			cb.Top = y;
			cb.Bottom = y + h;

			handle_.SetBounds(cb);
			DoLayoutImpl();
			base.UpdateBounds();

			OnHandleMoved();

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



	class TreeView : Panel
	{
		public override string TypeName { get { return "TreeView"; } }

		public class Item
		{
			public delegate void CheckedHandler(bool b);
			public event CheckedHandler CheckedChanged;

			private Item parent_ = null;
			private string text_;
			private List<Item> children_ = null;
			private bool expanded_ = false;
			private bool visible_ = true;
			private bool checkable_ = false;
			private bool checked_ = false;

			public Item(string text = "")
			{
				text_ = text;
			}

			public virtual TreeView TreeView
			{
				get
				{
					if (parent_ == null)
						return null;
					else
						return parent_.TreeView;
				}
			}

			public Item Parent
			{
				get { return parent_; }
				set { parent_ = value; }
			}

			public string Text
			{
				get
				{
					return text_;
				}

				set
				{
					string s;

					if (string.IsNullOrEmpty(value))
						s = "";
					else
						s = value;

					if (text_ != s)
					{
						text_ = s;
						NodesChanged();
					}
				}
			}

			public bool Checkable
			{
				get
				{
					return checkable_;
				}

				set
				{
					if (checkable_ != value)
					{
						checkable_ = value;
						NodesChanged();
					}
				}
			}

			public bool Checked
			{
				get
				{
					return checked_;
				}

				set
				{
					if (checked_ != value)
					{
						SetCheckedInternal(value);

						if (checkable_)
							NodesChanged();
					}
				}
			}

			public void SetCheckedInternal(bool b)
			{
				checked_ = b;

				if (checkable_)
					CheckedChanged?.Invoke(checked_);
			}

			public bool Visible
			{
				get
				{
					return visible_;
				}

				set
				{
					visible_ = value;
					VisibilityChanged();
				}
			}

			public List<Item> Children
			{
				get { return children_; }
			}

			public bool Expanded
			{
				get
				{
					return expanded_;
				}

				set
				{
					expanded_ = value;
					NodesChanged();
				}
			}

			public bool Selected
			{
				get
				{
					return TreeView?.Selected == this;
				}

				set
				{
					TreeView?.SetSelectedInternal(this, value);
				}
			}

			public void Toggle()
			{
				Expanded = !Expanded;
			}

			public void ExpandAll()
			{
				SetExpandedRecursive(true, false);
				NodesChanged();
			}

			public void ExpandAllVisible()
			{
				SetExpandedRecursive(true, true);
				NodesChanged();
			}

			public void SetAllVisible(bool b)
			{
				SetVisibleRecursiveInternal(b);
				VisibilityChanged();
			}

			public void SetVisibleRecursiveInternal(bool b)
			{
				visible_ = b;

				if (children_ != null)
				{
					for (int i = 0; i < children_.Count; ++i)
						children_[i].SetVisibleRecursiveInternal(b);
				}
			}

			private void NodesChanged()
			{
				TreeView?.NodesChangedInternal(this);
			}

			private void VisibilityChanged()
			{
				TreeView?.VisibilityChangedInternal(this);
			}

			private void SetExpandedRecursive(bool b, bool visibleOnly)
			{
				expanded_ = b;

				if (children_ != null)
				{
					for (int i = 0; i < children_.Count; ++i)
					{
						if (!visibleOnly || children_[i].Visible)
							children_[i].SetExpandedRecursive(b, visibleOnly);
					}
				}
			}

			public bool ShouldBeVisible(Func<Item, bool> matches)
			{
				// check user override first
				if (!visible_)
					return false;

				// todo: cache this, and it's really inefficient, keeps going
				// up and down

				if (matches == null)
				{
					// no filtering, always visible
					return true;
				}

				if (matches(this))
				{
					// item itself matches
					return true;
				}

				// item doesn't match, but show if any child matches
				if (children_ != null)
				{
					for (int i = 0; i < children_.Count; ++i)
					{
						if (children_[i].ShouldBeVisible(matches))
							return true;
					}
				}

				// item doesn't match, nor its children, but it might be visible
				// because its parent has to be
				var p = Parent;
				while (p != null)
				{
					if (matches(p))
						return true;

					p = p.Parent;
				}

				return false;
			}

			public virtual bool HasChildren
			{
				get { return children_ != null && children_.Count > 0; }
			}

			public void Add(Item child)
			{
				if (children_ == null)
					children_ = new List<Item>();

				children_.Add(child);
				child.Parent = this;
			}

			public void Clear()
			{
				if (children_ != null)
				{
					for (int i=0; i<children_.Count; ++i)
						children_[i].Parent = null;

					children_.Clear();
				}
			}

			public void FireCheckedChanged(bool b)
			{
				CheckedChanged?.Invoke(b);
			}

			public string DebugString()
			{
				string s = "";

				if (HasChildren)
					s += $"cs={children_.Count}";
				else
					s += $"cs=0";

				s += $" ex={expanded_} t='{text_}'";

				return s;
			}
		}


		class InternalRootItem : Item
		{
			private readonly TreeView tree_;

			public InternalRootItem(TreeView tree)
				: base("root")
			{
				tree_ = tree;
			}

			public override TreeView TreeView
			{
				get { return tree_; }
			}
		}


		class Node
		{
			private readonly TreeView tree_;
			private Item item_ = null;
			private Panel panel_ = null;
			private ToolButton toggle_ = null;
			private CheckBox checkbox_ = null;
			private Label label_ = null;
			private bool hovered_ = false;
			private bool ignore_ = false;

			public Node(TreeView t)
			{
				tree_ = t;
			}

			public Item Item
			{
				get { return item_; }
			}

			public bool Hovered
			{
				get { return hovered_; }
				set { hovered_ = value; UpdateState(); }
			}

			public void Clear()
			{
				Set(null, Rectangle.Zero, 0);
			}

			public void Set(Item i, Rectangle r, int indent)
			{
				try
				{
					ignore_ = true;
					DoSet(i, r, indent);
				}
				finally
				{
					ignore_ = false;
				}
			}

			private void DoSet(Item i, Rectangle r, int indent)
			{
				item_ = i;

				if (item_ == null)
				{
					if (panel_ != null)
						panel_.Render = false;
				}
				else
				{
					if (panel_ == null)
						CreatePanel(r);

					UpdatePanel(r);

					r.Left += indent * Style.Metrics.TreeIndentSize;

					if (item_.HasChildren)
					{
						if (toggle_ == null)
							CreateToggle(r);

						UpdateToggle(r);
					}
					else
					{
						if (toggle_ != null)
							toggle_.Render = false;
					}


					if (tree_.CheckBoxes)
					{
						if (checkbox_ == null && item_.Checkable)
							CreateCheckbox(r);

						UpdateCheckbox(r);
					}

					if (label_ == null)
						CreateLabel(r);

					UpdateLabel(r);
				}

				UpdateState();
			}

			private void CreatePanel(Rectangle r)
			{
				if (panel_ == null)
				{
					panel_ = new Panel("TreeViewNode", new AbsoluteLayout());
					tree_.Add(panel_);
					panel_.Create();
					panel_.Events.PointerClick += (e) => { e.Bubble = true; };
				}
			}

			private void CreateToggle(Rectangle r)
			{
				if (toggle_ == null)
				{
					toggle_ = new ToolButton("", OnToggle);
					panel_.Add(toggle_);
					toggle_.Create();
				}
			}

			private void CreateCheckbox(Rectangle r)
			{
				if (checkbox_ == null)
				{
					checkbox_ = new CheckBox("", OnChecked);
					panel_.Add(checkbox_);
					checkbox_.Create();
				}
			}

			private void CreateLabel(Rectangle r)
			{
				if (label_ == null)
				{
					label_ = new Label();
					label_.WrapMode = VUI.Label.Clip;
					panel_.Add(label_);
					label_.Create();
					label_.Events.PointerClick += (e) => { e.Bubble = true; };
				}
			}

			private void UpdatePanel(Rectangle r)
			{
				panel_.SetBounds(r);
				panel_.Render = true;
			}

			private void UpdateToggle(Rectangle r)
			{
				if (item_ == null)
					return;

				if (item_.Parent == tree_.RootItem && !tree_.RootToggles)
				{
					toggle_.Render = false;
				}
				else
				{
					if (item_.Expanded)
						toggle_.Text = "-";
					else
						toggle_.Text = "+";

					var tr = r;
					tr.Width = Style.Metrics.TreeToggleWidth;
					toggle_.SetBounds(tr);
					toggle_.Render = true;
				}
			}

			private void UpdateCheckbox(Rectangle r)
			{
				if (item_ == null)
					return;

				if (item_.Checkable)
				{
					var tr = r;

					if (item_.Parent != tree_.RootItem || tree_.RootToggles)
						tr.Left += Style.Metrics.TreeToggleWidth + Style.Metrics.TreeToggleSpacing;

					tr.Width = Style.Metrics.TreeToggleWidth;

					checkbox_.SetBounds(tr);
					checkbox_.Checked = item_.Checked;
					checkbox_.Render = true;
				}
				else
				{
					if (checkbox_ != null)
						checkbox_.Render = false;
				}
			}

			private void UpdateLabel(Rectangle r)
			{
				if (item_ == null)
					return;

				var lr = r;

				if (item_.Parent != tree_.RootItem || tree_.RootToggles)
					lr.Left += Style.Metrics.TreeToggleWidth + Style.Metrics.TreeToggleSpacing;

				if (tree_.CheckBoxes && item_.Checkable)
					lr.Left += Style.Metrics.TreeToggleWidth + Style.Metrics.TreeCheckboxSpacing;

				label_.Text = item_.Text;
				label_.SetBounds(lr);
			}

			public void UpdateState()
			{
				if (panel_ == null)
					return;

				if (item_ != null && hovered_)
					panel_.BackgroundColor = Style.Theme.HighlightBackgroundColor;
				else if (item_ != null && item_.Selected)
					panel_.BackgroundColor = Style.Theme.SelectionBackgroundColor;
				else
					panel_.BackgroundColor = new Color(0, 0, 0, 0);
			}

			private void OnToggle()
			{
				if (item_ != null)
					item_.Toggle();
			}

			private void OnChecked(bool b)
			{
				if (ignore_) return;

				if (item_ != null)
					item_.SetCheckedInternal(b);
			}
		}


		class NodeContext
		{
			public Rectangle av;
			public int nodeIndex;
			public int itemIndex;
			public float x, y;
			public int indent;
		}


		public delegate void ItemCallback(Item i);
		public event ItemCallback SelectionChanged;
		public event ItemCallback ItemClicked;

		private readonly InternalRootItem root_;
		private List<Node> nodes_ = new List<Node>();
		private ScrollBar vsb_ = new ScrollBar();
		private bool checkboxes_ = false;
		private bool rootToggles_ = true;
		private int topItemIndex_ = 0;
		private int itemCount_ = 0;
		private int visibleCount_ = 0;
		private bool ignoreVScroll_ = false;
		private Node hovered_ = null;
		private Item selected_ = null;
		private Timer staleTimer_ = null;
		private string filterString_ = "";
		private string filterStringLc_ = "";
		private Func<Item, bool> filterFunc_ = null;

		public TreeView()
		{
			root_ = new InternalRootItem(this);

			Borders = new Insets(1);
			Layout = new AbsoluteLayout();
			Clickthrough = false;

			Add(vsb_);

			Events.Wheel += OnWheel;
			Events.PointerMove += OnHover;
			Events.PointerExit += OnExit;
			Events.PointerClick += OnClick;
			Events.PointerDown += OnPointerDown;

			vsb_.ValueChanged += OnVerticalScroll;

			WantsFocus = true;
		}

		private float FullItemHeight
		{
			get
			{
				return
					Style.Metrics.TreeItemHeight +
					Style.Metrics.TreeItemSpacing;
			}
		}

		private float InternalPadding
		{
			get { return Style.Metrics.TreeInternalPadding; }
		}

		private void OnVerticalScroll(float v)
		{
			if (ignoreVScroll_) return;
			SetTopItem((int)(v / (FullItemHeight)), false);
		}

		private void SetTopItem(int index, bool updateSb, bool rebuild = true)
		{
			topItemIndex_ = Utilities.Clamp(index, 0, Math.Max(itemCount_ - visibleCount_, 0));

			if (updateSb)
			{
				float v = topItemIndex_ * (FullItemHeight);
				if (v + FullItemHeight > vsb_.Range)
					v = vsb_.Range;

				vsb_.Value = v;
			}

			if (rebuild)
				Rebuild();
		}

		public Item RootItem
		{
			get { return root_; }
		}

		public Item Selected
		{
			get { return selected_; }
		}

		public bool CheckBoxes
		{
			get { return checkboxes_; }
			set { checkboxes_ = value; }
		}

		public bool RootToggles
		{
			get { return rootToggles_; }
			set { rootToggles_ = value; }
		}

		public string Filter
		{
			get
			{
				return filterString_;
			}

			set
			{
				if (filterString_ != value)
				{
					filterString_ = value ?? "";
					filterStringLc_ = filterString_.ToLower();
					VisibilityChangedInternal(null);
				}
			}
		}

		public Func<Item, bool> FilterFunc
		{
			get
			{
				return filterFunc_;
			}

			set
			{
				filterFunc_ = value;
				VisibilityChangedInternal(null);
			}
		}

		public void ItemsChanged()
		{
			NodesChangedInternal(null);
		}

		public void NodesChangedInternal(Item i)
		{
			if (staleTimer_ == null && WidgetObject != null)
				staleTimer_ = Timer.Create(Timer.Immediate, OnStale);
		}

		public void VisibilityChangedInternal(Item i)
		{
			if (staleTimer_ == null && WidgetObject != null)
				staleTimer_ = Timer.Create(Timer.Immediate, OnStale);
		}

		private void OnStale()
		{
			if (staleTimer_ != null)
			{
				staleTimer_.Destroy();
				staleTimer_ = null;
			}

			Rebuild();
		}

		private void Rebuild()
		{
			UpdateNodes();
			base.UpdateBounds();

			if (topItemIndex_ > 0 && topItemIndex_ + itemCount_ < visibleCount_)
			{
				SetTopItem(topItemIndex_, true, false);
				UpdateNodes();
				base.UpdateBounds();
			}
			else
			{
				SetTopItem(topItemIndex_, true, false);
			}
		}

		private bool BuiltinFilterFunc(Item i)
		{
			Regex re = null;

			if (Utilities.IsRegex(filterString_))
				re = Utilities.CreateRegex(filterString_);

			if (re == null)
				return i.Text.ToLower().Contains(filterStringLc_);
			else
				return re.IsMatch(i.Text);
		}

		private Func<Item, bool> GetFilterFunc()
		{
			Func<Item, bool> f = null;

			if (filterFunc_ == null && filterString_ != "")
				f = BuiltinFilterFunc;
			else
				f = filterFunc_;

			return f;
		}

		public void SetSelectedInternal(Item item, bool b)
		{
			var old = selected_;

			if (b)
				selected_ = item;
			else if (selected_ == item)
				selected_ = null;

			for (int i = 0; i < nodes_.Count; ++i)
			{
				nodes_[i].UpdateState();
			}

			if (selected_ != old)
				SelectionChanged?.Invoke(selected_);
		}

		private void UpdateNodes()
		{
			try
			{
				var filter = GetFilterFunc();

				var cx = new NodeContext();
				cx.av = AbsoluteClientBounds;
				cx.nodeIndex = 0;
				cx.itemIndex = 0;
				cx.x = cx.av.Left + InternalPadding;
				cx.y = cx.av.Top + InternalPadding;
				cx.indent = 0;

				int nodeCount = (int)(cx.av.Height / FullItemHeight);

				while (nodes_.Count < nodeCount)
					nodes_.Add(new Node(this));

				while (nodes_.Count > nodeCount)
					nodes_.RemoveAt(nodes_.Count - 1);


				UpdateNode(root_, cx, filter);

				for (int i = cx.nodeIndex; i < nodes_.Count; ++i)
					nodes_[i].Clear();

				DoLayoutImpl();

				itemCount_ = cx.itemIndex;
				visibleCount_ = (int)(cx.av.Height / FullItemHeight);

				float requiredHeight = (itemCount_) * FullItemHeight;
				float missingHeight = requiredHeight - cx.av.Height;

				if (missingHeight <= 0)
				{
					vsb_.Visible = false;
					vsb_.Range = 0;
				}
				else
				{
					vsb_.Visible = true;
					vsb_.Range = missingHeight + Style.Metrics.TreeItemHeight;
				}
			}
			catch (Exception e)
			{
				Glue.LogError("TreeView: exception thrown while updating nodes");
				Glue.LogError(e.ToString());
			}
		}

		private void UpdateNode(Item item, NodeContext cx, Func<Item, bool> filter)
		{
			for (int i = 0; i < item.Children?.Count; ++i)
			{
				var child = item.Children[i];

				if (!child.ShouldBeVisible(filter))
					continue;

				if (cx.itemIndex >= topItemIndex_)
				{
					if (cx.nodeIndex < nodes_.Count)
					{
						var node = nodes_[cx.nodeIndex];

						var x = cx.x;
						var y = cx.y;

						var r = Rectangle.FromPoints(
							x, y,
							cx.av.Right - InternalPadding - Style.Metrics.ScrollBarWidth,
							y + Style.Metrics.TreeItemHeight);

						node.Set(child, r, cx.indent);

						cx.y += FullItemHeight;
						++cx.nodeIndex;
					}
				}

				++cx.itemIndex;

				if (child.Expanded || filter != null)
				{
					++cx.indent;
					UpdateNode(child, cx, filter);
					--cx.indent;
				}
			}
		}

		protected override void DoCreate()
		{
			base.DoCreate();
			GetRoot().TrackPointer(this, true);
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			//Style.Polish(this);
		}

		public override void UpdateBounds()
		{
			var r = AbsoluteClientBounds;
			r.Left = r.Right - Style.Metrics.ScrollBarWidth;
			vsb_.SetBounds(r);
			//hsb_.Set(0, 0, 10);

			UpdateNodes();

			base.UpdateBounds();
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(300, 200);
		}

		private void OnWheel(WheelEvent e)
		{
			try
			{
				ignoreVScroll_ = true;
				SetTopItem(topItemIndex_ + (int)-e.Delta.Y, true);
			}
			finally
			{
				ignoreVScroll_ = false;
			}

			e.Bubble = false;
		}

		private Node NodeAt(Point p)
		{
			var r = AbsoluteClientBounds;
			r.Right -= Style.Metrics.ScrollBarWidth;

			if (r.Contains(p))
			{
				var rp = p - r.TopLeft;

				int nodeIndex = (int)(rp.Y / FullItemHeight);
				if (nodeIndex >= 0 && nodeIndex < nodes_.Count)
					return nodes_[nodeIndex];
			}

			return null;
		}

		private void OnHover(PointerEvent e)
		{
			if (IsVisibleOnScreen())
			{
				var w = GetRoot().WidgetAt(e.Pointer);
				if (w.HasParent(this))
					SetHovered(NodeAt(e.Pointer));
			}

			e.Bubble = false;
		}

		private void OnExit(PointerEvent e)
		{
			SetHovered(null);
		}

		private void OnClick(PointerEvent e)
		{
			var n = NodeAt(e.Pointer);

			if (n?.Item != null)
			{
				n.Item.Selected = true;
				ItemClicked?.Invoke(n.Item);
			}

			e.Bubble = false;
		}

		private void OnPointerDown(PointerEvent e)
		{
			e.Bubble = false;
		}

		private void SetHovered(Node n)
		{
			if (hovered_ == n)
				return;

			if (hovered_ != null)
				hovered_.Hovered = false;

			hovered_ = n;

			if (hovered_ != null)
				hovered_.Hovered = true;
		}

		public override string DebugLine
		{
			get { return base.DebugLine; }
		}
	}
}
