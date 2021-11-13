using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	interface IObjectCreator
	{
		string Name { get; }
		void Create(Sys.IAtom user, string id, Action<IObject> callback);
		void Destroy(Sys.IAtom user, string id);
	}


	class ObjectResources
	{
		private Logger log_;
		private Dictionary<string, List<IObjectCreator>> objects_ =
			new Dictionary<string, List<IObjectCreator>>();

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

		public IObjectCreator Get(string type)
		{
			List<IObjectCreator> list;
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

		private IObjectCreator ParseObject(JSONClass o)
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

		private void Add(IObjectCreator o)
		{
			log_.Info(o.ToString());

			List<IObjectCreator> list;
			if (!objects_.TryGetValue(o.Name, out list))
			{
				list = new List<IObjectCreator>();
				objects_.Add(o.Name, list);
			}

			list.Add(o);
		}
	}
}
