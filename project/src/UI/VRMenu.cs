using System.Collections.Generic;

namespace Cue
{
	class VRMenu
	{
		interface IItem
		{
			VUI.Widget Panel { get; }
			bool Selected { set; }
			void Activate();
		}

		abstract class Item<W> : IItem
			where W : VUI.Widget
		{
			private VUI.Panel panel_;
			private W widget_;

			public Item(W w)
			{
				widget_ = w;

				panel_ = new VUI.Panel(new VUI.BorderLayout());
				panel_.Add(widget_, VUI.BorderLayout.Center);
			}

			public VUI.Widget Panel
			{
				get { return panel_; }
			}

			public W Widget
			{
				get { return widget_; }
			}

			public virtual bool Selected
			{
				set
				{
					if (value)
						panel_.BackgroundColor = VUI.Style.Theme.HighlightBackgroundColor;
					else
						panel_.BackgroundColor = new UnityEngine.Color(0, 0, 0, 0);
				}
			}

			public abstract void Activate();
		}


		class CheckBoxItem : Item<VUI.CheckBox>
		{
			public CheckBoxItem(VUI.CheckBox cb)
				: base(cb)
			{
			}

			public override void Activate()
			{
				Widget.Toggle();
			}
		}


		class ButtonItem : Item<VUI.Button>
		{
			public ButtonItem(VUI.Button cb)
				: base(cb)
			{
			}

			public override bool Selected
			{
				set
				{
					if (value)
						Widget.BackgroundColor = VUI.Style.Theme.HighlightBackgroundColor;
					else
						Widget.BackgroundColor = VUI.Style.Theme.ButtonBackgroundColor;
				}
			}

			public override void Activate()
			{
				Widget.Click();
			}
		}



		private VUI.Root root_ = null;
		private VUI.Label name_ = null;
		private List<IItem> items_ = new List<IItem>();
		private int widgetSel_ = -1;
		private int personSel_ = -1;
		private bool visible_ = false;
		private bool left_ = false;
		private VUI.IgnoreFlag ignore_ = new VUI.IgnoreFlag();

		public void Create()
		{
			items_.Add(new CheckBoxItem(new VUI.CheckBox("HJ", OnHJ)));
			items_.Add(new CheckBoxItem(new VUI.CheckBox("BJ", OnBJ)));
			items_.Add(new CheckBoxItem(new VUI.CheckBox("Thrust", OnThrust)));
			items_.Add(new CheckBoxItem(new VUI.CheckBox("Can kiss", OnCanKiss)));
			items_.Add(new ButtonItem(new VUI.Button("Genitals", OnGenitals)));
			items_.Add(new ButtonItem(new VUI.Button("Breasts", OnBreasts)));

			if (Cue.Instance.ActivePersons.Length == 0)
			{
				personSel_ = -1;
			}
			else if (personSel_ < 0 || personSel_ >= Cue.Instance.ActivePersons.Length)
			{
				personSel_ = 0;
			}

			root_ = Cue.Instance.Sys.Create2D(300, new Size(300, 420));

			//root_ = Cue.Instance.Sys.CreateAttached(
			//	true,
			//	new Vector3(0, 0.1f, 0),
			//	new Point(0, 0),
			//	new Size(300, 250));

			var ly = new VUI.VerticalFlow();
			var p = new VUI.Panel(ly);

			name_ = p.Add(new VUI.Label(""));
			p.Add(new VUI.Spacer(10));

			foreach (var i in items_)
				p.Add(i.Panel);

			root_.ContentPanel.Layout = new VUI.BorderLayout();
			root_.ContentPanel.Add(p, VUI.BorderLayout.Center);
			UpdateVisibility();

			if (personSel_ != -1)
				name_.Text = Cue.Instance.ActivePersons[personSel_].ID;
		}

		public void Destroy()
		{
			if (root_ != null)
			{
				root_.Destroy();
				root_ = null;
			}

			items_.Clear();
		}

		public void Update()
		{
			if (root_?.Visible ?? false)
			{
				var old = widgetSel_;

				if (Cue.Instance.Sys.Input.MenuDown)
				{
					++widgetSel_;
					if (widgetSel_ >= items_.Count)
						widgetSel_ = 0;
				}
				else if (Cue.Instance.Sys.Input.MenuUp)
				{
					--widgetSel_;
					if (widgetSel_ < 0)
						widgetSel_ = items_.Count - 1;
				}

				if (widgetSel_ == -1)
					widgetSel_ = 0;

				if (widgetSel_ != old)
				{
					for (int i = 0; i < items_.Count; ++i)
						items_[i].Selected = (i == widgetSel_);
				}


				if (Cue.Instance.Sys.Input.MenuRight)
				{
					++personSel_;
					if (personSel_ >= Cue.Instance.ActivePersons.Length)
						personSel_ = 0;
				}
				else if (Cue.Instance.Sys.Input.MenuLeft)
				{
					--personSel_;
					if (personSel_ < 0)
						personSel_ = Cue.Instance.ActivePersons.Length - 1;
				}

				if (personSel_ >= Cue.Instance.ActivePersons.Length)
					personSel_ = -1;

				if (personSel_ == -1)
					name_.Text = "";
				else
					name_.Text = Cue.Instance.ActivePersons[personSel_].ID;


				if (Cue.Instance.Sys.Input.MenuSelect)
				{
					if (widgetSel_ >= 0 && widgetSel_ < items_.Count)
						items_[widgetSel_].Activate();
				}
			}

			root_?.Update();
		}

		public void ShowLeft()
		{
			left_ = true;
			visible_ = true;
			UpdateVisibility();
		}

		public void ShowRight()
		{
			left_ = false;
			visible_ = true;
			UpdateVisibility();
		}

		public void Hide()
		{
			visible_ = false;
			UpdateVisibility();
		}

		private void UpdateVisibility()
		{
			if (root_ != null)
			{
				if (visible_)
				{
					root_.Visible = true;

					var s = root_.RootSupport as Sys.Vam.VRHandRootSupport;

					if (s != null)
					{
						if (left_)
							s.AttachLeft();
						else
							s.AttachRight();
					}
				}
				else
				{
					root_.Visible = false;
				}
			}
		}

		public IObject Selected
		{
			get
			{
				if (personSel_ < 0 || personSel_ >= Cue.Instance.ActivePersons.Length)
					return null;
				else
					return Cue.Instance.ActivePersons[personSel_] as Person;
			}

			set { }
		}

		private void OnHJ(bool b)
		{
			if (ignore_) return;
			UIActions.HJ(Selected as Person, b);
		}

		private void OnBJ(bool b)
		{
			if (ignore_) return;
			UIActions.BJ(Selected as Person, b);
		}

		private void OnThrust(bool b)
		{
			if (ignore_) return;
			UIActions.Thrust(Selected as Person, b);
		}

		private void OnCanKiss(bool b)
		{
			if (ignore_) return;
			UIActions.CanKiss(Selected as Person, b);
		}

		private void OnGenitals()
		{
			if (ignore_) return;
			UIActions.Genitals(Selected as Person);
		}

		private void OnBreasts()
		{
			if (ignore_) return;
			UIActions.Breasts(Selected as Person);
		}

	}
}
