using UnityEngine;

namespace VUI
{
	class CheckBox : Widget
	{
		public override string TypeName { get { return "CheckBox"; } }

		public delegate void ChangedCallback(bool b);
		public event ChangedCallback Changed;

		private string text_ = "";
		private bool checked_ = false;
		private UIDynamicToggle toggle_ = null;

		public CheckBox(
			string t = "", ChangedCallback changed = null, bool initial = false)
		{
			text_ = t;
			checked_ = initial;

			if (changed != null)
				Changed += changed;
		}

		public bool Checked
		{
			get
			{
				return checked_;
			}

			set
			{
				if (checked_ != value)
				{
					checked_ = value;

					if (toggle_ != null)
						toggle_.toggle.isOn = value;
				}
			}
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Glue.PluginManager.configurableTogglePrefab).gameObject;
		}

		protected override void DoCreate()
		{
			toggle_ = WidgetObject.GetComponent<UIDynamicToggle>();
			toggle_.labelText.text = text_;
			toggle_.toggle.isOn = checked_;
			toggle_.toggle.onValueChanged.AddListener(OnClicked);

			toggle_.toggle.graphic.rectTransform.localScale = new Vector3(
				0.75f, 0.75f, 0.75f);

			var rt = toggle_.toggle.image.rectTransform;
			rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y - 8);
			rt.offsetMax = new Vector2(rt.offsetMax.x - 20, rt.offsetMax.y - 28);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.anchoredPosition = new Vector2(
				rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
				rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);

			rt = toggle_.labelText.rectTransform;
			rt.offsetMin = new Vector2(rt.offsetMin.x - 15, rt.offsetMin.y);
			rt.offsetMax = new Vector2(rt.offsetMax.x - 15, rt.offsetMax.y);
			rt.anchoredPosition = new Vector2(
				rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
				rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);

			Style.Setup(this);
		}

		protected override void DoSetEnabled(bool b)
		{
			toggle_.toggle.interactable = b;
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return DoGetMinimumSize();
		}

		protected override Size DoGetMinimumSize()
		{
			var w = Root.TextLength(Font, FontSize, text_);
			return new Size(w + 20 + 40, 40);
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			Style.Polish(this);
		}

		private void OnClicked(bool b)
		{
			Utilities.Handler(() =>
			{
				GetRoot().SetFocus(this);
				checked_ = b;
				Changed?.Invoke(b);
			});
		}
	}
}
