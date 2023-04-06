using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VUI
{
	public abstract class Icon
	{
		private static int currentCacheToken_ = 0;

		private Texture texture_ = null;
		private readonly List<Action<Texture>> callbacks_ = new List<Action<Texture>>();
		private readonly bool rerender_;
		private bool loading_ = false;
		private int cacheToken_ = currentCacheToken_;


		public static Icon FromTexture(Texture t)
		{
			return TextureIcon.FromCache(t);
		}

		public static Icon FromFile(string path, Texture def = null)
		{
			return FileIcon.FromCache(path, def);
		}

		public static Icon FromThumbnail(string path)
		{
			return ThumbnailIcon.FromCache(path);
		}


		protected Icon(bool rerender)
		{
			rerender_ = rerender;
		}

		public static void ClearAllCache()
		{
			++currentCacheToken_;
		}

		public Texture CachedTexture
		{
			get
			{
				if (cacheToken_ != currentCacheToken_)
					return null;

				return texture_;
			}
		}

		public void ClearCache()
		{
			cacheToken_ = -1;
		}

		public void GetTexture(Action<Texture> f)
		{
			if (cacheToken_ != currentCacheToken_)
			{
				cacheToken_ = currentCacheToken_;
				SetTexture(f, null);
				Purge();
			}

			if (texture_ == null)
			{
				SetTexture(f, GetFromCache());

				if (texture_ == null)
				{
					callbacks_.Add(f);

					if (!loading_)
					{
						loading_ = true;
						Load();
					}
				}
				else
				{
					SetTexture(f, texture_);
				}
			}
			else
			{
				SetTexture(f, texture_);
			}
		}

		protected abstract void Purge();
		protected abstract Texture GetFromCache();
		protected abstract void Load();

		protected void LoadFinished(Texture t)
		{
			loading_ = false;

			SetTexture(null, t);
			RunCallbacks();
		}

		private void SetTexture(Action<Texture> f, Texture t)
		{
			texture_ = t;

			if (texture_ != null)
			{
				texture_.wrapMode = TextureWrapMode.Clamp;

				if (rerender_)
				{
					texture_ = ScaleTexture(
						texture_, texture_.width, texture_.height);
				}
			}

			f?.Invoke(texture_);
		}

		protected void LoadFromImageLoader(string path, Texture def)
		{
			ImageLoaderThreaded.QueuedImage q = new ImageLoaderThreaded.QueuedImage();
			q.imgPath = path;

			q.callback = (tt) =>
			{
				Texture tex = tt.tex ?? def;
				LoadFinished(tex);
			};

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


	public class TextureIcon : Icon
	{
		private readonly Texture t_;

		public TextureIcon(Texture t)
			: base(false)
		{
			t_ = t;
		}

		public static TextureIcon FromCache(Texture t)
		{
			return new TextureIcon(t);
		}

		protected override void Purge()
		{
			// no-op
		}

		protected override Texture GetFromCache()
		{
			return t_;
		}

		protected override void Load()
		{
			LoadFinished(t_);
		}

		public override string ToString()
		{
			return $"TextureIcon({t_})";
		}
	}


	public class FileIcon : Icon
	{
		private readonly string path_ = null;
		private readonly Texture def_ = null;

		public FileIcon(string path, Texture def = null)
			: this(path, def, false)
		{
		}

		protected FileIcon(string path, Texture def, bool rerender)
			: base(rerender)
		{
			path_ = path;
			def_ = def;
		}

		public static FileIcon FromCache(string path, Texture def = null)
		{
			return new FileIcon(path, def);
		}

		protected override void Purge()
		{
			ImageLoaderThreaded.singleton.ClearCacheThumbnail(path_);
		}

		protected override Texture GetFromCache()
		{
			return ImageLoaderThreaded.singleton.GetCachedThumbnail(path_);
		}

		protected override void Load()
		{
			LoadFromImageLoader(path_, def_);
		}

		public override string ToString()
		{
			return $"FileIcon({path_})";
		}
	}


	public class ThumbnailIcon : Icon
	{
		private readonly string path_;
		private string thumbPath_;

		private static Dictionary<string, ThumbnailIcon> exts_ = new Dictionary<string, ThumbnailIcon>();
		private static Dictionary<string, ThumbnailIcon> thumbs_ = new Dictionary<string, ThumbnailIcon>();

		public ThumbnailIcon(string path)
			: base(false)
		{
			path_ = path;
			thumbPath_ = GetThumbnailPath(path);
		}

		public static ThumbnailIcon FromCache(string path)
		{
			ThumbnailIcon icon;
			var thumbPath = GetThumbnailPath(path);

			if (thumbPath == null)
			{
				string ext = Path.Extension(path);

				if (!exts_.TryGetValue(ext, out icon))
				{
					icon = new ThumbnailIcon(path);
					exts_.Add(ext, icon);
				}
			}
			else
			{
				if (!thumbs_.TryGetValue(thumbPath, out icon))
				{
					icon = new ThumbnailIcon(path);
					thumbs_.Add(thumbPath, icon);
				}
			}

			return icon;
		}

		protected override void Purge()
		{
			thumbPath_ = GetThumbnailPath(path_);
			if (thumbPath_ != null)
				ImageLoaderThreaded.singleton.ClearCacheThumbnail(thumbPath_);
		}

		protected override Texture GetFromCache()
		{
			if (thumbPath_ == null)
				return GetFileIcon();
			else
				return ImageLoaderThreaded.singleton.GetCachedThumbnail(thumbPath_);
		}

		protected override void Load()
		{
			if (thumbPath_ == null)
				LoadFinished(GetFileIcon());
			else
				LoadFromImageLoader(thumbPath_, SuperController.singleton.fileBrowserUI.defaultIcon.texture);
		}


		public override string ToString()
		{
			return $"ThumbnailIcon({thumbPath_ ?? path_})";
		}

		private static string GetThumbnailPath(string file)
		{
			var exts = new string[] { ".jpg", ".JPG" };

			foreach (var e in exts)
			{
				var relImgPath = Path.Parent(file) + "/" + Path.Stem(file) + e;
				var imgPath = FileManagerSecure.GetFullPath(relImgPath);

				if (FileManagerSecure.FileExists(imgPath))
					return imgPath;
			}

			return null;
		}

		private Texture GetFileIcon()
		{
			string ext = Path.Extension(path_);
			return SuperController.singleton.fileBrowserUI.GetFileIcon(path_)?.texture;
		}
	}


	// for whatever reason, cursor textures are corrupted unless ScaleTexture()
	// is called, even if the size is the same
	//
	public class Cursor : FileIcon
	{
		public Cursor(string path, Texture def = null)
			: base(path, def, true)
		{
		}
	}
}
