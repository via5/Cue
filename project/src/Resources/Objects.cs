using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	class ObjectResources
	{
		private Logger log_;
		private Dictionary<string, List<Sys.IObjectCreator>> objects_ =
			new Dictionary<string, List<Sys.IObjectCreator>>();

		public ObjectResources()
		{
			log_ = new Logger(Logger.Resources, "resObject");
		}

		public bool Load()
		{
			try
			{
				objects_.Clear();
				LoadFromFile();

				return true;
			}
			catch (Exception e)
			{
				log_.Error("failed to load objects, " + e.Message);
				return false;
			}
		}

		public Sys.IObjectCreator Get(string type)
		{
			List<Sys.IObjectCreator> list;
			if (objects_.TryGetValue(type, out list))
			{
				// todo
				if (list.Count > 0)
					return list[0];
			}

			log_.Error($"no object creator type '{type}'");
			return null;
		}

		private void LoadFromFile()
		{
			var meta = Cue.Instance.Sys.GetResourcePath("objects.json");
			var doc = JSON.Parse(Cue.Instance.Sys.ReadFileIntoString(meta));

			if (doc == null)
			{
				log_.Error("failed to parse objects");
				return;
			}

			foreach (var an in doc.AsObject["objects"].AsArray.Childs)
			{
				var a = ParseObject(an.AsObject);

				if (a != null)
					Add(a);
			}
		}

		private Sys.IObjectCreator ParseObject(JSONClass o)
		{
			var name = o["name"].Value;
			var type = o["type"].Value;
			var opts = o["options"].AsObject;

			var pso = o["parameters"].AsObject;
			Sys.ObjectParameters ps = null;
			if (pso != null && pso.Count > 0)
				ps = new Sys.ObjectParameters(pso);

			return Cue.Instance.Sys.CreateObjectCreator(name, type, opts, ps);
		}

		private void Add(Sys.IObjectCreator o)
		{
			log_.Info(o.ToString());

			List<Sys.IObjectCreator> list;
			if (!objects_.TryGetValue(o.Name, out list))
			{
				list = new List<Sys.IObjectCreator>();
				objects_.Add(o.Name, list);
			}

			list.Add(o);
		}
	}
}
