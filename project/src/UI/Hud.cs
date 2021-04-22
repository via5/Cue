using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Cue
{
	interface IHud
	{
		void Create(bool vr);
		void Destroy();
		void Update();
		bool IsHovered(float x, float y);
	}


	interface IMenu
	{
		void Create(bool vr);
		void Destroy();
		void Update();
		bool IsHovered(float x, float y);
		void Toggle();
	}


	class MockHud : IHud
	{
		public void Create(bool vr)
		{
		}

		public void Destroy()
		{
		}

		public void Update()
		{
		}

		public bool IsHovered(float x, float y)
		{
			return false;
		}
	}


	interface IHudCanvas
	{
		void Create(Transform parent);
		void Destroy();
		bool IsHovered(float x, float y);
		Transform Transform { get; }
		void Toggle();
	}


	// vr, moves with the head
	//
	class WorldSpaceCanvas : IHudCanvas
	{
		private Vector3 offset_;
		private GameObject fullscreenPanel_ = null;
		private GameObject hudPanel_ = null;

		public WorldSpaceCanvas(Vector3 offset)
		{
			offset_ = offset;
		}

		public void Create(Transform parent)
		{
			CreateFullscreenPanel(parent);
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

		public Transform Transform
		{
			get { return hudPanel_.transform; }
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
			fullscreenPanel_.transform.position =
				parent.position + Vector3.ToUnity(offset_);


			var bg = fullscreenPanel_.AddComponent<Image>();
			bg.color = new Color(1, 0, 0, 0.2f);
			bg.raycastTarget = false;

			var rc = fullscreenPanel_.AddComponent<GraphicRaycaster>();
			rc.ignoreReversedGraphics = false;

			rt.offsetMin = new Vector2(0, 0);
			rt.offsetMax = new Vector2(1300, 100);
			rt.anchoredPosition = new Vector2(0, 0);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.pivot = new Vector2(0.5f, 0);
			rt.localScale = new UnityEngine.Vector3(0.0005f, 0.0005f, 0.0005f);
			rt.localPosition = new UnityEngine.Vector3(0, 0.1f, 0);

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

			rt.offsetMin = new Vector2(0.1f, 0.1f);
			rt.offsetMax = new Vector2(0.0f, 0.0f);
			rt.anchorMin = new Vector2(0, 0);
			rt.anchorMax = new Vector2(1, 1);
			rt.anchoredPosition = new Vector2(0, 0);
		}
	}


	// desktop
	//
	class OverlayCanvas : IHudCanvas
	{
		private GameObject fullscreenPanel_ = null;
		private GameObject hudPanel_ = null;

		public void Create(Transform parent)
		{
			CreateFullscreenPanel(parent);
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

		public Transform Transform
		{
			get { return hudPanel_.transform; }
		}

		public void Toggle()
		{
			fullscreenPanel_.SetActive(!fullscreenPanel_.activeSelf);
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

			rt.offsetMin = new Vector2(-500, 0);
			rt.offsetMax = new Vector2(500, 200);
			rt.anchorMin = new Vector2(0.5f, 1);
			rt.anchorMax = new Vector2(0.5f, 1);
			rt.anchoredPosition = new Vector2(
				(rt.offsetMax.x / 2),//- rt.offsetMin.x) / 4,
				-(rt.offsetMax.y - rt.offsetMin.y) / 2);
		}
	}


	class Menu : IMenu
	{
		private IHudCanvas canvas_ = null;
		private VUI.Root root_ = null;

		public void Create(bool vr)
		{
			if (vr)
			{
				canvas_ = new WorldSpaceCanvas(
					Vector3.FromUnity(
						new UnityEngine.Vector3(0, 0, 0)));

				canvas_.Create(SuperController.singleton.leftHand);
			}
			else
			{
				canvas_ = new OverlayCanvas();
				canvas_.Create(SuperController.singleton.mainMenuUI.root);
			}

			root_ = new VUI.Root(canvas_.Transform);
			root_.ContentPanel.Layout = new VUI.BorderLayout();

			var bottom = new VUI.Panel(new VUI.VerticalFlow());

			var p = new VUI.Panel(new VUI.HorizontalFlow());
			p.Add(new VUI.Button("Call", OnCall));
			p.Add(new VUI.Button("Sit", OnSit));
			p.Add(new VUI.Button("Kneel", OnKneel));
			p.Add(new VUI.Button("Reload", OnReload));
			p.Add(new VUI.Button("Handjob", OnHandjob));
			p.Add(new VUI.Button("Sex", OnSex));
			p.Add(new VUI.Button("Stand", OnStand));
			bottom.Add(p);

			p = new VUI.Panel(new VUI.HorizontalFlow());
			p.Add(new VUI.Button("Toggle genitals", OnToggleGenitals));
			p.Add(new VUI.Button("Toggle breasts", OnToggleBreasts));
			p.Add(new VUI.Button("Dump clothes", OnDumpClothes));
			bottom.Add(p);

			root_.ContentPanel.Add(bottom, VUI.BorderLayout.Bottom);
		}

		public bool IsHovered(float x, float y)
		{
			if (canvas_ == null)
				return false;

			return canvas_.IsHovered(x, y);
		}

		public void Destroy()
		{
			if (root_ != null)
			{
				root_.Destroy();
				root_ = null;
			}

			if (canvas_ != null)
			{
				canvas_.Destroy();
				canvas_ = null;
			}
		}

		public void Update()
		{
			root_.Update();
		}

		public void Toggle()
		{
			canvas_.Toggle();
		}

		private void OnCall()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);

				p.MakeIdle();
				p.AI.RunEvent(new CallEvent(p, Cue.Instance.Player));
			}
		}

		private void OnSit()
		{
			// sit on player
			//Cue.Instance.Persons[0].AI.Enabled = false;
			//Cue.Instance.Persons[0].MakeIdle();
			//Cue.Instance.Persons[0].Animator.Play(
			//	Resources.Animations.GetAny(
			//		Resources.Animations.SitOnSitting,
			//		Cue.Instance.Persons[0].Sex));

			if (Cue.Instance.Selected is Person)
			{
				var p = (Person)Cue.Instance.Selected;
				p.MakeIdle();
				p.Sit();
			}
		}

		private void OnKneel()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = (Person)Cue.Instance.Selected;
				p.MakeIdle();
				p.Kneel();
			}
		}

		private void OnReload()
		{
			Cue.Instance.ReloadPlugin();
		}

		private void OnHandjob()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);

				p.MakeIdle();
				p.AI.RunEvent(new HandjobEvent(p, Cue.Instance.Player));
			}
		}

		private void OnSex()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);

				p.MakeIdle();
				p.AI.RunEvent(new SexEvent(p, Cue.Instance.Player));
			}
		}

		private void OnStand()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);

				p.MakeIdle();
				p.AI.RunEvent(new StandAndThinkEvent(p));
			}
		}

		private void OnToggleGenitals()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);
				p.Clothing.GenitalsVisible = !p.Clothing.GenitalsVisible;
			}
		}

		private void OnToggleBreasts()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);
				p.Clothing.BreastsVisible = !p.Clothing.BreastsVisible;
			}
		}

		private void OnDumpClothes()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);
				p.Clothing.Dump();
			}
		}
	}


	class Hud : IHud
	{
		private IHudCanvas canvas_ = null;
		private VUI.Root root_ = null;
		private VUI.Label sel_ = null;
		private VUI.Label hovered_ = null;

		public void Create(bool vr)
		{
			Cue.Instance.SelectionChanged += OnSelectionChanged;
			Cue.Instance.HoveredChanged += OnHoveredChanged;

			if (vr)
			{
				canvas_ = new WorldSpaceCanvas(new Vector3(0, 0, 1));
				canvas_.Create(Camera.main.transform);
			}
			else
			{
				canvas_ = new OverlayCanvas();
				canvas_.Create(SuperController.singleton.mainMenuUI.root);
			}

			root_ = new VUI.Root(canvas_.Transform);
			root_.ContentPanel.Layout = new VUI.BorderLayout();

			var p = new VUI.Panel(new VUI.VerticalFlow());
			sel_ = p.Add(new VUI.Label());
			hovered_ = p.Add(new VUI.Label());
			root_.ContentPanel.Add(p, VUI.BorderLayout.Center);
		}

		public bool IsHovered(float x, float y)
		{
			if (canvas_ == null)
				return false;

			return canvas_.IsHovered(x, y);
		}

		public void Destroy()
		{
			if (root_ != null)
			{
				root_.Destroy();
				root_ = null;
			}

			if (canvas_ != null)
			{
				canvas_.Destroy();
				canvas_ = null;
			}
		}

		public void Update()
		{
			root_.Update();
		}

		private void OnSelectionChanged(IObject o)
		{
			sel_.Text = "Sel: " + (o == null ? "" : o.ToString());
		}

		private void OnHoveredChanged(IObject o)
		{
			hovered_.Text = "Hovered: " + (o == null ? "" : o.ToString());
		}
	}
}
