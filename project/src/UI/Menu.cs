using System;
using System.Collections.Generic;

namespace Cue
{
	static class UIActions
	{
		public interface IItem
		{
			Person Person { get; set; }
			VUI.Widget Panel { get; }
			bool Selected { set; }
			void Update();
			void Activate();
		}

		public abstract class Item<W> : IItem
			where W : VUI.Widget
		{
			private VUI.Panel panel_;
			private W widget_;
			private Person person_ = null;
			private Func<Person, bool> enabled_;

			public Item(W w, Func<Person, bool> enabled)
			{
				widget_ = w;
				enabled_ = enabled;

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

			public Person Person
			{
				get { return person_; }
				set { person_ = value; }
			}

			public virtual void Update()
			{
				if (Person == null)
				{
					Widget.Enabled = false;
				}
				else
				{
					if (enabled_ != null)
						Widget.Enabled = enabled_(Person);
					else
						Widget.Enabled = true;
				}
			}

			public abstract void Activate();
		}


		public class CheckBoxItem : Item<VUI.CheckBox>
		{
			private Action<Person, bool> f_;
			private Func<Person, bool> check_;
			private bool ignore_ = false;

			public CheckBoxItem(
				string caption,
				Action<Person, bool> f,
				Func<Person, bool> check = null,
				Func<Person, bool> enabled = null)
					: base(new VUI.CheckBox(caption), enabled)
			{
				f_ = f;
				check_ = check;
				Widget.Changed += OnChecked;
			}

			public override void Update()
			{
				base.Update();

				try
				{
					ignore_ = true;

					if (Person == null)
					{
						if (check_ != null)
							Widget.Checked = false;
					}
					else
					{
						if (check_ != null)
							Widget.Checked = check_(Person);
					}
				}
				finally
				{
					ignore_ = false;
				}
			}

			public override void Activate()
			{
				if (Widget.Enabled)
					Widget.Toggle();
			}

			private void OnChecked(bool b)
			{
				if (ignore_) return;

				if (Person != null)
					f_(Person, b);
			}
		}


		public class ButtonItem : Item<VUI.Button>
		{
			private Action<Person> f_;

			public ButtonItem(
				string caption,
				Action<Person> f,
				Func<Person, bool> enabled = null)
					: base(new VUI.Button(caption), enabled)
			{
				f_ = f;
				Widget.Clicked += OnClicked;
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

			private void OnClicked()
			{
				if (Person != null)
					f_(Person);
			}
		}


		public class CustomMenuItem : Item<VUI.Panel>
		{
			private ICustomMenuItem item_;

			public CustomMenuItem(ICustomMenuItem item)
				: base(item.CreateMenuWidget(), null)
			{
				item_ = item;
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
				item_.Activate();
			}
		}


		public static List<IItem> All()
		{
			var list = new List<IItem>
			{
				Hand(), Head(), Thrust(), Trib(), CanKiss(), Finish()
			};

			foreach (var m in Cue.Instance.Options.CustomMenuItems.Items)
				list.Add(new CustomMenuItem(m));

			//list.Add(Genitals());
			//list.Add(Breasts());
			list.Add(MovePlayer());

			return list;
		}

		private static IItem Hand()
		{
			return new CheckBoxItem("Hand",
				(p, b) =>
				{
					if (p != null)
						p.AI.GetEvent<HandEvent>().Active = b;
				},

				(p) => p.AI.GetEvent<HandEvent>()?.Active ?? false);
		}

		public static IItem Head()
		{
			return new CheckBoxItem("Head",
				(p, b) =>
				{
					if (p != null && !p.IsPlayer)
						p.AI.GetEvent<MouthEvent>().Active = b;
				},

				(p) => p.AI.GetEvent<MouthEvent>()?.Active ?? false);
		}

		public static IItem Thrust()
		{
			return new CheckBoxItem("Thrust",
				(p, b) =>
				{
					if (p != null)
						p.AI.GetEvent<ThrustEvent>().Active = b;
				},

				(p) => p.AI.GetEvent<ThrustEvent>()?.Active ?? false);
		}

		public static IItem Trib()
		{
			return new CheckBoxItem("Trib",
				(p, b) =>
				{
					if (p != null)
						p.AI.GetEvent<TribEvent>().Active = b;
				},

				(p) => p.AI.GetEvent<TribEvent>()?.Active ?? false);
		}

		public static IItem CanKiss()
		{
			return new CheckBoxItem("Can kiss",
				(p, b) =>
				{
					if (p != null)
						p.AI.GetEvent<KissEvent>().Enabled = b;
				},

				(p) => p.AI.GetEvent<KissEvent>()?.Enabled ?? false);
		}

		public static IItem Finish()
		{
			return new ButtonItem("Finish", (p) =>
			{
				Cue.Instance.Finish.Start();
			});
		}

		public static IItem Genitals()
		{
			return new ButtonItem("Genitals", (p) =>
			{
				if (p != null)
					p.Clothing.GenitalsVisible = !p.Clothing.GenitalsVisible;
			});
		}

		public static IItem Breasts()
		{
			return new ButtonItem("Breasts", (p) =>
			{
				if (p != null)
					p.Clothing.BreastsVisible = !p.Clothing.BreastsVisible;
			});
		}

		public static IItem MovePlayer()
		{
			return new CheckBoxItem("Move player",
				(p, b) =>
				{
					if (Cue.Instance.Player != null)
						Cue.Instance.Player.VamAtom?.SetControlsForMoving(b);
				},

				null,
				(p) => Cue.Instance.Player.Body.Exists);
		}
	}


	interface IMenu
	{
		Person SelectedPerson { get; set; }

		void Destroy();
		void CheckInput(float s);
		void Update();
	}


	abstract class BasicMenu : IMenu
	{
		private List<UIActions.IItem> items_ = new List<UIActions.IItem>();
		private CircularIndex<Person> personSel_;
		private VUI.Root root_ = null;

		public BasicMenu()
		{
			personSel_ = new CircularIndex<Person>(
				Cue.Instance.ActivePersons, (p) => p.Body.Exists);

			Cue.Instance.Options.CustomMenuItems.Changed += UpdateItems;
			UpdateItems();
		}

		private void UpdateItems()
		{
			items_.Clear();
			foreach (var i in UIActions.All())
				items_.Add(i);

			SetPerson(personSel_.Index, false);
		}

		public Person SelectedPerson
		{
			get
			{
				return personSel_.Value;
			}

			set
			{
				personSel_.Index = (value?.PersonIndex ?? -1) ;
				SetPerson(personSel_.Index);
			}
		}


		protected List<UIActions.IItem> Items
		{
			get { return items_; }
		}

		protected VUI.Root Root
		{
			get { return root_; }
		}

		protected void SetRoot(VUI.Root r)
		{
			root_ = r;
		}

		protected bool Visible
		{
			set { root_.Visible = value; }
		}

		public virtual void Destroy()
		{
			if (root_ != null)
			{
				root_.Destroy();
				root_ = null;
			}

			items_.Clear();
		}

		public abstract void CheckInput(float s);

		public virtual void Update()
		{
			if (root_ != null && root_.Visible)
			{
				for (int i = 0; i < items_.Count; ++i)
					items_[i].Update();

				root_?.Update();
			}
		}

		public void NextPerson()
		{
			personSel_.Next(+1);
			SetPerson(personSel_.Index);
		}

		public void PreviousPerson()
		{
			personSel_.Next(-1);
			SetPerson(personSel_.Index);
		}

		private void SetPerson(int index, bool fire = true)
		{
			var p = SelectedPerson;
			for (int i = 0; i < items_.Count; ++i)
				items_[i].Person = p;

			if (fire)
				PersonChanged();

			Cue.Instance.Save();
		}

		protected abstract void PersonChanged();
	}
}
