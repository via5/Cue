using System.Collections.Generic;

namespace Cue
{
	class PersonAITab : Tab
	{
		private Person person_;
		private PersonAI ai_;

		private VUI.Label enabled_ = new VUI.Label();
		private VUI.Label event_ = new VUI.Label();
		private VUI.Label personality_ = new VUI.Label();

		public PersonAITab(Person p)
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


			Layout = new VUI.BorderLayout();
			Add(state, VUI.BorderLayout.Top);
		}

		public override string Title
		{
			get { return "AI"; }
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
	}
}
