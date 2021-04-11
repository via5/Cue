using UnityEngine;
using UnityEngine.UI;

namespace Cue
{
	class Hud
	{
		private GameObject fullscreenPanel_ = null;
		private GameObject hudPanel_ = null;
		private VUI.Root root_ = null;

		public void Create(Transform parent)
		{
			CreateFullscreenPanel(parent);
			CreateHudPanel();

			root_ = new VUI.Root(hudPanel_.transform);
			root_.ContentPanel.Layout = new VUI.BorderLayout();
			root_.ContentPanel.Add(new VUI.Label("Test"), VUI.BorderLayout.Top);
		}

		public void Destroy()
		{
			Object.Destroy(fullscreenPanel_);
			fullscreenPanel_ = null;
		}

		public void Update()
		{
			root_.Update();
		}

		private void CreateFullscreenPanel(Transform parent)
		{
			fullscreenPanel_ = new GameObject();
			fullscreenPanel_.transform.SetParent(parent, false);
			var canvas = fullscreenPanel_.AddComponent<Canvas>();
			var cr = fullscreenPanel_.AddComponent<CanvasRenderer>();
			var bg = fullscreenPanel_.AddComponent<Image>();
			var rt = fullscreenPanel_.AddComponent<RectTransform>();
			if (rt == null)
				rt = fullscreenPanel_.GetComponent<RectTransform>();

			canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			rt.offsetMin = new Vector2(2000, 2000);
			rt.offsetMax = new Vector2(2000f, 2000);
			rt.anchorMin = new Vector2(0, 0);
			rt.anchorMax = new Vector2(1, 1);
			rt.anchoredPosition = new Vector2(0.5f, -0.5f);
			bg.color = new Color(1, 1, 1, 0);
		}

		private void CreateHudPanel()
		{
			hudPanel_ = new GameObject();
			hudPanel_.transform.SetParent(fullscreenPanel_.transform, false);

			var bg = hudPanel_.AddComponent<Image>();
			var rt = hudPanel_.AddComponent<RectTransform>();
			if (rt == null)
				rt = hudPanel_.GetComponent<RectTransform>();

			bg.color = new Color(1, 0, 0, 0.5f);

			rt.offsetMin = new Vector2(-100, 0f);
			rt.offsetMax = new Vector2(100, 100);
			rt.anchorMin = new Vector2(0.5f, 1);
			rt.anchorMax = new Vector2(0.5f, 1);
			rt.anchoredPosition = new Vector2(
				(rt.offsetMax.x - rt.offsetMin.x) / 2,
				-(rt.offsetMax.y - rt.offsetMin.y) / 2);
		}
	}
}
