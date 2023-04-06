using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VUI
{
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
			private Texture icon_ = null;
			private string tooltip_ = null;
			private bool gotChildren_ = false;

			public Item(string text = "")
			{
				text_ = text;
			}

			public Logger Log
			{
				get
				{
					return parent_?.Log ?? Logger.Global;
				}
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

			public int IndexInParent
			{
				get
				{
					if (parent_?.Children != null)
					{
						for (int i = 0; i < parent_.Children.Count; ++i)
						{
							if (parent_.Children[i] == this)
								return i;
						}
					}

					return -1;
				}
			}

			public string Tooltip
			{
				get
				{
					try
					{
						return GetTooltip();
					}
					catch (Exception e)
					{
						Log.Error($"tree view item exception in Tooltip");
						Log.Error(e.ToString());
						return "";
					}
				}

				set
				{
					tooltip_ = value;
				}
			}

			protected virtual string GetTooltip()
			{
				return tooltip_;
			}

			public string Text
			{
				get
				{
					return text_;
				}

				set
				{
					string s = value ?? "";

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

			public Texture Icon
			{
				get
				{
					return icon_;
				}

				set
				{
					if (icon_ != value)
					{
						icon_ = value;
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
				get
				{
					NeedsChildren();
					return children_;
				}
			}

			public List<Item> GetInternalChildren()
			{
				return children_;
			}


			public bool Expanded
			{
				get
				{
					return expanded_;
				}

				set
				{
					SetExpanded(value);
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

			public bool IsContainedBy(Item item)
			{
				var i = this;
				while (i != null)
				{
					if (item == i)
						return true;

					i = i.Parent;
				}

				return false;
			}

			public void Toggle()
			{
				if (HasChildren)
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

			private void SetExpanded(bool b)
			{
				expanded_ = b;

				if (expanded_)
					NeedsChildren();
			}

			private void NeedsChildren()
			{
				if (!gotChildren_)
				{
					gotChildren_ = true;

					try
					{
						GetChildren();
					}
					catch (Exception e)
					{
						Log.Error($"tree view item NeedsChildren exception");
						Log.Error(e.ToString());
					}
				}
			}

			protected virtual void GetChildren()
			{
				// no-op
			}

			private void SetExpandedRecursive(bool b, bool visibleOnly)
			{
				SetExpanded(b);

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

			public bool HasChildren
			{
				get
				{
					try
					{
						return GetHasChildren();
					}
					catch (Exception e)
					{
						Log.Error("tree view item GetHasChildren exception");
						Log.Error(e.ToString());
						return false;
					}
				}
			}

			protected virtual bool GetHasChildren()
			{
				if (children_ == null)
					return false;

				for (int i = 0; i < children_.Count; ++i)
				{
					if (children_[i].Visible)
						return true;
				}

				return false;
			}

			public T Add<T>(T item) where T : Item
			{
				return Insert(children_?.Count ?? 0, item);
			}

			public T Insert<T>(int index, T item) where T : Item
			{
				if (children_ == null)
					children_ = new List<Item>();

				children_.Insert(index, item);
				item.Parent = this;
				gotChildren_ = true;

				NodesChanged();

				return item;
			}

			public void Remove(Item child)
			{
				if (children_ == null)
					return;

				if (child.Parent != this)
					return;

				children_.Remove(child);
				TreeView?.NodeRemovedInternal(child);
			}

			public void Clear()
			{
				if (children_ != null && children_.Count > 0)
				{
					TreeView?.NodeAboutToGetClearedInternal(this);

					for (int i=0; i<children_.Count; ++i)
						children_[i].Parent = null;

					expanded_ = false;
					children_.Clear();
					NodesChanged();
				}

				gotChildren_ = false;
			}

			public void UpdateToggle()
			{
				NodesChanged();
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
			private Image icon_ = null;
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

					r.Left += Style.Metrics.TreeViewLeftMargin;
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

					if (tree_.Icons)
					{
						if (icon_ == null)
							CreateIcon(r);

						UpdateIcon(r);
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
					panel_.Tooltip.TextFunc = () => item_?.Tooltip ?? "";
					panel_.Clickthrough = false;

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
					toggle_.BackgroundColor = new Color(0, 0, 0, 0);
					toggle_.Borders = new Insets(1);
					toggle_.BorderColor = Style.Theme.TreeViewToggleBorderColor;

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

			private void CreateIcon(Rectangle r)
			{
				if (icon_ == null)
				{
					icon_ = new Image();
					panel_.Add(icon_);
					icon_.Create();
				}
			}

			private void CreateLabel(Rectangle r)
			{
				if (label_ == null)
				{
					label_ = new Label();
					label_.WrapMode = tree_.LabelWrap;
					label_.FontSize = tree_.FontSize;

					panel_.Add(label_);
					label_.Create();
					label_.Events.PointerClick += (e) => { e.Bubble = true; };
				}
			}

			private void UpdatePanel(Rectangle r)
			{
				panel_.Tooltip.FontSize = tree_.FontSize;
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

					tr.Top += 2;
					tr.Bottom -= 2;
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
						tr.Left += Style.Metrics.TreeCheckBoxWidth + Style.Metrics.TreeToggleSpacing;

					tr.Width = Style.Metrics.TreeCheckBoxWidth;

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

			private void UpdateIcon(Rectangle r)
			{
				if (item_ == null)
					return;

				var tr = r;

				tr.Left += Style.Metrics.TreeIconWidth + Style.Metrics.TreeIconSpacing;
				tr.Right = tr.Left + Style.Metrics.TreeIconWidth;
				tr.Bottom = tr.Top + Style.Metrics.TreeIconWidth;

				icon_.SetBounds(tr);
				icon_.Texture = item_.Icon;
				icon_.Render = true;
			}

			private void UpdateLabel(Rectangle r)
			{
				if (item_ == null)
					return;

				var lr = r;

				if (item_.Parent != tree_.RootItem || tree_.RootToggles)
					lr.Left += Style.Metrics.TreeToggleWidth + Style.Metrics.TreeToggleSpacing;

				if (tree_.CheckBoxes && item_.Checkable)
					lr.Left += Style.Metrics.TreeCheckBoxWidth + Style.Metrics.TreeCheckboxSpacing;

				if (tree_.Icons)
					lr.Left += Style.Metrics.TreeIconWidth + Style.Metrics.TreeIconSpacing;

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


		public const int ScrollToNone = 0;
		public const int ScrollToTop = 1;
		public const int ScrollToCenter = 2;
		public const int ScrollToBottom = 3;
		public const int ScrollToNearest = 4;

		public delegate void ItemCallback(Item i);
		public event ItemCallback SelectionChanged;
		public event ItemCallback ItemClicked;

		private readonly InternalRootItem root_;
		private List<Node> nodes_ = new List<Node>();
		private ScrollBar vsb_ = new ScrollBar();
		private bool checkboxes_ = false;
		private bool icons_ = false;
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
		private bool doubleClickToggle_ = false;
		private int labelWrap_ = VUI.Label.ClipEllipsis;

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
			Events.PointerDoubleClick += OnDoubleClick;
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

		private void OnVerticalScroll(float v)
		{
			if (ignoreVScroll_) return;
			SetTopItem((int)(v / (FullItemHeight)), false);
		}

		private void SetTopItem(int index, bool updateSb, bool rebuild = true)
		{
			int newTop = Utilities.Clamp(index, 0, Math.Max(itemCount_ - visibleCount_, 0));

			if (newTop != topItemIndex_)
			{
				topItemIndex_ = newTop;

				if (updateSb)
				{
					float v = topItemIndex_ * (FullItemHeight);
					if (v + FullItemHeight > vsb_.Range)
						v = vsb_.Range;

					try
					{
						ignoreVScroll_ = true;
						vsb_.Value = v;
					}
					finally
					{
						ignoreVScroll_ = false;
					}
				}

				if (rebuild)
					Rebuild();
			}
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

		public bool Icons
		{
			get { return icons_; }
			set { icons_ = value; }
		}

		public bool RootToggles
		{
			get { return rootToggles_; }
			set { rootToggles_ = value; }
		}

		public bool DoubleClickToggle
		{
			get { return doubleClickToggle_; }
			set { doubleClickToggle_ = value; }
		}

		public int LabelWrap
		{
			get { return labelWrap_; }
			set { labelWrap_ = value; }
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

		public void NodeRemovedInternal(Item i)
		{
			NodesChangedInternal(i);

			if (Selected != null && Selected.IsContainedBy(i))
				SelectedGone();

			i.Parent = null;
		}

		public void NodeAboutToGetClearedInternal(Item node)
		{
			if (Selected != null && Selected.IsContainedBy(node))
				SelectedGone();
		}

		public void VisibilityChangedInternal(Item i)
		{
			if (staleTimer_ == null && WidgetObject != null)
				staleTimer_ = Timer.Create(Timer.Immediate, OnStale);

			if (i != null)
			{
				if (!i.Visible && Selected != null && Selected.IsContainedBy(i))
					SelectedGone();
			}
		}

		private void SelectedGone()
		{
			var ns = Selected?.Parent;
			bool found = false;

			while (ns != null)
			{
				if (ns.Visible)
				{
					Select(ns);
					found = true;
					break;
				}

				ns = ns.Parent;
			}

			if (!found)
				Select(RootItem);
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
				int oldTop = topItemIndex_;
				SetTopItem(topItemIndex_, true, false);

				if (oldTop != topItemIndex_)
				{
					Log.Info($"top changed from {oldTop} to {topItemIndex_}, updating");
					UpdateNodes();
					UpdateBounds();
				}

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

		public void Select(Item item, bool expandParents = true, int scrollTo = ScrollToNone)
		{
			SetSelectedInternal(item, true);

			if (expandParents)
			{
				Item parent = item?.Parent;
				while (parent != null)
				{
					parent.Expanded = true;
					parent = parent.Parent;
				}

				if (scrollTo != ScrollToNone)
					ScrollTo(item, scrollTo);
			}
		}

		public void ScrollTo(Item item, int where)
		{
			int i = 0;
			if (!AbsoluteItemIndex(root_, item, ref i) || i < 0)
			{
				Log.Error($"TreeView: ScrollTo item not found: {item}");
				return;
			}

			if (i >= 0)
				Glue.PluginManager.StartCoroutine(CoScrollTo(i, where));
		}

		private IEnumerator CoScrollTo(int index, int where)
		{
			yield return new WaitForEndOfFrame();
			DoScrollTo(index, where);
		}

		private void DoScrollTo(int i, int where)
		{
			switch (where)
			{
				case ScrollToCenter:
				{
					i = Math.Max(i - nodes_.Count / 2, 0);
					break;
				}

				case ScrollToBottom:
				{
					i = Math.Max(i - nodes_.Count + 1, 0);
					break;
				}

				case ScrollToNearest:
				{
					if (i > (topItemIndex_ + nodes_.Count))
					{
						// scroll to bottom
						i = Math.Max(i - nodes_.Count + 1, 0);
					}
					else if (i >= topItemIndex_)
					{
						// already visible, don't scroll
						i = -1;
					}
					else
					{
						// scroll to top, no-op
					}

					break;
				}

				case ScrollToTop:  // fall-through
				default:
				{
					// nothing
					break;
				}
			}

			if (i != -1)
				SetTopItem(i, true, true);
		}

		private bool AbsoluteItemIndex(Item parent, Item item, ref int index)
		{
			if (parent.Expanded && parent.Children != null)
			{
				foreach (var c in parent.Children)
				{
					if (c == item)
						return true;

					++index;

					if (AbsoluteItemIndex(c, item, ref index))
						return true;
				}
			}

			return false;
		}

		public void SetSelectedInternal(Item item, bool b)
		{
			var old = selected_;

			if (b)
				selected_ = item;
			else if (selected_ == item)
				selected_ = null;

			for (int i = 0; i < nodes_.Count; ++i)
				nodes_[i].UpdateState();

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
				cx.x = cx.av.Left;
				cx.y = cx.av.Top + Style.Metrics.TreeViewTopMargin;
				cx.indent = 0;

				int nodeCount = Math.Max(0, (int)(cx.av.Height / FullItemHeight));

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
					SetScrollbarVisibility(false);
					vsb_.Range = 0;
				}
				else
				{
					SetScrollbarVisibility(true);
					vsb_.Range = missingHeight + Style.Metrics.TreeItemHeight;
				}
			}
			catch (Exception e)
			{
				Log.Error("TreeView: exception thrown while updating nodes");
				Log.Error(e.ToString());
			}
		}

		private void SetScrollbarVisibility(bool b)
		{
			if (vsb_.Visible != b)
			{
				vsb_.Visible = b;
				UpdateBounds();
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
							cx.av.Right,
							y + Style.Metrics.TreeItemHeight);

						if (vsb_.Visible)
							r.Right -= Style.Metrics.ScrollBarWidth;

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

		protected override void BeforeUpdateBounds()
		{
			var r = AbsoluteClientBounds;
			r.Left = r.Right - vsb_.GetRealMinimumSize().Width;
			vsb_.SetBounds(r);

			UpdateNodes();
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(300, 200);
		}

		protected override Size DoGetMinimumSize()
		{
			var m = Style.Metrics;

			float w =
				m.TreeToggleWidth + m.TreeToggleSpacing +
				m.TreeCheckBoxWidth + m.TreeCheckboxSpacing +
				m.TreeIconWidth + m.TreeIconSpacing +
				50;

			return new Size(w, 200);
		}

		private void OnWheel(WheelEvent e)
		{
			SetTopItem(topItemIndex_ + (int)-e.Delta.Y, true);
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
			if (e.Button == PointerEvent.LeftButton)
			{
				var n = NodeAt(e.Pointer);

				if (n?.Item != null)
				{
					n.Item.Selected = true;
					ItemClicked?.Invoke(n.Item);
				}

				e.Bubble = false;
			}
		}

		private void OnDoubleClick(PointerEvent e)
		{
			if (e.Button == PointerEvent.LeftButton)
			{
				if (doubleClickToggle_)
				{
					var n = NodeAt(e.Pointer);

					if (n?.Item != null)
					{
						n.Item.Selected = true;
						n.Item.Toggle();
					}

					e.Bubble = false;
				}
			}
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
