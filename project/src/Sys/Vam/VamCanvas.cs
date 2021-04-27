using UnityEngine;
using UnityEngine.UI;

namespace Cue.W
{
	using Vector3 = UnityEngine.Vector3;

	// vr, moves with the head
	//
	class WorldSpaceAttachedCanvas : ICanvas
	{
		private Vector3 offset_;
		private Vector2 pos_, size_;
		private GameObject fullscreenPanel_ = null;
		private GameObject hudPanel_ = null;

		public WorldSpaceAttachedCanvas(Vector3 offset, Vector2 pos, Vector2 size)
		{
			offset_ = offset;
			pos_ = pos;
			size_ = size;
		}

		public void Create()
		{
			CreateFullscreenPanel(Camera.main.transform);
			CreateHudPanel();
		}

		public void Destroy()
		{
			SuperController.singleton.RemoveCanvas(
				fullscreenPanel_.GetComponent<Canvas>());

			Object.Destroy(fullscreenPanel_);
			fullscreenPanel_ = null;
		}

		public void Toggle()
		{
			fullscreenPanel_.SetActive(!fullscreenPanel_.activeSelf);
		}

		public bool IsHovered(float x, float y)
		{
			return false;
		}

		public VUI.Root CreateRoot()
		{
			return new VUI.Root(hudPanel_.transform);
		}

		private void CreateFullscreenPanel(Transform parent)
		{
			fullscreenPanel_ = new GameObject();
			fullscreenPanel_.transform.SetParent(parent, false);

			var canvas = fullscreenPanel_.AddComponent<Canvas>();
			var cr = fullscreenPanel_.AddComponent<CanvasRenderer>();
			var cs = fullscreenPanel_.AddComponent<CanvasScaler>();
			var rt = fullscreenPanel_.AddComponent<RectTransform>();
			if (rt == null)
				rt = fullscreenPanel_.GetComponent<RectTransform>();

			canvas.renderMode = RenderMode.WorldSpace;
			canvas.worldCamera = Camera.main;
			fullscreenPanel_.transform.position = parent.position + offset_;


			var bg = fullscreenPanel_.AddComponent<Image>();
			bg.color = new Color(0, 0, 0, 0);
			bg.raycastTarget = false;

			var rc = fullscreenPanel_.AddComponent<GraphicRaycaster>();
			rc.ignoreReversedGraphics = false;

			rt.offsetMin = pos_;
			rt.offsetMax = size_;
			rt.anchoredPosition = new Vector2(0, 0);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.pivot = new Vector2(0.5f, 0);
			rt.localScale = new UnityEngine.Vector3(0.0005f, 0.0005f, 0.0005f);

			SuperController.singleton.AddCanvas(canvas);
		}

		private void CreateHudPanel()
		{
			hudPanel_ = new GameObject();
			hudPanel_.transform.SetParent(fullscreenPanel_.transform, false);

			var bg = hudPanel_.AddComponent<Image>();
			var rt = hudPanel_.AddComponent<RectTransform>();
			if (rt == null)
				rt = hudPanel_.GetComponent<RectTransform>();

			bg.color = new Color(0, 0, 0, 0.8f);
			bg.raycastTarget = true;

			rt.offsetMin = new Vector2(10, 10);
			rt.offsetMax = new Vector2(0, 0);
			rt.anchoredPosition = new Vector2(0, 0);
			rt.anchorMin = new Vector2(0, 0);
			rt.anchorMax = new Vector2(1, 1);
			rt.pivot = new Vector2(0.5f, 0);
		}
	}


	// vr, moves with the head
	//
	class WorldSpaceCameraCanvas : ICanvas
	{
		private Vector3 offset_;
		private Vector2 pos_, size_;
		private GameObject fullscreenPanel_ = null;
		private GameObject hudPanel_ = null;

		public WorldSpaceCameraCanvas(UnityEngine.Vector3 offset, Vector2 pos, Vector2 size)
		{
			offset_ = offset;
			pos_ = pos;
			size_ = size;
		}

		public void Create()
		{
			CreateFullscreenPanel(SuperController.singleton.leftHand);
			CreateHudPanel();
		}

		public void Destroy()
		{
			SuperController.singleton.RemoveCanvas(
				fullscreenPanel_.GetComponent<Canvas>());

			Object.Destroy(fullscreenPanel_);
			fullscreenPanel_ = null;
		}

		public void Toggle()
		{
			fullscreenPanel_.SetActive(!fullscreenPanel_.activeSelf);
		}

		public bool IsHovered(float x, float y)
		{
			return false;
		}

		public VUI.Root CreateRoot()
		{
			return new VUI.Root(hudPanel_.transform);
		}

