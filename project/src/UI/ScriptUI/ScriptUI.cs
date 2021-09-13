using System.Collections.Generic;

namespace Cue
{
	class TabContainer
	{
		private VUI.Tabs tabsWidget_ = new VUI.Tabs();
		private List<Tab> tabs_ = new List<Tab>();
		private float updateInterval_ = 0;
		private float updateElapsed_ = 1000;
		private bool dirty_ = false;

		public TabContainer()
		{
			tabsWidget_.SelectionChanged += (i) => { dirty_ = true; };
		}

		public float UpdateInterval
		{
			get { return updateInterval_; }
			set { updateInterval_ = value; }
		}

		public VUI.Tabs TabsWidget
		{
			get { return tabsWidget_; }
		}

		public void AddTab(Tab t)
		{
			tabs_.Add(t);
			tabsWidget_.AddTab(t.Title, t);
		}

		public void Update(float s)
		{
			updateElapsed_ += s;
			if (updateElapsed_ > updateInterval_ || dirty_)
			{
				updateElapsed_ = 0;
				dirty_ = false;

				for (int i = 0; i < tabs_.Count; ++i)
				{
					if (tabs_[i].IsVisibleOnScreen())
						tabs_[i].Update(s);
				}
			}
		}

		public void OnPluginState(bool b)
		{
			foreach (var t in tabs_)
				t.OnPluginState(b);
		}
	}


	class ScriptUI
	{
		private VUI.Root root_ = null;

		private VUI.Panel panel_ = new VUI.Panel();
		private MiscTab misc_;
		private TabContainer tabs_ = new TabContainer();
		private bool inited_ = false;

		public void Init()
		{
			if (inited_)
				return;

			tabs_.UpdateInterval = 0.3f;

			inited_ = true;
			misc_ = new MiscTab();

			foreach (var p in Cue.Instance.AllPersons)
				tabs_.AddTab(new PersonTab(p));

			tabs_.AddTab(misc_);
			tabs_.AddTab(new UnityTab());

			root_ = new VUI.Root(CueMain.Instance.MVRScriptUI);
			root_.ContentPanel.Layout = new VUI.BorderLayout();
			root_.ContentPanel.Add(panel_, VUI.BorderLayout.Center);

			panel_.Layout = new VUI.BorderLayout();
			panel_.Add(tabs_.TabsWidget, VUI.BorderLayout.Center);
		}

		public void Update(float s)
		{
			tabs_.Update(s);
			root_.Update();
		}

		public void UpdateTickers()
		{
			if (I.Updated)
				misc_.UpdateTickers();
		}

		public void OnPluginState(bool b)
		{
			tabs_.OnPluginState(b);
		}
	}


	abstract class Tab : VUI.Panel
	{
		private string title_;

		protected Tab(string title)
		{
			title_ = title;
		}

		public string Title
		{
			get { return title_; }
		}

		public abstract void Update(float s);

		public virtual void OnPluginState(bool b)
		{
			// no-op
		}
	}


	class PersonTab : Tab
	{
		private Person person_;
		private TabContainer tabs_ = new TabContainer();

		public PersonTab(Person p)
			: base(p.ID)
		{
			person_ = p;

			tabs_.AddTab(new PersonSettingsTab(person_));
			tabs_.AddTab(new PersonStateTab(person_));
			tabs_.AddTab(new PersonAITab(person_));
			tabs_.AddTab(new PersonExcitementTab(person_));
			tabs_.AddTab(new PersonBodyTab(person_));
//			tabs_.AddTab(new PersonAnimationsTab(person_));
			tabs_.AddTab(new PersonDumpTab(person_));

			Layout = new VUI.BorderLayout();
			Add(tabs_.TabsWidget, VUI.BorderLayout.Center);
		}

		public override void Update(float s)
		{
			tabs_.Update(s);
		}

		public override void OnPluginState(bool b)
		{
			base.OnPluginState(b);
			tabs_.OnPluginState(b);
		}
	}
}
