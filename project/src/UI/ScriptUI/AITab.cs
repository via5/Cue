using System;
using System.Collections.Generic;

namespace Cue
{
	class PersonAITab : Tab
	{
		private Person person_;

		public PersonAITab(Person p)
			: base("AI", true)
		{
			person_ = p;

			AddSubTab(new PersonAIStateTab(person_));
			AddSubTab(new PersonAIPersonalityTab(person_));
			AddSubTab(new PersonAIPhysiologyTab(person_));
			AddSubTab(new PersonAIGazeTab(person_));
			AddSubTab(new PersonAIEventsTab(person_));
		}
	}


	class PersonAIStateTab : Tab
	{
		private Person person_;
		private PersonAI ai_;

		private VUI.Label enabled_ = new VUI.Label();
		private VUI.Label traits_ = new VUI.Label();
		private VUI.Label command_ = new VUI.Label();
		private VUI.CheckBox close_ = new VUI.CheckBox();


		public PersonAIStateTab(Person p)
			: base("State", false)
		{
			person_ = p;
			ai_ = (PersonAI)person_.AI;

			var gl = new VUI.GridLayout(2);
			gl.HorizontalSpacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true };

			var state = new VUI.Panel(gl);
			state.Add(new VUI.Label("Enabled"));
			state.Add(enabled_);

			state.Add(new VUI.Label("Traits"));
			state.Add(traits_);

			state.Add(new VUI.Label("Command"));
			state.Add(command_);

			state.Add(new VUI.Label("Force close"));
			state.Add(close_);


			Layout = new VUI.BorderLayout();
			Add(state, VUI.BorderLayout.Top);

			close_.Changed += OnClose;
		}

		protected override void DoUpdate(float s)
		{
			string es = "";

			if (ai_.CommandsEnabled)
			{
				if (es != "")
					es += "|";

				es += "commands";
			}

			if (ai_.EventsEnabled)
			{
				if (es != "")
					es += "|";

				es += "events";
			}

			if (es == "")
				es = "nothing";

			enabled_.Text = es;
			traits_.Text = string.Join(", ", person_.Traits);

			command_.Text =
				(ai_.Command == null ? "(none)" : ai_.Command.ToString()) + " " +
				(ai_.ForcedCommand == null ? "(forced: none)" : $"(forced: {ai_.ForcedCommand})");
		}

		private void OnClose(bool b)
		{
			person_.Personality.ForceSetClose(b, b);
		}
	}


	class PersonAIPhysiologyTab : Tab
	{
		private Person person_;
		private Physiology pp_;

		private VUI.ListView<string> list_ = new VUI.ListView<string>();
		private bool inited_ = false;

		public PersonAIPhysiologyTab(Person p)
			: base("Physiology", false)
		{
			person_ = p;
			pp_ = p.Physiology;

			Layout = new VUI.BorderLayout();
			Add(list_, VUI.BorderLayout.Center);

			list_.Font = VUI.Style.Theme.MonospaceFont;
		}

		protected override void DoUpdate(float s)
		{
			if (inited_)
				return;

			inited_ = true;

			var sms = new List<string[]>();
			for (int i = 0; i < pp_.SpecificModifiers.Length; ++i)
			{
				var sm = pp_.SpecificModifiers[i];

				sms.Add(new string[]{
					$"{BP.ToString(sm.bodyPart)}=>{BP.ToString(sm.sourceBodyPart)}",
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
			: base("Personality", false)
		{
			person_ = p;

			Layout = new VUI.BorderLayout();
			Add(states_, VUI.BorderLayout.Top);
			Add(list_, VUI.BorderLayout.Center);

			states_.SetItems(Personality.StateNames);
			list_.Font = VUI.Style.Theme.MonospaceFont;
		}

		protected override void DoUpdate(float s)
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
		private VUI.Label gazerVariance_ = new VUI.Label();
		private VUI.Label debug_ = new VUI.Label();
		private VUI.Label targetType_ = new VUI.Label();
		private VUI.Label targetEmergency_ = new VUI.Label();
		private VUI.Label avoid_ = new VUI.Label();
		private VUI.Label next_ = new VUI.Label();

		public PersonAIGazeTab(Person person)
			: base("Gaze", false)
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

			p.Add(new VUI.Label("Variance"));
			p.Add(gazerVariance_);

			p.Add(new VUI.Label("Debug"));
			p.Add(debug_);


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


			p = new VUI.Panel(new VUI.VerticalFlow(5));

			p.Add(new VUI.CheckBox(
				"Render frustums",
				(b) => person_.Gaze.Render.Frustums = b,
				person_.Gaze.Render.Frustums));

			p.Add(new VUI.CheckBox(
				"Render front plane",
				(b) => person_.Gaze.Render.FrontPlane = b,
				person_.Gaze.Render.FrontPlane));

			p.Add(new VUI.ComboBox<string>(ForceLooks.Names, OnForceLook));
			Add(p);
		}

		protected override void DoUpdate(float s)
		{
			var g = person_.Gaze;

			eyesBlink_.Text = $"{g.Eyes.Blink}";
			eyesPos_.Text = $"{g.Eyes.TargetPosition}";

			gazerType_.Text = $"{g.Gazer.Name}";
			gazerEnabled_.Text = $"{g.Gazer.Enabled}";
			gazerDuration_.Text = $"{g.Gazer.Duration:0.00}s";
			gazerVariance_.Text = $"{g.Gazer.Variance:0.00}s";
			debug_.Text = g.DebugString();

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

		private void OnForceLook(int s)
		{
			person_.Gaze.ForceLook = s;
		}
	}



	class PersonAIEventsTab : Tab
	{
		private Person person_;

		private VUI.ComboBox<IEvent> events_;
		private VUI.ListView<string> list_ = new VUI.ListView<string>();

		public PersonAIEventsTab(Person person)
			: base("Events", false)
		{
			person_ = person;

			var es = person_.AI.Events;
			U.NatSort(es);

			Layout = new VUI.BorderLayout(10);
			events_ = new VUI.ComboBox<IEvent>(es, OnSelection);
			list_.Font = VUI.Style.Theme.MonospaceFont;

			Add(events_, VUI.BorderLayout.Top);
			Add(list_, VUI.BorderLayout.Center);
		}

		protected override void DoUpdate(float s)
		{
			var d = events_.Selected?.Debug();

			if (d == null)
			{
				list_.Clear();
			}
			else
			{
				list_.SetItems(d);
			}
		}

		private void OnSelection(IEvent e)
		{
		}
	}
}
