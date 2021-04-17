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

			Cue.LogError("unknown anim type '" + os + "'");
			return NoType;
		}

		public static void Load()
		{
			var meta = Cue.Instance.Sys.GetResourcePath("animations/meta.json");
			var doc = JSON.Parse(Cue.Instance.Sys.ReadFileIntoString(meta));

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

				Cue.LogError(a["type"] + " anim: " + anim.ToString());

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
}
