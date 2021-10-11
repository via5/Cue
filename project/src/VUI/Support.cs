using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

namespace VUI
{
	interface IRootSupport
	{
		bool Init();
		void Destroy();
		void SetActive(bool b);
		void Update(float s);

		Rectangle Bounds { get; }
		float TopOffset { get; }
		Transform RootParent { get; }

		Point ToLocal(Vector2 v);
	}


	abstract class BasicRootSupport : IRootSupport
	{
		protected class InitResults
		{
			public bool ok;
			public Rectangle bounds;
			public float topOffset;

			public InitResults(Rectangle b, float to)
			{
				ok = true;
				bounds = b;
				topOffset = to;
			}

			public static InitResults Failed
			{
				get
				{
					var r = new InitResults(Rectangle.Zero, 0);
					r.ok = false;
					return r;
				}
			}
		}

		private Canvas canvas_ = null;
		private Rectangle bounds_ = Rectangle.Zero;
		private float topOffset_ = 0;

		public virtual Rectangle Bounds
		{
			get { return bounds_; }
		}

		public float TopOffset
		{
			get { return topOffset_; }
		}


		public abstract Transform RootParent { get; }

		public abstract void Destroy();
		public abstract void SetActive(bool b);

		public virtual void Update(float s)
		{
			// no-op
		}

		public bool Init()
		{
			var r = DoInit();
			if (!r.ok)
				return false;

			bounds_ = r.bounds;
			topOffset_ = r.topOffset;

			return true;
		}

		public virtual Point ToLocal(Vector2 v)
		{
			if (canvas_ == null)
			{
				canvas_ = GetCanvas();
				if (canvas_ == null)
					return Root.NoMousePos;
			}

			Vector2 pp;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				canvas_.transform as RectTransform, v,
				canvas_.worldCamera, out pp);

			pp.x = bounds_.Left + bounds_.Width / 2 + pp.x;
			pp.y = bounds_.Top + (bounds_.Height - pp.y + topOffset_);

			return new Point(pp.x, pp.y);
		}

