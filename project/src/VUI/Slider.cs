using System;
using System.Text;
using UnityEngine;

namespace VUI
{
	abstract class BasicSlider<T> : Widget
	{
		public override string TypeName { get { return "Slider"; } }

		public delegate void ValueCallback(T f);
		public event ValueCallback ValueChanged;

		protected UIDynamicSlider slider_ = null;

		private T value_ = default(T);
		private T min_ = default(T);
		private T max_ = default(T);
		private T tickValue_ = default(T);
		private T pageValue_ = default(T);
		private bool hor_ = true;
		private bool ignore_ = false;

		public BasicSlider(T tickValue, T pageValue, ValueCallback changed = null)
		{
			Borders = new Insets(2);
			tickValue_ = tickValue;
			pageValue_ = pageValue;

			if (changed != null)
				ValueChanged += changed;

			Events.Wheel += HandleWheelInternal;
			Events.PointerDown += OnPointerDown;
		}

		public bool Horizontal
		{
			get
			{
				return hor_;
			}

			set
			{
				if (value != hor_)
				{
					hor_ = value;
					SetDirection();
				}
			}
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
				value_ = value;

				if (slider_ != null)
				{
					if (SetValue(value))
					{
						if (!ignore_)
							ValueChanged?.Invoke(value);
					}
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
				min_ = value;

				if (slider_ != null)
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
				max_ = value;

				if (slider_ != null)
					SetMaximum(value);
			}
		}

		public T TickValue
		{
			get { return tickValue_; }
			set { tickValue_ = value; }
		}

		public T PageValue
		{
			get { return pageValue_; }
			set { pageValue_ = value; }
		}

		public bool Set(T value, T min, T max)
		{
			return DoSet(value, min, max);
		}

		public void SetRange(T min, T max)
		{
			Minimum = min;
			Maximum = max;
		}

		public void Tick(int d)
		{
			DoTick(d);
		}

		public void Page(int d)
		{
			DoPage(d);
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
			SetDirection();

			// the MouseCallbacks on slider_ added by Widget.Create() don't
			// seem to work at all, it needs to be added on the sub slider
			// object
			slider_.slider.gameObject.AddComponent<MouseCallbacks>().Widget = this;

			ignore_ = true;
			Set(value_, min_, max_);
			ignore_ = false;

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

		private void SetDirection()
		{
			if (slider_?.slider != null)
			{
				var d = hor_ ?
					UnityEngine.UI.Slider.Direction.LeftToRight :
					UnityEngine.UI.Slider.Direction.BottomToTop;

				slider_.slider.SetDirection(d, false);
			}
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return Style.Metrics.SliderMinimumSize;
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			Style.Polish(this);
		}

		protected override void DoSetRender(bool b)
		{
			slider_.slider.gameObject.SetActive(b);
		}

		private void OnChanged(float v)
		{
			value_ = GetValue();
			ValueChanged?.Invoke(value_);
		}

		public void HandleWheelInternal(WheelEvent e)
		{
			DoTick(Math.Sign(e.Delta.Y));
			e.Bubble = false;
		}

		private void OnPointerDown(PointerEvent e)
		{
			e.Bubble = false;
		}

		protected abstract T GetValue();
		protected abstract bool SetValue(T v);

		protected abstract T GetMinimum();
		protected abstract void SetMinimum(T v);

		protected abstract T GetMaximum();
		protected abstract void SetMaximum(T v);

		protected abstract bool DoSet(T value, T min, T max);
		protected abstract void DoTick(int d);
		protected abstract void DoPage(int d);
	}


	class FloatSlider : BasicSlider<float>
	{
		private bool wholeNumbers_ = false;

		public FloatSlider(ValueCallback changed = null)
			: base(0.1f, 1, changed)
		{
		}

		public FloatSlider(float min, float max, ValueCallback changed = null)
			: this(min, min ,max, changed)
		{
		}

