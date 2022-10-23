using System.Collections.Generic;

namespace Cue
{
	class OptionsTab : Tab
	{
		public OptionsTab()
			: base("Options", true)
		{
			AddSubTab(new MainOptionsTab());
			AddSubTab(new FinishOptionsTab());
			AddSubTab(new EffectsOptionsTab());
			AddSubTab(new SoundOptionsTab());
			AddSubTab(new MenuOptionsTab());
		}

		public override bool DebugOnly
		{
			get { return false; }
		}
	}


	class TriggersPanel : VUI.Panel
	{
		private TriggersOptions opts_;
		private string triggerNamePlaceholder_;
		private VUI.Panel buttons_;

		public TriggersPanel(
			TriggersOptions opts, string addCaption, string infoCaption,
			string triggerNamePlaceholder)
		{
			opts_ = opts;
			triggerNamePlaceholder_ = triggerNamePlaceholder;

			var center = new VUI.Panel(new VUI.BorderLayout(10));
			var controls = new VUI.Panel(new VUI.HorizontalFlow(20));

			controls.Add(new VUI.Button(addCaption, OnAdd));
			controls.Add(new VUI.Label(infoCaption));
			buttons_ = new VUI.Panel(new VUI.VerticalFlow(10));

			center.Add(controls, VUI.BorderLayout.Top);
			center.Add(buttons_, VUI.BorderLayout.Center);

			Layout = new VUI.BorderLayout();
			Add(center, VUI.BorderLayout.Center);
		}

		public void Update()
		{
			if (buttons_.Children.Count != opts_.Triggers.Length)
				Rebuild();
		}

		private void Rebuild()
		{
			buttons_.RemoveAllChildren();
			foreach (var m in opts_.Triggers)
				buttons_.Add(CreatePanel(m));
		}

		private VUI.Panel CreatePanel(CustomTrigger m)
		{
			var p = new VUI.Panel(new VUI.HorizontalFlow(10));
			var c = p.Add(new VUI.TextBox(m.Caption, triggerNamePlaceholder_));
			c.Edited += (s) => { OnCaption(m, s); };
			p.Add(new VUI.Button("Edit trigger", () => OnEditTrigger(m)));
			p.Add(new VUI.ToolButton("X", () => OnDelete(m)));
			return p;
		}

		private void OnAdd()
		{
			opts_.AddTrigger();
		}

		private void OnEditTrigger(CustomTrigger m)
		{
			m.Trigger.Edit(() => opts_.FireTriggersChanged());
		}

		private void OnCaption(CustomTrigger m, string s)
		{
			m.Caption = s;
		}

		private void OnDelete(CustomTrigger m)
		{
			opts_.RemoveTrigger(m);
		}
	}


	class MainOptionsTab : Tab
	{
		private VUI.FloatTextSlider excitement_;
		private VUI.CheckBox idlePose_, autoHands_, autoHead_, handLinking_;
		private VUI.CheckBox straponPhysical_, ignoreCamera_, devMode_;
		private bool ignore_ = false;

