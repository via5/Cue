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
		private VUI.Label sel_ = null;
		private VUI.Label hovered_ = null;

		public void Create()
		{
			Create(SuperController.singleton.mainMenuUI.root);
		}

		public void Create(Transform parent)
		{
			Cue.Instance.SelectionChanged += OnSelectionChanged;
			Cue.Instance.HoveredChanged += OnHoveredChanged;

			CreateFullscreenPanel(parent);
			CreateHudPanel();

			root_ = new VUI.Root(hudPanel_.transform);
			root_.ContentPanel.Layout = new VUI.BorderLayout();

			var p = new VUI.Panel(new VUI.VerticalFlow());
			sel_ = p.Add(new VUI.Label());
			hovered_ = p.Add(new VUI.Label());
			root_.ContentPanel.Add(p, VUI.BorderLayout.Center);

			p = new VUI.Panel(new VUI.HorizontalFlow());
			p.Add(new VUI.Button("Call", OnCall));
			p.Add(new VUI.Button("Sit", OnSit));
			p.Add(new VUI.Button("Kneel", OnKneel));
			p.Add(new VUI.Button("Reload", OnReload));
			p.Add(new VUI.Button("Handjob", OnHandjob));
			p.Add(new VUI.Button("Stand", OnStand));

			root_.ContentPanel.Add(p, VUI.BorderLayout.Bottom);
		}

		public bool IsHovered(Vector2 p)
		{
			var rt = hudPanel_.GetComponent<RectTransform>();
			Vector2 local = rt.InverseTransformPoint(p);
			return rt.rect.Contains(local);
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
			sel_.Text = "Sel: " + (o == null ? "" : o.ToString());
		}

		private void OnHoveredChanged(IObject o)
		{
			hovered_.Text = "Hovered: " + (o == null ? "" : o.ToString());
		}

		private void OnCall()
		{
			if (Cue.Instance.Selected is Person)
				((Person)Cue.Instance.Selected).Call(Cue.Instance.Player);
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
				p.AI.RunEvent(new HandjobEvent(Cue.Instance.Player));
			}
		}

		private void OnStand()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);

				p.MakeIdle();
				p.Stand();
			}
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

			var c2 = SuperController.singleton.errorLogPanel.GetComponent<Canvas>();
			Cue.LogError(canvas.renderMode.ToString() + " " + c2.renderMode.ToString());
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
				(rt.offsetMax.x /2),//- rt.offsetMin.x) / 4,
				-(rt.offsetMax.y - rt.offsetMin.y) / 2);
		}
	}
}
