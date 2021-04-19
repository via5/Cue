﻿using System;
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
		public const int SitOnSitting = 8;
		public const int KneelFromStanding = 9;
		public const int StandFromKneeling = 10;

		private static Dictionary<int, List<IAnimation>> anims_ =
			new Dictionary<int, List<IAnimation>>();

		private static int TypeFromString(string os)
		{
			var s = os.ToLower();

			if (s == "walk")
				return Walk;
			else if (s == "turnleft")
				return TurnLeft;
			else if (s == "turnright")
				return TurnRight;
			else if (s == "sitidle")
				return SitIdle;
			else if (s == "standidle")
				return StandIdle;
			else if (s == "sitfromstanding")
				return SitFromStanding;
			else if (s == "standfromsitting")
				return StandFromSitting;
			else if (s == "sitonsitting")
				return SitOnSitting;
			else if (s == "kneelfromstanding")
				return KneelFromStanding;
			else if (s == "standfromkneeling")
				return StandFromKneeling;

			Cue.LogError("unknown anim type '" + os + "'");
			return NoType;
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
			if (!anims_.TryGetValue(type, out list))
				return new List<IAnimation>();

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

			public Item(string what, int sex)
			{
				this.what = what;
				this.sex = sex;
			}

			public override string ToString()
			{
				string s = what + " sex=" + Sexes.ToString(sex);

				if (showsGenitalsState == "")
					s += $"showsGenitals={showsGenitalsBool} ";
				else
					s += $"showsGenitals={showsGenitalsState} ";

				if (hidesGenitalsState == "")
					s += $"hidesGenitals={hidesGenitalsBool} ";
				else
					s += $"hidesGenitals={hidesGenitalsState} ";

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

			foreach (var an in doc.AsObject["clothing"].AsArray.Childs)
			{
				var a = an.AsObject;
				int sex = Sexes.Any;
				string id = "", tag = "";

				if (a.HasKey("id"))
					id = a["id"].Value;
				else
					tag = a["tag"].Value;

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