		public MainOptionsTab()
			: base("Main", false)
		{
			var ly = new VUI.VerticalFlow(5);
			var p = new VUI.Panel(ly);

			var o = Cue.Instance.Options;

			var ep = new VUI.Panel(new VUI.HorizontalFlow());
			ep.Add(new VUI.Label("Global excitement speed"));
			excitement_ = ep.Add(new VUI.FloatTextSlider(0, 10, OnExcitementChanged));

			p.Add(ep);
			p.Add(new VUI.Spacer(20));

			idlePose_ = p.Add(new VUI.CheckBox("Idle animation", OnIdlePose, o.IdlePose));
			p.Add(new VUI.Label(
				"Moves body parts around randomly.",
				VUI.Label.Wrap));
			p.Add(new VUI.Spacer(20));

			autoHands_ = p.Add(new VUI.CheckBox("Auto hands", OnAutoHands, o.AutoHands));
			autoHead_ = p.Add(new VUI.CheckBox("Auto head", OnAutoHead, o.AutoHead));
			p.Add(new VUI.Label(
				"Starts events automatically when a hand or head is close to " +
				"genitals.",
				VUI.Label.Wrap));
			p.Add(new VUI.Spacer(20));

			handLinking_ = p.Add(new VUI.CheckBox("Hand linking", OnHandLinking, o.HandLinking));
			p.Add(new VUI.Label(
				"Enables linking hands to body parts when they're close enough.",
				VUI.Label.Wrap));
			p.Add(new VUI.Spacer(20));

			straponPhysical_ = p.Add(new VUI.CheckBox("Strapon physical", OnStraponPhysical, o.StraponPhysical));
			p.Add(new VUI.Label(
				"Treats strapons the same as genitals instead of limiting " +
				"their impact on excitement.",
				VUI.Label.Wrap));
			p.Add(new VUI.Spacer(20));

			ignoreCamera_ = p.Add(new VUI.CheckBox("Ignore camera", OnIgnoreCamera, o.IgnoreCamera));
			p.Add(new VUI.Label(
				"Never look at the camera.",
				VUI.Label.Wrap));
			p.Add(new VUI.Spacer(20));

			devMode_ = p.Add(new VUI.CheckBox("Dev mode", OnDevMode, o.DevMode));
			p.Add(new VUI.Label("Enables a bunch of tabs.", VUI.Label.Wrap));


			var export = new VUI.Panel(new VUI.HorizontalFlow(10));
			export.Add(new VUI.Button("Export options...", OnExport));
			export.Add(new VUI.Button("Import options...", OnImport));
			p.Add(new VUI.Spacer(20));
			p.Add(export);

			Layout = new VUI.BorderLayout();
			Add(p, VUI.BorderLayout.Top);

			o.Changed += OnOptionsChanged;
			OnOptionsChanged();
		}

		public override bool DebugOnly
		{
			get { return false; }
		}