		public FloatSlider(float value, float min, float max, ValueCallback changed = null)
			: this(changed)
		{
			Set(value, min, max);
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

		protected override bool SetValue(float v)
		{
			if (slider_.slider.value != v)
			{
				slider_.slider.value = v;
				return true;
			}

			return false;
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

		protected override bool DoSet(float value, float min, float max)
		{
			if (min != Minimum || max != Maximum || value != Value || value < min || value > max)
			{
				Minimum = min;
				Maximum = max;
				Value = value;
				return true;
			}

			return false;
		}

		protected override void DoTick(int d)
		{
			SetValue(GetValue() + TickValue * d);
		}

		protected override void DoPage(int d)
		{
			SetValue(GetValue() + PageValue * d);
		}
	}


	class IntSlider : BasicSlider<int>
	{
		public IntSlider(ValueCallback changed = null)
			: base(1, 10, changed)
		{
		}

		public IntSlider(int min, int max, ValueCallback changed = null)
			: this(min, min, max, changed)
		{
		}

		public IntSlider(int value, int min, int max, ValueCallback changed = null)
			: this(changed)
		{
			Set(value, min, max);
		}

		protected override int GetValue()
		{
			return (int)slider_.slider.value;
		}

		protected override bool SetValue(int v)
		{
			if (slider_.slider.value != v)
			{
				slider_.slider.value = v;
				return true;
			}

			return false;
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

		protected override bool DoSet(int value, int min, int max)
		{
			if (min != Minimum || max != Maximum || value != Value || value < min || value > max)
			{
				Minimum = min;
				Maximum = max;
				Value = value;
				return true;
			}

			return false;
		}

		protected override void DoTick(int d)
		{
			SetValue(GetValue() + TickValue * d);
		}

		protected override void DoPage(int d)
		{
			SetValue(GetValue() + PageValue * d);
		}
	}


	class SliderTextBox<T> : TextBox
	{
		private readonly BasicTextSlider<T> slider_;

		public SliderTextBox(BasicTextSlider<T> s)
		{
			slider_ = s;
			Events.Wheel += (e) => slider_.Slider.HandleWheelInternal(e);
		}
	}


	abstract class BasicTextSlider<T> : Panel
	{
		public override string TypeName { get { return "TextSlider"; } }

		public delegate void ValueCallback(T f);
		public event ValueCallback ValueChanged;

		protected readonly BasicSlider<T> slider_;
		protected readonly SliderTextBox<T> text_;

		private bool changingText_ = false;
		private readonly StringBuilder sb_ = new StringBuilder();


		protected BasicTextSlider(
			BasicSlider<T> s, T value, T min, T max,
			ValueCallback valueChanged = null)
		{
			slider_ = s;
			text_ = new SliderTextBox<T>(this);
			text_.MaximumSize = new Size(100, DontCare);

			Layout = new BorderLayout(5);
			Add(slider_, BorderLayout.Center);
			Add(text_, BorderLayout.Right);

			UpdateText();

			Set(value, min, max);

			text_.Edited += OnTextChanged;
			slider_.ValueChanged += OnValueChanged;
			slider_.Created += UpdateText;

			if (valueChanged != null)
				ValueChanged += valueChanged;
		}

		public BasicSlider<T> Slider
		{
			get { return slider_; }
		}

		public SliderTextBox<T> TextBox
		{
			get { return text_; }
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

		public T TickValue
		{
			get { return slider_.TickValue ; }
			set { slider_.TickValue = value; }
		}

		public T PageValue
		{
			get { return slider_.PageValue; }
			set { slider_.PageValue = value; }
		}


		public void Set(T value, T min, T max)
		{
			if (slider_.Set(value, min, max))
				UpdateText();
		}

		public void SetRange(T min, T max)
		{
			Minimum = min;
			Maximum = max;
		}

		private void OnTextChanged(string s)
		{
			if (changingText_)
				return;

			T f = FromString(s);

			try
			{
				changingText_ = true;
				slider_.Value = f;
			}
			finally
			{
				changingText_ = false;
			}
		}

		private void OnValueChanged(T f)
		{
			UpdateText();
			ValueChanged?.Invoke(f);
		}

		private void UpdateText()
		{
			sb_.Length = 0;
			ToString(slider_.Value, sb_);
			text_.Text = sb_.ToString();
		}

		protected abstract T FromString(string s);
		protected abstract void ToString(T v, StringBuilder sb);
	}


	class FloatTextSlider : BasicTextSlider<float>
	{
		private uint decimals_ = 2;

		public FloatTextSlider(ValueCallback valueChanged = null)
			: this(0, 0, 1, valueChanged)
		{
		}

		public FloatTextSlider(float min, float max, ValueCallback valueChanged = null)
			: this(min, min, max, valueChanged)
		{
		}

		public FloatTextSlider(float value, float min, float max, ValueCallback valueChanged = null)
			: base(new FloatSlider(), value, min, max, valueChanged)
		{
		}

		public bool WholeNumbers
		{
			get { return ((FloatSlider)slider_).WholeNumbers; }
			set { ((FloatSlider)slider_).WholeNumbers = value; }
		}

		public int Decimals
		{
			get { return (int)decimals_; }
			set { decimals_ = (uint)value; }
		}

		protected override float FromString(string s)
		{
			float f;
			if (float.TryParse(s, out f))
				return Utilities.Clamp(f, Minimum, Maximum);

			return 0;
		}

		protected override void ToString(float v, StringBuilder sb)
		{
			if (WholeNumbers)
				sb.Append((int)Math.Round(v));
			else
				sb.AppendFloat(v, (int)decimals_);
		}
	}


	class IntTextSlider : BasicTextSlider<int>
	{
		private Func<int, string> toString_ = null;

		public IntTextSlider(ValueCallback valueChanged = null)
			: this(0, 0, 1, valueChanged)
		{
		}

		public IntTextSlider(int min, int max, ValueCallback valueChanged = null)
			: this(min, min, max, valueChanged)
		{
		}

		public IntTextSlider(int value, int min, int max, ValueCallback valueChanged = null)
			: base(new IntSlider(), value, min, max, valueChanged)
		{
		}

		public Func<int, string> ToStringCallback
		{
			get { return toString_; }
			set { toString_ = value; }
		}

		protected override int FromString(string s)
		{
			int f;
			if (int.TryParse(s, out f))
				return Utilities.Clamp(f, Minimum, Maximum);

			return 0;
		}

		protected override void ToString(int v, StringBuilder sb)
		{
			if (toString_ == null)
				sb.Append(v);
			else
				sb.Append(toString_(v));
		}
	}
}
