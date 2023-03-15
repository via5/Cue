using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VUI
{
	public class Icon
	{
		private Texture texture_ = null;
		private readonly string path_ = null;
		private readonly int w_ = 0, h_ = 0;
		private readonly Texture def_ = null;
		private readonly List<Action<Texture>> callbacks_ = new List<Action<Texture>>();
		private bool loading_ = false;

		public Icon(Texture t)
		{
			texture_ = t;
		}

		public Icon(string path, Texture def = null)
			: this(path, 0, 0, def)
		{
		}

		public Icon(string path, int w, int h, Texture def = null)
		{
			path_ = path;
			w_ = w;
			h_ = h;
			def_ = def;
		}

		public Texture CachedTexture
		{
			get { return texture_; }
		}

		public void GetTexture(Action<Texture> f)
		{
			if (texture_ != null)
			{
				f(texture_);
			}
			else
			{
				if (GetFromCache())
				{
					f(texture_);
				}
				else
				{
					callbacks_.Add(f);

					if (!loading_)
					{
						loading_ = true;
						Load();
					}
				}
			}
		}

		private bool GetFromCache()
		{
			if (path_ == null)
			{
				texture_ = def_;
				return true;
			}
			else
			{
				var tex = ImageLoaderThreaded.singleton.GetCachedThumbnail(path_);
				if (tex != null)
				{
					texture_ = tex;

					if (w_ != 0 && h_ != 0)
						texture_ = ScaleTexture(texture_, w_, h_);

					texture_.wrapMode = TextureWrapMode.Clamp;

					return true;
				}
			}

			return false;
		}

		private void Load()
		{
			ImageLoaderThreaded.QueuedImage q = new ImageLoaderThreaded.QueuedImage();
			q.imgPath = path_;

			q.callback = (tt) =>
			{
				Texture tex = tt.tex;
				if (tex == null)
				{
					texture_ = def_;
				}
				else
				{
					texture_ = tex;

					if (w_ != 0 && h_ != 0)
						texture_ = ScaleTexture(texture_, w_, h_);

					texture_.wrapMode = TextureWrapMode.Clamp;
				}

				RunCallbacks();
			};

			//ImageLoaderThreaded.singleton.ClearCacheThumbnail(q.imgPath);
			ImageLoaderThreaded.singleton.QueueThumbnail(q);
		}

		private void RunCallbacks()
		{
			foreach (var f in callbacks_)
				f(texture_);

			callbacks_.Clear();
		}

		private static Texture2D ScaleTexture(Texture src, int width, int height)
		{
			RenderTexture rt = RenderTexture.GetTemporary(width, height);
			Graphics.Blit(src, rt);

			RenderTexture currentActiveRT = RenderTexture.active;
			RenderTexture.active = rt;
			Texture2D tex = new Texture2D(rt.width, rt.height);

			tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
			tex.Apply();

			RenderTexture.ReleaseTemporary(rt);
			RenderTexture.active = currentActiveRT;

			return tex;
		}
	}


	class ImageObject
	{
		private static Material emptyMat_ = null;

		private readonly Widget parent_;
		private readonly RectTransform rt_ = null;
		private readonly RawImage raw_ = null;
		private Texture tex_ = null;
		private Texture grey_ = null;
		private Size size_ = new Size(Widget.DontCare, Widget.DontCare);
		private bool enabled_ = true;

		public ImageObject(Widget parent)
		{
			parent_ = parent;

			var rawObject = new GameObject();
			rawObject.transform.SetParent(parent.WidgetObject.transform, false);

			raw_ = rawObject.AddComponent<RawImage>();
			rt_ = rawObject.GetComponent<RectTransform>();

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

			rt_.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, scaled.Width);
			rt_.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, scaled.Height);
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


	class Image : Widget
	{
		public override string TypeName { get { return "Image"; } }

		private ImageObject image_ = null;
		private Texture tex_ = null;

		public Texture Texture
		{
			get { return tex_; }
			set { tex_ = value; UpdateTexture();  }
		}

		protected override GameObject CreateGameObject()
		{
			return new GameObject();
		}

		protected override void DoCreate()
		{
		}

		protected override void AfterUpdateBounds()
		{
			if (image_ == null)
				image_ = new ImageObject(this);

			image_.Texture = tex_;
			image_.SetEnabled(Enabled);
			image_.UpdateAspect();
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

		private void UpdateTexture()
		{
			if (image_ != null)
				image_.Texture = tex_;
		}
	}
}
