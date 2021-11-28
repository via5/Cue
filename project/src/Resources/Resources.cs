﻿using SimpleJSON;
using System.Collections.Generic;

namespace Cue
{
	class ResourceFile
	{
		public JSONNode root = null;
		public string path = "";
		public string name = "";
		public string inherit = "";
		public int prio = 0;
	}


	class Resources
	{
		private static AnimationResources animations_ = new AnimationResources();
		private static ObjectResources objects_ = new ObjectResources();
		private static PersonalityResources personalities_ = new PersonalityResources();

		public static string DefaultPersonality = "standard";

		public static void LoadAll()
		{
			animations_.Load();
			objects_.Load();
			personalities_.Load();
		}

		public static AnimationResources Animations
		{
			get { return animations_; }
		}

		public static ObjectResources Objects
		{
			get { return objects_; }
		}

		public static PersonalityResources Personalities
		{
			get { return personalities_; }
		}


		public static List<ResourceFile> LoadFiles(
			string rootPath, string pattern = "*.json")
		{
			var files = new Dictionary<string, ResourceFile>();
			var filenames = Cue.Instance.Sys.GetFilenames(rootPath, pattern);

			Cue.LogVerbose("found files:");
			foreach (var f in filenames)
				Cue.LogVerbose($"  - {f}");

			foreach (var path in filenames)
			{
				var f = LoadResourceFile(path);
				if (f == null)
					continue;

				ResourceFile existing;
				if (files.TryGetValue(f.name, out existing))
					Cue.LogVerbose($"ignoring {f.path}, overriden by {existing.path}");
				else
					files.Add(f.name, f);
			}

			FixPriorities(files);

			var sorted = new List<ResourceFile>(files.Values);

			sorted.Sort((a, b) =>
			{
				if (a.prio > b.prio)
					return -1;
				else if (a.prio < b.prio)
					return 1;
				else
					return 0;
			});

			return sorted;
		}

		private static ResourceFile LoadResourceFile(string path)
		{
			var doc = JSON.Parse(Cue.Instance.Sys.ReadFileIntoString(path));

			if (doc == null)
			{
				Cue.LogError($"failed to parse file {path}");
				return null;
			}

			var f = new ResourceFile();
			f.path = path;
			f.root = doc;
			f.name = doc.AsObject["name"].Value;
			f.inherit = doc.AsObject["inherit"].Value;

			return f;
		}

		private static void FixPriorities(Dictionary<string, ResourceFile> files)
		{
			// just to avoid infinite loops if there's a bug somewhere
			int getOut = 0;

			var seen = new List<string>();

			foreach (var p in files)
			{
				var f = files[p.Key];
				var inherit = f.inherit;

				seen.Clear();
				++getOut;

				while (inherit != "")
				{
					++getOut;
					if (getOut > 1000)
						throw new LoadFailed("bailing out");

					if (seen.Contains(inherit))
						throw new LoadFailed($"cyclic dependency, '{inherit}'");

					seen.Add(inherit);

					ResourceFile parent;
					if (!files.TryGetValue(inherit, out parent))
						throw new LoadFailed($"parent '{inherit}' of '{f.name}' not found");

					++parent.prio;
					inherit = parent.inherit;
				}
			}
		}


		public static void LoadEnumValues(
			EnumValueManager v, JSONClass o, bool inherited)
		{
			foreach (var i in v.Values.GetDurationIndexes())
			{
				string key = v.Values.GetDurationName(i);

				if (inherited)
				{
					if (o.HasKey(key))
						v.SetDuration(i, Duration.FromJSON(o, key, false));
				}
				else
				{
					v.SetDuration(i, Duration.FromJSON(o, key, true));
				}
			}


			foreach (var i in v.Values.GetBoolIndexes())
			{
				string key = v.Values.GetBoolName(i);

				if (inherited)
				{
					bool b = false;
					if (J.OptBool(o, key, ref b))
						v.SetBool(i, b);
				}
				else
				{
					v.SetBool(i, J.ReqBool(o, key));
				}
			}


			foreach (var i in v.Values.GetFloatIndexes())
			{
				string key = v.Values.GetFloatName(i);

				if (inherited)
				{
					float f = 0;
					if (J.OptFloat(o, key, ref f))
						v.Set(i, f);
				}
				else
				{
					v.Set(i, J.ReqFloat(o, key));
				}
			}


			foreach (var i in v.Values.GetStringIndexes())
			{
				string key = v.Values.GetStringName(i);

				if (inherited)
				{
					string s = "";
					if (J.OptString(o, key, ref s))
						v.SetString(i, s);
				}
				else
				{
					v.SetString(i, J.ReqString(o, key));
				}
			}
		}
	}
}
