using System.Collections.Generic;

namespace Cue
{
	class PersonAnimationsTab : Tab
	{
		private Person person_;
		private VUI.ComboBox<IAnimation> anims_ = new VUI.ComboBox<IAnimation>();
		private VUI.ListView<string> list_ = new VUI.ListView<string>();
		private List<IAnimation> oldList_ = new List<IAnimation>();
		private VUI.CheckBox setDebug_ = new VUI.CheckBox("Debug");
		private VUI.CheckBox all_ = new VUI.CheckBox("All");
		private bool ignore_ = false;
		private DebugLines debug_ = null;

		public PersonAnimationsTab(Person person)
			: base("Anim", false)
		{
			person_ = person;

			list_.Font = VUI.Style.Theme.MonospaceFont;
			list_.FontSize = 22;

			var top = new VUI.Panel(new VUI.BorderLayout(5));
			top.Add(anims_, VUI.BorderLayout.Center);

			var right = new VUI.Panel(new VUI.HorizontalFlow(5));
			right.Add(setDebug_);
			right.Add(all_);

			top.Add(right, VUI.BorderLayout.Right);

			Layout = new VUI.BorderLayout(10);
			Add(top, VUI.BorderLayout.Top);
			Add(list_, VUI.BorderLayout.Center);

			UpdateList();

			list_.SelectionChanged += (s) => Changed();
			setDebug_.Changed += (b) => SetDebug(b);
			all_.Changed += (b) => Changed();
		}

		private void Changed()
		{
			try
			{
				ignore_ = true;
				DoUpdate(0);
			}
			finally
			{
				ignore_ = false;
			}
		}

		protected override void DoUpdate(float s)
		{
			if (ignore_) return;

			UpdateList();

			if (debug_ == null)
				debug_ = new DebugLines();

			debug_.Clear();

			anims_.Selected?.Debug(debug_);
			list_.SetItems(debug_.MakeArray());

			var a = anims_.Selected;
			if (a == null)
				setDebug_.Checked = false;
			else
				setDebug_.Checked = a.DebugRender;
		}

		private void SetDebug(bool b)
		{
			var a = anims_.Selected;
			if (a != null)
				a.DebugRender = b;
		}

		private void UpdateList()
		{
			if (ignore_) return;

			var items = new List<IAnimation>();
			var oldSel = anims_.Selected;

			if (all_.Checked)
			{
				foreach (var a in Resources.Animations.GetAll())
					items.Add(a.Sys);
			}
			else
			{
				foreach (var p in person_.Animator.Players)
				{
					foreach (var a in p.GetPlaying())
						items.Add(a);
				}
			}

			if (!ListsEqual(items, oldList_))
			{
				anims_.SetItems(items, oldSel);
				oldList_ = items;
			}
		}

		private bool ListsEqual(List<IAnimation> a, List<IAnimation> b)
		{
			if (a.Count != b.Count)
				return false;

			for (int i = 0; i < a.Count; ++i)
			{
				if (a[i] != b[i])
					return false;
			}

			return true;
		}
	}
}
