using UnityEngine;

namespace VUI
{
	class Slider : Widget
	{
		public override string TypeName { get { return "Slider"; } }

		public delegate void ValueCallback(float f);
		public event ValueCallback ValueChanged;

		private UIDynamicSlider slider_ = null;

		// used before creation
		private float value_ = 0;
		private float min_ = 0;
		private float max_ = 0;

		public Slider(ValueCallback changed = null)
		{
			Borders = new Insets(2);

			if (changed != null)
				ValueChanged += changed;
		}

		public float Value
		{
			get
			{
				if (slider_ == null)
					return value_;
				else
					return slider_.slider.value;
			}

			set
			{
				if (slider_ == null)
				{
					value_ = value;
				}
				else
				{
					slider_.slider.value = value;
					ValueChanged?.Invoke(value);
				}
			}
		}

		public float Minimum
		{
			get
			{
				if (slider_ == null)
					return min_;
				else
					return slider_.slider.minValue;
			}

			set
			{
				if (slider_ == null)
					min_ = value;
				else
					slider_.slider.minValue = value;
			}
		}

		public float Maximum
		{
			get
			{
				if (slider_ == null)
					return max_;
				else
					return slider_.slider.maxValue;
			}

			set
			{
				if (slider_ == null)
					max_ = value;
				else
					slider_.slider.maxValue = value;
			}
		}

		public void Set(float value, float min, float max)
		{
			if (min > max)
			{
				var temp = min;
				min = max;
				max = temp;
			}

			value = Utilities.Clamp(value, min, max);

			Minimum = min;
			Maximum = max;
			Value = value;
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Glue.PluginManager.configurableSliderPrefab).gameObject;
		}

		protected override void DoCreate()
		{
			slider_ = WidgetObject.GetComponent<UIDynamicSlider>();
			slider_.quickButtonsEnabled = false;
			slider_.defaultButtonEnabled = false;
			slider_.rangeAdjustEnabled = false;

			slider_.slider.minValue = min_;
			slider_.slider.maxValue = max_;
			slider_.slider.value = value_;

			slider_.slider.onValueChanged.AddListener(OnChanged);

			slider_.labelText.gameObject.SetActive(false);
			slider_.sliderValueTextFromFloat.gameObject.SetActive(false);
			Utilities.FindChildRecursive(WidgetObject, "Panel").SetActive(false);

			var rt = slider_.slider.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(
				rt.offsetMin.x - 12, rt.offsetMin.y - 30);
			rt.offsetMax = new Vector2(
				rt.offsetMax.x + 12, rt.offsetMax.y - 30);

			Style.Setup(this);
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(100, 40);
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			Style.Polish(this);
		}

		private void OnChanged(float v)
		{
			ValueChanged?.Invoke(v);
		}
	}


	class TextSlider : Panel
	{
		public override string TypeName { get { return "TextSlider"; } }

		public delegate void ValueCallback(float f);
		public event ValueCallback ValueChanged;

		private readonly Slider slider_ = new Slider();
		private readonly TextBox text_ = new TextBox();

		private bool changingText_ = false;


		public TextSlider(ValueCallback valueChanged = null)
			: this(0, 0, 1, valueChanged)
		{
		}

		public TextSlider(
			float value, float min, float max,
			ValueCallback valueChanged = null)
		{
			Layout = new BorderLayout(5);
			Add(slider_, BorderLayout.Center);
			Add(text_, BorderLayout.Right);

			UpdateText();

			Set(value, min, max);

			text_.Edited += OnTextChanged;
			slider_.ValueChanged += OnValueChanged;

			if (valueChanged != null)
				ValueChanged += valueChanged;
		}

		public float Value
		{
			get { return slider_.Value; }
			set { slider_.Value = value; }
		}

		public float Minimum
		{
			get { return slider_.Minimum; }
			set { slider_.Minimum = value; }
		}

		public float Maximum
		{
			get { return slider_.Maximum; }
			set { slider_.Maximum = value; }
		}

		public void Set(float value, float min, float max)
		{
			slider_.Set(value, min, max);
			UpdateText();
		}

		private void OnTextChanged(string s)
		{
			if (changingText_)
				return;

			float f;
			if (float.TryParse(s, out f))
			{
				f = Utilities.Clamp(f, Minimum, Maximum);

				using (new ScopedFlag((b) => changingText_ = b))
				{
					slider_.Value = f;
				}
			}
		}

		private void OnValueChanged(float f)
		{
			UpdateText();
			ValueChanged?.Invoke(f);
		}

		private void UpdateText()
		{
			text_.Text = slider_.Value.ToString("0.00");
		}
	}
}
