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
		private VUI.ListView<string> anims_ = new VUI.ListView<string>();

		public void Init()
		{
			MVRScriptUI scriptui = null;
			VUI.Glue.PluginManagerDelegate getManager = null;

#if (VAM_GT_1_20)
			scriptui = CueMain.Instance.UITransform.GetComponentInChildren<MVRScriptUI>();
			getManager = () => CueMain.Instance.manager;
#endif

			VUI.Glue.Set(
				getManager,
				(s, ps) => Strings.Get(s, ps),
				(s) => Cue.LogVerbose(s),
				(s) => Cue.LogInfo(s),
				(s) => Cue.LogWarning(s),
				(s) => Cue.LogError(s));

			root_ = new VUI.Root(scriptui.fullWidthUIContent);
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

			//var items = new List<string>();
			//AddAnims(items, "Custom\\Animations\\V3_BVH_Ambient_Motions");
			//anims_.SetItems(items);
		}

		private void AddAnims(List<string> items, string path)
		{
			//foreach (var f in SuperController.singleton.GetFilesAtPath(path, "*.bvh"))
			//	items.Add(f);
			//
			//foreach (var d in SuperController.singleton.GetDirectoriesAtPath(path))
			//	AddAnims(items, d);
		}

		public void Update()
		{
			var ps = Cue.Instance.Persons;
			if (ps.Count == 0)
				return;

			var p = ps[0];

			action_.Text = "Action: " + p.Action.ToString();
			anim_.Text = "Anim: " + p.Animator.ToString();
			state_.Text = "PF: " + p.Atom.NavState.ToString();

			root_.Update();
		}

		private void OnPlay()
		{
			var f = anims_.Selected;
			if (string.IsNullOrEmpty(f))
				return;

			var a = new BVH.Animation();
			a.file = new BVH.File(f);
			a.rootXZ = true;
			a.rootY = true;
			//Cue.Instance.Person.Animator.Play(a);
		}
	}
}
