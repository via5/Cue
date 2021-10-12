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
				Hand(), BJ(), Thrust(), CanKiss(), Strapon(),
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

		public static IItem BJ()
		{
			return new CheckBoxItem("BJ",
				(p, b) =>
				{
					if (p != null && p != Cue.Instance.Player)
					{
						if (b)
							p.Blowjob.Start();
						else
							p.Blowjob.Stop();
					}
				},

				(p) => p.Blowjob.Active);
		}

		public static IItem Thrust()
		{
			return new CheckBoxItem("Thrust",
				(p, b) =>
				{
					if (p != null)
						p.AI.GetEvent<SexEvent>().Active = b;
				},

				(p) => p.AI.GetEvent<SexEvent>()?.Active ?? false);
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
}
