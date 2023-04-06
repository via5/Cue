using System;
using UnityEngine;
using UnityEngine.UI;

namespace VUI
{
	public class ImageObject
	{
		private static Material emptyMat_ = null;

		private readonly Widget parent_;
		private readonly GameObject o_;
		private readonly RectTransform rt_ = null;
		private readonly RawImage raw_ = null;
		private Texture tex_ = null;
		private Texture grey_ = null;
		private Size size_ = new Size(Widget.DontCare, Widget.DontCare);
		private int align_ = Image.AlignDefault;
		private bool enabled_ = true;

		public ImageObject(Widget parent, int align = Image.AlignDefault)
		{
			parent_ = parent;
			align_ = align;

			o_ = new GameObject();
			o_.transform.SetParent(parent.WidgetObject.transform, false);

			raw_ = o_.AddComponent<RawImage>();
			rt_ = o_.GetComponent<RectTransform>();

			rt_.anchorMin = new Vector2(0, 0);
			rt_.anchorMax = new Vector2(1, 1);
			rt_.offsetMin = new Vector2(0, 0);
			rt_.offsetMax = new Vector2(0, 0);

			if (emptyMat_ == null)
			{
				emptyMat_ = new Material(raw_.material);
				emptyMat_.mainTexture = Texture2D.blackTexture;
			}

			raw_.material = emptyMat_;
			UpdateTexture();
		}

		public GameObject GameObject
		{
			get { return o_; }
		}

		public Texture Texture
		{
			get
			{
				return tex_;
			}

			set
			{
				if (tex_ != value)
				{
					if (grey_ != null)
						grey_ = null;

					tex_ = value;
					UpdateTexture();
				}
			}
		}

		public Size Size
		{
			get { return size_; }
			set { size_ = value; UpdateAspect(); }
		}

		public int Alignment
		{
			get { return align_; }
			set { align_ = value; UpdateAspect(); }
		}

		public void SetRender(bool b)
		{
			if (raw_ != null)
				raw_.gameObject.SetActive(b);
		}

		public void SetEnabled(bool b)
		{
			enabled_ = b;
			UpdateTexture();
		}

		public static Size SGetPreferredSize(Texture t, float maxWidth, float maxHeight)
		{
			Size s;

			if (t == null)
			{
				s = new Size(
					Math.Max(maxWidth, maxHeight),
					Math.Max(maxWidth, maxHeight));
			}
			else
			{
				s = new Size(t.width, t.height);

				if (maxWidth != Widget.DontCare)
					s.Width = Math.Min(maxWidth, s.Width);

				if (maxHeight != Widget.DontCare)
					s.Height = Math.Min(maxHeight, s.Height);
			}

			s.Width = Math.Min(s.Width, s.Height);
			s.Height = Math.Min(s.Width, s.Height);

			return s;
		}

		public Size GetPreferredSize(float maxWidth, float maxHeight)
		{
			return SGetPreferredSize(tex_, maxWidth, maxHeight);
		}

		public void UpdateAspect()
		{
			Size scaled;

			var maxSize = parent_.ClientBounds.Size;
			if (size_.Width != Widget.DontCare)
				maxSize.Width = Math.Min(maxSize.Width, size_.Height);

			if (size_.Height != Widget.DontCare)
				maxSize.Height = Math.Min(maxSize.Height, size_.Height);

			if (tex_ == null)
			{
				scaled = maxSize;
			}
			else
			{
				maxSize.Width = Math.Min(maxSize.Width, tex_.width);
				maxSize.Height = Math.Min(maxSize.Height, tex_.height);

				scaled = Aspect(
					tex_.width, tex_.height,
					maxSize.Width, maxSize.Height);
			}

			Vector2 anchorMin;
			Vector2 anchorMax;
			Vector2 offsetMin;
			Vector2 offsetMax;

			if (Bits.IsSet(align_, Align.Right))
			{
				anchorMin.x = 1;
				anchorMax.x = 1;
				offsetMin.x = -scaled.Width;
				offsetMax.x = 0;
			}
			else if (Bits.IsSet(align_, Align.Center))
			{
				anchorMin.x = 0.5f;
				anchorMax.x = 0.5f;
				offsetMin.x = -scaled.Width / 2;
				offsetMax.x = scaled.Width / 2;
			}
			else  // left
			{
				anchorMin.x = 0;
				anchorMax.x = 0;
				offsetMin.x = 0;
				offsetMax.x = scaled.Width;
			}


			if (Bits.IsSet(align_, Align.Bottom))
			{
				anchorMin.y = 0;
				anchorMax.y = 0;
				offsetMin.y = 0;
				offsetMax.y = scaled.Height;
			}
			else if (Bits.IsSet(align_, Align.VCenter))
			{
				anchorMin.y = 0.5f;
				anchorMax.y = 0.5f;
				offsetMin.y = -scaled.Height / 2;
				offsetMax.y = scaled.Height / 2;
			}
			else  // top
			{
				anchorMin.y = 1;
				anchorMax.y = 1;
				offsetMin.y = -scaled.Height;
				offsetMax.y = 0;
			}


			rt_.anchorMin = anchorMin;
			rt_.anchorMax = anchorMax;
			rt_.offsetMin = offsetMin;
			rt_.offsetMax = offsetMax;
		}

