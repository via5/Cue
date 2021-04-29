using UnityEngine;
using UnityEngine.UI;

namespace Cue.W
{
	using Vector3 = UnityEngine.Vector3;
	using Color = UnityEngine.Color;

	// vr, moves with the head
	//
	class VRTopHudRootSupport : VUI.IRootSupport
	{
		private Vector3 offset_;
		private Vector2 pos_, size_;
		private GameObject fullscreenPanel_ = null;
		private GameObject hudPanel_ = null;
		private Canvas canvas_ = null;

		public VRTopHudRootSupport(Vector3 offset, Vector2 pos, Vector2 size)
		{
			offset_ = offset;
			pos_ = pos;
			size_ = size;

			CreateFullscreenPanel(Camera.main.transform);
			CreateHudPanel();
		}

		public MVRScriptUI ScriptUI
		{
			get { return null; }
		}

		public Canvas Canvas
		{
			get { return canvas_; }
		}

		public Transform RootParent
		{
			get { return hudPanel_.transform; }
		}

		public void Destroy()
		{
			if (canvas_ != null)
				SuperController.singleton.RemoveCanvas(canvas_);

			Object.Destroy(fullscreenPanel_);
			fullscreenPanel_ = null;
		}

		private void CreateFullscreenPanel(Transform parent)
		{
			fullscreenPanel_ = new GameObject();
			fullscreenPanel_.transform.SetParent(parent, false);

			canvas_ = fullscreenPanel_.AddComponent<Canvas>();
			var cr = fullscreenPanel_.AddComponent<CanvasRenderer>();
			var cs = fullscreenPanel_.AddComponent<CanvasScaler>();
			var rt = fullscreenPanel_.AddComponent<RectTransform>();
			if (rt == null)
				rt = fullscreenPanel_.GetComponent<RectTransform>();

			canvas_.renderMode = RenderMode.WorldSpace;
			canvas_.worldCamera = Camera.main;
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
			rt.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);

			SuperController.singleton.AddCanvas(canvas_);
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
	class VRHandRootSupport : VUI.IRootSupport
	{
		private Vector3 offset_;
		private Vector2 pos_, size_;
		private GameObject fullscreenPanel_ = null;
		private GameObject hudPanel_ = null;
		private Canvas canvas_ = null;

		public VRHandRootSupport(UnityEngine.Vector3 offset, Vector2 pos, Vector2 size)
		{
			offset_ = offset;
			pos_ = pos;
			size_ = size;

			CreateFullscreenPanel(SuperController.singleton.leftHand);
			CreateHudPanel();
		}

		public MVRScriptUI ScriptUI
		{
			get { return null; }
		}

		public Canvas Canvas
		{
			get { return canvas_; }
		}

		public Transform RootParent
		{
			get { return hudPanel_.transform; }
		}

		public void Destroy()
		{
			if (canvas_ != null)
				SuperController.singleton.RemoveCanvas(canvas_);

			Object.Destroy(fullscreenPanel_);
			fullscreenPanel_ = null;
		}

		private void CreateFullscreenPanel(Transform parent)
		{
			fullscreenPanel_ = new GameObject();
			fullscreenPanel_.transform.SetParent(parent, false);

			canvas_ = fullscreenPanel_.AddComponent<Canvas>();
			var cr = fullscreenPanel_.AddComponent<CanvasRenderer>();
			var cs = fullscreenPanel_.AddComponent<CanvasScaler>();
			var rt = fullscreenPanel_.AddComponent<RectTransform>();
			if (rt == null)
				rt = fullscreenPanel_.GetComponent<RectTransform>();

			canvas_.renderMode = RenderMode.WorldSpace;
			canvas_.worldCamera = Camera.main;
			fullscreenPanel_.transform.position = parent.position + offset_;


			var bg = fullscreenPanel_.AddComponent<Image>();
			bg.color = new Color(0, 0, 0, 0);
			bg.raycastTarget = false;

			var rc = fullscreenPanel_.AddComponent<GraphicRaycaster>();
			rc.ignoreReversedGraphics = false;

			rt.offsetMin = new Vector2(-2000, -200);
			rt.offsetMax = new Vector2(10, 100);
			rt.anchoredPosition = new Vector2(0.5f, 0.5f);
			rt.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);

			SuperController.singleton.AddCanvas(canvas_);
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
	class OverlayRootSupport : VUI.IRootSupport
	{
		private GameObject panel_ = null;
		private GameObject ui_ = null;
		private Canvas canvas_ = null;

		public OverlayRootSupport(float topOffset, float width, float height)
		{
			panel_ = new GameObject("OverlayRootSupport");
			panel_.transform.SetParent(Cue.Instance.VamSys.RootTransform, false);

			canvas_ = panel_.AddComponent<Canvas>();
			panel_.AddComponent<CanvasRenderer>();
			panel_.AddComponent<CanvasScaler>();

			canvas_.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas_.gameObject.AddComponent<GraphicRaycaster>();
			canvas_.scaleFactor = 0.5f;
			canvas_.pixelPerfect = true;

			ui_ = new GameObject("OverlayRootSupportUI");
			ui_.transform.SetParent(panel_.transform, false);
			var rt = ui_.AddComponent<RectTransform>();
			rt.anchorMin = new Vector2(0.5f, 1);
			rt.anchorMax = new Vector2(0.5f, 1);
			rt.offsetMin = new Vector2(-width/2, -(height + topOffset));
			rt.offsetMax = new Vector2(width/2, -topOffset);

			var bg = ui_.AddComponent<Image>();
			bg.color = new Color(0, 0, 0, 0.8f);
			bg.raycastTarget = false;
		}

		public MVRScriptUI ScriptUI
		{
			get { return null; }
		}

		public Canvas Canvas
		{
			get { return canvas_; }
		}

		public Transform RootParent
		{
			get { return ui_.transform; }
		}

		public void Destroy()
		{
			Object.Destroy(panel_);
		}
	}


	class ScriptUIRootSupport : VUI.IRootSupport
	{
		private MVRScriptUI sui_;

		public ScriptUIRootSupport(MVRScriptUI sui)
		{
			sui_ = sui;
		}

		public MVRScriptUI ScriptUI
		{
			get { return sui_; }
		}

		public Canvas Canvas
		{
			get
			{
				return sui_.GetComponentInChildren<Image>()?.canvas;
			}
		}

		public Transform RootParent
		{
			get
			{
				return sui_.fullWidthUIContent;
			}
		}

		public void Destroy()
		{
			// no-op
		}
	}
}
