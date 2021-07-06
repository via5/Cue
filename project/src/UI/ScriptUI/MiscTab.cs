using System.Collections.Generic;

namespace Cue
{
	class MiscTab : Tab
	{
		private VUI.CheckBox navmeshes_ = new VUI.CheckBox("Navmeshes");
		private VUI.Button renav_ = new VUI.Button("Update nav");
		private VUI.Label update_ = new VUI.Label();
		private VUI.Label fixedUpdate_ = new VUI.Label();
		private VUI.Label input_ = new VUI.Label();
		private VUI.Label objects_ = new VUI.Label();
		private VUI.Label ui_ = new VUI.Label();

		private VUI.CheckBox logAnimation_;
		private VUI.CheckBox logAction_;
		private VUI.CheckBox logInteraction_;
		private VUI.CheckBox logAI_;
		private VUI.CheckBox logEvent_;
		private VUI.CheckBox logIntegration_;
		private VUI.CheckBox logObject_;
		private VUI.CheckBox logSlots_;
		private VUI.CheckBox logSys_;
		private VUI.CheckBox logClothing_;
		private VUI.CheckBox logResources_;

		public MiscTab()
			: base("Misc")
		{
			logAnimation_ = new VUI.CheckBox("Animation", CheckLog);
			logAction_ = new VUI.CheckBox("Action", CheckLog);
			logInteraction_ = new VUI.CheckBox("Interaction", CheckLog);
			logAI_ = new VUI.CheckBox("AI", CheckLog);
			logEvent_ = new VUI.CheckBox("Event", CheckLog);
			logIntegration_ = new VUI.CheckBox("Integration", CheckLog);
			logObject_ = new VUI.CheckBox("Object", CheckLog);
			logSlots_ = new VUI.CheckBox("Slots", CheckLog);
			logSys_ = new VUI.CheckBox("Sys", CheckLog);
			logClothing_ = new VUI.CheckBox("Clothing", CheckLog);
			logResources_ = new VUI.CheckBox("Resources", CheckLog);

			Layout = new VUI.VerticalFlow();
			Add(navmeshes_);
			Add(renav_);

			var gl = new VUI.GridLayout(2);
			gl.HorizontalStretch = new List<bool>() { false, true };
			gl.HorizontalSpacing = 40;

			var p = new VUI.Panel(gl);

			p.Add(new VUI.Label("Update"));
			p.Add(update_);

			p.Add(new VUI.Label("  Input"));
			p.Add(input_);

			p.Add(new VUI.Label("  Objects"));
			p.Add(objects_);

			p.Add(new VUI.Label("  UI"));
			p.Add(ui_);

			p.Add(new VUI.Label("Fixed Update"));
			p.Add(fixedUpdate_);

			Add(p);
			Add(new VUI.Spacer(30));

			Add(new VUI.Label("Logs", UnityEngine.FontStyle.Bold));
			Add(logAnimation_);
			Add(logAction_);
			Add(logInteraction_);
			Add(logAI_);
			Add(logEvent_);
			Add(logIntegration_);
			Add(logObject_);
			Add(logSlots_);
			Add(logSys_);
			Add(logClothing_);
			Add(logResources_);

			navmeshes_.Changed += (b) => Cue.Instance.Sys.Nav.Render = b;
			renav_.Clicked += Cue.Instance.Sys.Nav.Update;
		}

		public override void Update(float s)
		{
		}

		public void UpdateTickers(Tickers tickers)
		{
			if (IsVisibleOnScreen())
			{
				update_.Text = tickers.update.ToString();
				input_.Text = tickers.input.ToString();
				objects_.Text = tickers.objects.ToString();
				ui_.Text = tickers.ui.ToString();
				fixedUpdate_.Text = tickers.fixedUpdate.ToString();
			}
		}

		private void CheckLog(bool b)
		{
			int e = 0;

			if (logAnimation_.Checked) e |= Logger.Animation;
			if (logAction_.Checked) e |= Logger.Action;
			if (logInteraction_.Checked) e |= Logger.Interaction;
			if (logAI_.Checked) e |= Logger.AI;
			if (logEvent_.Checked) e |= Logger.Event;
			if (logIntegration_.Checked) e |= Logger.Integration;
			if (logObject_.Checked) e |= Logger.Object;
			if (logSlots_.Checked) e |= Logger.Slots;
			if (logSys_.Checked) e |= Logger.Sys;
			if (logClothing_.Checked) e |= Logger.Clothing;
			if (logResources_.Checked) e |= Logger.Resources;

			Logger.Enabled = e;
		}
	}
}
