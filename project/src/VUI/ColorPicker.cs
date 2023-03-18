using System;
using UnityEngine;

namespace VUI
{
	class ColorPicker : Widget
	{
		public override string TypeName { get { return "ColorPicker"; } }

		public delegate void ColorCallback(Color c);
		public event ColorCallback Changed;

		private string text_ = null;
		private Color color_ = Color.white;
		private UIDynamicColorPicker picker_ = null;
		private bool ignore_ = false;

		public ColorPicker(string text = null, ColorCallback callback = null)
		{
			text_ = text;

			if (callback != null)
				Changed += callback;

			Events.PointerDown += OnPointerDown;
		}

		public Color Color
		{
			get
			{
				return color_;
			}

			set
			{
				if (color_ != value)
				{
					color_ = value;
					if (picker_ != null)
						SetColor();
				}
			}
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Glue.PluginManager.configurableColorPickerPrefab).gameObject;
		}

		protected override void DoCreate()
		{
			try
			{
				ignore_ = true;

				picker_ = WidgetObject.GetComponent<UIDynamicColorPicker>();
				picker_.colorPicker.onColorChangedHandlers = OnChanged;
				SetColor();

				if (text_ != null)
					picker_.label = text_;

				Style.Setup(this);
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void SetColor()
		{
			picker_.colorPicker.SetHSV(
				HSVColorPicker.RGBToHSV(color_.r, color_.g, color_.b));
		}

		protected override void SetWidgetObjectBounds()
		{
			var b = new Rectangle(ClientBounds);

			// small buttons at the bottom are too low
			b.Height -= 11;

			Utilities.SetRectTransform(WidgetObjectRT, b);
		}

		protected override void AfterUpdateBounds()
		{
			try
			{
				ignore_ = true;

				// color has to be set again here because the bounds of the
				// saturation image have changed and the handle needs to be moved
				// back in bounds
				ForcePickerUpdate();
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void ForcePickerUpdate()
		{
			// force a change by setting the color to something that's
			// guaranteed to be different
			var different = HSVColorPicker.RGBToHSV(
				1.0f - color_.r, color_.g, color_.b);

			picker_.colorPicker.SetHSV(different);

			SetColor();
		}

		private UnityEngine.UI.Image oldImage_ = null;

		protected override void DoSetEnabled(bool b)
		{
			base.DoSetEnabled(b);

			foreach (var bn in picker_.GetComponentsInChildren<UnityEngine.UI.Button>())
			{
				bn.interactable = b;
			}

			foreach (var bn in picker_.GetComponentsInChildren<UnityEngine.UI.Slider>())
			{
				bn.interactable = b;
			}

			foreach (var bn in picker_.GetComponentsInChildren<UnityEngine.UI.InputField>())
			{
				bn.interactable = b;
			}


			if (b)
			{
				if (oldImage_ != null)
				{
					oldImage_.gameObject.SetActive(true);
					picker_.colorPicker.Selector = oldImage_;
				}

				try
				{
					ignore_ = true;
					ForcePickerUpdate();
				}
				finally
				{
					ignore_ = false;
				}
			}
			else
			{
				oldImage_ = picker_.colorPicker.Selector;

				if (oldImage_ != null)
					oldImage_.gameObject.SetActive(false);

				picker_.colorPicker.Selector = null;
			}
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return DoGetMinimumSize();
		}

		protected override Size DoGetMinimumSize()
		{
			return new Size(480, 320);
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			Style.Polish(this);
		}

		private void OnPointerDown(PointerEvent e)
		{
			e.Bubble = false;
		}

		private void OnChanged(Color color)
		{
			// clamp the saturation image, it can have a stray line at the
			// bottom; this needs to be done every time the color changes
			// because the texture is recreated
			picker_.colorPicker.saturationImage.sprite.texture.wrapMode =
				TextureWrapMode.Clamp;

			if (ignore_) return;

			try
			{
				Changed?.Invoke(color);
			}
			catch (Exception e)
			{
				Log.ErrorST(e.ToString());
			}
		}
	}
}
