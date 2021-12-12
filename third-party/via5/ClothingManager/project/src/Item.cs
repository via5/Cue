using UnityEngine;
using SimpleJSON;

namespace ClothingManager
{
	public class Item
	{
		public class Collider
		{
			public bool enabled = false;
			public Vector3 rotation = Vector3.zero;
			public Vector3 size = new Vector3(0.1f, 0.1f, 0.1f);
			public Vector3 center = Vector3.zero;

			public override bool Equals(object obj)
			{
				var c = obj as Collider;
				if (c == null)
					return false;

				return (
					c.enabled == enabled &&
					c.rotation == rotation &&
					c.size == size &&
					c.center == center);
			}

			public override int GetHashCode()
			{
				return HashHelper.GetHashCode(enabled, rotation, size, center);
			}
		}

		public string id, tag;

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


		public Item(string id, string tag)
		{
			this.id = id;
			this.tag = tag;
		}

		public static Item FromJSON(JSONClass a)
		{
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
				Log.Error("clothing item missing id or tag");
				return null;
			}

			var item = new Item(id, tag);

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
					item.left.rotation = U.VectorFromJSON(cs, "leftRotation");
					item.left.size = U.VectorFromJSON(cs, "leftSize");
					item.left.center = U.VectorFromJSON(cs, "leftCenter");
				}

				if (cs.HasKey("right") && cs["right"].AsBool)
				{
					item.right.enabled = true;
					item.right.rotation = U.VectorFromJSON(cs, "rightRotation");
					item.right.size = U.VectorFromJSON(cs, "rightSize");
					item.right.center = U.VectorFromJSON(cs, "rightCenter");
				}

				if (cs.HasKey("both") && cs["both"].AsBool)
				{
					item.left.enabled = true;
					item.left.rotation = U.VectorFromJSON(cs, "rotation");
					item.left.size = U.VectorFromJSON(cs, "size");
					item.left.center = U.VectorFromJSON(cs, "center");

					item.right.enabled = true;
					item.right.rotation = U.VectorFromJSON(cs, "rotation");
					item.right.size = U.VectorFromJSON(cs, "size");
					item.right.center = U.VectorFromJSON(cs, "center");
				}
			}

			return item;
		}

		public JSONClass ToJSON()
		{
			var o = new JSONClass();

			if (!string.IsNullOrEmpty(id))
				o["id"] = id;
			else
				o["tag"] = tag;

			if (showsGenitalsBool)
				o["showsGenitals"] = new JSONData(true);
			else if (showsGenitalsState != "")
				o["showsGenitalsState"] = showsGenitalsState;

			if (hidesGenitalsBool)
				o["hidesGenitals"] = new JSONData(true);
			else if (hidesGenitalsState != "")
				o["hidesGenitalsState"] = hidesGenitalsState;

			if (showsBreastsBool)
				o["showsBreasts"] = new JSONData(true);
			else if (showsBreastsState != "")
				o["showsBreastsState"] = showsBreastsState;

			if (hidesBreastsBool)
				o["hidesBreasts"] = new JSONData(true);
			else if (hidesBreastsState != "")
				o["hidesBreastsState"] = hidesBreastsState;

			if (left.Equals(right) && left.enabled)
			{
				var cs = new JSONClass();

				cs["both"] = new JSONData(true);
				cs["rotation"] = U.ToJSON(left.rotation);
				cs["size"] = U.ToJSON(left.size);
				cs["center"] = U.ToJSON(left.center);

				o["colliders"] = cs;
			}
			else
			{
				JSONClass cs = null;

				if (left.enabled)
				{
					if (cs == null)
						cs = new JSONClass();

					cs["left"] = new JSONData(true);
					cs["leftRotation"] = U.ToJSON(left.rotation);
					cs["leftSize"] = U.ToJSON(left.size);
					cs["leftCenter"] = U.ToJSON(left.center);
				}

				if (right.enabled)
				{
					if (cs == null)
						cs = new JSONClass();

					o["right"] = new JSONData(true);
					o["rightRotation"] = U.ToJSON(right.rotation);
					o["rightSize"] = U.ToJSON(right.size);
					o["rightCenter"] = U.ToJSON(right.center);
				}

				if (cs != null)
					o["colliders"] = cs;
			}

			return o;
		}

		public override string ToString()
		{
			string s = (id == "" ? tag : id) + " ";

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
}
