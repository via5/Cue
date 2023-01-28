using System;
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
			string t = "", ChangedCallback changed = null,
			bool initial = false, string tooltip = "")
		{
			text_ = t;
			checked_ = initial;

			if (tooltip != "")
				Tooltip.Text = tooltip;

			if (changed != null)
				Changed += changed;

			Events.PointerDown += OnPointerDown;
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

		public void Toggle()
		{
			Checked = !Checked;
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
			toggle_.toggle.onValueChanged.AddListener(OnValueChanged);

			Style.Setup(this);
		}

		protected override void DoSetEnabled(bool b)
		{
			toggle_.toggle.interactable = b;
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			var s = Root.FitText(
				Font, FontSize, text_, new Size(maxWidth, maxHeight));

			s.Width += Style.Metrics.ToggleLabelSpacing + 35;

			// todo: text doesn't appear without this, sounds like FitText() is
			// off by one?
			s.Height += 1;

			return s;
		}

		protected override Size DoGetMinimumSize()
		{
			return DoGetPreferredSize(DontCare, DontCare);
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			Style.Polish(this);
		}

		protected override void DoSetRender(bool b)
		{
			if (toggle_ != null)
				toggle_.gameObject.SetActive(b);
		}

		private void OnPointerDown(PointerEvent e)
		{
			e.Bubble = false;
		}

		private void OnValueChanged(bool b)
		{
			try
			{
				checked_ = b;
				Changed?.Invoke(b);
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
			}
		}
	}
}
