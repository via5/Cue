using System;
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
		private VUI.IntTextSlider maxExcitement_;
		private VUI.Label warning_ = new VUI.Label();
		private VUI.TextBox traits_ = new VUI.TextBox("", "Unused for now");
		private VUI.CheckBox strapon_ = new VUI.CheckBox("");
		private VUI.Label straponWarning_ = new VUI.Label("Only available for female characters");
		private bool ignore_ = false;
		private bool firstUpdate_ = true;

		public PersonSettingsTab(Person person)
			: base("Settings", false)
		{
			person_ = person;

			var gl = new VUI.GridLayout(3, 10);
			gl.HorizontalStretch = new List<bool>() { false, true, false };
			gl.HorizontalFill = true;
			gl.UniformHeight = false;

			var straponPanel = new VUI.Panel(new VUI.HorizontalFlow(10));
			straponPanel.Add(strapon_);
			straponPanel.Add(straponWarning_);

			var pp = new VUI.Panel(gl);
			pp.Add(new VUI.Label("Personality"));
			pp.Add(personality_);
			pp.Add(loadPose_);

			pp.Add(new VUI.Label("Max excitement"));
			maxExcitement_ = pp.Add(new VUI.IntTextSlider(0, 100, OnMaxExcitement));
			pp.Add(new VUI.Spacer());

			pp.Add(new VUI.Label("Traits"));
			pp.Add(traits_);
			pp.Add(new VUI.Spacer());

			pp.Add(new VUI.Label("Strapon"));
			pp.Add(straponPanel);
			pp.Add(new VUI.Spacer());

			var w = new VUI.Panel(new VUI.VerticalFlow(10));
			w.Add(warning_);

			var p = new VUI.Panel(new VUI.VerticalFlow(40));

			p.Add(new VUI.Label($"Settings for {person.ID}", UnityEngine.FontStyle.Bold));
			p.Add(pp);
			p.Add(new VUI.Label(
				"Load pose: if checked, some personalities like Sleeping " +
				"will also set joints to Off and change their physics settings.",
				UnityEngine.FontStyle.Italic, VUI.Label.Wrap));

			p.Add(new VUI.Spacer(20));

			Layout = new VUI.BorderLayout(10);
			Add(p, VUI.BorderLayout.Top);
			Add(new VUI.Spacer(1), VUI.BorderLayout.Center);
			Add(w, VUI.BorderLayout.Bottom);


			maxExcitement_.ToStringCallback = v => $"{v}%";

			personality_.SelectionChanged += OnPersonality;
			loadPose_.Changed += OnLoadPose;
			traits_.Edited += OnTraits;
			strapon_.Changed += OnStrapon;

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

				loadPose_.Checked = person_.LoadPose;

				maxExcitement_.Value = (int)Math.Round(person_.Options.MaxExcitement * 100);
				strapon_.Checked = person_.Body.Strapon;

				if (person_.Atom.IsMale)
				{
					strapon_.Enabled = false;
					straponWarning_.Visible = true;
				}
				else
				{
					strapon_.Enabled = true;
					straponWarning_.Visible = false;
				}

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
			if (!string.IsNullOrEmpty(w))
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

			U.NatSort(items, i => i.Personality.Name);

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
			person_.SetPersonality(item.Personality.Name);
		}

		private void OnLoadPose(bool b)
		{
			if (ignore_) return;
			person_.LoadPose = b;
		}

		private void OnMaxExcitement(int i)
		{
			if (ignore_) return;

			person_.Options.MaxExcitement = i / 100.0f;
			Cue.Instance.Save();
		}

		private void OnTraits(string s)
		{
			person_.Traits = s.Split(' ');
		}

		private void OnStrapon(bool b)
		{
			person_.Body.Strapon = b;
		}
	}


	class PersonAnimationsTab : Tab
	{
		private class AnimationOptions
		{
			private PersonOptions.AnimationOptions o_;
			private VUI.CheckBox cb_;

			public AnimationOptions(PersonOptions.AnimationOptions o, VUI.CheckBox cb)
			{
				o_ = o;
				cb_ = cb;
			}

			public void Update(float s)
			{
				cb_.Checked = o_.Play;
			}
		}

		private Person person_;
		private VUI.CheckBox idlePose_, excitedPose_;
		private VUI.Label idlePoseWarning_, excitedPoseWarning_;
		private List<AnimationOptions> animOptions_ = new List<AnimationOptions>();
		private bool ignore_ = false;

		public PersonAnimationsTab(Person person)
			: base("Animations", false)
		{
			person_ = person;

			var gl = new VUI.GridLayout(2, 10);
			gl.HorizontalStretch = new List<bool>() { false, true, false };
			gl.HorizontalFill = true;
			var ao = new VUI.Panel(gl);

			{
				idlePose_ = new VUI.CheckBox("Play", (b) =>
				{
					if (ignore_) return;
					person_.Options.IdlePose = b;
				});

				idlePoseWarning_ = new VUI.Label("Idle animation disabled in the main options");
				idlePoseWarning_.Visible = false;
				idlePoseWarning_.TextColor = new UnityEngine.Color(1, 0, 0);
				idlePoseWarning_.WrapMode = VUI.Label.Wrap;

				var ip = new VUI.Panel(new VUI.HorizontalFlow(10));
				ip.Add(idlePose_);
				ip.Add(idlePoseWarning_);

				ao.Add(new VUI.Label("Idle"));
				ao.Add(ip);
			}

			{
				excitedPose_ = new VUI.CheckBox("Play", (b) =>
				{
					if (ignore_) return;
					person_.Options.ExcitedPose = b;
				});

				excitedPoseWarning_ = new VUI.Label("Excited animation disabled in the main options");
				excitedPoseWarning_.Visible = false;
				excitedPoseWarning_.TextColor = new UnityEngine.Color(1, 0, 0);
				excitedPoseWarning_.WrapMode = VUI.Label.Wrap;

				var ip = new VUI.Panel(new VUI.HorizontalFlow(10));
				ip.Add(excitedPose_);
				ip.Add(excitedPoseWarning_);

				ao.Add(new VUI.Label("Excited"));
				ao.Add(ip);
			}

			foreach (var o in person_.Options.GetAnimationOptions())
				AddAnimationOptions(ao, o);

			var p = new VUI.Panel(new VUI.VerticalFlow(40));

			p.Add(new VUI.Label($"Animations for {person.ID}", UnityEngine.FontStyle.Bold));
			p.Add(ao);

			Layout = new VUI.BorderLayout(10);
			Add(p, VUI.BorderLayout.Top);
			Add(new VUI.Spacer(1), VUI.BorderLayout.Center);
		}

		private void AddAnimationOptions(VUI.Panel p, PersonOptions.AnimationOptions o)
		{
			var cb = new VUI.CheckBox("Play", (b) =>
			{
				if (ignore_) return;
				o.Play = b;
			});

			var ap = new VUI.Panel(new VUI.HorizontalFlow(10));
			ap.Add(cb);
			ap.Add(new VUI.Button("Start actions...", () =>
			{
				o.TriggerOn.Edit(() => Cue.Instance.Save());
			}));

			ap.Add(new VUI.Button("Stop actions...", () =>
			{
				o.TriggerOff.Edit(() => Cue.Instance.Save());
			}));

			p.Add(new VUI.Label(o.Name));
			p.Add(ap);

			animOptions_.Add(new AnimationOptions(o, cb));
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

				idlePose_.Checked = person_.Options.IdlePose;
				idlePoseWarning_.Visible = !Cue.Instance.Options.IdlePose;

				excitedPose_.Checked = person_.Options.ExcitedPose;
				excitedPoseWarning_.Visible = !Cue.Instance.Options.ExcitedPose;

				for (int i = 0; i < animOptions_.Count; ++i)
					animOptions_[i].Update(s);
			}
			finally
			{
				ignore_ = false;
			}
		}
	}
}
