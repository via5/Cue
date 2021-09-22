using System.Collections.Generic;

namespace Cue
{
	class PersonSettingsTab : Tab
	{
		private Person person_;
		private VUI.ComboBox<string> personality_ = new VUI.ComboBox<string>();
		private VUI.FloatTextSlider voicePitch_ = new VUI.FloatTextSlider();
		private VUI.TextBox traits_ = new VUI.TextBox();
		private VUI.IgnoreFlag ignore_ = new VUI.IgnoreFlag();

		public PersonSettingsTab(Person person)
			: base("Settings", false)
		{
			person_ = person;

			var gl = new VUI.GridLayout(2, 10);
			gl.HorizontalStretch = new List<bool>() { false, false };

			var p = new VUI.Panel(gl);

			p.Add(new VUI.Label("Personality"));
			p.Add(personality_);

			p.Add(new VUI.Label("Voice pitch"));
			p.Add(voicePitch_);

			p.Add(new VUI.Label("Traits"));
			p.Add(traits_);

			Layout = new VUI.BorderLayout();
			Add(p, VUI.BorderLayout.Top);

			personality_.SelectionChanged += OnPersonality;
			voicePitch_.ValueChanged += OnVoicePitch;
			traits_.Changed += OnTraits;

			ignore_.Do(() =>
			{
				voicePitch_.Set(
					person_.Personality.Voice.GetNormalPitch(person_),
					0, 1);

				traits_.Text = string.Join(" ", person_.Traits);
			});
		}

		protected override void DoUpdate(float s)
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
			{
				var old = person_.Personality;

				person_.Personality = Resources.Personalities.Clone(name, person_);

				if (old.Voice.ForcedPitch >= 0)
					person_.Personality.Voice.ForcePitch(old.Voice.ForcedPitch);

				Cue.Instance.Save();
			}
		}

		private void OnVoicePitch(float f)
		{
			if (ignore_) return;

			person_.Personality.Voice.ForcePitch(f);
			Cue.Instance.Save();
		}

		private void OnTraits(string s)
		{
			person_.Traits = s.Split(' ');
		}
	}
}