		protected abstract InitResults DoInit();
		protected abstract Canvas GetCanvas();
	}


	class ScriptUIRootSupport : BasicRootSupport
	{
		private MVRScript s_ = null;
		private MVRScriptUI sui_ = null;
		private Style.RootRestore rr_ = null;

		public ScriptUIRootSupport(MVRScript s)
		{
			s_ = s;
		}

		public ScriptUIRootSupport(MVRScriptUI sui)
		{
			sui_ = sui;
		}

		public override Transform RootParent
		{
			get { return sui_?.fullWidthUIContent; }
		}

		protected override InitResults DoInit()
		{
			if (sui_ == null)
			{
				if (s_.UITransform == null)
				{
					Glue.LogVerbose("scriptui support: not ready, no UITransform");
					return InitResults.Failed;
				}

				sui_ = s_.UITransform.GetComponentInChildren<MVRScriptUI>();
				if (sui_ == null)
				{
					Glue.LogVerbose("scriptui support: not ready, no scriptui");
					return InitResults.Failed;
				}
			}

			var scrollView = sui_.GetComponentInChildren<ScrollRect>();
			var scrollViewRT = scrollView.GetComponent<RectTransform>();

			if (scrollViewRT.rect.width <= 0 || scrollViewRT.rect.height <= 0)
			{
				Glue.LogVerbose(
					$"scriptui support: not ready, scroll view size is " +
					$"{scrollViewRT.rect}");

				return InitResults.Failed;
			}

			Glue.LogVerbose("scriptui support: ready, initing");

			rr_ = Style.SetupRoot(sui_.transform);

			var bounds = Rectangle.FromPoints(
					1, 1,
					scrollViewRT.rect.width - 3,
					scrollViewRT.rect.height - 3);

			var topOffset = scrollViewRT.offsetMin.y - scrollViewRT.offsetMax.y;

			return new InitResults(bounds, topOffset);
		}

		public override void Destroy()
		{
			if (sui_ != null)
				Style.RevertRoot(sui_.transform, rr_);
		}

		public override void SetActive(bool b)
		{
			if (sui_ != null)
			{
				if (b)
					rr_ = Style.SetupRoot(sui_.transform);
				else
					Style.RevertRoot(sui_.transform, rr_);
			}
		}

		protected override Canvas GetCanvas()
		{
			return sui_?.GetComponentInChildren<Image>()?.canvas;
		}
	}


	class TransformUIRootSupport : BasicRootSupport
	{
		private const float StyleCheckInterval = 1;

		private Transform t_;
		private List<Transform> restore_ = new List<Transform>();
		private Style.RootRestore rr_ = null;
		private float styleCheck_ = 0;

		public TransformUIRootSupport(Transform t)
		{
			t_ = t;
		}

		public override Transform RootParent
		{
			get { return t_; }
		}

		protected override InitResults DoInit()
		{
			var rt = t_.GetComponent<RectTransform>();

			var bounds = Rectangle.FromPoints(
				1, 1,
				rt.rect.width - 3,
				rt.rect.height - 3);

			var topOffset = 0;// scrollViewRT.offsetMin.y - scrollViewRT.offsetMax.y;

			restore_.Clear();
			foreach (Transform t in t_)
			{
				if (t.gameObject.activeSelf)
					restore_.Add(t);
			}

			SetActive(true);

			return new InitResults(bounds, topOffset);
		}

		public override void Destroy()
		{
			SetActive(false);
		}

		public override void SetActive(bool b)
		{
			if (t_ != null)
			{
				if (b)
				{
					foreach (Transform t in t_)
					{
						if (t.gameObject.activeSelf)
							t.gameObject.SetActive(false);
					}

					rr_ = Style.SetupRoot(t_);
				}
				else
				{
					foreach (Transform t in restore_)
						t.gameObject.SetActive(true);

					Style.RevertRoot(t_, rr_);
				}
			}
		}

		public override void Update(float s)
		{
			styleCheck_ += s;

			if (styleCheck_ >= StyleCheckInterval)
			{
				styleCheck_ = 0;
				Style.CheckRoot(t_, rr_);
			}
		}

		protected override Canvas GetCanvas()
		{
			return t_?.GetComponentInChildren<Image>()?.canvas;
		}
	}


	// vr, moves with the head
	//
	class VRTopHudRootSupport : BasicRootSupport
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
		}

		public override Transform RootParent
		{
			get { return hudPanel_.transform; }
		}

		protected override InitResults DoInit()
		{
			CreateFullscreenPanel(Camera.main.transform);
			CreateHudPanel();

			var rt = RootParent.GetComponent<RectTransform>();

			var bounds = Rectangle.FromPoints(
				0, 0, rt.rect.width, rt.rect.height);

			var topOffset = rt.offsetMin.y - rt.offsetMax.y;

			return new InitResults(bounds, topOffset);
		}

		public override void Destroy()
		{
			if (canvas_ != null)
				SuperController.singleton.RemoveCanvas(canvas_);

			Object.Destroy(fullscreenPanel_);
			fullscreenPanel_ = null;
		}

		public override void SetActive(bool b)
		{
			// todo
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

		protected override Canvas GetCanvas()
		{
			return canvas_;
		}
	}


	class FaceCamera : MonoBehaviour
	{
		public void LateUpdate()
		{
			var c = Camera.main.transform;
			transform.LookAt(c.position);
		}
	}


	// vr, attaches to a hand
	//
	class VRHandRootSupport : BasicRootSupport
	{
		public const int LeftHand = 0;
		public const int RightHand = 1;

		private Vector3 offset_;
		private Vector2 pos_, size_;
		private GameObject fullscreenPanel_ = null;
		private GameObject hudPanel_ = null;
		private Canvas canvas_ = null;
		private int hand_ = LeftHand;

		public VRHandRootSupport(int hand, Vector3 offset, Vector2 pos, Vector2 size)
		{
			hand_ = hand;
			offset_ = offset;
			pos_ = pos;
			size_ = size;
		}

		public override Transform RootParent
		{
			get { return hudPanel_.transform; }
		}

		public Transform HandTransform
		{
			get
			{
				return (hand_ == LeftHand) ?
					SuperController.singleton.leftHand :
					SuperController.singleton.rightHand;
			}
		}

		protected override InitResults DoInit()
		{
			CreateFullscreenPanel(HandTransform);
			CreateHudPanel();

			var rt = RootParent.GetComponent<RectTransform>();

			var bounds = Rectangle.FromPoints(
				0, 0, rt.rect.width, rt.rect.height);

			var topOffset = rt.offsetMin.y - rt.offsetMax.y;

			return new InitResults(bounds, topOffset);
		}

		public override void Destroy()
		{
			if (canvas_ != null)
				SuperController.singleton.RemoveCanvas(canvas_);

			Object.Destroy(fullscreenPanel_);
			fullscreenPanel_ = null;
		}

		public override void SetActive(bool b)
		{
			fullscreenPanel_?.SetActive(b);
		}

		public void Attach(int hand)
		{
			hand_ = hand;
			Attach(HandTransform);
		}

		private void Attach(Transform parent)
		{
			fullscreenPanel_.transform.SetParent(parent, false);
			fullscreenPanel_.transform.position = parent.position + offset_;
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
			var fc = fullscreenPanel_.AddComponent<FaceCamera>();

			float w = size_.x;
			float h = size_.y;
			float yoffset = 0;
			float s = 0.1f;

			rt.anchorMin = new Vector2(0.5f, 1);
			rt.anchorMax = new Vector2(0.5f, 1);
			rt.offsetMin = new Vector2(-w / 2, -(h + yoffset));
			rt.offsetMax = new Vector2(w / 2, -yoffset);
			rt.anchoredPosition3D = new Vector3(0, 0, 0);

			//rt.anchoredPosition = new Vector2(0.5f, 0.5f);
			rt.localPosition = new Vector3(0, 0.08f, -0.05f);
			rt.localScale = new Vector3(-s / w, s / w, s / w);

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

		protected override Canvas GetCanvas()
		{
			return canvas_;
		}
	}


	// desktop
	//
	class OverlayRootSupport : BasicRootSupport
	{
		private float topOffset_;
		private float width_, height_;

		private GameObject panel_ = null;
		private GameObject ui_ = null;
		private Canvas canvas_ = null;

		public OverlayRootSupport(float topOffset, float width, float height)
		{
			topOffset_ = topOffset;
			width_ = width;
			height_ = height;
		}

		public override Transform RootParent
		{
			get { return ui_.transform; }
		}

		protected override InitResults DoInit()
		{
			panel_ = new GameObject("OverlayRootSupport");

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
			rt.offsetMin = new Vector2(-width_ / 2, -(height_ + topOffset_));
			rt.offsetMax = new Vector2(width_ / 2, -topOffset_);

			var bg = ui_.AddComponent<Image>();
			bg.color = new Color(0, 0, 0, 0.8f);
			bg.raycastTarget = true;

			SuperController.singleton.AddCanvas(canvas_);

			var bounds = Rectangle.FromPoints(
				0, 0, rt.rect.width, rt.rect.height);

			var topOffset = rt.offsetMin.y - rt.offsetMax.y;

			return new InitResults(bounds, topOffset);
		}

		public override void Destroy()
		{
			if (canvas_ != null)
				SuperController.singleton.RemoveCanvas(canvas_);

			if (panel_ != null)
			{
				Object.Destroy(panel_);
				panel_ = null;
			}
		}

		public override void SetActive(bool b)
		{
			// todo
		}

		protected override Canvas GetCanvas()
		{
			return canvas_;
		}
	}
}
