using System;
using System.Collections.Generic;

namespace Cue
{
	class MiscTab : Tab
	{
		private MiscTimesTab times_;
		private MiscInputTab input_;
		private MiscDebugInputTab dinput_;

		public MiscTab()
			: base("Misc", true)
		{
			input_ = AddSubTab(new MiscInputTab());
			dinput_ = AddSubTab(new MiscDebugInputTab());
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
			dinput_.UpdateInput(s);
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
				var name = new VUI.Label(new string(' ', inst.Depth(i) * 2) + inst.Name(i));
				name.Font = VUI.Style.Theme.MonospaceFont;
				name.FontSize = 22;

				var ticker = new VUI.Label();
				ticker.Font = VUI.Style.Theme.MonospaceFont;
				ticker.FontSize = 22;

				p.Add(name);
				p.Add(ticker);

				tickers_[i] = ticker;
			}


			Layout = new VUI.VerticalFlow();
			Add(p);
		}

		public void UpdateTickers()
		{
			if (IsVisibleOnScreen())
			{
				var inst = I.Instance;
				inst.Enabled = true;

				if (I.Instance.Updated)
				{
					for (int i = 0; i < I.TickerCount; ++i)
						tickers_[i].Text = inst.Get(i).ToString();
				}
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

	class InputDisplay
	{
		private string name_;
		private Func<bool> fbool_ = null;
		private Func<string> fstring_ = null;
		private VUI.Label label_ = null;
		private float onElapsed_ = 1000;

		public InputDisplay(string name)
		{
			name_ = name;
			label_ = new VUI.Label();
		}

		public InputDisplay(string name, Func<bool> f)
		{
			name_ = name;
			fbool_ = f;
			label_ = new VUI.Label();
		}

		public InputDisplay(string name, Func<string> f)
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

		public void UpdateValue(float s, string v)
		{
			onElapsed_ += s;

			if (v != label_.Text)
			{
				if (v != "" || onElapsed_ > 1)
				{
					label_.TextColor = Sys.Vam.U.ToUnity(Color.Green);
					label_.Text = v;
					onElapsed_ = 0;
				}
			}

			if (onElapsed_ < 1)
				label_.TextColor = MakeColor();
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

		private UnityEngine.Color MakeTextColor()
		{
			var from = Color.Green;
			var to = Sys.Vam.U.FromUnity(VUI.Style.Theme.TextColor);
			var c = Color.Lerp(from, to, onElapsed_);

			return Sys.Vam.U.ToUnity(c);
		}
	}


	class MiscInputTab : Tab
	{
		private List<InputDisplay> inputs_ = new List<InputDisplay>();


		public MiscInputTab()
			: base("Input", false)
		{
			var i = Cue.Instance.Sys.Input;

			inputs_.Add(new InputDisplay("ShowLeftMenu", () => i.ShowLeftMenu));
			inputs_.Add(new InputDisplay("ShowRightMenu", () => i.ShowRightMenu));
			inputs_.Add(new InputDisplay("LeftAction", () => i.LeftAction));
			inputs_.Add(new InputDisplay("RightAction", () => i.RightAction));
			inputs_.Add(new InputDisplay("Select", () => i.Select));
			inputs_.Add(new InputDisplay("Action", () => i.Action));
			inputs_.Add(new InputDisplay("ToggleControls", () => i.ToggleControls));
			inputs_.Add(new InputDisplay("MenuUp", () => i.MenuUp));
			inputs_.Add(new InputDisplay("MenuDown", () => i.MenuDown));
			inputs_.Add(new InputDisplay("MenuLeft", () => i.MenuLeft));
			inputs_.Add(new InputDisplay("MenuRight", () => i.MenuRight));
			inputs_.Add(new InputDisplay("MenuSelect", () => i.MenuSelect));

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


	class MiscDebugInputTab : Tab
	{
		private VUI.Panel panel_;
		private List<InputDisplay> values_ = new List<InputDisplay>();

		public MiscDebugInputTab()
			: base("Debug input", false)
		{
			var gl = new VUI.GridLayout(2);
			gl.HorizontalStretch = new List<bool> { false, true };
			panel_ = new VUI.Panel(gl);

			Layout = new VUI.BorderLayout();
			Add(panel_, VUI.BorderLayout.Center);
		}

		public void UpdateInput(float s)
		{
			if (IsVisibleOnScreen())
			{
				var sys = Cue.Instance.Sys.Input;
				var d = sys.Debug();

				if (values_.Count == 0)
				{
					for (int i = 0; i < d.Count; ++i)
					{
						var caption = new VUI.Label(d[i].first);
						caption.FontSize = 20;

						var v = new InputDisplay(d[i].first);
						var label = v.Widget;
						label.FontSize = 20;

						values_.Add(v);

						panel_.Add(caption);
						panel_.Add(label);
					}
				}
				else
				{
					for (int i = 0; i < d.Count; ++i)
						values_[i].UpdateValue(s, d[i].second);
				}
			}
		}
	}
}
