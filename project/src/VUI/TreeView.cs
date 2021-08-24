using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
			DoLayout();

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

		private bool OnPointerDown(PointerEvent e)
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
			DoLayout();
			base.UpdateBounds();

			OnHandleMoved();

			var d = e.EventData as PointerEventData;
			SuperController.singleton.StartCoroutine(StartDrag(d));

			return false;
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
			private Item parent_ = null;
			private string text_;
			private List<Item> children_ = null;
			private bool expanded_ = false;

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
				get { return text_; }
				set { text_ = value; }
			}

			public List<Item> Children
			{
				get{ return children_; }
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
					TreeView?.ItemExpandedInternal(this);
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
			private Label label_ = null;
			private bool hovered_ = false;

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

					r.Left += indent * IndentSize;

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
					panel_ = new Panel(new AbsoluteLayout());
					tree_.Add(panel_);
					panel_.Create();
					panel_.Events.PointerClick += (e) => true;
				}
			}

			private void UpdatePanel(Rectangle r)
			{
				panel_.SetBounds(r);
				panel_.Render = true;
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

			private void UpdateToggle(Rectangle r)
			{
				if (item_ == null)
					return;

				if (item_.Expanded)
					toggle_.Text = "-";
				else
					toggle_.Text = "+";

				var tr = r;
				tr.Width = 30;
				toggle_.SetBounds(tr);
				toggle_.Render = true;
			}

			private void CreateLabel(Rectangle r)
			{
				if (label_ == null)
				{
					label_ = new Label();
					label_.WrapMode = VUI.Label.Clip;
					panel_.Add(label_);
					label_.Create();
					label_.Events.PointerClick += (e) => true;
				}
			}

			private void UpdateLabel(Rectangle r)
			{
				if (item_ == null)
					return;

				var lr = r;
				lr.Left += 35;
				label_.Text = item_.Text;
				label_.SetBounds(lr);
			}

			public void UpdateState()
			{
				if (panel_ == null)
					return;

				if (hovered_)
					panel_.BackgroundColor = Style.Theme.HighlightBackgroundColor;
				else if (item_?.Selected ?? false)
					panel_.BackgroundColor = Style.Theme.SelectionBackgroundColor;
				else
					panel_.BackgroundColor = Style.Theme.BackgroundColor;
			}

			private void OnToggle()
			{
				if (item_ != null)
					item_.Toggle();
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


		private const int InternalPadding = 5;
		private const int ItemHeight = 35;
		private const int ItemPadding = 2;
		private const int IndentSize = 50;
		private const int ScrollBarWidth = 40;

		private readonly InternalRootItem root_;
		private List<Node> nodes_ = new List<Node>();
		private ScrollBar vsb_ = new ScrollBar();
		private int topItemIndex_ = 0;
		private int itemCount_ = 0;
		private int visibleCount_ = 0;
		private IgnoreFlag ignoreVScroll_ = new IgnoreFlag();
		private Node hovered_ = null;
		private Item selected_ = null;

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

			vsb_.ValueChanged += OnVerticalScroll;
		}

		private void OnVerticalScroll(float v)
		{
			if (ignoreVScroll_) return;
			SetTopItem((int)(v / (ItemHeight + ItemPadding)), false);
		}

		private void SetTopItem(int index, bool updateSb)
		{
			topItemIndex_ = Utilities.Clamp(index, 0, itemCount_ - visibleCount_);

			if (updateSb)
			{
				float v = topItemIndex_ * (ItemHeight + ItemPadding);
				if (v + (ItemHeight + ItemPadding) > vsb_.Range)
					v = vsb_.Range;

				vsb_.Value = v;
			}

			UpdateNodes();
			base.UpdateBounds();
		}

		public Item RootItem
		{
			get { return root_; }
		}

		public Item Selected
		{
			get { return selected_; }
		}

		public void ItemExpandedInternal(Item i)
		{
			UpdateNodes();
			base.UpdateBounds();
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
			var cx = new NodeContext();
			cx.av = AbsoluteClientBounds;
			cx.nodeIndex = 0;
			cx.itemIndex = 0;
			cx.x = cx.av.Left + InternalPadding;
			cx.y = cx.av.Top + InternalPadding;
			cx.indent = 0;

			int nodeCount = (int)(cx.av.Height / (ItemHeight + ItemPadding));

			while (nodes_.Count < nodeCount)
				nodes_.Add(new Node(this));

			while (nodes_.Count > nodeCount)
				nodes_.RemoveAt(nodes_.Count - 1);


			UpdateNode(root_, cx);

			for (int i = cx.nodeIndex; i < nodes_.Count; ++i)
				nodes_[i].Clear();

			DoLayout();

			itemCount_ = cx.itemIndex;
			visibleCount_ = (int)(cx.av.Height / (ItemHeight + ItemPadding));

			float requiredHeight = (itemCount_ + 1) * (ItemHeight + ItemPadding);
			float missingHeight = requiredHeight - cx.av.Height;

			if (missingHeight <= 0)
			{
				vsb_.Visible = false;
			}
			else
			{
				vsb_.Visible = true;
				vsb_.Range = missingHeight;
			}
		}

		private void UpdateNode(Item item, NodeContext cx)
		{
			for (int i = 0; i < item.Children?.Count; ++i)
			{
				var child = item.Children[i];

				if (cx.itemIndex >= topItemIndex_)
				{
					if (cx.nodeIndex < nodes_.Count)
					{
						var node = nodes_[cx.nodeIndex];

						var x = cx.x;
						var y = cx.y;

						var r = Rectangle.FromPoints(
							x, y,
							cx.av.Right - InternalPadding - ScrollBarWidth,
							y + ItemHeight);

						node.Set(child, r, cx.indent);

						cx.y += ItemHeight + ItemPadding;
						++cx.nodeIndex;
					}
				}

				++cx.itemIndex;

				if (child.Expanded)
				{
					++cx.indent;
					UpdateNode(child, cx);
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
			r.Left = r.Right - ScrollBarWidth;
			vsb_.SetBounds(r);
			//hsb_.Set(0, 0, 10);

			UpdateNodes();

			base.UpdateBounds();
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return base.DoGetPreferredSize(maxWidth, maxHeight);
		}

		protected override Size DoGetMinimumSize()
		{
			return base.DoGetMinimumSize();
		}

		protected override void DoSetRender(bool b)
		{
			base.DoSetRender(b);
		}

		private bool OnWheel(WheelEvent e)
		{
			ignoreVScroll_.Do(() =>
			{
				SetTopItem(topItemIndex_ + (int)-e.Delta.Y, true);
			});

			return false;
		}

		private Node NodeAt(Point p)
		{
			var r = AbsoluteClientBounds;
			r.Right -= ScrollBarWidth;

			if (r.Contains(p))
			{
				var rp = p - r.TopLeft;

				int nodeIndex = (int)(rp.Y / (ItemHeight + ItemPadding));
				if (nodeIndex >= 0 && nodeIndex < nodes_.Count)
					return nodes_[nodeIndex];
			}

			return null;
		}

		private bool OnHover(PointerEvent e)
		{
			if (!IsVisibleOnScreen())
				return false;

			SetHovered(NodeAt(e.Pointer));

			return false;
		}

		private void OnExit(PointerEvent e)
		{
			SetHovered(null);
		}

		private bool OnClick(PointerEvent e)
		{
			var n = NodeAt(e.Pointer);

			if (n?.Item != null)
				n.Item.Selected = true;

			return false;
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
