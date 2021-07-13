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
				states = new Personality.State[PSE.StateCount];

				for (int si = 0; si < PSE.StateCount; ++si)
					states[si] = new Personality.State(si);
			}


			for (int si = 0; si < PSE.StateCount; ++si)
			{
				var s = states[si];

				if (!o.HasKey(s.name))
					throw new LoadFailed($"missing state {s.name}");
				try
				{

					ParseState(s, o[s.name].AsObject, inherited);
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
			for (int i = 0; i < s.bools.Length; ++i)
			{
				string key = PSE.BoolToString(i);

				if (inherited)
					J.OptBool(o, key, ref s.bools[i]);
				else
					s.bools[i] = J.ReqBool(o, key);
			}

			for (int i = 0; i < s.floats.Length; ++i)
			{
				string key = PSE.FloatToString(i);

				if (inherited)
					J.OptFloat(o, key, ref s.floats[i]);
				else
					s.floats[i] = J.ReqFloat(o, key);
			}

			for (int i = 0; i < s.strings.Length; ++i)
			{
				string key = PE.StringToString(i);

				if (inherited)
					J.OptString(o, key, ref s.strings[i]);
				else
					s.strings[i] = J.ReqString(o, key);
			}

			for (int i = 0; i < s.slidingDurations.Length; ++i)
			{
				string key = PSE.SlidingDurationToString(i);

				if (inherited)
				{
					if (o.HasKey(key))
						s.slidingDurations[i] = SlidingDuration.FromJSON(o, key, false);
				}
				else
				{
					s.slidingDurations[i] = SlidingDuration.FromJSON(o, key, true);
				}
			}


			var exps = new List<Personality.ExpressionIntensity>(s.expressions);

			foreach (JSONClass en in o.AsObject["expressions"].AsArray.Childs)
			{
				var ne = new Personality.ExpressionIntensity();
				ne.type = Expressions.FromString(J.ReqString(en, "type"));
				ne.intensity = J.ReqFloat(en, "intensity");

				bool found = false;
				for (int j = 0; j < exps.Count; ++j)
				{
					if (exps[j].type == ne.type)
					{
						found = true;
						exps[j] = ne;
					}
				}

				if (!found)
					exps.Add(ne);
			}

			s.expressions = exps.ToArray();
		}

		private void Add(Personality p)
		{
			log_.Info(p.ToString());
			ps_.Add(p);
		}
	}
}
