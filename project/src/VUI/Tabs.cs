using System.Collections.Generic;

namespace VUI
{
	class Stack : Widget
	{
		public override string TypeName { get { return "Stack"; } }

		private readonly List<Widget> widgets_ = new List<Widget>();
		private int selection_ = -1;

		public Stack()
		{
			Layout = new BorderLayout();
		}

		public int Selected
		{
			get { return selection_; }
		}

		public void AddToStack(Widget w)
		{
			w.Visible = false;
			widgets_.Add(w);
			Add(w, BorderLayout.Center);

			if (selection_ == -1)
				Select(0);
		}

		public void Select(int sel)
		{
			if (sel < 0 || sel >= widgets_.Count)
				sel = -1;

			selection_ = sel;

			for (int i = 0; i < widgets_.Count; ++i)
				widgets_[i].Visible = (i == selection_);
		}
	}

	// a panel with an absolute layout to avoid relayouts when selecting tabs
	//
	class TabButtons : Panel
	{
		public override string TypeName { get { return "TabButtons"; } }

		public TabButtons()
		{
			Layout = new AbsoluteLayout();
		}

		public void Update()
		{
			if (MainObject != null)
			{
				UpdateChildren();
				DoLayout();
			}
		}

		protected override void BeforeUpdateBounds()
		{
			UpdateChildren();
		}

		protected override Size DoGetMinimumSize()
		{
			var m = Style.Metrics;

			return new Size(
				DontCare,
				m.TabButtonMinimumSize.Height + m.SelectedTabPadding.Height);
		}

		private void UpdateChildren()
		{
			var av = AbsoluteClientBounds;
			float left = av.Left;

			foreach (var b in GetChildren())
			{
				if (!b.Visible)
					continue;

				var ps = b.GetRealPreferredSize(av.Width, av.Height);

				b.SetBounds(Rectangle.FromSize(
					left, av.Bottom - ps.Height, ps.Width, ps.Height));

				left += ps.Width;
			}
		}
	}


	class TabButton : Button
	{
		public TabButton(string text)
			: base(text)
		{
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			var s = FitText(Text, new Size(maxWidth, maxHeight));

			s += Style.Metrics.ButtonPadding;

			return Size.Max(s, Style.Metrics.TabButtonMinimumSize);
		}

		protected override Size DoGetMinimumSize()
		{
			var s = TextSize(Text);
			return Size.Max(s, Style.Metrics.TabButtonMinimumSize);
		}
	}


	class Tabs : Widget
	{
		public override string TypeName { get { return "Tabs"; } }

		class Tab
		{
			private readonly Tabs tabs_;
			private readonly TabButton button_;
			private readonly Panel panel_;
			private readonly Widget widget_;
			private bool selected_ = false;

			public Tab(Tabs tabs, string text, Widget w)
			{
				tabs_ = tabs;
				button_ = new TabButton(text);
				panel_ = new Panel();
				widget_ = w;

				panel_.Layout = new BorderLayout();
				panel_.Add(widget_, BorderLayout.Center);

				button_.Alignment = Label.AlignCenter | Label.AlignVCenter;
				button_.Clicked += () => { tabs_.SelectImpl(this); };
			}

			public Button Button
			{
				get { return button_; }
			}

			public Widget Panel
			{
				get { return panel_; }
			}

			public Widget Widget
			{
				get { return widget_; }
			}

			public bool Selected
			{
				get { return selected_; }
			}

			public void SetSelected(bool b)
			{
				selected_ = b;

				if (selected_)
				{
					button_.MinimumSize =
						Style.Metrics.TabButtonMinimumSize +
						Style.Metrics.SelectedTabPadding;

					button_.BackgroundColor =
						Style.Theme.SelectedTabBackgroundColor;

					button_.HighlightBackgroundColor =
						Style.Theme.SelectedTabBackgroundColor;

					button_.TextColor =
						Style.Theme.SelectedTabTextColor;
				}
				else
				{
					button_.MinimumSize = Style.Metrics.TabButtonMinimumSize;

					button_.BackgroundColor =
						Style.Theme.ButtonBackgroundColor;

					button_.HighlightBackgroundColor =
						Style.Theme.HighlightBackgroundColor;

					button_.TextColor = Style.Theme.TextColor;
				}
			}
		}


