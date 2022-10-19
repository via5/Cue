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
		private VUI.CheckBox loadPose_ = new VUI.CheckBox("Load pose");
		private VUI.Label warning_ = new VUI.Label();
		private VUI.TextBox traits_ = new VUI.TextBox();
		private bool ignore_ = false;
		private bool firstUpdate_ = true;

		public PersonSettingsTab(Person person)
			: base("Settings", false)
		{
			person_ = person;

			var gl = new VUI.GridLayout(2, 10);
			gl.HorizontalStretch = new List<bool>() { false, false };

			var p = new VUI.Panel(gl);

			var pp = new VUI.Panel(new VUI.HorizontalFlow(10));
			pp.Add(personality_);
			pp.Add(loadPose_);

			p.Add(new VUI.Label("Personality"));
			p.Add(pp);

			p.Add(new VUI.Label("Traits"));
			p.Add(traits_);

			var w = new VUI.Panel(new VUI.VerticalFlow(10));
			w.Add(warning_);

			Layout = new VUI.VerticalFlow(20);
			Add(new VUI.Label($"Settings for {person.ID}", UnityEngine.FontStyle.Bold));
			Add(p);
			Add(w);

			personality_.SelectionChanged += OnPersonality;
			loadPose_.Changed += OnLoadPose;
			traits_.Edited += OnTraits;

			traits_.MinimumSize = new VUI.Size(500, DontCare);
			warning_.Visible = false;
			warning_.TextColor = new UnityEngine.Color(1, 0, 0);
			warning_.WrapMode = VUI.Label.Wrap;
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

				if (firstUpdate_)
				{
					traits_.Text = string.Join(" ", person_.Traits);
					firstUpdate_ = false;
				}

				if (personality_.Count == 0)
					RebuildPersonalities();
				else
					SelectPersonality(person_.Personality.Name);

				warning_.Text = MakeWarnings();
				warning_.Visible = (warning_.Text.Length > 0);
			}
			finally
			{
				ignore_ = false;
			}
		}

		private string MakeWarnings()
		{
			string s = "";

			AddWarning(ref s, person_.Homing.Warning);
			AddWarning(ref s, person_.Voice.Warning);
			AddWarning(ref s, person_.Atom.Warning);
			AddWarning(ref s, ClockwiseHJAnimation.GetWarning(person_));
			AddWarning(ref s, ClockwiseBJAnimation.GetWarning(person_));
			AddWarning(ref s, ClockwiseKissAnimation.GetWarning(person_));

			return s;
		}

		private void AddWarning(ref string s, string w)
		{
			if (w != "")
			{
				if (s != "")
					s += "\n";

				s += w;
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
			loadPose_.Checked = person_.LoadPose;
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
				var p = Resources.Personalities.Clone(
					item.Personality.Name, person_);

				person_.SetPersonality(p);
				Cue.Instance.Save();
			}
		}

		private void OnLoadPose(bool b)
		{
			if (ignore_) return;

			person_.LoadPose = b;
			Cue.Instance.Save();
		}

		private void OnTraits(string s)
		{
			person_.Traits = s.Split(' ');
		}
	}
}
