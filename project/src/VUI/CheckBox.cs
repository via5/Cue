using System;
using System.Collections.Generic;
using UnityEngine;

namespace VUI
{
	public class CheckBox : Widget
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
			Events.PointerUp += OnPointerUp;
			Events.PointerClick += OnPointerClick;
		}

		public string Text
		{
			get
			{
				return text_;
			}

			set
			{
				if (text_ != value)
				{
					text_ = value;

					if (toggle_ != null)
						toggle_.labelText.text = text_;
				}
			}
		}

		public bool Checked
		{
			get { return checked_; }
			set { SetChecked(value); }
		}

		public void Toggle()
		{
			Checked = !Checked;
		}

		protected virtual void SetChecked(bool b)
		{
			if (checked_ != b)
			{
				checked_ = b;

				if (toggle_ != null)
					toggle_.toggle.isOn = b;
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
			var s = FitText(text_, new Size(maxWidth, maxHeight));

			s.Width += Style.Metrics.ToggleLabelSpacing + 40;

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

		private void OnPointerUp(PointerEvent e)
		{
			e.Bubble = false;
		}

		private void OnPointerClick(PointerEvent e)
		{
			e.Bubble = false;
		}

		private void OnValueChanged(bool b)
		{
			try
			{
				SetChecked(b);
				Changed?.Invoke(b);
			}
			catch (Exception e)
			{
				Log.ErrorST(e.ToString());
			}
		}
	}


	public class RadioButton : CheckBox
	{
		public class Group
		{
			private List<RadioButton> list_ = new List<RadioButton>();

			public Group(string name)
			{
			}

			public void Add(RadioButton b)
			{
				list_.Add(b);
			}

			public void Remove(RadioButton b)
			{
				list_.Remove(b);
			}

			public void Check(RadioButton b)
			{
				for (int i = 0; i < list_.Count; ++i)
				{
					var o = list_[i];
					if (o != b)
						o.UncheckInternal();
				}
			}
		}

		private bool ignore_ = false;
		private Group group_ = null;

		public RadioButton(string text, ChangedCallback changed, bool initial = false, Group g = null)
			: base(text, changed, initial)
		{
			group_ = g;
			group_?.Add(this);
		}

		protected override void Destroy()
		{
			base.Destroy();
			group_?.Remove(this);
		}

		public void UncheckInternal()
		{
			try
			{
				ignore_ = true;
				Checked = false;
			}
			finally
			{
				ignore_ = false;
			}
		}

		protected override void SetChecked(bool b)
		{
			base.SetChecked(b);

			if (ignore_) return;

			if (b)
				group_?.Check(this);
		}
	}
}
