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

			personality_.SetItems(
				Resources.Personalities.AllNames(),
				person_.Personality.Name);
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

		private VUI.Label[] floats_ = new VUI.Label[PE.FloatCount];
		private VUI.Label[] strings_ = new VUI.Label[PE.StringCount];

		public PersonAIPhysiologyTab(Person p)
			: base("Physiology")
		{
			person_ = p;
			pp_ = p.Physiology;

			var gl = new VUI.GridLayout(2);
			var panel = new VUI.Panel(gl);

			int fontSize = 22;

			for (int i = 0; i < floats_.Length; ++i)
			{
				floats_[i] = new VUI.Label();
				floats_[i].FontSize = fontSize;

				var caption = new VUI.Label(PE.FloatToString(i));
				caption.FontSize = fontSize;

				panel.Add(caption);
				panel.Add(floats_[i]);
			}

			for (int i = 0; i < strings_.Length; ++i)
			{
				strings_[i] = new VUI.Label();
				strings_[i].FontSize = fontSize;

				var caption = new VUI.Label(PE.StringToString(i));
				caption.FontSize = fontSize;

				panel.Add(caption);
				panel.Add(strings_[i]);
			}

			Layout = new VUI.BorderLayout();
			Add(panel, VUI.BorderLayout.Top);
		}

		public override void Update(float s)
		{
			for (int i = 0; i < floats_.Length; ++i)
				floats_[i].Text = pp_.Get(i).ToString();

			for (int i = 0; i < strings_.Length; ++i)
				strings_[i].Text = pp_.GetString(i);
		}
	}


	class PersonAIPersonalityTab : Tab
	{
		private Person person_;
		private Personality ps_;

		private VUI.Panel panel_;
		private List<VUI.Label> bools_ = new List<VUI.Label>();
		private List<VUI.Label> floats_ = new List<VUI.Label>();
		private List<VUI.Label> strings_ = new List<VUI.Label>();
		private List<VUI.Label> slidingDurations_ = new List<VUI.Label>();
		private List<VUI.Label> expressions_ = new List<VUI.Label>();
		private int currentState_ = 0;

		public PersonAIPersonalityTab(Person p)
			: base("Personality")
		{
			person_ = p;
			ps_ = p.Personality;

			var gl = new VUI.GridLayout(2);
			panel_ = new VUI.Panel(gl);

			Layout = new VUI.BorderLayout();
			Add(new VUI.ComboBox<string>(PSE.StateNames, OnState), VUI.BorderLayout.Top);
			Add(panel_, VUI.BorderLayout.Center);

			Rebuild();
		}

		private void OnState(int i)
		{
			currentState_ = i;
			Rebuild();
		}

		private void Rebuild()
		{
			Remove(panel_);
			panel_ = new VUI.Panel(new VUI.GridLayout(2));
			Add(panel_, VUI.BorderLayout.Center);
			//panel_.RemoveAllChildren();

			int fontSize = 20;

			bools_ = new List<VUI.Label>();
			for (int i = 0; i < PSE.BoolCount; ++i)
			{
				bools_.Add(new VUI.Label());
				bools_[i].FontSize = fontSize;

				var caption = new VUI.Label(PSE.BoolToString(i));
				caption.FontSize = fontSize;

				panel_.Add(caption);
				panel_.Add(bools_[i]);
			}

			floats_ = new List<VUI.Label>();
			for (int i = 0; i < PSE.FloatCount; ++i)
			{
				floats_.Add(new VUI.Label());
				floats_[i].FontSize = fontSize;

				var caption = new VUI.Label(PSE.FloatToString(i));
				caption.FontSize = fontSize;

				panel_.Add(caption);
				panel_.Add(floats_[i]);
			}

			strings_ = new List<VUI.Label>();
			for (int i = 0; i < PSE.StringCount; ++i)
			{
				strings_.Add(new VUI.Label());
				strings_[i].FontSize = fontSize;

				var caption = new VUI.Label(PSE.StringToString(i));
				caption.FontSize = fontSize;

				panel_.Add(caption);
				panel_.Add(strings_[i]);
			}

			slidingDurations_ = new List<VUI.Label>();
			for (int i = 0; i < PSE.SlidingDurationCount; ++i)
			{
				slidingDurations_.Add(new VUI.Label());
				slidingDurations_[i].FontSize = fontSize;

				var caption = new VUI.Label(PSE.SlidingDurationToString(i));
				caption.FontSize = fontSize;

				panel_.Add(caption);
				panel_.Add(slidingDurations_[i]);
			}

			var st = ps_.GetState(currentState_);
			expressions_ = new List<VUI.Label>();
			for (int i = 0; i < st.expressions.Length; ++i)
			{
				var ex = st.expressions[i];

				var val = new VUI.Label();
				val.FontSize = fontSize;
				expressions_.Add(val);

				var caption = new VUI.Label(Expressions.ToString(ex.type));
				caption.FontSize = fontSize;

				panel_.Add(caption);
				panel_.Add(val);
			}
		}

		public override void Update(float s)
		{
			for (int i = 0; i < bools_.Count; ++i)
				bools_[i].Text = ps_.GetBool(i).ToString();

			for (int i = 0; i < floats_.Count; ++i)
				floats_[i].Text = ps_.Get(i).ToString();

			for (int i = 0; i < strings_.Count; ++i)
				strings_[i].Text = ps_.GetString(i);

			for (int i = 0; i < slidingDurations_.Count; ++i)
				slidingDurations_[i].Text = ps_.GetSlidingDuration(i).ToString();

			var st = ps_.GetState(currentState_);
			for (int i = 0; i < expressions_.Count; ++i)
				expressions_[i].Text = st.expressions[i].intensity.ToString();
		}
	}
}
