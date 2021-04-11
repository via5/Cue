using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Cue
{
	class Hud
	{
		private GameObject fullscreenPanel_ = null;
		private GameObject hudPanel_ = null;
		private VUI.Root root_ = null;
		private VUI.Label label_ = null;

		public void Create(Transform parent)
		{
			Cue.Instance.SelectionChanged += OnSelectionChanged;

			CreateFullscreenPanel(parent);
			CreateHudPanel();

			root_ = new VUI.Root(hudPanel_.transform);
			root_.ContentPanel.Layout = new VUI.BorderLayout();
			label_ = root_.ContentPanel.Add(new VUI.Label(), VUI.BorderLayout.Center);
			root_.ContentPanel.Add(new VUI.Button("Reload", OnReload), VUI.BorderLayout.Bottom);
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

		private void OnSelectionChanged(IObject o)
		{
			label_.Text = (o == null ? "" : o.ToString());
		}

		private void OnReload()
		{
			Cue.Instance.ReloadPlugin();
		}

		private void CreateFullscreenPanel(Transform parent)
		{
			fullscreenPanel_ = new GameObject();
			fullscreenPanel_.transform.SetParent(parent, false);

			var canvas = fullscreenPanel_.AddComponent<Canvas>();
			var cr = fullscreenPanel_.AddComponent<CanvasRenderer>();
			var rt = fullscreenPanel_.AddComponent<RectTransform>();
			if (rt == null)
				rt = fullscreenPanel_.GetComponent<RectTransform>();

			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.gameObject.AddComponent<GraphicRaycaster>();

			rt.offsetMin = new Vector2(2000, 2000);
			rt.offsetMax = new Vector2(2000f, 2000);
			rt.anchorMin = new Vector2(0, 0);
			rt.anchorMax = new Vector2(1, 1);
			rt.anchoredPosition = new Vector2(0.5f, -0.5f);
		}

		private void CreateHudPanel()
		{
			hudPanel_ = new GameObject();
			hudPanel_.transform.SetParent(fullscreenPanel_.transform, false);

			var bg = hudPanel_.AddComponent<Image>();
			var rt = hudPanel_.AddComponent<RectTransform>();
			if (rt == null)
				rt = hudPanel_.GetComponent<RectTransform>();

			bg.color = new Color(0, 0, 0, 0.5f);
			bg.raycastTarget = true;

			rt.offsetMin = new Vector2(-300, 0);
			rt.offsetMax = new Vector2(300, 100);
			rt.anchorMin = new Vector2(0.5f, 1);
			rt.anchorMax = new Vector2(0.5f, 1);
			rt.anchoredPosition = new Vector2(
				(rt.offsetMax.x - rt.offsetMin.x) / 4,
				-(rt.offsetMax.y - rt.offsetMin.y) / 2);
		}
	}
}
