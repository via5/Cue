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
		}

		public Color Color
		{
			get
			{
				return color_;
			}

			set
			{
				color_ = value;
				if (picker_ != null)
					SetColor();
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

			Utilities.SetRectTransform(WidgetObject, b);

			// make the sliders on the right a bit smaller
			int offset = 25;


			{
				// shrink the right column with the sliders
				var right = Utilities.FindChildRecursive(WidgetObject, "RightColumn");
				var rt = right.GetComponent<RectTransform>();
				rt.offsetMin = new Vector2(rt.offsetMin.x + offset, rt.offsetMin.y);
			}

			{
				// move the hue slider to the right
				var rt = picker_.colorPicker.hueImage.transform.parent.GetComponent<RectTransform>();
				rt.offsetMin = new Vector2(rt.offsetMin.x + offset, rt.offsetMin.y);
				rt.offsetMax = new Vector2(rt.offsetMax.x + offset, rt.offsetMax.y);
			}

			{
				// expand the saturation image to use the new space
				var rt = picker_.colorPicker.saturationImage.GetComponent<RectTransform>();
				rt.offsetMax = new Vector2(rt.offsetMax.x + offset, rt.offsetMax.y);
			}
		}

		public override void UpdateBounds()
		{
			base.UpdateBounds();

			try
			{
				ignore_ = true;

				// color has to be set again here because the bounds of the
				// saturation image have changed and the handle needs to be moved
				// back in bounds

				// force a change by setting the color to something that's
				// guaranteed to be different
				var different = HSVColorPicker.RGBToHSV(
					1.0f - color_.r, color_.g, color_.b);
				picker_.colorPicker.SetHSV(different);

				SetColor();
			}
			finally
			{
				ignore_ = false;
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

		private void OnChanged(Color color)
		{
			if (ignore_) return;

			try
			{
				Changed?.Invoke(color);
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
			}
		}
	}
}
