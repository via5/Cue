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
		private VUI.Label personality_ = new VUI.Label();
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

			personality_.Text = person_.Personality.ToString();
		}

		private void OnClose(bool b)
		{
			var sp = person_.Personality as BasicPersonality;
			sp.ForceSetClose(b, b);
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
}
