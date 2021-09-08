using System;
using System.Collections.Generic;

namespace Cue
{
	class PersonAITab : Tab
	{
		private Person person_;
		private VUI.Tabs tabsWidget_ = new VUI.Tabs();
		private List<Tab> tabs_ = new List<Tab>();

		public PersonAITab(Person p)
			: base("AI")
		{
			person_ = p;

			tabs_.Add(new PersonAIStateTab(person_));
			tabs_.Add(new PersonAIPersonalityTab(person_));
			tabs_.Add(new PersonAIPhysiologyTab(person_));
			tabs_.Add(new PersonAIGazeTab(person_));

			foreach (var t in tabs_)
				tabsWidget_.AddTab(t.Title, t);

			Layout = new VUI.BorderLayout();
			Add(tabsWidget_, VUI.BorderLayout.Center);
		}

		public override void Update(float s)
		{
			for (int i = 0; i < tabs_.Count; ++i)
			{
				if (tabs_[i].IsVisibleOnScreen())
					tabs_[i].Update(s);
			}
		}

		public override void OnPluginState(bool b)
		{
			base.OnPluginState(b);
			foreach (var t in tabs_)
				t.OnPluginState(b);
		}
	}


	class PersonAIStateTab : Tab
	{
		private Person person_;
		private PersonAI ai_;

		private VUI.Label enabled_ = new VUI.Label();
		private VUI.Label event_ = new VUI.Label();
		private VUI.ComboBox<string> personality_ = new VUI.ComboBox<string>();
		private VUI.CheckBox close_ = new VUI.CheckBox();


		public PersonAIStateTab(Person p)
			: base("State")
		{
			person_ = p;
			ai_ = (PersonAI)person_.AI;

			var gl = new VUI.GridLayout(2);
			gl.HorizontalSpacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true };

			var state = new VUI.Panel(gl);
			state.Add(new VUI.Label("Enabled"));
			state.Add(enabled_);

			state.Add(new VUI.Label("Event"));
			state.Add(event_);

			state.Add(new VUI.Label("Personality"));
			state.Add(personality_);

			state.Add(new VUI.Label("Force close"));
			state.Add(close_);


			Layout = new VUI.BorderLayout();
			Add(state, VUI.BorderLayout.Top);

			personality_.SelectionChanged += OnPersonality;
			close_.Changed += OnClose;
		}

		public override void Update(float s)
		{
			string es = "";

			if (ai_.EventsEnabled)
			{
				if (es != "")
					es += "|";

				es += "events";
			}

			if (ai_.InteractionsEnabled)
			{
				if (es != "")
					es += "|";

				es += "interactions";
			}

			if (es == "")
				es = "nothing";

			enabled_.Text = es;

			event_.Text =
				(ai_.Event == null ? "(none)" : ai_.Event.ToString()) + " " +
				(ai_.ForcedEvent == null ? "(forced: none)" : $"(forced: {ai_.ForcedEvent})");

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
		}

		private void OnClose(bool b)
		{
			person_.Personality.ForceSetClose(b, b);
		}

