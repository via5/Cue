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

		public Physiology Clone(string name, Person p)
		{
			foreach (var ps in ps_)
			{
				if (ps.Name == name)
					return ps.Clone(p);
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

			float[] fs = new float[PE.FloatCount];
			for (int i = 0; i < PE.FloatCount; ++i)
			{
				string key = PE.FloatToString(i);
				fs[i] = J.ReqFloat(o, key);
			}

			string[] ss = new string[PE.StringCount];
			for (int i = 0; i < PE.StringCount; ++i)
			{
				string key = PE.StringToString(i);
				ss[i] = J.ReqString(o, key);
			}

			var sms = new List<Physiology.SpecificModifier>();
			if (o.HasKey("specificModifiers"))
			{
				foreach (JSONClass smn in o.AsObject["specificModifiers"].AsArray.Childs)
				{
					var sm = new Physiology.SpecificModifier();
					sm.bodyPart = BodyParts.FromString(J.ReqString(smn, "bodyPart"));
					sm.sourceBodyPart = BodyParts.FromString(J.ReqString(smn, "sourceBodyPart"));
					sm.modifier = J.ReqFloat(smn, "modifier");
				}
			}

			p.Set(fs, ss, sms.ToArray());

			return p;
		}

		private void Add(Physiology p)
		{
			log_.Info(p.ToString());
			ps_.Add(p);
		}
	}
}
