using System.Collections.Generic;

namespace Cue
{
	class PersonalityItem
	{
		private Personality p_;

		public PersonalityItem(Personality p)
		{
			p_ = p;
		}

		public Personality Personality
		{
			get { return p_; }
		}

		public override string ToString()
		{
			string s = p_.Name;

			if (Cue.Instance.Options.DevMode)
			{
				if (p_.Origin != "")
					s += $" (from {p_.Origin})";
			}

			return s;
		}
	}


	class PersonSettingsTab : Tab
	{
		private Person person_;
		private VUI.ComboBox<PersonalityItem> personality_ = new VUI.ComboBox<PersonalityItem>();
		private VUI.FloatTextSlider voicePitch_ = new VUI.FloatTextSlider();
		private VUI.TextBox traits_ = new VUI.TextBox();
		private bool ignore_ = false;

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

			Layout = new VUI.VerticalFlow(20);
			Add(new VUI.Label($"Settings for {person.ID}", UnityEngine.FontStyle.Bold));
			Add(p);

			personality_.SelectionChanged += OnPersonality;
			voicePitch_.ValueChanged += OnVoicePitch;
			traits_.Edited += OnTraits;

			traits_.MinimumSize = new VUI.Size(500, DontCare);

			try
			{
				ignore_ = true;

				voicePitch_.Set(
					person_.Personality.Voice.GetNormalPitch(),
					0, 1);

				traits_.Text = string.Join(" ", person_.Traits);
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
			try
			{
				ignore_ = true;

				if (personality_.Count == 0)
					RebuildPersonalities();
				else
					SelectPersonality(person_.Personality.Name);
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void RebuildPersonalities()
		{
			var items = new List<PersonalityItem>();
			PersonalityItem sel = null;

			foreach (var p in Resources.Personalities.All)
			{
				var item = new PersonalityItem(p);

				if (p.Name == person_.Personality.Name)
					sel = item;

				items.Add(item);
			}

			personality_.SetItems(items, sel);
		}

		private void SelectPersonality(string name)
		{
			for (int i = 0; i < personality_.Items.Count; ++i)
			{
				if (personality_.At(i).Personality.Name == name)
				{
					if (personality_.SelectedIndex != i)
						personality_.Select(i);

					break;
				}
			}
		}

		private void OnPersonality(PersonalityItem item)
		{
			if (ignore_) return;

			if (item.Personality.Name != person_.Personality.Name)
			{
				var old = person_.Personality;

				person_.Personality = Resources.Personalities.Clone(
					item.Personality.Name, person_);

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