		private void UpdateTexture()
		{
			if (raw_ != null)
			{
				if (enabled_ || tex_ == null)
				{
					raw_.texture = tex_;
				}
				else
				{
					if (grey_ == null)
						CreateGrey();

					raw_.texture = grey_;
				}

				UpdateAspect();
			}
		}

		private void CreateGrey()
		{
			var t = new Texture2D(tex_.width, tex_.height, TextureFormat.ARGB32, false);

			Color32[] pixels = (tex_ as Texture2D).GetPixels32();
			for (int x = 0; x < tex_.width; x++)
			{
				for (int y = 0; y < tex_.height; y++)
				{
					Color32 pixel = pixels[x + y * tex_.width];
					Color c;

					int p = ((256 * 256 + pixel.r) * 256 + pixel.b) * 256 + pixel.g;
					int b = p % 256;
					p = Mathf.FloorToInt(p / 256);
					int g = p % 256;
					p = Mathf.FloorToInt(p / 256);
					int r = p % 256;
					float l = (0.2126f * r / 255f) + 0.7152f * (g / 255f) + 0.0722f * (b / 255f);
					l = l / 3;
					c = new Color(l, l, l, (float)pixel.a / 255.0f);

					t.SetPixel(x, y, c);
				}
			}

			t.Apply(false);

			grey_ = t;
		}

		private Size Aspect(float width, float height, float maxWidth, float maxHeight)
		{
			double ratioX = (double)maxWidth / (double)width;
			double ratioY = (double)maxHeight / (double)height;
			double ratio = ratioX < ratioY ? ratioX : ratioY;

			int newHeight = Convert.ToInt32(Math.Round(height * ratio));
			int newWidth = Convert.ToInt32(Math.Round(width * ratio));

			return new Size(newWidth, newHeight);
		}
	}


	class Image : Panel
	{
		public const int AlignDefault = Align.VCenterCenter;

		public override string TypeName { get { return "Image"; } }

		private ImageObject image_ = null;
		private Texture tex_ = null;
		private int align_ = AlignDefault;


		public Image(int align = AlignDefault)
			: this(null, align)
		{
		}

		public Image(Texture t, int align = AlignDefault)
		{
			tex_ = t;
			align_ = align;
		}

		public Texture Texture
		{
			get { return tex_; }
			set { tex_ = value; TextureChanged();  }
		}

		public int Alignment
		{
			get
			{
				return align_;
			}

			set
			{
				if (align_ != value)
				{
					align_ = value;

					if (image_ != null)
						image_.Alignment = align_;
				}
			}
		}

		protected override void AfterUpdateBounds()
		{
			base.AfterUpdateBounds();

			if (image_ == null)
				image_ = new ImageObject(this, align_);

			TextureChanged();
		}

		protected override void DoSetRender(bool b)
		{
			if (image_ != null)
				image_.SetRender(b);
		}

		protected override void DoSetEnabled(bool b)
		{
			if (image_ != null)
				image_.SetEnabled(b);
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return ImageObject.SGetPreferredSize(tex_, maxWidth, maxHeight);
		}

		private void CheckBounds()
		{
			if (image_ != null)
			{
				image_.SetEnabled(Enabled);
				image_.UpdateAspect();
			}
		}

		private void TextureChanged()
		{
			if (image_ != null)
			{
				image_.Texture = tex_;
				CheckBounds();
			}
		}
	}
}
