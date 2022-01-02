namespace Cue
{
	class OptionsTab : Tab
	{
		public OptionsTab()
			: base("Options", true)
		{
			AddSubTab(new MainOptionsTab());
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
		private VUI.CheckBox playSfx_, skinColor_, skinGloss_, hairLoose_;
		private VUI.CheckBox handLinking_, devMode_;
		private VUI.CheckBox leftMenu_, rightMenu_;
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


			playSfx_ = p.Add(new VUI.CheckBox("Play sfx", OnPlaySfx, o.MuteSfx));
			p.Add(new VUI.Label("Play sound effects during hj/bj.", VUI.Label.Wrap));
			p.Add(new VUI.Spacer(20));

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

			handLinking_ = p.Add(new VUI.CheckBox("Hand linking", OnHandLinking, o.HandLinking));
			p.Add(new VUI.Label(
				"Enables linking hands to body parts when they're close " +
				"enough. Hands sometimes become impossible to move in Play " +
				"Mode, but they can always be moved in Edit Mode.",
				VUI.Label.Wrap));
			p.Add(new VUI.Spacer(20));

			leftMenu_ = p.Add(new VUI.CheckBox("Left hand menu", OnLeftMenu, o.LeftMenu));
			rightMenu_ = p.Add(new VUI.CheckBox("Right hand menu", OnRightMenu, o.RightMenu));
			p.Add(new VUI.Label(
				"Enables the VR menu on the left or right hand.",
				VUI.Label.Wrap));
			p.Add(new VUI.Spacer(20));

			devMode_ = p.Add(new VUI.CheckBox("Dev mode", OnDevMode, o.DevMode));
			p.Add(new VUI.Label("Enables a bunch of tabs.", VUI.Label.Wrap));

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
				playSfx_.Checked = !o.MuteSfx;
				skinColor_.Checked = o.SkinColor;
				skinGloss_.Checked = o.SkinGloss;
				hairLoose_.Checked = o.HairLoose;
				handLinking_.Checked = o.HandLinking;
				leftMenu_.Checked = o.LeftMenu;
				rightMenu_.Checked = o.RightMenu;
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

		private void OnPlaySfx(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.MuteSfx = !b;
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

		private void OnHandLinking(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.HandLinking = b;
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

		private void OnDevMode(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.DevMode = b;
		}
	}


	class MenuOptionsTab : Tab
	{
		private VUI.Panel buttons_;

		public MenuOptionsTab()
			: base("Menu", false)
		{
			var top = new VUI.Panel(new VUI.HorizontalFlow());
			top.Add(new VUI.Button("Add button", OnAdd));

			buttons_ = new VUI.Panel(new VUI.VerticalFlow());

			Layout = new VUI.BorderLayout();
			Add(top, VUI.BorderLayout.Top);
			Add(buttons_, VUI.BorderLayout.Center);
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

		private void OnAdd()
		{
			Cue.Instance.Options.AddCustomMenu();
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
}
