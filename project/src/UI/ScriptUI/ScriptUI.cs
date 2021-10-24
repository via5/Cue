using SimpleJSON;
using System.Collections.Generic;

namespace Cue
{
	class ScriptUI
	{
		private VUI.Root root_ = null;

		private VUI.Panel panel_ = new VUI.Panel();
		private MiscTab misc_;
		private SubTabs tabs_ = new SubTabs();
		private bool inited_ = false;
		private string initialSelection_ = "";

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

			if (initialSelection_ != "")
				tabs_.SetSelectionFromString(initialSelection_);
		}

		public JSONClass ToJSON()
		{
			if (!inited_)
				return null;

			var o = new JSONClass();

			o["tab"] = tabs_.GetSelectedString();

			return o;
		}

		public void Load(JSONClass o)
		{
			if (o != null && o.HasKey("tab"))
				initialSelection_ = o["tab"].Value;
		}

		public void Update(float s)
		{
			tabs_.Update(s);
			misc_.UpdateInput(s);
			root_.Update();
		}

		public void UpdateTickers()
		{
			if (I.Instance.Updated)
				misc_.UpdateTickers();
		}

		public void OnPluginState(bool b)
		{
			tabs_.OnPluginState(b);
		}
	}


	class SubTabs
	{
		private VUI.Tabs tabsWidget_ = new VUI.Tabs();
		private List<Tab> tabs_ = new List<Tab>();
		private float updateInterval_ = 0;
		private float updateElapsed_ = 1000;
		private bool dirty_ = false;

		public SubTabs()
		{
			tabsWidget_.SelectionChanged += (i) =>
			{
				dirty_ = true;
				Cue.Instance.Save();
			};
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

		public string GetSelectedString()
		{
			var s = tabsWidget_.SelectedCaption;

			var st = (tabsWidget_.SelectedWidget as Tab)?.SubTabs;
			if (st != null)
				s += "/" + st.GetSelectedString();

			return s;
		}

		public void SetSelectionFromString(string s)
		{
			var slash = s.IndexOf("/");

			string caption;
			if (slash == -1)
				caption = s;
			else
				caption = s.Substring(0, slash);

			tabsWidget_.Select(caption);

			var st = (tabsWidget_.SelectedWidget as Tab)?.SubTabs;
			if (st != null)
				st.SetSelectionFromString(s.Substring(slash + 1));
		}

		public T AddTab<T>(T t) where T : Tab
		{
			tabs_.Add(t);
			tabsWidget_.AddTab(t.Title, t);
			return t;
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


	abstract class Tab : VUI.Panel
	{
		private string title_;
		private SubTabs subTabs_ = null;

		protected Tab(string title, bool hasSubTabs)
		{
			title_ = title;

			if (hasSubTabs)
			{
				subTabs_ = new SubTabs();
				Layout = new VUI.BorderLayout();
				Add(subTabs_.TabsWidget, VUI.BorderLayout.Center);
			}
		}

		public string Title
		{
			get { return title_; }
		}

		public SubTabs SubTabs
		{
			get { return subTabs_; }
		}

		protected T AddSubTab<T>(T t) where T : Tab
		{
			return subTabs_.AddTab(t);
		}

		public void Update(float s)
		{
			if (subTabs_ != null)
				subTabs_.Update(s);

			DoUpdate(s);
		}

		protected virtual void DoUpdate(float s)
		{
			// no-op
		}

		public void OnPluginState(bool b)
		{
			if (subTabs_ != null)
				subTabs_.OnPluginState(b);

			DoOnPluginState(b);
		}

		protected virtual void DoOnPluginState(bool b)
		{
			// no-op
		}
	}


	class PersonTab : Tab
	{
		private Person person_;

		public PersonTab(Person p)
			: base(p.ID, true)
		{
			person_ = p;

			AddSubTab(new PersonSettingsTab(person_));
			AddSubTab(new PersonStateTab(person_));
			AddSubTab(new PersonAITab(person_));
			AddSubTab(new PersonBodyTab(person_));
			AddSubTab(new PersonAnimationsTab(person_));
			AddSubTab(new PersonDumpTab(person_));
		}
	}
}
