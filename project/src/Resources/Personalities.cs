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
						break;
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
			ParseVoice(p, o, inherited);
			ParseSensitivities(p.Sensitivities, o, inherited);
			ParseEvents(p, o, inherited);
			ParsePose(p, o, inherited);
			ParseAnimations(p, o, inherited);
			ParseExpressions(p, o, inherited);

			CheckUnused(p, o);

			return p;
		}

		private void CheckUnused(Personality p, JSONClass o)
		{
			var others = new List<string>
			{
				"name", "abstract", "inherit", "voice", "expressions", "events",
				"animations", "sensitivities", "pose", "expressionsInherit"
			};

			foreach (var k in o.Keys)
			{
				if (PS.BoolFromString(k) == -1 &&
					PS.DurationFromString(k) == -1 &&
					PS.FloatFromString(k) == -1 &&
					PS.StringFromString(k) == -1)
				{
					if (!others.Contains(k))
						Log.Warning($"{p.Name}: unused key '{k}'");
				}
			}
		}

		private void ParseExpressions(Personality p, JSONClass o, bool inherited)
		{
			if (o.HasKey("expressions"))
			{
				var es = new List<Expression>();

				bool inh = true;
				if (o.HasKey("expressionsInherit"))
					inh = o["expressionsInherit"]?.AsBool ?? true;

				if (inherited && inh)
					es.AddRange(p.GetExpressions());

				foreach (JSONClass en in o["expressions"].AsArray)
				{
					var morphs = new List<MorphGroup.MorphInfo>();

					foreach (JSONClass mn in en["morphs"].AsArray)
					{
						bool closesEyes = false;
						if (en.HasKey("closesEyes"))
							closesEyes = mn["closesEyes"].AsBool;

						morphs.Add(new MorphGroup.MorphInfo(
							mn["id"].Value,
							J.OptFloat(mn, "min", 0),
							J.OptFloat(mn, "max", 1.0f),
							BP.None,
							J.OptFloat(mn, "eyesClosed", Morph.NoEyesClosed)));
					}

					string name = en["name"].Value;

					var c = new Expression.Config();

					c.weight = J.OptFloat(en, "weight", 1.0f);
					c.exclusive = en["exclusive"].AsBool;
					c.minExcitement = en["minExcitement"].AsFloat;
					c.maxOnly = en["maxOnly"].AsBool;
					c.minHoldTime = J.OptFloat(en, "minHoldTime", -1);
					c.maxHoldTime = J.OptFloat(en, "maxHoldTime", -1);
					c.forMale = true;
					c.forFemale = true;

					if (en.HasKey("sex"))
					{
						var s = en["sex"].Value;

						if (s == "male")
						{
							c.forMale = true;
							c.forFemale = false;
						}
						else if (s == "female")
						{
							c.forMale = false;
							c.forFemale = true;
						}
						else if (s == "all" || s == "")
						{
							c.forMale = true;
							c.forFemale = true;
						}
						else
						{
							throw new LoadFailed(
								$"bad sex value '{s}' for expression {name}, " +
								$"must be 'male', 'female', 'all' or empty");
						}
					}

					foreach (var m in MoodType.FromStringMany(en["moods"].Value))
					{
						es.Add(new Expression(
							name, m, c,
							new MorphGroup(
								name,
								BodyPartType.FromStringMany(en["bodyParts"].Value),
								morphs.ToArray())));
					}
				}

				U.NatSort(es, (e) => e.Name);

				if (es.Count > 0)
				{
					int add = 2;
					string lastName = es[0].Name;

					for (int i = 1; i < es.Count; ++i)
					{
						if (es[i].Name == lastName)
						{
							es[i].Name = $"{es[i].Name}({add})";
							++add;
						}
						else
						{
							lastName = es[i].Name;
							add = 2;
						}
					}
				}

				p.SetExpressions(es.ToArray());
			}
		}

		private void ParsePose(Personality p, JSONClass o, bool inherited)
		{
			if (o.HasKey("pose"))
			{
				var po = o["pose"].AsObject;

				var pose = new Pose(po["type"].Value);

				if (po.HasKey("controllers"))
				{
					foreach (JSONClass co in po["controllers"].AsArray)
					{
						var receivers = new List<string>();

						if (co.HasKey("receivers"))
						{
							foreach (JSONNode n in co["receivers"].AsArray)
								receivers.Add(n.Value);
						}
						else
						{
							receivers.Add(co["receiver"].Value);
						}

						foreach (var r in receivers)
						{
							var c = new Pose.Controller(r);

							if (co.HasKey("parameters"))
							{
								foreach (JSONClass ppo in co["parameters"].AsArray)
								{
									c.ps.Add(new Pose.Controller.Parameter(
										ppo["name"].Value, ppo["value"].Value));
								}
							}

							Log.Info($"controller {c.receiver}");
							pose.controllers.Add(c);
						}
					}
				}

				p.Pose = pose;
			}
			else
			{
				if (!inherited)
					throw new LoadFailed("missing pose");
			}
		}

		private void ParseVoice(Personality p, JSONClass o, bool inherited)
		{
			if (o.HasKey("voice"))
			{
				p.LoadVoice(o["voice"].AsObject, inherited);
			}
			else
			{
				if (!inherited)
					throw new LoadFailed("missing voice");
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

					a[s.Type.Int] = s;
				}

				foreach (ZoneType z in ZoneType.Values)
				{
					if (a[z.Int] == null)
					{
						throw new LoadFailed(
							$"missing sensitivity " +
							$"{ZoneType.ToString(z)}");
					}
				}

				ss.Set(a);
			}
			else if (!inherited)
			{
				throw new LoadFailed("missing sensitivities");
			}
		}

		private void ParseAnimations(Personality p, JSONClass o, bool inherited)
		{
			if (o.HasKey("animations"))
			{
				foreach (var an in o["animations"].AsArray.Childs)
				{
					var a = ParseAnimation(an.AsObject);

					if (a != null)
						p.Animations.Add(a);
				}
			}
		}

		private Animation ParseAnimation(JSONClass o)
		{
			IAnimation a = CreateIntegrationAnimation(o);
			if (a == null)
				return null;

			if (o.HasKey("enabled") && !o["enabled"].AsBool)
				return null;

			if (!o.HasKey("animation"))
			{
				log_.Error("object missing 'animation'");
				return null;
			}

			AnimationType type = AnimationType.FromString(o["animation"].Value);
			if (type == AnimationType.None)
			{
				log_.Error($"bad animation type '{o["animation"].Value}'");
				return null;
			}

			int ms = MovementStyles.Any;
			if (o.HasKey("sex"))
				ms = MovementStyles.FromString(o["sex"]);
			else if (o.HasKey("style"))
				ms = MovementStyles.FromString(o["style"]);

			return new Animation(type, ms, a);
		}

		private IAnimation CreateIntegrationAnimation(JSONClass o)
		{
			if (!o.HasKey("type"))
			{
				log_.Error("object missing 'type'");
				return null;
			}

			string type = o["type"];

			JSONClass options;

			if (o.HasKey("options"))
				options = o["options"].AsObject;
			else
				options = new JSONClass();

			IAnimation a = null;

			if (type == "bvh")
				a = BVH.Animation.Create(options);
			else if (type == "timeline")
				a = TimelineAnimation.Create(options);
			else if (type == "synergy")
				a = SynergyAnimation.Create(options);
			else if (type == "proc")
				a = Proc.ProcAnimation.Create(options);
			else if (type == "internal")
				a = BuiltinAnimations.Get(options["name"].Value);
			else if (type == "none")
				return new DummyAnimation();
			else
				log_.Error($"unknown animation type '{type}'");

			return a;
		}

		private Sensitivity ParseSensitivity(JSONClass o)
		{
			var typeName = o["type"].Value;
			var type = ZoneType.FromString(typeName);
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

			BodyPartType sourcePart = BP.None;
			if (sourcePartName != "" && sourcePartName != "any")
			{
				sourcePart = BodyPartType.FromString(sourcePartName);
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
					var e = PersonAI.CreateEvent(eo["type"].Value);
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
