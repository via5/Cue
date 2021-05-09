using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	class ClothingResources
	{
		public class Item
		{
			public string what;
			public int sex;

			public bool showsGenitalsBool = false;
			public bool hidesGenitalsBool = false;
			public string showsGenitalsState = "";
			public string hidesGenitalsState = "";

			public bool showsBreastsBool = false;
			public bool hidesBreastsBool = false;
			public string showsBreastsState = "";
			public string hidesBreastsState = "";

			public Item(string what, int sex)
			{
				this.what = what;
				this.sex = sex;
			}

			public override string ToString()
			{
				string s = what + " sex=" + Sexes.ToString(sex) + " ";

				if (showsGenitalsState == "")
					s += $"showsGenitals={showsGenitalsBool} ";
				else
					s += $"showsGenitals={showsGenitalsState} ";

				if (hidesGenitalsState == "")
					s += $"hidesGenitals={hidesGenitalsBool} ";
				else
					s += $"hidesGenitals={hidesGenitalsState} ";

				if (showsBreastsState == "")
					s += $"showsBreasts={showsBreastsBool} ";
				else
					s += $"showsBreasts={showsBreastsState} ";

				if (hidesBreastsState == "")
					s += $"hidesBreasts={hidesBreastsBool} ";
				else
					s += $"hidesBreasts={hidesBreastsState} ";

				return s;
			}
		}

		private static Dictionary<string, Item> ids_ =
			new Dictionary<string, Item>();

		private static Dictionary<string, Item> tags_ =
			new Dictionary<string, Item>();

		private Logger log_;

		public ClothingResources()
		{
			log_ = new Logger(Logger.Clothing, () => "ClothingRes");
		}

		public bool Load()
		{
			try
			{
				DoLoad();
				return true;
			}
			catch (Exception e)
			{
				log_.Error("failed to load clothing, " + e.Message);
				return false;
			}
		}

		private void DoLoad()
		{
			var meta = Cue.Instance.Sys.GetResourcePath("clothing.json");
			var doc = JSON.Parse(Cue.Instance.Sys.ReadFileIntoString(meta));

			if (doc == null)
			{
				log_.Error("failed to parse json");
				return;
			}

			foreach (var an in doc.AsObject["clothing"].AsArray.Childs)
			{
				var a = an.AsObject;
				int sex = Sexes.Any;
				string id = "", tag = "";

				if (a.HasKey("id"))
				{
					id = a["id"].Value;
				}
				else if (a.HasKey("tag"))
				{
					tag = a["tag"].Value;
				}
				else
				{
					log_.Error("clothing item missing id or tag");
					continue;
				}


				if (a.HasKey("sex"))
					sex = Sexes.FromString(a["sex"].Value);

				var item = new Item(id != "" ? id : tag, sex);

				if (a.HasKey("showsGenitals"))
					item.showsGenitalsBool = a["showsGenitals"].AsBool;
				else if (a.HasKey("showsGenitalsState"))
					item.showsGenitalsState = a["showsGenitalsState"];

				if (a.HasKey("hidesGenitals"))
					item.hidesGenitalsBool = a["hidesGenitals"].AsBool;
				else if (a.HasKey("hidesGenitalsState"))
					item.hidesGenitalsState = a["hidesGenitalsState"];

				if (a.HasKey("showsBreasts"))
					item.showsBreastsBool = a["showsBreasts"].AsBool;
				else if (a.HasKey("showsBreastsState"))
					item.showsBreastsState = a["showsBreastsState"];

				if (a.HasKey("hidesBreasts"))
					item.hidesBreastsBool = a["hidesBreasts"].AsBool;
				else if (a.HasKey("hidesBreastsState"))
					item.hidesBreastsState = a["hidesBreastsState"];

				log_.Info("clothing item: " + item.ToString());

				if (id != "")
					ids_.Add(id, item);
				else
					tags_.Add(tag, item);
			}
		}

		public Item FindItem(int sex, string id, string[] tags)
		{
			Item item;

			if (ids_.TryGetValue(id, out item))
			{
				if (Sexes.Match(sex, item.sex))
					return item;
			}

			if (tags != null)
			{
				for (int i = 0; i < tags.Length; ++i)
				{
					if (tags_.TryGetValue(tags[i], out item))
					{
						if (Sexes.Match(item.sex, sex))
							return item;
					}
				}
			}

			return null;
		}
	}
}
