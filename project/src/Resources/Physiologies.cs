using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	class PhysiologyResources
	{
		private Logger log_;
		private readonly List<Physiology> ps_ = new List<Physiology>();

		public PhysiologyResources()
		{
			log_ = new Logger(Logger.Resources, "PhysRes");
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
				log_.Error("failed to load physiologies, " + e.ToString());
				return false;
			}
		}

		public Physiology Clone(string name, Person person)
		{
			foreach (var ps in ps_)
			{
				if (ps.Name == name)
					return ps.Clone(person);
			}

			Cue.LogError($"physiology '{name}' not found");
			return new Physiology(name);
		}

		private void LoadFromFile()
		{
			var meta = Cue.Instance.Sys.GetResourcePath("physiologies.json");
			var doc = JSON.Parse(Cue.Instance.Sys.ReadFileIntoString(meta));

			if (doc == null)
			{
				log_.Error("failed to parse physiologies");
				return;
			}

			foreach (var an in doc.AsObject["physiologies"].AsArray.Childs)
			{
				var p = ParsePhysiology(an.AsObject);

				if (p != null)
					Add(p);
			}
		}

		private Physiology ParsePhysiology(JSONClass o)
		{
			var p = new Physiology(J.ReqString(o, "name"));

			Resources.LoadEnumValues(p, o, false);

			var sms = new List<Physiology.SpecificModifier>();
			if (o.HasKey("specificModifiers"))
			{
				foreach (JSONClass smn in o.AsObject["specificModifiers"].AsArray.Childs)
				{
					var sm = new Physiology.SpecificModifier();

					var s = J.ReqString(smn, "bodyPart");
					sm.bodyPart = BP.FromString(s);
					if (sm.bodyPart == -1 && s != "unknown")
						log_.Error($"{p}: bad bodyPart {s}");

					s = J.ReqString(smn, "sourceBodyPart");
					sm.sourceBodyPart = BP.FromString(s);
					if (sm.sourceBodyPart == -1 && s != "unknown")
						log_.Error($"{p}: bad sourceBodyPart {s}");

					sm.modifier = J.ReqFloat(smn, "modifier");
					sms.Add(sm);
				}
			}

			p.Set(sms.ToArray());

			return p;
		}

		private void Add(Physiology p)
		{
			log_.Info(p.ToString());
			ps_.Add(p);
		}
	}
}
