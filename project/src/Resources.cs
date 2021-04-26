using System;
using System.Collections.Generic;
using SimpleJSON;

namespace Cue.Resources
{
	class Animations
	{
		public const int NoType = 0;
		public const int Walk = 1;
		public const int TurnLeft = 2;
		public const int TurnRight = 3;
		public const int SitIdle = 4;
		public const int StandIdle = 5;
		public const int SitFromStanding = 6;
		public const int StandFromSitting = 7;
		public const int StraddleSitFromStanding = 8;
		public const int KneelFromStanding = 9;
		public const int StandFromKneeling = 10;
		public const int StandFromStraddleSit = 11;
		public const int StraddleSitSex = 12;

		private static Dictionary<int, List<IAnimation>> anims_ =
			new Dictionary<int, List<IAnimation>>();

		private static Dictionary<string, int> typeMap_ = null;
		private static Dictionary<int, string> typeMapRev_ = null;

		public static Dictionary<string, int> TypeMap
		{
			get
			{
				if (typeMap_ == null)
				{
					typeMap_ = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
					{
						{ "Walk",                    Walk},
						{ "TurnLeft",                TurnLeft },
						{ "TurnRight",               TurnRight },
						{ "SitIdle",                 SitIdle },
						{ "StandIdle",               StandIdle },
						{ "SitFromStanding",         SitFromStanding },
						{ "StandFromSitting",        StandFromSitting },
						{ "StraddleSitFromStanding", StraddleSitFromStanding },
						{ "KneelFromStanding",       KneelFromStanding },
						{ "StandFromKneeling",       StandFromKneeling },
						{ "StandFromStraddleSit",    StandFromStraddleSit },
						{ "StraddleSitSex",          StraddleSitSex }
					};
				}

				return typeMap_;
			}
		}

		public static Dictionary<int, string> ReverseTypeMap
		{
			get
			{
				if (typeMapRev_ == null)
				{
					typeMapRev_ = new Dictionary<int, string>();
					foreach (var kv in typeMap_)
						typeMapRev_.Add(kv.Value, kv.Key);
				}

				return typeMapRev_;
			}
		}

		public static int TypeFromString(string s)
		{
			int t;
			if (TypeMap.TryGetValue(s, out t))
				return t;

			Cue.LogError("unknown anim type '" + s + "'");
			return NoType;
		}

		private static string TypeToString(int t)
		{
			string s;
			if (ReverseTypeMap.TryGetValue(t, out s))
				return s;

			Cue.LogError("unknown anim type " + t.ToString());
			return "none";
		}

		public static bool Load()
		{
			try
			{
				DoLoad();
				return true;
			}
			catch (Exception e)
			{
				Cue.LogError("failed to load animations, " + e.Message);
				return false;
			}
		}

		private static void DoLoad()
		{
			var meta = Cue.Instance.Sys.GetResourcePath("animations.json");
			var doc = JSON.Parse(Cue.Instance.Sys.ReadFileIntoString(meta));

			if (doc == null)
			{
				Cue.LogError("failed to parse animations");
				return;
			}

			foreach (var an in doc.AsObject["animations"].AsArray.Childs)
			{
				var a = an.AsObject;
				var t = TypeFromString(a["type"]);
				if (t == NoType)
					continue;

				IAnimation anim = null;

				if (a.HasKey("bvh"))
				{
					var path = a["bvh"].Value;
					if (path.StartsWith("/") || path.StartsWith("\\"))
					{
						path = path.Substring(1);
					}
					else
					{
						path = Cue.Instance.Sys.GetResourcePath("animations/" + path);
					}

					anim = new BVH.Animation(
						path,
						(a.HasKey("rootXZ") ? a["rootXZ"].AsBool : true),
						(a.HasKey("rootY") ? a["rootY"].AsBool : true),
						(a.HasKey("reverse") ? a["reverse"].AsBool : false),
						(a.HasKey("start") ? a["start"].AsInt : 0),
						(a.HasKey("end") ? a["end"].AsInt : -1));
				}
				else if (a.HasKey("timeline"))
				{
					anim = new TimelineAnimation(a["timeline"]);
				}
				else if (a.HasKey("synergy"))
				{
					anim = new SynergyAnimation(a["synergy"]);
				}
				else
				{
					Cue.LogError("unknown animation key");
					continue;
				}

				if (a.HasKey("sex"))
					anim.Sex = Sexes.FromString(a["sex"]);

				Cue.LogInfo(a["type"] + " anim: " + anim.ToString());

				List<IAnimation> list;
				if (!anims_.TryGetValue(t, out list))
				{
					list = new List<IAnimation>();
					anims_.Add(t, list);
				}

				list.Add(anim);
			}
		}

		public static IAnimation GetAny(int type, int sex)
		{
			List<IAnimation> list;
			if (!anims_.TryGetValue(type, out list))
				return null;

			foreach (var a in list)
			{
				if (Sexes.Match(a.Sex, sex))
					return a;
			}

			return null;
		}

		public static List<IAnimation> GetAll(int type, int sex)
		{
			List<IAnimation> list;

			if (type == NoType)
			{
				list = new List<IAnimation>();
				foreach (var kv in anims_)
					list.AddRange(kv.Value);
			}
			else
			{
				if (!anims_.TryGetValue(type, out list))
					return new List<IAnimation>();
			}

			var matched = new List<IAnimation>();
			foreach (var a in list)
			{
				if (Sexes.Match(a.Sex, sex))
					matched.Add(a);
			}

			return matched;
		}
	}


	class Clothing
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

		public static bool Load()
		{
			try
			{
				DoLoad();
				return true;
			}
			catch (Exception e)
			{
				Cue.LogError("failed to load clothing, " + e.Message);
				return false;
			}
		}

		private static void DoLoad()
		{
			var meta = Cue.Instance.Sys.GetResourcePath("clothing.json");
			var doc = JSON.Parse(Cue.Instance.Sys.ReadFileIntoString(meta));

			if (doc == null)
			{
				Cue.LogError("failed to parse clothing");
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
					Cue.LogError("clothing item missing id or tag");
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

				Cue.LogInfo("clothing item: " + item.ToString());

				if (id != "")
					ids_.Add(id, item);
				else
					tags_.Add(tag, item);
			}
		}

		public static Item FindItem(int sex, string id, string[] tags)
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

