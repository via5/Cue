namespace Cue
{
	class OptionsTab : Tab
	{
		public OptionsTab()
			: base("Options", true)
		{
			AddSubTab(new MainOptionsTab());
			AddSubTab(new EffectsOptionsTab());
			AddSubTab(new SoundOptionsTab());
			AddSubTab(new MenuOptionsTab());
		}

		public override bool DebugOnly
		{
			get { return false; }
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
		private VUI.Panel buttons_;
		private bool ignore_ = false;

		public MenuOptionsTab()
			: base("Menu", false)
		{
			var o = Cue.Instance.Options;

			var ly = new VUI.VerticalFlow(5);
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

			var center = new VUI.Panel(new VUI.BorderLayout(10));
			var controls = new VUI.Panel(new VUI.HorizontalFlow(20));

			controls.Add(new VUI.Button("Add button", OnAdd));
			controls.Add(new VUI.Label("Adds custom buttons to the menu."));
			buttons_ = new VUI.Panel(new VUI.VerticalFlow(10));

			center.Add(controls, VUI.BorderLayout.Top);
			center.Add(buttons_, VUI.BorderLayout.Center);

			Layout = new VUI.BorderLayout(20);
			Add(top, VUI.BorderLayout.Top);
			Add(center, VUI.BorderLayout.Center);

			o.Changed += OnOptionsChanged;
			OnOptionsChanged();
		}

		public override bool DebugOnly
		{
			get { return false; }
		}

		protected override void DoUpdate(float s)
		{
			if (buttons_.Children.Count != Cue.Instance.Options.Menus.Length)
				Rebuild();
		}

		private void Rebuild()
		{
			buttons_.RemoveAllChildren();
			foreach (var m in Cue.Instance.Options.Menus)
				buttons_.Add(CreatePanel(m));
		}

		private VUI.Panel CreatePanel(CustomMenu m)
		{
			var p = new VUI.Panel(new VUI.HorizontalFlow(10));
			var c = p.Add(new VUI.TextBox(m.Caption));
			c.Edited += (s) => { OnCaption(m, s); };
			p.Add(new VUI.Button("Edit trigger", () => OnEditTrigger(m)));
			p.Add(new VUI.ToolButton("X", () => OnDelete(m)));
			return p;
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

		private void OnAdd()
		{
			Cue.Instance.Options.AddCustomMenu();
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

		private void OnEditTrigger(CustomMenu m)
		{
			m.Trigger.Edit(() => Cue.Instance.Options.ForceMenusChanged());
		}

		private void OnCaption(CustomMenu m, string s)
		{
			m.Caption = s;
		}

		private void OnDelete(CustomMenu m)
		{
			Cue.Instance.Options.RemoveCustomMenu(m);
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

			var ly = new VUI.VerticalFlow(5);
			ly.Expand = false;

			var p = new VUI.Panel(ly);

			hjAudio_ = p.Add(new VUI.CheckBox("Play HJ sounds", OnHJAudio, o.HJAudio));
			p.Add(new VUI.Spacer(20));

			bjAudio_ = p.Add(new VUI.CheckBox("Play BJ sounds", OnBJAudio, o.BJAudio));
			p.Add(new VUI.Spacer(20));

			kissAudio_ = p.Add(new VUI.CheckBox("Play kissing sounds", OnKissAudio, o.KissAudio));
			p.Add(new VUI.Spacer(20));

			mutePlayer_ = p.Add(new VUI.CheckBox("Mute possessed atom", OnMutePlayer, o.MutePlayer));
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

			var ly = new VUI.VerticalFlow(5);
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
}
