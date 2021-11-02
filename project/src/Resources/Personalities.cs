using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	class PersonalityResources
	{
		private Logger log_;
		private readonly List<Personality> all_ = new List<Personality>();
		private readonly List<Personality> valid_ = new List<Personality>();

		public PersonalityResources()
		{
			log_ = new Logger(Logger.Resources, "PersRes");
		}

		public bool Load()
		{
			try
			{
				all_.Clear();
				LoadFiles();
				return true;
			}
			catch (Exception e)
			{
				log_.Error("failed to load personalities, " + e.ToString());
				return false;
			}
		}

		public Personality Clone(string name, Person p)
		{
			foreach (var ps in valid_)
			{
				if (ps.Name == name)
					return ps.Clone(null, p);
			}

			Cue.LogError($"can't clone personality '{name}', not found");
			return null;
		}

		public List<Personality> All
		{
			get { return new List<Personality>(valid_); }
		}

		public List<string> AllNames()
		{
			var names = new List<string>();

			foreach (var p in valid_)
				names.Add(p.Name);

			return names;
		}

		private void LoadFiles()
		{
			foreach (var f in Resources.LoadFiles("personalities", "*.json"))
				LoadFile(f.name, f.root.AsObject);
		}

		private void LoadFile(string name, JSONClass root)
		{
			log_.Info($"loading personality '{name}'");

			try
			{
				var p = ParsePersonality(root.AsObject);

				if (p != null)
					Add(p, J.OptBool(root.AsObject, "abstract", false));
			}
			catch (LoadFailed e)
			{
				log_.Error($"failed to load personality '{name}'");
				log_.Error(e.ToString());
			}
		}

		private Personality ParsePersonality(JSONClass o)
		{
			Personality p = null;
			bool inherited = false;

			if (o.HasKey("inherit"))
			{
				foreach (var ps in all_)
				{
					if (ps.Name == o["inherit"].Value)
					{
						p = ps.Clone(J.ReqString(o, "name"), null);
					}
				}

				if (p == null)
				{
					throw new LoadFailed(
						$"base personality '{o["inherit"].Value}' not found");
				}

				inherited = true;
			}
			else
			{
				p = new Personality(J.ReqString(o, "name"));
			}


			Resources.LoadEnumValues(p, o, inherited);
			ParseVoice(p.Voice, o, inherited);


			if (o.HasKey("expressions"))
			{
				var es = new List<Expression>();

				if (inherited)
					es.AddRange(p.GetExpressions());

				foreach (JSONClass en in o["expressions"].AsArray)
				{
					var morphs = new List<MorphGroup.MorphInfo>();

					foreach (JSONClass mn in en["morphs"].AsArray)
					{
						morphs.Add(new MorphGroup.MorphInfo(
							mn["id"].Value,
							J.OptFloat(mn, "max", 1.0f),
							BP.None));
					}

					es.Add(new Expression(
						en["name"].Value,
						Moods.FromStringMany(en["moods"].Value),
						new MorphGroup(
							en["name"].Value,
							BP.FromStringMany(en["bodyParts"].Value),
							morphs.ToArray())));
				}

				U.NatSort(es, (e) => e.Name);

				p.SetExpressions(es.ToArray());
			}

			return p;
		}

		private void ParseVoice(Voice v, JSONClass o, bool inherited)
		{
			List<Voice.DatasetForIntensity> dss = null;
			Voice.Dataset orgasmDs = null;
			var voice = o["voice"].AsObject;

			if (voice.HasKey("datasets"))
			{
				dss = new List<Voice.DatasetForIntensity>();

				foreach (JSONClass dn in voice["datasets"].AsArray.Childs)
				{
					var ds = new Voice.DatasetForIntensity(
						new Voice.Dataset(
							J.ReqString(dn, "dataset"),
							J.ReqFloat(dn, "pitch")),
						J.ReqFloat(dn, "intensityMin"),
						J.ReqFloat(dn, "intensityMax"));

					dss.Add(ds);
				}
			}
			else
			{
				if (!inherited)
					throw new LoadFailed("missing voice datasets");
			}

			if (voice.HasKey("orgasm"))
			{
				var oo = voice["orgasm"].AsObject;
				orgasmDs = new Voice.Dataset(
					J.ReqString(oo, "dataset"),
					J.ReqFloat(oo, "pitch"));
			}
			else if (!inherited)
			{
				throw new LoadFailed("missing orgasm dataset");
			}

			v.Set(dss, orgasmDs);
		}

		private void Add(Personality p, bool abst)
		{
			log_.Info(p.ToString());
			all_.Add(p);

			if (!abst)
				valid_.Add(p);
		}
	}
}
