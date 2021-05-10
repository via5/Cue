using System;
using UnityEngine;

namespace VUI
{
	abstract class BasicSlider<T> : Widget
	{
		public override string TypeName { get { return "Slider"; } }

		public delegate void ValueCallback(T f);
		public event ValueCallback ValueChanged;

		protected UIDynamicSlider slider_ = null;

		// used before creation
		private T value_ = default(T);
		private T min_ = default(T);
		private T max_ = default(T);

		public BasicSlider(ValueCallback changed = null)
		{
			Borders = new Insets(2);

			if (changed != null)
				ValueChanged += changed;
		}

		public T Value
		{
			get
			{
				if (slider_ == null)
					return value_;
				else
					return GetValue();
			}

			set
			{
				if (slider_ == null)
				{
					value_ = value;
				}
				else
				{
					SetValue(value);
					ValueChanged?.Invoke(value);
				}
			}
		}

		public T Minimum
		{
			get
			{
				if (slider_ == null)
					return min_;
				else
					return GetMinimum();
			}

			set
			{
				if (slider_ == null)
					min_ = value;
				else
					SetMinimum(value);
			}
		}

		public T Maximum
		{
			get
			{
				if (slider_ == null)
					return max_;
				else
					return GetMaximum();
			}

			set
			{
				if (slider_ == null)
					max_ = value;
				else
					SetMaximum(value);
			}
		}

		public void Set(T value, T min, T max)
		{
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

			Set(value_, min_, max_);

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
			ValueChanged?.Invoke(GetValue());
		}

		protected abstract T GetValue();
		protected abstract void SetValue(T v);

		protected abstract T GetMinimum();
		protected abstract void SetMinimum(T v);

		protected abstract T GetMaximum();
		protected abstract void SetMaximum(T v);
	}


	class Slider : BasicSlider<float>
	{
		private bool wholeNumbers_ = false;

		public Slider(ValueCallback changed = null)
			: base(changed)
		{
		}

		public bool WholeNumbers
		{
			get
			{
				return wholeNumbers_;
			}

			set
			{
				wholeNumbers_ = value;
				if (slider_?.slider != null)
					slider_.slider.wholeNumbers = value;
			}
		}

		protected override void DoCreate()
		{
			base.DoCreate();
			slider_.slider.wholeNumbers = wholeNumbers_;
		}

		protected override float GetValue()
		{
			return slider_.slider.value;
		}

		protected override void SetValue(float v)
		{
			slider_.slider.value = v;
		}

		protected override float GetMinimum()
		{
			return slider_.slider.minValue;
		}

		protected override void SetMinimum(float v)
		{
			slider_.slider.minValue = v;
		}

		protected override float GetMaximum()
		{
			return slider_.slider.maxValue;
		}

		protected override void SetMaximum(float v)
		{
			slider_.slider.maxValue = v;
		}
	}


	class FloatSlider : BasicSlider<float>
	{
		public FloatSlider(ValueCallback changed = null)
			: base(changed)
		{
		}

		protected override float GetValue()
		{
			return slider_.slider.value;
		}

		protected override void SetValue(float v)
		{
			slider_.slider.value = v;
		}

		protected override float GetMinimum()
		{
			return slider_.slider.minValue;
		}

		protected override void SetMinimum(float v)
		{
			slider_.slider.minValue = v;
		}

		protected override float GetMaximum()
		{
			return slider_.slider.maxValue;
		}

		protected override void SetMaximum(float v)
		{
			slider_.slider.maxValue = v;
		}
	}


	class IntSlider : BasicSlider<int>
	{
		public IntSlider(ValueCallback changed = null)
			: base(changed)
		{
		}

		protected override int GetValue()
		{
			return (int)slider_.slider.value;
		}

		protected override void SetValue(int v)
		{
			slider_.slider.value = v;
		}

		protected override int GetMinimum()
		{
			return (int)slider_.slider.minValue;
		}

