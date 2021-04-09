using System.Collections.Generic;

namespace Cue.UI
{
	interface IUI
	{
		void Init();
		void Update();
	}

	class MockUI : IUI
	{
		public void Init()
		{
		}

		public void Update()
		{
		}
	}

	class VamUI : IUI
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
			VUI.Glue.Set(
				() => Cue.Instance.manager,
				() => Cue.Instance.UITransform.GetComponentInChildren<MVRScriptUI>(),
				(s, ps) => Strings.Get(s, ps),
				(s) => Cue.LogVerbose(s),
				(s) => Cue.LogInfo(s),
				(s) => Cue.LogWarning(s),
				(s) => Cue.LogError(s));

			root_ = new VUI.Root();
			root_.ContentPanel.Layout = new VUI.BorderLayout();
			root_.ContentPanel.Add(panel_, VUI.BorderLayout.Center);

			panel_.Layout = new VUI.BorderLayout();

			var top = new VUI.Panel(new VUI.VerticalFlow());
			top.Add(action_);
			top.Add(anim_);
			top.Add(state_);
			top.Add(navmeshes_);
			top.Add(play_);

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
			var p = Cue.Instance.Person;

			action_.Text = "Action: " + p.Action.ToString();
			anim_.Text = "Anim: " + p.Animator.ToString();
			state_.Text = "State: " + p.StateString;

			root_.DoLayoutIfNeeded();
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
			Cue.Instance.Person.Animator.Play(a);
		}
	}
}
