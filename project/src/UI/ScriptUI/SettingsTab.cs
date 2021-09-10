using System.Collections.Generic;

namespace Cue
{
	class PersonSettingsTab : Tab
	{
		private Person person_;
		private VUI.ComboBox<string> personality_ = new VUI.ComboBox<string>();
		private VUI.FloatTextSlider voicePitch_ = new VUI.FloatTextSlider();
		private VUI.IgnoreFlag ignore_ = new VUI.IgnoreFlag();

		public PersonSettingsTab(Person person)
			: base("Settings")
		{
			person_ = person;

			var gl = new VUI.GridLayout(2, 10);
			gl.HorizontalStretch = new List<bool>() { false, false };

			var p = new VUI.Panel(gl);

			p.Add(new VUI.Label("Personality"));
			p.Add(personality_);

			p.Add(new VUI.Label("Voice pitch"));
			p.Add(voicePitch_);

			Layout = new VUI.BorderLayout();
			Add(p, VUI.BorderLayout.Top);

			personality_.SelectionChanged += OnPersonality;
			voicePitch_.ValueChanged += OnVoicePitch;

			voicePitch_.Value = person_.Personality.Voice.GetNormalPitch(person_);
		}

		public override void Update(float s)
		{
			ignore_.Do(() =>
			{
				if (personality_.Count == 0)
				{
					personality_.SetItems(
						Resources.Personalities.AllNames(),
						person_.Personality.Name);
				}
				else
				{
					if (personality_.Selected != person_.Personality.Name)
						personality_.Select(person_.Personality.Name);
				}
			});
		}

		private void OnPersonality(string name)
		{
			if (ignore_) return;

			if (name != "" && name != person_.Personality.Name)
				person_.Personality = Resources.Personalities.Clone(name, person_);
		}

		private void OnVoicePitch(float f)
		{
			person_.Personality.Voice.SetPitchForAll(f);
		}
	}
}
