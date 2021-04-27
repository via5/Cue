using System;
using System.Collections.Generic;

namespace Cue.UI
{
	class ScriptUI
	{
		private VUI.Root root_ = null;

		private VUI.Panel panel_ = new VUI.Panel();
		private VUI.Label action_ = new VUI.Label();
		private VUI.Label anim_ = new VUI.Label();
		private VUI.Label state_ = new VUI.Label();
		private VUI.CheckBox navmeshes_ = new VUI.CheckBox("Navmeshes");
		private VUI.Button play_ = new VUI.Button("Play");
		private VUI.ListView<IAnimation> anims_ = new VUI.ListView<IAnimation>();

		public void Init()
		{
			root_ = Cue.Instance.Sys.CreateScriptUI();
			root_.ContentPanel.Layout = new VUI.BorderLayout();
			root_.ContentPanel.Add(panel_, VUI.BorderLayout.Center);

			panel_.Layout = new VUI.BorderLayout();

			var top = new VUI.Panel(new VUI.VerticalFlow());
			top.Add(action_);
			top.Add(anim_);
			top.Add(state_);
			top.Add(navmeshes_);
			top.Add(play_);

			navmeshes_.Tooltip.Text = "12#";

			panel_.Add(top, VUI.BorderLayout.Top);
			panel_.Add(anims_, VUI.BorderLayout.Center);

			navmeshes_.Changed += (b) => Cue.Instance.Sys.Nav.Render = b;
			play_.Clicked += OnPlay;

			var items = new List<IAnimation>();
			foreach (var a in Resources.Animations.GetAll(Resources.Animations.NoType, Sexes.Female))
				items.Add(a);

			anims_.SetItems(items);
		}

		public void Update()
		{
			var ps = Cue.Instance.Persons;
			if (ps.Count == 0)
				return;

			var p = ps[0];

			action_.Text = "Action: " + p.Actions.ToString();
			anim_.Text = "Anim: " + p.Animator.ToString();
			state_.Text = "PF: " + p.Atom.NavState.ToString();

			root_.Update();
		}

		private void OnPlay()
		{
			var a = anims_.Selected;
			if (a == null)
				return;

			if (Cue.Instance.Selected is Person)
				((Person)Cue.Instance.Selected).Animator.Play(a);
		}
	}
}