		private void OnPersonality(string name)
		{
			if (name != "" && name != person_.Personality.Name)
				person_.Personality = Resources.Personalities.Clone(name, person_);
		}
	}


	class PersonAIPhysiologyTab : Tab
	{
		private Person person_;
		private Physiology pp_;

		private VUI.ListView<string> list_ = new VUI.ListView<string>();
		private bool inited_ = false;

		public PersonAIPhysiologyTab(Person p)
			: base("Physiology")
		{
			person_ = p;
			pp_ = p.Physiology;

			Layout = new VUI.BorderLayout();
			Add(list_, VUI.BorderLayout.Center);

			list_.Font = VUI.Style.Theme.MonospaceFont;
		}

		public override void Update(float s)
		{
			if (inited_)
				return;

			inited_ = true;

			var sms = new List<string[]>();
			for (int i = 0; i < pp_.SpecificModifiers.Length; ++i)
			{
				var sm = pp_.SpecificModifiers[i];

				sms.Add(new string[]{
					$"{BodyParts.ToString(sm.bodyPart)}=>{BodyParts.ToString(sm.sourceBodyPart)}",
					$"{sm.modifier}" });
			}

			list_.SetItems(MakeTable(pp_, sms.ToArray()));
		}

		public static List<string> MakeTable(EnumValueManager v, string[][] more)
		{
			int longest = 0;

			foreach (var n in v.Values.GetAllNames())
				longest = Math.Max(longest, n.Length);


			var items = new List<string>();

			for (int i = 0; i < v.Values.GetSlidingDurationCount(); ++i)
			{
				items.Add(
					v.Values.GetSlidingDurationName(i).PadRight(longest) +
					"   " +
					v.GetSlidingDuration(i).ToString());
			}

			for (int i = 0; i < v.Values.GetBoolCount(); ++i)
			{
				items.Add(
					v.Values.GetBoolName(i).PadRight(longest) +
					"   " +
					v.GetBool(i).ToString());
			}

			for (int i = 0; i < v.Values.GetFloatCount(); ++i)
			{
				items.Add(
					v.Values.GetFloatName(i).PadRight(longest) +
					"   " +
					v.Get(i).ToString());
			}

			for (int i = 0; i < v.Values.GetStringCount(); ++i)
			{
				items.Add(
					v.Values.GetStringName(i).PadRight(longest) +
					"   " +
					v.GetString(i));
			}

			for (int i = 0; i < more.Length; ++i)
			{
				items.Add(
					more[i][0].PadRight(longest) +
					"   " +
					more[i][1]);
			}

			return items;
		}
	}


	class PersonAIPersonalityTab : Tab
	{
		private Person person_;

		private VUI.ComboBox<string> states_ = new VUI.ComboBox<string>();
		private VUI.ListView<string> list_ = new VUI.ListView<string>();
		private int currentState_ = -1;

		public PersonAIPersonalityTab(Person p)
			: base("Personality")
		{
			person_ = p;

			Layout = new VUI.BorderLayout();
			Add(states_, VUI.BorderLayout.Top);
			Add(list_, VUI.BorderLayout.Center);

			states_.SetItems(Personality.StateNames);
			list_.Font = VUI.Style.Theme.MonospaceFont;
		}

		public override void Update(float s)
		{
			if (currentState_ == states_.SelectedIndex)
				return;

			currentState_ = states_.SelectedIndex;

			var ps = person_.Personality;
			var st = ps.GetState(currentState_);

			var exps = new List<string[]>();
			var maxs = st.Maximums;
			for (int i = 0; i < maxs.Length; ++i)
			{
				var m = maxs[i];

				exps.Add(new string[] {
					Expressions.ToString(m.type),
					m.maximum.ToString() });
			}

			var v = ps.Voice;
			exps.Add(new string[] { "orgasm ds", v.OrgasmDataset.Name });

			foreach (var ds in v.Datasets)
			{
				exps.Add(new string[] {
					ds.dataset.Name,
					$"[{ds.intensityMin}, {ds.intensityMax}]" });
			}

			list_.SetItems(PersonAIPhysiologyTab.MakeTable(st, exps.ToArray()));
		}
	}

	class PersonAIGazeTab : Tab
	{
		private Person person_;
		private PersonAI ai_;

		private VUI.Label eyesBlink_ = new VUI.Label();
		private VUI.Label eyesPos_ = new VUI.Label();
		private VUI.Label gazerType_ = new VUI.Label();
		private VUI.Label gazerEnabled_ = new VUI.Label();
		private VUI.Label gazerDuration_ = new VUI.Label();
		private VUI.Label targetType_ = new VUI.Label();
		private VUI.Label targetEmergency_ = new VUI.Label();
		private VUI.Label avoid_ = new VUI.Label();
		private VUI.Label next_ = new VUI.Label();

		public PersonAIGazeTab(Person person)
			: base("Gaze")
		{
			person_ = person;
			ai_ = (PersonAI)person_.AI;

			Layout = new VUI.VerticalFlow();


			var gl = new VUI.GridLayout(2);
			gl.HorizontalSpacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true };

			var p = new VUI.Panel(gl);

			p.Add(new VUI.Label("Eyes",	UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Spacer());

			p.Add(new VUI.Label("Blink"));
			p.Add(eyesBlink_);

			p.Add(new VUI.Label("Look at"));
			p.Add(eyesPos_);


			p.Add(new VUI.Spacer(20));
			p.Add(new VUI.Spacer(20));


			p.Add(new VUI.Label("Gazer", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Spacer());

			p.Add(new VUI.Label("Type"));
			p.Add(gazerType_);

			p.Add(new VUI.Label("Enabled"));
			p.Add(gazerEnabled_);

			p.Add(new VUI.Label("Duration"));
			p.Add(gazerDuration_);


			p.Add(new VUI.Spacer(20));
			p.Add(new VUI.Spacer(20));


			p.Add(new VUI.Label("Target", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Spacer());

			p.Add(new VUI.Label("Type"));
			p.Add(targetType_);

			p.Add(new VUI.Label("Emergency"));
			p.Add(targetEmergency_);

			p.Add(new VUI.Label("Avoid"));
			p.Add(avoid_);

			p.Add(new VUI.Label("Next in"));
			p.Add(next_);

			p.Add(new VUI.Spacer(20));
			p.Add(new VUI.Spacer(20));

			Add(p);


			p = new VUI.Panel(new VUI.VerticalFlow());

			p.Add(new VUI.CheckBox("Render frustums", OnRenderFrustums));
			p.Add(new VUI.ComboBox<string>(
				new string[] { "Free look", "Force camera", "Force up" },
				OnForceLook));

			Add(p);
		}

		public override void Update(float s)
		{
			var g = person_.Gaze;

			eyesBlink_.Text = $"{g.Eyes.Blink}";
			eyesPos_.Text = $"{g.Eyes.TargetPosition}";

			gazerType_.Text = $"{g.Gazer.Name}";
			gazerEnabled_.Text = $"{g.Gazer.Enabled}";
			gazerDuration_.Text = $"{g.Gazer.Duration:0.00}s";

			if (g.Picker.HasTarget)
			{
				targetType_.Text = $"{g.Picker.CurrentTarget}";
				targetEmergency_.Text = $"{g.IsEmergency}";
			}
			else
			{
				targetType_.Text = "none";
				targetEmergency_.Text = "no";
			}

			avoid_.Text = g.Picker.AvoidString;
			next_.Text = $"{g.Picker.TimeBeforeNext:0.00}s";
		}

		private void OnRenderFrustums(bool b)
		{
			person_.Gaze.Picker.Render = b;
		}

		private void OnForceLook(int s)
		{
			person_.Gaze.ForceLook = s;
		}
	}
}