		private void OnOptionsChanged()
		{
			try
			{
				ignore_ = true;

				var o = Cue.Instance.Options;

				excitement_.Value = o.Excitement;
				idlePose_.Checked = o.IdlePose;
				autoHands_.Checked = o.AutoHands;
				autoHead_.Checked = o.AutoHead;
				handLinking_.Checked = o.HandLinking;
				straponPhysical_.Checked = o.StraponPhysical;
				ignoreCamera_.Checked = o.IgnoreCamera;
				devMode_.Checked = o.DevMode;
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void OnExcitementChanged(float f)
		{
			if (ignore_) return;
			Cue.Instance.Options.Excitement = f;
		}

		private void OnIdlePose(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.IdlePose = b;
		}

		private void OnAutoHands(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.AutoHands = b;
		}

		private void OnAutoHead(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.AutoHead = b;
		}

		private void OnHandLinking(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.HandLinking = b;
		}

		private void OnStraponPhysical(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.StraponPhysical = b;
		}

		private void OnIgnoreCamera(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.IgnoreCamera = b;
		}

		private void OnDevMode(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.DevMode = b;
		}

		private void OnExport()
		{
			var o = Cue.Instance.Options.ToJSON();
			Cue.Instance.Sys.SaveFileDialog("options", (f) =>
			{
				if (string.IsNullOrEmpty(f))
					return;

				Cue.Instance.Sys.WriteJSON(f, o);
			});
		}

		private void OnImport()
		{
			Cue.Instance.Sys.LoadFileDialog("options", (f) =>
			{
				if (string.IsNullOrEmpty(f))
					return;

				var o = Cue.Instance.Sys.ReadJSON(f);
				if (o == null)
				{
					Logger.Global.Error("failed to read json");
					return;
				}

				Cue.Instance.Options.Load(o.AsObject);
			});
		}
	}


	class MenuOptionsTab : Tab
	{
		private VUI.CheckBox leftMenu_, rightMenu_;
		private VUI.FloatTextSlider menuDelay_;
		private TriggersPanel triggers_;
		private bool ignore_ = false;

		public MenuOptionsTab()
			: base("Menu", false)
		{
			var o = Cue.Instance.Options;

			triggers_ = new TriggersPanel(
				o.Menus, "Add button", "Adds custom buttons to the menu.",
				"Button name");

			var ly = new VUI.VerticalFlow(10);
			ly.Expand = false;

			var top = new VUI.Panel(ly);

			leftMenu_ = top.Add(new VUI.CheckBox("Left hand menu", OnLeftMenu, o.LeftMenu));
			rightMenu_ = top.Add(new VUI.CheckBox("Right hand menu", OnRightMenu, o.RightMenu));

			top.Add(new VUI.Label(
				"Enables the VR menu on the left or right hand.",
				VUI.Label.Wrap));
			top.Add(new VUI.Spacer(20));

			menuDelay_ = top.Add(new VUI.FloatTextSlider(0, 5, OnMenuDelay));
			top.Add(new VUI.Label("Delay in seconds before showing the hand menus."));
			menuDelay_.MaximumSize = new VUI.Size(300, DontCare);

			top.Add(new VUI.Spacer(20));

			Layout = new VUI.BorderLayout(20);
			Add(top, VUI.BorderLayout.Top);
			Add(triggers_, VUI.BorderLayout.Center);

			o.Changed += OnOptionsChanged;
			OnOptionsChanged();
		}

		public override bool DebugOnly
		{
			get { return false; }
		}

		protected override void DoUpdate(float s)
		{
			triggers_.Update();
		}

		private void OnOptionsChanged()
		{
			try
			{
				ignore_ = true;

				var o = Cue.Instance.Options;

				menuDelay_.Value = o.MenuDelay;
				leftMenu_.Checked = o.LeftMenu;
				rightMenu_.Checked = o.RightMenu;
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void OnMenuDelay(float f)
		{
			if (ignore_) return;
			Cue.Instance.Options.MenuDelay = f;
		}

		private void OnLeftMenu(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.LeftMenu = b;
		}

		private void OnRightMenu(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.RightMenu = b;
		}
	}


	class SoundOptionsTab : Tab
	{
		private VUI.CheckBox hjAudio_, bjAudio_, kissAudio_;
		private VUI.CheckBox mutePlayer_;
		private bool ignore_ = false;

		public SoundOptionsTab()
			: base("Sound", false)
		{
			var o = Cue.Instance.Options;

			var ly = new VUI.VerticalFlow(10);
			ly.Expand = false;

			var p = new VUI.Panel(ly);

			hjAudio_ = p.Add(new VUI.CheckBox("Play HJ sounds", OnHJAudio, o.HJAudio));
			bjAudio_ = p.Add(new VUI.CheckBox("Play BJ sounds", OnBJAudio, o.BJAudio));
			kissAudio_ = p.Add(new VUI.CheckBox("Play kissing sounds", OnKissAudio, o.KissAudio));
			mutePlayer_ = p.Add(new VUI.CheckBox("Mute possessed atom", OnMutePlayer, o.MutePlayer));

			Layout = new VUI.BorderLayout(20);
			Add(p, VUI.BorderLayout.Top);

			o.Changed += OnOptionsChanged;
			OnOptionsChanged();
		}

		public override bool DebugOnly
		{
			get { return false; }
		}

		protected override void DoUpdate(float s)
		{
		}

		private void OnOptionsChanged()
		{
			try
			{
				ignore_ = true;

				var o = Cue.Instance.Options;

				hjAudio_.Checked = o.HJAudio;
				bjAudio_.Checked = o.BJAudio;
				kissAudio_.Checked = o.KissAudio;
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void OnHJAudio(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.HJAudio = b;
		}

		private void OnBJAudio(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.BJAudio = b;
		}

		private void OnKissAudio(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.KissAudio = b;
		}

		private void OnMutePlayer(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.MutePlayer = b;
		}
	}


	class EffectsOptionsTab : Tab
	{
		private VUI.CheckBox skinColor_, skinGloss_, hairLoose_;
		private bool ignore_ = false;

		public EffectsOptionsTab()
			: base("Effects", false)
		{
			var o = Cue.Instance.Options;

			var ly = new VUI.VerticalFlow(10);
			ly.Expand = false;

			var p = new VUI.Panel(ly);

			skinColor_ = p.Add(new VUI.CheckBox("Skin color", OnSkinColor, o.SkinColor));
			skinGloss_ = p.Add(new VUI.CheckBox("Skin gloss", OnSkinGloss, o.SkinGloss));
			p.Add(new VUI.Label(
				"Enables skin color and gloss changes depending on body " +
				"temperature. Might not work well for darker skin colors.",
				VUI.Label.Wrap));
			p.Add(new VUI.Spacer(20));

			hairLoose_ = p.Add(new VUI.CheckBox("Loose hair", OnHairLoose, o.HairLoose));
			p.Add(new VUI.Label(
				"Enables loose hair depending on body temperature. Might not " +
				"work well for some hair items.",
				VUI.Label.Wrap));
			p.Add(new VUI.Spacer(20));

			Layout = new VUI.BorderLayout(20);
			Add(p, VUI.BorderLayout.Top);

			o.Changed += OnOptionsChanged;
			OnOptionsChanged();
		}

		public override bool DebugOnly
		{
			get { return false; }
		}

		protected override void DoUpdate(float s)
		{
		}

		private void OnOptionsChanged()
		{
			try
			{
				ignore_ = true;

				var o = Cue.Instance.Options;

				skinColor_.Checked = o.SkinColor;
				skinGloss_.Checked = o.SkinGloss;
				hairLoose_.Checked = o.HairLoose;
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void OnSkinColor(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.SkinColor = b;
		}

		private void OnSkinGloss(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.SkinGloss = b;
		}

		private void OnHairLoose(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.HairLoose = b;
		}
	}


	class FinishOptionsTab : Tab
	{
		class EnumItem
		{
			public string text;
			public int value;

			public EnumItem(string t, int v)
			{
				text = t;
				value = v;
			}

			public override string ToString()
			{
				return text;
			}
		}

		private VUI.FloatTextBox initialDelay_;
		private VUI.FloatTextBox orgasmsTime_;

		private VUI.ComboBox<EnumItem> lookAt_;
		private VUI.ComboBox<EnumItem> orgasms_;
		private VUI.ComboBox<EnumItem> events_;

		private TriggersPanel triggers_;

		private VUI.ListView<string> list_ = new VUI.ListView<string>();
		private DebugLines debug_ = null;

		private bool ignore_ = false;


		public FinishOptionsTab()
			: base("Finish", false)
		{
			initialDelay_ = new VUI.FloatTextBox(OnInitialDelay);
			orgasmsTime_ = new VUI.FloatTextBox(OnOrgasmsTime);
			lookAt_ = new VUI.ComboBox<EnumItem>(OnLookAt);
			orgasms_ = new VUI.ComboBox<EnumItem>(OnOrgasms);
			events_ = new VUI.ComboBox<EnumItem>(OnEvents);

			triggers_ = new TriggersPanel(
				Cue.Instance.Options.Finish.Triggers,
				"Add trigger",
				"These triggers will be fired when Finishing starts.",
				"Trigger name");

			list_.Font = VUI.Style.Theme.MonospaceFont;
			list_.FontSize = 22;
			list_.Visible = false;// Cue.Instance.Options.DevMode;

			Cue.Instance.Options.Changed += () =>
			{
				//list_.Visible = Cue.Instance.Options.DevMode;
			};

			var ly = new VUI.GridLayout(2, 10);
			ly.HorizontalStretch = new List<bool> { false, true };
			ly.HorizontalFill = true;

			var settingsPanel = new VUI.Panel(ly);

			var pp = new VUI.Panel(new VUI.HorizontalFlow(5));
			initialDelay_.MaximumSize = new VUI.Size(100, VUI.Widget.DontCare);
			pp.Add(initialDelay_);
			pp.Add(new VUI.Label("s"));
			settingsPanel.Add(new VUI.Label("Initial delay"));
			settingsPanel.Add(pp);

			pp = new VUI.Panel(new VUI.HorizontalFlow(5));
			orgasmsTime_.MaximumSize = new VUI.Size(100, VUI.Widget.DontCare);
			pp.Add(orgasmsTime_);
			pp.Add(new VUI.Label("s"));
			settingsPanel.Add(new VUI.Label("Time to orgasm"));
			settingsPanel.Add(pp);

			settingsPanel.Add(new VUI.Spacer(30));
			settingsPanel.Add(new VUI.Spacer(30));

			settingsPanel.Add(new VUI.Label("Look at player"));
			settingsPanel.Add(lookAt_);

			settingsPanel.Add(new VUI.Label("Orgasms"));
			settingsPanel.Add(orgasms_);

			settingsPanel.Add(new VUI.Label("Events"));
			settingsPanel.Add(events_);

			settingsPanel.Add(new VUI.Spacer(30));
			settingsPanel.Add(new VUI.Spacer(30));


			lookAt_.AddItem(new EnumItem("Do nothing", Finish.LookAtNothing));
			lookAt_.AddItem(new EnumItem("Look at player if involved", Finish.LookAtPlayerInvolved));
			lookAt_.AddItem(new EnumItem("Everybody look at player", Finish.LookAtPlayerAll));
			lookAt_.AddItem(new EnumItem("Use personality", Finish.LookAtPersonality));

			orgasms_.AddItem(new EnumItem("Do nothing", Finish.OrgasmsNothing));
			orgasms_.AddItem(new EnumItem("Force orgasm if involved with player", Finish.OrgasmsInvolved));
			orgasms_.AddItem(new EnumItem("Force orgasm for everybody", Finish.OrgasmsAll));
			orgasms_.AddItem(new EnumItem("Use personality", Finish.OrgasmsPersonality));

			events_.AddItem(new EnumItem("Do nothing", Finish.StopEventsNothing));
			events_.AddItem(new EnumItem("Stop events if involved with player", Finish.StopEventsInvolved));
			events_.AddItem(new EnumItem("Stop events for everybody", Finish.StopEventsAll));


			var p = new VUI.Panel(new VUI.VerticalFlow(20));
			p.Add(settingsPanel);

			Layout = new VUI.BorderLayout();
			Add(p, VUI.BorderLayout.Top);
			Add(triggers_, VUI.BorderLayout.Center);
			//Add(list_, VUI.BorderLayout.Center);


			Cue.Instance.Options.Changed += OnOptionsChanged;
			OnOptionsChanged();
		}

		private void OnOptionsChanged()
		{
			var f = Cue.Instance.Options.Finish;

			try
			{
				ignore_ = true;

				initialDelay_.Text = $"{f.InitialDelay:0.00}";
				orgasmsTime_.Text = $"{f.OrgasmsTime:0.00}";

				for (int i = 0; i < lookAt_.Count; ++i)
				{
					if (lookAt_.Items[i].value == f.LookAt)
					{
						lookAt_.Select(i);
						break;
					}
				}

				for (int i = 0; i < orgasms_.Count; ++i)
				{
					if (orgasms_.Items[i].value == f.Orgasms)
					{
						orgasms_.Select(i);
						break;
					}
				}

				for (int i = 0; i < events_.Count; ++i)
				{
					if (events_.Items[i].value == f.Events)
					{
						events_.Select(i);
						break;
					}
				}
			}
			finally
			{
				ignore_ = false;
			}
		}

		public override bool DebugOnly
		{
			get { return false; }
		}

		protected override void DoUpdate(float s)
		{
			triggers_.Update();

			if (Cue.Instance.Options.DevMode)
			{
				if (debug_ == null)
					debug_ = new DebugLines();

				debug_.Clear();
				Cue.Instance.Finish.Debug(debug_);

				list_.SetItems(debug_.MakeArray());
			}
		}

		private void OnInitialDelay(float s)
		{
			if (ignore_) return;
			Cue.Instance.Options.Finish.InitialDelay = s;
		}

		private void OnOrgasmsTime(float s)
		{
			if (ignore_) return;
			Cue.Instance.Options.Finish.OrgasmsTime = s;
		}

		private void OnLookAt(EnumItem i)
		{
			if (ignore_) return;
			Cue.Instance.Options.Finish.LookAt = i.value;
		}

		private void OnOrgasms(EnumItem i)
		{
			if (ignore_) return;
			Cue.Instance.Options.Finish.Orgasms = i.value;
		}

		private void OnEvents(EnumItem i)
		{
			if (ignore_) return;
			Cue.Instance.Options.Finish.Events = i.value;
		}
	}
}
