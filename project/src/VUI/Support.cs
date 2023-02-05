using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace VUI
{
	public interface IRootSupport
	{
		bool Init();
		void Destroy();
		void SetActive(bool b);
		void Update(float s);

		Rectangle Bounds { get; }
		float TopOffset { get; }
		Transform RootParent { get; }

		void SetSize(Vector2 v);
		Point ToLocal(Vector2 v);
	}


	abstract class BasicRootSupport : IRootSupport
	{
		private Canvas canvas_ = null;
		private Rectangle bounds_ = Rectangle.Zero;
		private float topOffset_ = 0;

		private static List<Transform> destroy_ = new List<Transform>();
		private static bool cleaningUp_ = false;

		public static void Cleanup()
		{
			try
			{
				cleaningUp_ = true;
				destroy_.Clear();

				ScriptUIRootSupport.DoCleanup();
				TransformUIRootSupport.DoCleanup();
				VRTopHudRootSupport.DoCleanup();
				VRHandRootSupport.DoCleanup();
				OverlayRootSupport.DoCleanup();

				foreach (var t in destroy_)
					DoDestroy(t);
			}
			catch (Exception e)
			{
				Glue.LogError("exception during vui cleanup:");
				Glue.LogError(e.ToString());
			}
			finally
			{
				cleaningUp_ = false;
			}
		}

		protected static void DestroyRootObject(Transform t)
		{
			if (cleaningUp_)
				destroy_.Add(t);
			else
				DoDestroy(t);
		}

		private static void DoDestroy(Transform t)
		{
			var temp = new GameObject().transform;
			t.transform.SetParent(temp);
			UnityEngine.Object.Destroy(temp.gameObject);
		}

		protected static GameObject CreateRootObject(string prefix)
		{
			return new GameObject(
				prefix + UnityEngine.Random.Range(10000, 100000).ToString());
		}

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
		public abstract void SetSize(Vector2 v);

		public virtual void Update(float s)
		{
			// no-op
		}

		public bool Init()
		{
			return DoInit();
		}

		protected void SetBounds(Rectangle b, float topOffset)
		{
			bounds_ = b;
			topOffset_ = topOffset;
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

		protected abstract bool DoInit();
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

		public static void DoCleanup()
		{
			// no-op
		}

		public override Transform RootParent
		{
			get { return sui_?.fullWidthUIContent; }
		}

		protected override bool DoInit()
		{
			if (sui_ == null)
			{
				if (s_.UITransform == null)
				{
					Glue.LogVerbose("scriptui support: not ready, no UITransform");
					return false;
				}

				sui_ = s_.UITransform.GetComponentInChildren<MVRScriptUI>();
				if (sui_ == null)
				{
					Glue.LogVerbose("scriptui support: not ready, no scriptui");
					return false;
				}
			}

			var scrollView = sui_.GetComponentInChildren<ScrollRect>();
			var scrollViewRT = scrollView.GetComponent<RectTransform>();

			if (scrollViewRT.rect.width <= 0 || scrollViewRT.rect.height <= 0)
			{
				Glue.LogVerbose(
					$"scriptui support: not ready, scroll view size is " +
					$"{scrollViewRT.rect}");

				return false;
			}

			Glue.LogVerbose("scriptui support: ready, initing");

			rr_ = Style.SetupRoot(sui_.transform);

			var bounds = Rectangle.FromPoints(
					1, 1,
					scrollViewRT.rect.width - 3,
					scrollViewRT.rect.height - 3);

			var topOffset = scrollViewRT.offsetMin.y - scrollViewRT.offsetMax.y;

			SetBounds(bounds, topOffset);

			return true;
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

		public override void SetSize(Vector2 v)
		{
			// no-op
		}

		protected override Canvas GetCanvas()
		{
			return sui_?.GetComponentInChildren<Image>()?.canvas;
		}
	}


	class TransformUIRootSupport : BasicRootSupport
	{
		private const float StyleCheckInterval = 1;

		private readonly Transform t_;
		private GameObject root_ = null;
		private readonly List<Transform> restore_ = new List<Transform>();
		private Style.RootRestore rr_ = null;
		private float styleCheck_ = 0;

		public TransformUIRootSupport(Transform t)
		{
			t_ = t;
		}

		private static string RootObjectPrefix
		{
			get { return Glue.Prefix + ".TransformUIRootSupport."; }
		}

		public static void DoCleanup()
		{
			// no-op
		}

		public override Transform RootParent
		{
			get
			{
				return root_.transform;
			}
		}

		protected override bool DoInit()
		{
			foreach (Transform t in t_)
			{
				if (t.name.StartsWith(RootObjectPrefix))
					DestroyRootObject(t);
			}

			CreateRoot();

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
			SetBounds(bounds, topOffset);

			return true;
		}

		private void CreateRoot()
		{
			root_ = CreateRootObject(RootObjectPrefix);
			root_.transform.SetParent(t_, false);

			var rt = root_.AddComponent<RectTransform>();
			if (rt == null)
				rt = root_.GetComponent<RectTransform>();

			rt.offsetMin = new Vector2(0, 0);
			rt.offsetMax = new Vector2(0, 0);
			rt.anchoredPosition = new Vector2(0, 0);
			rt.anchorMin = new Vector2(0, 0);
			rt.anchorMax = new Vector2(1, 1);
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

			if (root_ != null)
				root_.SetActive(b);
		}

		public override void SetSize(Vector2 v)
		{
			// no-op
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

		private static string RootObjectPrefix
		{
			get { return Glue.Prefix + ".VRTopHudRootSupport."; }
		}

		public static void DoCleanup()
		{
			foreach (Transform t in Camera.main.transform)
			{
				if (t.name.StartsWith(RootObjectPrefix))
				{
					DestroyRootObject(t);
					break;
				}
			}
		}

		public override Transform RootParent
		{
			get { return hudPanel_.transform; }
		}

		protected override bool DoInit()
		{
			CreateFullscreenPanel(Camera.main.transform);
			CreateHudPanel();

			var rt = RootParent.GetComponent<RectTransform>();

			var bounds = Rectangle.FromPoints(
				0, 0, rt.rect.width, rt.rect.height);

			var topOffset = rt.offsetMin.y - rt.offsetMax.y;

			SetBounds(bounds, topOffset);

			return true;
		}

		public override void Destroy()
		{
			if (canvas_ != null)
				SuperController.singleton.RemoveCanvas(canvas_);

			UnityEngine.Object.Destroy(fullscreenPanel_);
			fullscreenPanel_ = null;
		}

		public override void SetActive(bool b)
		{
			// todo
		}

		public override void SetSize(Vector2 v)
		{
			// todo
		}

		private void CreateFullscreenPanel(Transform parent)
		{
			fullscreenPanel_ = CreateRootObject(RootObjectPrefix);
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

		private static string RootObjectPrefix
		{
			get { return Glue.Prefix + ".VRHandRootSupport."; }
		}

		public static void DoCleanup()
		{
			foreach (Transform t in SuperController.singleton.leftHand)
			{
				if (t.name.StartsWith(RootObjectPrefix))
					DestroyRootObject(t);
			}

			foreach (Transform t in SuperController.singleton.rightHand)
			{
				if (t.name.StartsWith(RootObjectPrefix))
				{
					DestroyRootObject(t);
					break;
				}
			}
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

		protected override bool DoInit()
		{
			CreateFullscreenPanel(HandTransform);
			CreateHudPanel();

			return true;
		}

		public override void Destroy()
		{
			if (canvas_ != null)
				SuperController.singleton.RemoveCanvas(canvas_);

			UnityEngine.Object.Destroy(fullscreenPanel_);
			fullscreenPanel_ = null;
		}

		public override void SetActive(bool b)
		{
			fullscreenPanel_?.SetActive(b);
		}

		public override void SetSize(Vector2 v)
		{
			size_ = v;
			SetRect();
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
			fullscreenPanel_ = CreateRootObject(RootObjectPrefix);
			fullscreenPanel_.transform.SetParent(parent, false);

			canvas_ = fullscreenPanel_.AddComponent<Canvas>();
			var cr = fullscreenPanel_.AddComponent<CanvasRenderer>();
			var cs = fullscreenPanel_.AddComponent<CanvasScaler>();
			fullscreenPanel_.AddComponent<RectTransform>();

			canvas_.renderMode = RenderMode.WorldSpace;
			canvas_.worldCamera = Camera.main;
			fullscreenPanel_.transform.position = parent.position + offset_;


			var bg = fullscreenPanel_.AddComponent<Image>();
			bg.color = new Color(0, 0, 0, 0);
			bg.raycastTarget = false;

			var rc = fullscreenPanel_.AddComponent<GraphicRaycaster>();
			var fc = fullscreenPanel_.AddComponent<FaceCamera>();

			SetRect();
			SuperController.singleton.AddCanvas(canvas_);
		}

		private void SetRect()
		{
			var rt = fullscreenPanel_.GetComponent<RectTransform>();

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

			var bounds = Rectangle.FromPoints(
				0, 0, rt.rect.width, rt.rect.height);

			var topOffset = rt.offsetMin.y - rt.offsetMax.y;

			SetBounds(bounds, topOffset);
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
		private const float MonitorOffset = 240;

		private float topOffset_;
		private Vector2 size_;

		private GameObject panel_ = null;
		private GameObject ui_ = null;
		private RectTransform rt_ = null;
		private Canvas canvas_ = null;
		private CanvasScaler scaler_ = null;
		private bool monitorWasVisible_ = false;
		private MeshVR.PerfMon perf_ = null;

		public OverlayRootSupport(float topOffset, float width, float height)
		{
			topOffset_ = topOffset;
			size_ = new Vector2(width, height);

			perf_ = SuperController.singleton.transform.root
				.GetComponentInChildren<MeshVR.PerfMon>();

			if (perf_ == null)
				Glue.LogError("OverlayRootSupport: no perfmon");
		}

		private static string RootObjectPrefix
		{
			get { return Glue.Prefix + ".OverlayRootSupport."; }
		}

		public static void DoCleanup()
		{
			foreach (Transform t in SuperController.singleton.transform.root)
			{
				if (t.name.StartsWith(RootObjectPrefix))
					DestroyRootObject(t);
			}
		}

		public override Transform RootParent
		{
			get { return ui_.transform; }
		}

		protected override bool DoInit()
		{
			panel_ = CreateRootObject(RootObjectPrefix);
			panel_.transform.SetParent(SuperController.singleton.transform.root);

			canvas_ = panel_.AddComponent<Canvas>();
			panel_.AddComponent<CanvasRenderer>();
			scaler_ = panel_.AddComponent<CanvasScaler>();

			canvas_.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas_.gameObject.AddComponent<GraphicRaycaster>();
			canvas_.scaleFactor = 0.5f;
			canvas_.pixelPerfect = true;

			ui_ = new GameObject("OverlayRootSupportUI");
			ui_.transform.SetParent(panel_.transform, false);
			rt_ = ui_.AddComponent<RectTransform>();

			var bg = ui_.AddComponent<Image>();
			bg.color = new Color(0, 0, 0, 0.8f);
			bg.raycastTarget = true;

			SuperController.singleton.AddCanvas(canvas_);
			SetRect();

			return true;
		}

		private void SetRect()
		{
			rt_.anchorMin = new Vector2(1, 1);
			rt_.anchorMax = new Vector2(1, 1);

			bool withOffset = false;
			if (perf_ != null)
			{
				monitorWasVisible_ = perf_.on;
				withOffset = monitorWasVisible_;
			}

			SetPosition(withOffset);

			var bounds = Rectangle.FromPoints(
				0, 0, rt_.rect.width, rt_.rect.height);

			var topOffset = rt_.offsetMin.y - rt_.offsetMax.y;

			SetBounds(bounds, topOffset);
		}

		private bool ShowUI
		{
			get
			{
				var go = SuperController.singleton?.mainHUD?.gameObject;

				if (go == null)
					return true;
				else
					return go.activeSelf;
			}
		}

		public override void Update(float s)
		{
			if (ShowUI != panel_.activeSelf)
				SetActive(ShowUI);

			scaler_.scaleFactor = SuperController.singleton.monitorUIScale / 2;

			if (perf_ != null)
			{
				if (monitorWasVisible_ != perf_.on)
				{
					monitorWasVisible_ = perf_.on;
					SetPosition(monitorWasVisible_);
				}
			}
		}

		private void SetPosition(bool withOffset)
		{
			if (withOffset)
			{
				rt_.offsetMin = new Vector2(-size_.x - MonitorOffset, -(size_.y + topOffset_));
				rt_.offsetMax = new Vector2(-MonitorOffset, -topOffset_);
			}
			else
			{
				rt_.offsetMin = new Vector2(-size_.x, -(size_.y + topOffset_));
				rt_.offsetMax = new Vector2(0, -topOffset_);
			}
		}

		public override void Destroy()
		{
			if (canvas_ != null)
				SuperController.singleton.RemoveCanvas(canvas_);

			if (panel_ != null)
			{
				UnityEngine.Object.Destroy(panel_);
				panel_ = null;
			}
		}

		public override void SetActive(bool b)
		{
			if (panel_ != null)
				panel_.SetActive(b && ShowUI);
		}

		public override void SetSize(Vector2 v)
		{
			size_ = v;
			SetRect();
		}

		protected override Canvas GetCanvas()
		{
			return canvas_;
		}
	}
}