		protected override void SetMinimum(int v)
		{
			slider_.slider.minValue = v;
		}

		protected override int GetMaximum()
		{
			return (int)slider_.slider.maxValue;
		}

		protected override void SetMaximum(int v)
		{
			slider_.slider.maxValue = v;
		}

		protected override void DoCreate()
		{
			base.DoCreate();
			slider_.slider.wholeNumbers = true;
		}
	}


	abstract class BasicTextSlider<T> : Panel
	{
		public override string TypeName { get { return "TextSlider"; } }

		public delegate void ValueCallback(T f);
		public event ValueCallback ValueChanged;

		protected readonly BasicSlider<T> slider_;
		protected readonly TextBox text_ = new TextBox();

		private bool changingText_ = false;


		protected BasicTextSlider(
			BasicSlider<T> s, T value, T min, T max,
			ValueCallback valueChanged = null)
		{
			slider_ = s;

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

		public T Value
		{
			get { return slider_.Value; }
			set { slider_.Value = value; }
		}

		public T Minimum
		{
			get { return slider_.Minimum; }
			set { slider_.Minimum = value; }
		}

		public T Maximum
		{
			get { return slider_.Maximum; }
			set { slider_.Maximum = value; }
		}

		public void Set(T value, T min, T max)
		{
			slider_.Set(value, min, max);
			UpdateText();
		}

		private void OnTextChanged(string s)
		{
			if (changingText_)
				return;

			T f = FromString(s);

			using (new ScopedFlag((b) => changingText_ = b))
			{
				slider_.Value = f;
			}
		}

		private void OnValueChanged(T f)
		{
			UpdateText();
			ValueChanged?.Invoke(f);
		}

		private void UpdateText()
		{
			text_.Text = ToString(slider_.Value);
		}

		protected abstract T FromString(string s);
		protected abstract string ToString(T v);
	}


	class TextSlider : BasicTextSlider<float>
	{
		public TextSlider(ValueCallback valueChanged = null)
			: this(0, 0, 1, valueChanged)
		{
		}

		public TextSlider(float value, float min, float max, ValueCallback valueChanged = null)
			: base(new Slider(), value, min, max, valueChanged)
		{
		}

		public bool WholeNumbers
		{
			get { return ((Slider)slider_).WholeNumbers; }
			set { ((Slider)slider_).WholeNumbers = value; }
		}

		protected override float FromString(string s)
		{
			float f;
			if (float.TryParse(s, out f))
				return Utilities.Clamp(f, Minimum, Maximum);

			return 0;
		}

		protected override string ToString(float v)
		{
			if (WholeNumbers)
				return ((int)Math.Round(v)).ToString();
			else
				return v.ToString("0.00");
		}
	}


	class FloatTextSlider : BasicTextSlider<float>
	{
		public FloatTextSlider(ValueCallback valueChanged = null)
			: this(0, 0, 1, valueChanged)
		{
		}

		public FloatTextSlider(float value, float min, float max, ValueCallback valueChanged = null)
			: base(new FloatSlider(), value, min, max, valueChanged)
		{
		}

		protected override float FromString(string s)
		{
			float f;
			if (float.TryParse(s, out f))
				return Utilities.Clamp(f, Minimum, Maximum);

			return 0;
		}

		protected override string ToString(float v)
		{
			return v.ToString("0.00");
		}
	}


	class IntTextSlider : BasicTextSlider<int>
	{
		public IntTextSlider(ValueCallback valueChanged = null)
			: this(0, 0, 1, valueChanged)
		{
		}

		public IntTextSlider(int value, int min, int max, ValueCallback valueChanged = null)
			: base(new IntSlider(), value, min, max, valueChanged)
		{
		}

		protected override int FromString(string s)
		{
			int f;
			if (int.TryParse(s, out f))
				return Utilities.Clamp(f, Minimum, Maximum);

			return 0;
		}

		protected override string ToString(int v)
		{
			return v.ToString();
		}
	}
}
