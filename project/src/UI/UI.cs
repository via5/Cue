namespace Cue.UI
{
	interface IUI
	{
		void Init();
		void Update();
	}


	class VamUI : IUI
	{
		private VUI.Root root_ = null;

		private VUI.Panel panel_ = new VUI.Panel();
		private VUI.Label action_ = new VUI.Label();
		private VUI.Label anim_ = new VUI.Label();
		private VUI.Label state_ = new VUI.Label();

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

			panel_.Layout = new VUI.VerticalFlow();
			panel_.Add(action_);
			panel_.Add(anim_);
			panel_.Add(state_);
		}

		public void Update()
		{
			var p = Cue.Instance.Person;

			action_.Text = "Action: " + p.Action.ToString();
			anim_.Text = "Anim: " + p.Animation.ToString();
			state_.Text = "State: " + p.StateString;

			root_.DoLayoutIfNeeded();
		}
	}
}
