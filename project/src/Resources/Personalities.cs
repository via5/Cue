using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	class PersonalityResources
	{
		private Logger log_;
		private readonly List<Personality> ps_ = new List<Personality>();

		public PersonalityResources()
		{
			log_ = new Logger(Logger.Resources, "PersRes");
		}

		public bool Load()
		{
			try
			{
				ps_.Clear();
				LoadFromFile();
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
			foreach (var ps in ps_)
			{
				if (ps.Name == name)
					return ps.Clone(null, p);
			}

			Cue.LogError($"personality '{name}' not found");
			return new Personality(name);
		}

		public List<Personality> All
		{
			get { return new List<Personality>(ps_); }
		}

		public List<string> AllNames()
		{
			var names = new List<string>();

			foreach (var p in ps_)
				names.Add(p.Name);

			return names;
		}

		private void LoadFromFile()
		{
			var meta = Cue.Instance.Sys.GetResourcePath("personalities.json");
			var doc = JSON.Parse(Cue.Instance.Sys.ReadFileIntoString(meta));

			if (doc == null)
			{
				log_.Error("failed to parse personalities");
				return;
			}

			foreach (var an in doc.AsObject["personalities"].AsArray.Childs)
			{
				try
				{
					var p = ParsePersonality(an.AsObject);

					if (p != null)
						Add(p);
				}
				catch (LoadFailed e)
				{
					log_.Error($"failed to load personality '{an["name"].Value}'");
					log_.Error(e.ToString());
				}
			}
		}

		private Personality ParsePersonality(JSONClass o)
		{
			Personality p = null;
			Personality.State[] states = null;
			bool inherited = false;

			if (o.HasKey("inherit"))
			{
				foreach (var ps in ps_)
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

				states = p.States;
				inherited = true;
			}
			else
			{
				p = new Personality(J.ReqString(o, "name"));
				states = new Personality.State[Personality.StateCount];

				for (int si = 0; si < Personality.StateCount; ++si)
					states[si] = new Personality.State(si);
			}


			for (int si = 0; si < Personality.StateCount; ++si)
			{
				var s = states[si];

				if (!o.HasKey(s.name))
					throw new LoadFailed($"missing state {s.name}");

				try
				{
					var so = o[s.name].AsObject;
					bool stateInherited = false;

					if (so.HasKey("inherit"))
					{
						stateInherited = true;

						if (so["inherit"].Value == s.name)
						{
							throw new LoadFailed(
								$"state {s.name} inheriting from itself");
						}

						bool found = false;
						for (int ssi = 0; ssi < si; ++ssi)
						{
							if (states[ssi].name == so["inherit"].Value)
							{
								s.CopyFrom(states[ssi]);
								found = true;
								break;
							}
						}

						if (!found)
						{
							throw new LoadFailed(
								$"state {s.name} inherits from non existing " +
								$"state {so["inherit"].Value}");
						}
					}

					ParseState(s, so, inherited || stateInherited);
				}
				catch (LoadFailed e)
				{
					log_.Error($"failed to load personality state '{s.name}'");
					throw e;
				}
			}

			p.Set(states);

			return p;
		}

		private void ParseState(Personality.State s, JSONClass o, bool inherited)
		{
			Resources.LoadEnumValues(s, o, inherited);

			foreach (JSONClass en in o.AsObject["expressions"].AsArray.Childs)
			{
				int type = Expressions.FromString(J.ReqString(en, "type"));
				float maximum = J.ReqFloat(en, "maximum");

				s.SetMaximum(type, maximum);
			}
		}

		private void Add(Personality p)
		{
			log_.Info(p.ToString());
			ps_.Add(p);
		}
	}
}
