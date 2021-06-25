using System;
using System.Collections;
using System.Collections.Generic;

namespace Cue
{
	class ScriptUI
	{
		private const float UpdateInterval = 0.3f;

		private VUI.Root root_ = null;

		private VUI.Panel panel_ = new VUI.Panel();
		private VUI.Tabs tabsWidget_ = new VUI.Tabs();
		private List<Tab> tabs_ = new List<Tab>();
		private float updateElapsed_ = 1000;
		private MiscTab misc_;

		public void Init()
		{
			misc_ = new MiscTab();

			foreach (var p in Cue.Instance.AllPersons)
				tabs_.Add(new PersonTab(p));

			tabs_.Add(misc_);
			tabs_.Add(new UnityTab());

			root_ = Cue.Instance.Sys.CreateScriptUI();
			root_.ContentPanel.Layout = new VUI.BorderLayout();
			root_.ContentPanel.Add(panel_, VUI.BorderLayout.Center);

			panel_.Layout = new VUI.BorderLayout();
			panel_.Add(tabsWidget_, VUI.BorderLayout.Center);

			foreach (var t in tabs_)
				tabsWidget_.AddTab(t.Title, t);
		}

		public void Update(float s, Tickers tickers)
		{
			updateElapsed_ += s;
			if (updateElapsed_ > UpdateInterval)
			{
				for (int i = 0; i < tabs_.Count; ++i)
				{
					if (tabs_[i].IsVisibleOnScreen())
						tabs_[i].Update(s);
				}

				updateElapsed_ = 0;
			}

			if (tickers.update.Updated)
				misc_.UpdateTickers(tickers);

			root_.Update();
		}

		public void OnPluginState(bool b)
		{
			foreach (var t in tabs_)
				t.OnPluginState(b);
		}
	}


	abstract class Tab : VUI.Panel
	{
		public abstract string Title { get; }
		public abstract void Update(float s);

		public virtual void OnPluginState(bool b)
		{
			// no-op
		}
	}


	class PersonTab : Tab
	{
		private Person person_;
		private VUI.Tabs tabsWidget_ = new VUI.Tabs();
		private List<Tab> tabs_ = new List<Tab>();

		public PersonTab(Person p)
		{
			person_ = p;

			tabs_.Add(new PersonStateTab(person_));
			tabs_.Add(new PersonAITab(person_));
			tabs_.Add(new PersonExcitementTab(person_));
			tabs_.Add(new PersonBodyTab(person_));
			tabs_.Add(new PersonAnimationsTab(person_));
			tabs_.Add(new PersonDumpTab(person_));

			foreach (var t in tabs_)
				tabsWidget_.AddTab(t.Title, t);

			Layout = new VUI.BorderLayout();
			Add(tabsWidget_, VUI.BorderLayout.Center);
		}

		public override string Title
		{
			get { return person_.ID; }
		}

		public override void Update(float s)
		{
			for (int i = 0; i < tabs_.Count; ++i)
			{
				if (tabs_[i].IsVisibleOnScreen())
					tabs_[i].Update(s);
			}
		}

		public override void OnPluginState(bool b)
		{
			base.OnPluginState(b);
			foreach (var t in tabs_)
				t.OnPluginState(b);
		}
	}
}
