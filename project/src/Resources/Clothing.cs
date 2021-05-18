using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	class ClothingResources
	{
		public class Item
		{
			public class Collider
			{
				public bool enabled = false;
				public Vector3 rotation = Vector3.Zero;
				public Vector3 size = Vector3.Zero;
				public Vector3 center = Vector3.Zero;
			}

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

			public Collider left = new Collider();
			public Collider right = new Collider();


			public Item(string what, int sex)
			{
				this.what = what;
				this.sex = sex;
			}

			public override string ToString()
			{
				string s = what + " sex=" + Sexes.ToString(sex) + " ";

				if (showsGenitalsState == "")
					s += $"sg={showsGenitalsBool} ";
				else
					s += $"sg={showsGenitalsState} ";

				if (hidesGenitalsState == "")
					s += $"hg={hidesGenitalsBool} ";
				else
					s += $"hg={hidesGenitalsState} ";

				if (showsBreastsState == "")
					s += $"sb={showsBreastsBool} ";
				else
					s += $"sb={showsBreastsState} ";

				if (hidesBreastsState == "")
					s += $"hb={hidesBreastsBool} ";
				else
					s += $"hb={hidesBreastsState} ";

				if (left.enabled)
					s += "lcoll ";

				if (right.enabled)
					s += "rcoll ";

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
			log_ = new Logger(Logger.Resources, "ClothingRes");
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

				if (a.HasKey("colliders"))
				{
					var cs = a["colliders"].AsObject;

					if (cs.HasKey("left") && cs["left"].AsBool)
					{
						item.left.enabled = true;

						if (cs.HasKey("leftRotation"))
						{
							if (!ParseVector3(cs["leftRotation"], out item.left.rotation))
								log_.Error($"{item.what}: bad leftRotation");
						}

						if (cs.HasKey("leftSize"))
						{
							if (!ParseVector3(cs["leftSize"], out item.left.size))
								log_.Error($"{item.what}: bad leftSize");
						}

						if (cs.HasKey("leftCenter"))
						{
							if (!ParseVector3(cs["leftCenter"], out item.left.center))
								log_.Error($"{item.what}: bad leftCenter");
						}
					}

					if (cs.HasKey("right") && cs["right"].AsBool)
					{
						item.right.enabled = true;

						if (cs.HasKey("rightRotation"))
						{
							if (!ParseVector3(cs["rightRotation"], out item.right.rotation))
								log_.Error($"{item.what}: bad rightRotation");
						}

						if (cs.HasKey("rightSize"))
						{
							if (!ParseVector3(cs["rightSize"], out item.right.size))
								log_.Error($"{item.what}: bad rightSize");
						}

						if (cs.HasKey("rightCenter"))
						{
							if (!ParseVector3(cs["rightCenter"], out item.right.center))
								log_.Error($"{item.what}: bad rightCenter");
						}
					}
				}

				log_.Info("clothing item: " + item.ToString());

				if (id != "")
					ids_.Add(id, item);
				else
					tags_.Add(tag, item);
			}
		}

		private bool ParseVector3(JSONNode n, out Vector3 v)
		{
			v = Vector3.Zero;

			var a = n.AsArray;
			if (a == null)
				return false;

			if (a.Count != 3)
				return false;

			if (!float.TryParse(a[0], out v.X))
				return false;

			if (!float.TryParse(a[1], out v.Y))
				return false;

			if (!float.TryParse(a[2], out v.Z))
				return false;

			return true;
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
