namespace Cue
{
	class OptionsTab : Tab
	{
		private VUI.CheckBox muteSfx_, skinColor_, skinGloss_, hairLoose_;
		private VUI.CheckBox handLinking_, devMode_;
		private bool ignore_ = false;

		public OptionsTab()
			: base("Options", false)
		{
			var ly = new VUI.VerticalFlow(5);
			var p = new VUI.Panel(ly);

			var o = Cue.Instance.Options;

			o.Changed += OnOptionsChanged;

			muteSfx_ = p.Add(new VUI.CheckBox("Mute sfx", OnMuteSfx, o.MuteSfx));
			p.Add(new VUI.Label("Mutes sound effects during hj/bj.", VUI.Label.Wrap));
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

			devMode_ = p.Add(new VUI.CheckBox("Dev mode", OnDevMode, o.DevMode));
			p.Add(new VUI.Label("Enables a bunch of tabs.", VUI.Label.Wrap));

			Layout = new VUI.BorderLayout();
			Add(p, VUI.BorderLayout.Top);
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

				muteSfx_.Checked = o.MuteSfx;
				skinColor_.Checked = o.SkinColor;
				skinGloss_.Checked = o.SkinGloss;
				hairLoose_.Checked = o.HairLoose;
				handLinking_.Checked = o.HandLinking;
				devMode_.Checked = o.DevMode;
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void OnMuteSfx(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.MuteSfx = b;
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

		private void OnDevMode(bool b)
		{
			if (ignore_) return;
			Cue.Instance.Options.DevMode = b;
		}
	}
}
