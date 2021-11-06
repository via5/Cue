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


		public static List<IItem> All()
		{
			return new List<IItem>
			{
				Hand(), Mouth(), Thrust(), CanKiss(), Strapon(),
				Genitals(), Breasts(), MovePlayer()
			};
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

		public static IItem Mouth()
		{
			return new CheckBoxItem("Mouth",
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

		public static IItem CanKiss()
		{
			return new CheckBoxItem("Can kiss",
				(p, b) =>
				{
					if (p != null)
						p.Options.CanKiss = b;
				},

				(p) => p.Options.CanKiss);
		}

		public static IItem Strapon()
		{
			return new CheckBoxItem("Strapon",
				(p, b) =>
				{
					if (p != null)
						p.Body.Strapon = b;
				},

				(p) => p.Body.Strapon,
				(p) => !p.Atom.IsMale);
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
		void CheckInput();
		void Update();
	}


	abstract class BasicMenu : IMenu
	{
		private List<UIActions.IItem> items_ = new List<UIActions.IItem>();
		private int personSel_ = -1;
		private VUI.Root root_ = null;

		public BasicMenu()
		{
			foreach (var i in UIActions.All())
				items_.Add(i);

			if (Cue.Instance.ActivePersons.Length == 0)
				SetPerson(-1, false);
			else if (personSel_ < 0 || personSel_ >= Cue.Instance.ActivePersons.Length)
				SetPerson(GetPerson(-1, +1), false);
		}

		public Person SelectedPerson
		{
			get
			{
				if (personSel_ < 0 || personSel_ >= Cue.Instance.ActivePersons.Length)
					return null;
				else
					return Cue.Instance.ActivePersons[personSel_] as Person;
			}

			set
			{
				if (value == null)
					SetPerson(-1);
				else
					SetPerson(value.PersonIndex);
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


		public void Destroy()
		{
			if (root_ != null)
			{
				root_.Destroy();
				root_ = null;
			}

			items_.Clear();
		}

		public abstract void CheckInput();

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
			DoChangePerson(+1);
		}

		public void PreviousPerson()
		{
			DoChangePerson(-1);
		}

		private void DoChangePerson(int dir)
		{
			if (dir == 0)
				return;

			int newSel = GetPerson(personSel_, dir);

			if (newSel >= Cue.Instance.ActivePersons.Length)
				newSel = -1;

			if (newSel != personSel_)
				SetPerson(newSel);
		}

		private int GetPerson(int current, int dir)
		{
			var ps = Cue.Instance.ActivePersons;

			int s = current;

			if (s < 0 || s >= ps.Length)
			{
				current = 0;
				s = 0;
			}
			else
			{
				s += dir;
			}


			for (; ; )
			{
				if (s < 0)
					s = ps.Length - 1;
				else if (s >= ps.Length)
					s = 0;

				if (s == current)
					break;

				if (ValidPerson(ps[s]))
					return s;

				s += dir;
			}

			return current;
		}

		private bool ValidPerson(Person p)
		{
			return p.Body.Exists;
		}

		private void SetPerson(int index, bool fire = true)
		{
			personSel_ = index;

			var p = SelectedPerson;
			for (int i = 0; i < items_.Count; ++i)
				items_[i].Person = p;

			if (fire)
				PersonChanged();
		}

		protected abstract void PersonChanged();
	}
}