		private void CreateFullscreenPanel(Transform parent)
		{
			fullscreenPanel_ = new GameObject();
			fullscreenPanel_.transform.SetParent(parent, false);

			var canvas = fullscreenPanel_.AddComponent<Canvas>();
			var cr = fullscreenPanel_.AddComponent<CanvasRenderer>();
			var cs = fullscreenPanel_.AddComponent<CanvasScaler>();
			var rt = fullscreenPanel_.AddComponent<RectTransform>();
			if (rt == null)
				rt = fullscreenPanel_.GetComponent<RectTransform>();

			canvas.renderMode = RenderMode.WorldSpace;
			canvas.worldCamera = Camera.main;
			fullscreenPanel_.transform.position = parent.position + offset_;


			var bg = fullscreenPanel_.AddComponent<Image>();
			bg.color = new Color(0, 0, 0, 0);
			bg.raycastTarget = false;

			var rc = fullscreenPanel_.AddComponent<GraphicRaycaster>();
			rc.ignoreReversedGraphics = false;

			rt.offsetMin = new Vector2(-2000, -200);
			rt.offsetMax = new Vector2(10, 100);
			rt.anchoredPosition = new Vector2(0.5f, 0.5f);
			rt.localScale = new UnityEngine.Vector3(0.0005f, 0.0005f, 0.0005f);

			SuperController.singleton.AddCanvas(canvas);
		}

		private void CreateHudPanel()
		{
			hudPanel_ = new GameObject();
			hudPanel_.transform.SetParent(fullscreenPanel_.transform, false);

			var bg = hudPanel_.AddComponent<Image>();
			var rt = hudPanel_.AddComponent<RectTransform>();
			if (rt == null)
				rt = hudPanel_.GetComponent<RectTransform>();

			bg.color = new Color(0, 0, 0, 0.8f);
			bg.raycastTarget = true;

			rt.offsetMin = new Vector2(10, 10);
			rt.offsetMax = new Vector2(0, 0);
			rt.anchoredPosition = new Vector2(0, 0);
			rt.anchorMin = new Vector2(0, 0);
			rt.anchorMax = new Vector2(1, 1);
			rt.pivot = new Vector2(0.5f, 0);
		}
	}


	// desktop
	//
	class OverlayCanvas : ICanvas
	{
		private GameObject fullscreenPanel_ = null;
		private GameObject hudPanel_ = null;

		public void Create()
		{
			CreateFullscreenPanel(SuperController.singleton.mainMenuUI.root);
			CreateHudPanel();
		}

		public void Destroy()
		{
			Object.Destroy(fullscreenPanel_);
			fullscreenPanel_ = null;
		}

		public bool IsHovered(float x, float y)
		{
			var rt = hudPanel_.GetComponent<RectTransform>();
			Vector2 local = rt.InverseTransformPoint(new Vector2(x, y));
			return rt.rect.Contains(local);
		}

		public void Toggle()
		{
			fullscreenPanel_.SetActive(!fullscreenPanel_.activeSelf);
		}

		public VUI.Root CreateRoot()
		{
			return new VUI.Root(hudPanel_.transform);
		}

		private void CreateFullscreenPanel(Transform parent)
		{
			fullscreenPanel_ = new GameObject();
			fullscreenPanel_.transform.SetParent(parent, false);

			var canvas = fullscreenPanel_.AddComponent<Canvas>();
			var cr = fullscreenPanel_.AddComponent<CanvasRenderer>();
			var cs = fullscreenPanel_.AddComponent<CanvasScaler>();
			var rt = fullscreenPanel_.AddComponent<RectTransform>();
			if (rt == null)
				rt = fullscreenPanel_.GetComponent<RectTransform>();

			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.gameObject.AddComponent<GraphicRaycaster>();


			var cs2 = SuperController.singleton.errorLogPanel.GetComponent<CanvasScaler>();
			cs.uiScaleMode = cs2.uiScaleMode;
			cs.referenceResolution = cs2.referenceResolution;
			cs.screenMatchMode = cs2.screenMatchMode;
			cs.matchWidthOrHeight = cs2.matchWidthOrHeight;
			cs.defaultSpriteDPI = cs2.defaultSpriteDPI;
			cs.fallbackScreenDPI = cs2.fallbackScreenDPI;
			cs.referencePixelsPerUnit = cs2.referencePixelsPerUnit;
			cs.dynamicPixelsPerUnit = cs2.dynamicPixelsPerUnit;
			cs.physicalUnit = cs2.physicalUnit;
			cs.scaleFactor = cs2.scaleFactor;

			canvas.scaleFactor = 0.5f;

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

			bg.color = new Color(0, 0, 0, 0.8f);
			bg.raycastTarget = true;

			rt.offsetMin = new Vector2(-800, 0);
			rt.offsetMax = new Vector2(800, 200);
			rt.anchorMin = new Vector2(0.5f, 1);
			rt.anchorMax = new Vector2(0.5f, 1);
			rt.anchoredPosition = new Vector2(
				(rt.offsetMax.x / 2),//- rt.offsetMin.x) / 4,
				-(rt.offsetMax.y - rt.offsetMin.y) / 2);
		}
	}
}