		public delegate void SelectionCallback(int index);
		public event SelectionCallback SelectionChanged;


		private readonly TabButtons top_ = new TabButtons();
		private readonly Stack stack_ = new Stack();
		private readonly List<Tab> tabs_ = new List<Tab>();

		public Tabs()
		{
			Layout = new BorderLayout();

			Add(top_, BorderLayout.Top);
			Add(stack_, BorderLayout.Center);

			stack_.Layout = new BorderLayout();
			stack_.Borders = new Insets(1);
			stack_.Padding = new Insets(20);
		}

		public List<Widget> TabWidgets
		{
			get
			{
				var list = new List<Widget>();

				foreach (var t in tabs_)
					list.Add(t.Widget);

				return list;
			}
		}

		public int Selected
		{
			get
			{
				for (int i = 0; i < tabs_.Count; ++i)
				{
					if (tabs_[i].Selected)
						return i;
				}

				return -1;
			}
		}

		public Widget SelectedWidget
		{
			get
			{
				var i = Selected;
				if (i < 0 || i >= tabs_.Count)
					return null;

				return tabs_[i].Widget;
			}
		}

		public string SelectedCaption
		{
			get
			{
				var i = Selected;
				if (i < 0 || i >= tabs_.Count)
					return "";

				return tabs_[i]?.Button?.Text ?? "";
			}
		}

		public void AddTab(string text, Widget w)
		{
			var t = new Tab(this, text, w);
			tabs_.Add(t);

			top_.Add(t.Button);
			stack_.AddToStack(t.Panel);

			SelectImpl(tabs_[0]);
		}

		public void Select(int i)
		{
			if (i < 0 || i >= tabs_.Count)
				SelectImpl(null);
			else
				SelectImpl(tabs_[i]);
		}

		public void Select(Widget w)
		{
			var i = IndexOfWidget(w);
			if (i == -1)
			{
				Log.Error($"Select: widget '{w}' not found");
				return;
			}

			Select(i);
		}

		public void Select(string caption)
		{
			var i = IndexOfCaption(caption);
			if (i == -1)
			{
				Log.Error($"Select: caption '{caption}' not found");
				return;
			}

			Select(i);
		}

		public void SetTabVisible(int i, bool b)
		{
			if (i < 0 || i >= tabs_.Count)
				return;

			if (tabs_[i].Button.Visible == b)
				return;

			if (!b && i == Selected)
			{
				if (i < (tabs_.Count - 1))
					Select(i + 1);
				else if (i > 0)
					Select(i - 1);
				else
					Select(-1);
			}

			tabs_[i].Button.Visible = b;
			NeedsLayout("SetTabVisible");
		}

		public void SetTabVisible(Widget w, bool b)
		{
			var i = IndexOfWidget(w);
			if (i == -1)
			{
				Log.Error("SetTabVisible: widget not found");
				return;
			}

			SetTabVisible(i, b);
		}

		public int IndexOfWidget(Widget w)
		{
			for (int i = 0; i < tabs_.Count; ++i)
			{
				if (tabs_[i].Widget == w)
					return i;
			}

			return -1;
		}

		public int IndexOfCaption(string s)
		{
			for (int i = 0; i < tabs_.Count; ++i)
			{
				if (tabs_[i].Button?.Text == s)
					return i;
			}

			return -1;
		}

		private void SelectImpl(Tab t)
		{
			int sel = -1;

			for (int i = 0; i < tabs_.Count; ++i)
			{
				if (tabs_[i] == t)
				{
					sel = i;
					stack_.Select(i);
					tabs_[i].SetSelected(true);
				}
				else
				{
					tabs_[i].SetSelected(false);
				}
			}

			top_.Update();
			SelectionChanged?.Invoke(sel);
		}
	}
}
