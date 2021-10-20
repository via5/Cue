using System;
using System.Collections.Generic;

namespace Cue
{
	class MiscTab : Tab
	{
		private MiscTimesTab times_;
		private MiscInputTab input_;

		public MiscTab()
			: base("Misc", true)
		{
			input_ = AddSubTab(new MiscInputTab());
			times_ = AddSubTab(new MiscTimesTab());
			AddSubTab(new MiscLogTab());
		}

		public void UpdateTickers()
		{
			times_.UpdateTickers();
		}

		public void UpdateInput(float s)
		{
			input_.UpdateInput(s);
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
			var inst = I.Instance;

			for (int i = 0; i < I.TickerCount; ++i)
			{
				tickers_[i] = new VUI.Label();
				p.Add(new VUI.Label(new string(' ', inst.Depth(i) * 2) + inst.Name(i)));
				p.Add(tickers_[i]);
			}


			Layout = new VUI.VerticalFlow();
			Add(p);
		}

		public void UpdateTickers()
		{
			if (IsVisibleOnScreen())
			{
				var inst = I.Instance;

				for (int i = 0; i < I.TickerCount; ++i)
					tickers_[i].Text = inst.Get(i).ToString();
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
		class Input
		{
			private string name_;
			private Func<bool> fbool_ = null;
			private Func<string> fstring_ = null;
			private VUI.Label label_ = null;
			private float onElapsed_ = 1000;

			public Input(string name, Func<bool> f)
			{
				name_ = name;
				fbool_ = f;
				label_ = new VUI.Label();
			}

			public Input(string name, Func<string> f)
			{
				name_ = name;
				fstring_ = f;
				label_ = new VUI.Label();
				label_.Font = VUI.Style.Theme.MonospaceFont;
			}

			public string Name
			{
				get { return name_; }
			}

			public VUI.Widget Widget
			{
				get { return label_; }
			}

			public void Update(float s)
			{
				if (fbool_ != null)
					UpdateBool(s);
				else if (fstring_ != null)
					UpdateString(s);
			}

			private void UpdateBool(float s)
			{
				if (fbool_())
				{
					label_.TextColor = Sys.Vam.U.ToUnity(Color.Green);
					label_.Text = "on";
					onElapsed_ = 0;
				}
				else
				{
					onElapsed_ += s;

					if (onElapsed_ < 1)
					{
						label_.Text = "on";
						label_.TextColor = MakeColor();
					}
					else
					{
						label_.Text = "";
					}
				}
			}

			private void UpdateString(float s)
			{
				label_.Text = fstring_();
			}

			private UnityEngine.Color MakeColor()
			{
				var from = Sys.Vam.U.FromUnity(VUI.Style.Theme.TextColor);
				var to = Sys.Vam.U.FromUnity(VUI.Style.Theme.BackgroundColor);
				var c = Color.Lerp(from, to, onElapsed_);

				return Sys.Vam.U.ToUnity(c);
			}
		}


		private List<Input> inputs_ = new List<Input>();


		public MiscInputTab()
			: base("Input", false)
		{
			var i = Cue.Instance.Sys.Input;

			inputs_.Add(new Input("ShowLeftMenu", () => i.ShowLeftMenu));
			inputs_.Add(new Input("ShowRightMenu", () => i.ShowRightMenu));
			inputs_.Add(new Input("LeftAction", () => i.LeftAction));
			inputs_.Add(new Input("RightAction", () => i.RightAction));
			inputs_.Add(new Input("Select", () => i.Select));
			inputs_.Add(new Input("Action", () => i.Action));
			inputs_.Add(new Input("ToggleControls", () => i.ToggleControls));
			inputs_.Add(new Input("MenuUp", () => i.MenuUp));
			inputs_.Add(new Input("MenuDown", () => i.MenuDown));
			inputs_.Add(new Input("MenuLeft", () => i.MenuLeft));
			inputs_.Add(new Input("MenuRight", () => i.MenuRight));
			inputs_.Add(new Input("MenuSelect", () => i.MenuSelect));
			inputs_.Add(new Input("Sys", () => i.DebugString()));

			var gl = new VUI.GridLayout(2);
			gl.HorizontalStretch = new List<bool> { false, true };
			var p = new VUI.Panel(gl);

			p.Add(new VUI.Label("VR"));
			p.Add(new VUI.Label(i.VRInfo()));

			foreach (var w in inputs_)
			{
				p.Add(new VUI.Label(w.Name));
				p.Add(w.Widget);
			}

			Layout = new VUI.VerticalFlow();
			Add(p);
		}

		public void UpdateInput(float s)
		{
			if (IsVisibleOnScreen())
			{
				for (int i = 0; i < inputs_.Count; ++i)
					inputs_[i].Update(s);
			}
		}
	}
}
