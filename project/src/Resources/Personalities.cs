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
			log_ = new Logger(Logger.Resources, "resPs");
		}

		public Logger Log
		{
			get { return log_; }
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
				Log.Error("failed to load personalities, " + e.ToString());
				return false;
			}
		}

		public Personality Clone(string name, Person p)
		{
			foreach (var ps in valid_)
			{
				if (ps.Name.ToLower() == name.ToLower())
					return ps.Clone(null, p);
			}

			Log.Error($"can't clone personality '{name}', not found");
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
				LoadFile(f.name, f.origin, f.root.AsObject);
		}

		private void LoadFile(string name, string origin, JSONClass root)
		{
			Log.Info($"loading personality '{name}'");

			try
			{
				var p = ParsePersonality(root.AsObject);

				if (p != null)
				{
					p.Origin = origin;
					Add(p, J.OptBool(root.AsObject, "abstract", false));
				}
			}
			catch (LoadFailed e)
			{
				Log.Error($"failed to load personality '{name}'");
				Log.Error(e.ToString());
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
					if (ps.Name.ToLower() == o["inherit"].Value.ToLower())
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
			ParseBreathing(p, o, inherited);
			ParseOrgasmer(p, o, inherited);
			ParseSensitivities(p.Sensitivities, o, inherited);
			ParseEvents(p, o, inherited);

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

		private void ParseBreathing(Personality p, JSONClass o, bool inherited)
		{
			if (o.HasKey("breathing"))
			{
				var provider = o["breathing"]["provider"].Value;
				var options = o["breathing"]["options"].AsObject;

				IBreather b = Integration.CreateBreather(provider, options);

				if (b == null)
					throw new LoadFailed($"unknown breather provider '{provider}'");

				p.SetBreather(b);
			}
			else
			{
				if (!inherited)
					throw new LoadFailed("missing breathing");
			}
		}

		private void ParseOrgasmer(Personality p, JSONClass o, bool inherited)
		{
			if (o.HasKey("orgasm"))
			{
				var provider = o["orgasm"]["provider"].Value;
				var options = o["orgasm"]["options"].AsObject;

				IOrgasmer b = Integration.CreateOrgasmer(provider, options);

				if (b == null)
					throw new LoadFailed($"unknown orgasm provider '{provider}'");

				p.SetOrgasmer(b);
			}
			else
			{
				if (!inherited)
					throw new LoadFailed("missing orgasm");
			}
		}

		private void ParseSensitivities(Sensitivities ss, JSONClass o, bool inherited)
		{
			if (o.HasKey("sensitivities"))
			{
				var a = new Sensitivity[SS.Count];

				foreach (var c in o["sensitivities"].AsArray.Childs)
				{
					var s = ParseSensitivity(c.AsObject);
					if (s == null)
						continue;

					a[s.Type] = s;
				}

				for (int i = 0; i < a.Length; ++i)
				{
					if (a[i] == null)
					{
						throw new LoadFailed(
							$"missing sensitivity " +
							$"{SS.ToString(i)}");
					}
				}

				ss.Set(a);
			}
			else if (!inherited)
			{
				throw new LoadFailed("missing sensitivities");
			}
		}

		private Sensitivity ParseSensitivity(JSONClass o)
		{
			var typeName = o["type"].Value;
			var type = SS.FromString(typeName);
			if (type == SS.None)
			{
				Log.Error($"bad sensitivity type {typeName}");
				return null;
			}

			float physicalRate, physicalMax, nonPhysicalRate, nonPhysicalMax;

			if (o.HasKey("rate"))
			{
				physicalRate = J.ReqFloat(o, "rate");
				physicalMax = J.OptFloat(o, "max", 1.0f);
				nonPhysicalRate = physicalRate;
				nonPhysicalMax = physicalMax;
			}
			else
			{
				physicalRate = J.ReqFloat(o, "physicalRate");
				physicalMax = J.OptFloat(o, "physicalMax", 1.0f);
				nonPhysicalRate = J.ReqFloat(o, "nonPhysicalRate");
				nonPhysicalMax = J.OptFloat(o, "nonPhysicalMax", 1.0f);
			}

			var mods = new List<SensitivityModifier>();

			if (o.HasKey("modifiers"))
			{
				foreach (var c in o["modifiers"].AsArray.Childs)
				{
					var m = ParseSensitivityModifier(c.AsObject);
					if (m != null)
						mods.Add(m);
				}
			}

			return new Sensitivity(
				type,
				physicalRate, physicalMax,
				nonPhysicalRate, nonPhysicalMax,
				mods.ToArray());
		}

		private SensitivityModifier ParseSensitivityModifier(JSONClass o)
		{
			var source = J.OptString(o, "source");
			var sourcePartName = J.OptString(o, "sourcePart");
			float modifier = J.ReqFloat(o, "modifier");

			int sourcePart = BP.None;
			if (sourcePartName != "" && sourcePartName != "any")
			{
				sourcePart = BP.FromString(sourcePartName);
				if (sourcePart == BP.None)
					Log.Error($"bad sourcePart '{sourcePartName}'");
			}

			return new SensitivityModifier(source, sourcePart, modifier);
		}

		private void ParseEvents(Personality p, JSONClass o, bool inherited)
		{
			if (o.HasKey("events"))
			{
				foreach (var eo in o["events"].AsArray.Childs)
				{
					var e = BasicEvent.Create(eo["type"].Value);
					if (e == null)
						throw new LoadFailed($"unknown event '{eo["type"].Value}'");

					var d = e.ParseEventData(eo.AsObject);
					if (d != null)
						p.SetEventData(e.Name, d);
				}
			}
		}

		private void Add(Personality p, bool abst)
		{
			Log.Info(p.ToString());
			all_.Add(p);

			if (!abst)
				valid_.Add(p);
		}
	}
}
