using System;
using SimpleJSON;
using MVR.FileManagementSecure;
using System.Collections.Generic;

namespace ClothingManager
{
	class Loader
	{
		public static string Root = "Custom\\PluginData\\ClothingManager";

		public string[] MakePath(string id)
		{
			return new string[] { Root, id.Replace(":", ".") + ".meta.json" };
		}

		public List<Item> LoadGlobal()
		{
			return LoadFiles("clothing.meta.json", true);
		}

		public List<Item> TryLoad(string id)
		{
			var pattern = id.Replace(":", ".") + ".meta.json";
			return LoadFiles(pattern);
		}

		private List<Item> LoadFiles(string pattern, bool includePlugin = false)
		{
			Log.Verbose($"finding files for '{pattern}'");
			var list = new List<Item>();

			foreach (var f in GetFilenames(pattern, includePlugin))
			{
				try
				{
					Load(list, f);
				}
				catch (Exception e)
				{
					Log.Error($"failed to load clothing from {f}, {e.Message}");
				}
			}

			return list;
		}

		private List<string> GetFilenames(string pattern, bool includePlugin = false)
		{
			var scs = FileManagerSecure.GetShortCutsForDirectory(Root);
			var list = new List<string>();

			foreach (var s in scs)
			{
				var fs = FileManagerSecure.GetFiles(s.path, pattern);
				if (fs.Length == 0)
					continue;

				foreach (var f in fs)
					list.Add(f);
			}

			if (includePlugin)
				list.Add(via5.ClothingManager.Instance.PluginPath + "\\" + pattern);

			return list;
		}

		private void Load(List<Item> list, string metaFile)
		{
			Log.Verbose($"loading {metaFile}");
			var doc = JSON.Parse(FileManagerSecure.ReadAllText(metaFile));

			if (doc == null)
				throw new LoadFailed("failed to parse json");

			foreach (var an in doc.AsObject["clothing"].AsArray.Childs)
			{
				var item = Item.FromJSON(an.AsObject);
				if (item != null)
					list.Add(item);
			}
		}
	}
}
