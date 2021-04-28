using SimpleJSON;
using System;
using System.Collections.Generic;

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

				Cue.LogVerbose(a["type"] + " anim: " + anim.ToString());

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
}
