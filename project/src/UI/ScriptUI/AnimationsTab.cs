﻿using System.Collections.Generic;

namespace Cue
{
	class PersonDebugAnimationsTab : Tab
	{
		private Person person_;
		private VUI.ComboBox<Animation> anims_ = new VUI.ComboBox<Animation>();
		private VUI.ListView<string> list_ = new VUI.ListView<string>();
		private List<Animation> oldList_ = new List<Animation>();
		private VUI.CheckBox setDebug_ = new VUI.CheckBox("Debug");
		private VUI.CheckBox all_ = new VUI.CheckBox("All");
		private bool ignore_ = false;
		private DebugLines debug_ = null;

		public PersonDebugAnimationsTab(Person person)
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
				setDebug_.Checked = a.Sys.DebugRender;
		}

		private void SetDebug(bool b)
		{
			var a = anims_.Selected;
			if (a != null)
				a.Sys.DebugRender = b;
		}

		private void UpdateList()
		{
			if (ignore_) return;

			var items = new List<Animation>();
			var oldSel = anims_.Selected;

			if (all_.Checked)
			{
				foreach (var a in person_.Personality.Animations.GetAll())
					items.Add(a);
			}
			else
			{
				foreach (var a in person_.Animator.GetPlaying())
					items.Add(a);
			}

			if (!ListsEqual(items, oldList_))
			{
				anims_.SetItems(items, oldSel);
				oldList_ = items;
			}
		}

		private bool ListsEqual(List<Animation> a, List<Animation> b)
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
