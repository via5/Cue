using System.Collections.Generic;

namespace Cue
{
	class MiscTab : Tab
	{
		private MiscTimesTab times_;

		public MiscTab()
			: base("Misc", true)
		{
			AddSubTab(new MiscInputTab());
			AddSubTab(new MiscNavTab());
			times_ = AddSubTab(new MiscTimesTab());
			AddSubTab(new MiscLogTab());
		}

		public void UpdateTickers()
		{
			times_.UpdateTickers();
		}
	}


	class MiscNavTab : Tab
	{
		private VUI.CheckBox navmeshes_ = new VUI.CheckBox("Navmeshes");
		private VUI.Button renav_ = new VUI.Button("Update nav");

		public MiscNavTab()
			: base("Nav", false)
		{
			Layout = new VUI.VerticalFlow(0, false);

			Add(new VUI.Label("nav is disabled in this build"));
			Add(new VUI.Spacer(20));
			Add(navmeshes_);
			Add(renav_);

			navmeshes_.Enabled = false;
			renav_.Enabled = false;

			navmeshes_.Changed += (b) => Cue.Instance.Sys.Nav.Render = b;
			renav_.Clicked += Cue.Instance.Sys.Nav.Update;
		}
	}


	class MiscTimesTab : Tab
	{
		private VUI.Label[] tickers_ = new VUI.Label[I.TickerCount];

		public MiscTimesTab()
			: base("Times", false)
		{
			var gl = new VUI.GridLayout(2);
			gl.HorizontalStretch = new List<bool>() { false, true };
			gl.HorizontalSpacing = 40;

			var p = new VUI.Panel(gl);

			for (int i = 0; i < I.TickerCount; ++i)
			{
				tickers_[i] = new VUI.Label();
				p.Add(new VUI.Label(new string(' ', I.Depth(i) * 2) + I.Name(i)));
				p.Add(tickers_[i]);
			}


			Layout = new VUI.VerticalFlow();
			Add(p);
		}

		public void UpdateTickers()
		{
			if (IsVisibleOnScreen())
			{
				for (int i = 0; i < I.TickerCount; ++i)
					tickers_[i].Text = I.Get(i).ToString();
			}
		}
	}


	class MiscLogTab : Tab
	{
		private List<VUI.CheckBox> cbs_ = new List<VUI.CheckBox>();

		public MiscLogTab()
			: base("Log", false)
		{
			var names = Logger.Names;
			var enabled = Logger.Enabled;

			Layout = new VUI.VerticalFlow(0, false);

			for (int i=0; i < names.Length; ++i)
			{
				var cb = new VUI.CheckBox(
					names[i], OnChecked, (enabled & (1 << i)) != 0);

				cbs_.Add(cb);
				Add(cb);
			}
		}

		private void OnChecked(bool b)
		{
			int e = 0;

			for (int i = 0; i < cbs_.Count; ++i)
			{
				if (cbs_[i].Checked)
					e |= (1 << i);
			}

			Logger.Enabled = e;
			Cue.Instance.Save();
		}
	}

	class MiscInputTab : Tab
	{
		public MiscInputTab()
			: base("Input", false)
		{
		}
	}
}
