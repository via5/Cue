﻿using SimpleJSON;
using System.Collections.Generic;

namespace Cue
{
	public class SensitivityModifier
	{
		public const int Unresolved = -1;
		public const int Any = -2;
		public const int Player = -3;
		public const int Self = -4;
		public const int Toy = -5;
		public const int External = -6;

		private string sourceName_;
		private int sourceIndex_ = Unresolved;
		private BodyPartType sourcePart_;
		private float modifier_;

		public SensitivityModifier(string source, BodyPartType sourcePart, float modifier)
		{
			sourceName_ = source;
			sourcePart_ = sourcePart;
			modifier_ = modifier;
		}

		public SensitivityModifier Clone()
		{
			return new SensitivityModifier(sourceName_, sourcePart_, modifier_);
		}

		public float Modifier
		{
			get { return modifier_; }
		}

		public void Resolve()
		{
			if (sourceIndex_ == Unresolved)
				sourceIndex_ = Resolve(sourceName_);
		}

		public bool AppliesTo(Person self, int triggerType, int sourcePersonIndex, BodyPartType sourcePart)
		{
			if (sourceIndex_ == Player)
			{
				if (triggerType != Sys.TriggerInfo.PersonType)
					return false;

				if (sourcePersonIndex != Cue.Instance.Player.PersonIndex)
					return false;
			}
			else if (sourceIndex_ == Self)
			{
				if (triggerType != Sys.TriggerInfo.PersonType)
					return false;

				if (sourcePersonIndex != self.PersonIndex)
					return false;
			}
			else if (sourceIndex_ == Toy)
			{
				if (triggerType != Sys.TriggerInfo.ToyType)
					return false;
			}
			else if (sourceIndex_ == External)
			{
				if (triggerType != Sys.TriggerInfo.NoneType)
					return false;
			}
			else if (sourceIndex_ != Any)  // this must be last
			{
				if (sourceIndex_ != sourcePersonIndex)
					return false;
			}


			if (sourcePart_ != BP.None)
			{
				if (sourcePart_ != sourcePart)
					return false;
			}


			return true;
		}

		private int Resolve(string s)
		{
			if (s == "" || s == "any")
				return Any;
			else if (s == "player")
				return Player;
			else if (s == "self")
				return Self;
			else if (s == "toy")
				return Toy;
			else if (s == "external")
				return External;

			Person p = Cue.Instance.FindPerson(s);
			if (p == null)
			{
				Logger.Global.Error($"specific modifier: cannot resolve '{s}'");
				return Any;
			}

			return p.PersonIndex;
		}

		public override string ToString()
		{
			string s = $"{IndexToString(sourceIndex_)}.";

			if (sourcePart_ == BP.None)
				s += "any";
			else
				s += BodyPartType.ToString(sourcePart_);

			return s + $" {modifier_}";
		}

		private string IndexToString(int i)
		{
			if (i == Unresolved)
				return "??";
			else if (i == Any)
				return "any";
			else if (i == Player)
				return "player";
			else if (i == Self)
				return "self";
			else if (i == Toy)
				return "toy";
			else if (i == External)
				return "external";
			else
				return Cue.Instance.GetPerson(i)?.ID ?? "?";
		}
	}


	public class Sensitivity
	{
		private ZoneType type_;
		private float physicalRate_;
		private float physicalMax_;
		private float nonPhysicalRate_;
		private float nonPhysicalMax_;
		private SensitivityModifier[] modifiers_ = new SensitivityModifier[0];


		public Sensitivity(
			ZoneType type,
			float physicalRate, float physicalMax,
			float nonPhysicalRate, float nonPhysicalMax,
			SensitivityModifier[] mods)
		{
			type_ = type;
			physicalRate_ = physicalRate;
			physicalMax_ = physicalMax;
			nonPhysicalRate_ = nonPhysicalRate;
			nonPhysicalMax_ = nonPhysicalMax;
			modifiers_ = mods ?? new SensitivityModifier[0];
		}

		public ZoneType Type
		{
			get { return type_; }
		}

		public float PhysicalRate
		{
			get { return physicalRate_; }
		}

		public float PhysicalMaximum
		{
			get { return physicalMax_; }
		}

		public float NonPhysicalRate
		{
			get { return nonPhysicalRate_; }
		}

		public float NonPhysicalMaximum
		{
			get { return nonPhysicalMax_; }
		}

		public SensitivityModifier[] Modifiers
		{
			get { return modifiers_; }
		}

		public Sensitivity Clone()
		{
			var s = new Sensitivity(
				type_,
				physicalRate_, physicalMax_,
				nonPhysicalRate_, nonPhysicalMax_,
				null);

			s.CopyFrom(this);
			return s;
		}

		private void CopyFrom(Sensitivity s)
		{
			modifiers_ = new SensitivityModifier[s.modifiers_.Length];
			for (int i = 0; i < modifiers_.Length; ++i)
				modifiers_[i] = s.modifiers_[i].Clone();
		}

		public void Init()
		{
			for (int i = 0; i < modifiers_.Length; ++i)
				modifiers_[i].Resolve();
		}

		public float GetModifier(Person self, int triggerType, int sourcePersonIndex, BodyPartType sourcePart)
		{
			for (int i = 0; i < modifiers_.Length; ++i)
			{
				var m = modifiers_[i];

				if (m.AppliesTo(self, triggerType, sourcePersonIndex, sourcePart))
					return m.Modifier;
			}

			return 1;
		}

		public override string ToString()
		{
			return $"{ZoneType.ToString(type_)}";
		}
	}


	public class Sensitivities
	{
		private Person person_ = null;
		private Sensitivity[] s_ = new Sensitivity[SS.Count];

		public Sensitivities()
		{
			foreach (ZoneType z in ZoneType.Values)
				s_[z.Int] = new Sensitivity(z, 0, 0, 0, 0, null);
		}

		public Sensitivities Clone(Person p)
		{
			var s = new Sensitivities();
			s.CopyFrom(this, p);
			return s;
		}

		private void CopyFrom(Sensitivities s, Person p)
		{
			person_ = p;
			for (int i = 0; i < s_.Length; ++i)
				s_[i] = s.s_[i].Clone();
		}

		public void Init()
		{
			for (int i = 0; i < s_.Length; ++i)
				s_[i].Init();
		}

		public void Set(Sensitivity[] ss)
		{
			s_ = ss;
		}

		public Sensitivity Get(ZoneType type)
		{
			return s_[type.Int];
		}
	}


	public class Pose
	{
		public class Controller
		{
			public class Parameter
			{
				public string name;
				public string value;

				public Parameter(string name, string value)
				{
					this.name = name;
					this.value = value;
				}
			}

			public string receiver;
			public List<Parameter> ps = new List<Parameter>();

			public Controller(string receiver)
			{
				this.receiver = receiver;
			}

			public Controller Clone()
			{
				var c = new Controller(receiver);

				foreach (var p in ps)
					c.ps.Add(new Parameter(p.name, p.value));

				return c;
			}
		}

		public string type;
		public List<Controller> controllers = new List<Controller>();

		public Pose(string type)
		{
			this.type = type;
		}

		public static Pose CreateDefault()
		{
			return new Pose("keyJoints");
		}

		public Pose Clone()
		{
			var p = new Pose(type);
			p.CopyFrom(this);
			return p;
		}

		private void CopyFrom(Pose p)
		{
			controllers.Clear();
			foreach (var j in p.controllers)
				controllers.Add(j.Clone());
		}

		public void Set(Person p)
		{
			p.Log.Verbose($"setting pose '{type}'");
			p.Atom.SetPose(this);
		}
	}


	public class AnimationFactory
	{
		private readonly Personality ps_;
		private readonly Logger log_;
		private readonly List<Animation> anims_ = new List<Animation>();

		public AnimationFactory(Personality ps)
		{
			ps_ = ps;
			log_ = new Logger(Logger.Animation, $"[{ps.Name}].anims");
		}

		public AnimationFactory Clone(Personality ps)
		{
			var f = new AnimationFactory(ps);
			f.CopyFrom(this);
			return f;
		}

		private void CopyFrom(AnimationFactory f)
		{
			foreach (var a in f.anims_)
				anims_.Add(a);
		}

		public Logger Log
		{
			get { return log_; }
		}

		public bool Has(AnimationType type)
		{
			for (int i = 0; i < anims_.Count; ++i)
			{
				if (anims_[i].Type == type)
					return true;
			}

			return false;
		}

		public Animation GetAny(AnimationType type, int style)
		{
			for (int i = 0; i < anims_.Count; ++i)
			{
				if (anims_[i].Type == type)
				{
					if (MovementStyles.Match(anims_[i].MovementStyle, style))
						return anims_[i];
				}
			}

			return null;
		}

		public List<Animation> GetAll()
		{
			return GetAll(AnimationType.None, MovementStyles.Any);
		}

		public List<Animation> GetAll(AnimationType type, int style)
		{
			if (type == AnimationType.None && style == MovementStyles.Any)
				return anims_;

			var list = new List<Animation>();

			for (int i = 0; i < anims_.Count; ++i)
			{
				if (anims_[i].Type == type)
				{
					if (style == MovementStyles.Any ||
						MovementStyles.Match(anims_[i].MovementStyle, style))
					{
						list.Add(anims_[i]);
					}
				}
			}

			return list;
		}

		public Animation Find(string name)
		{
			for (int i = 0; i < anims_.Count; ++i)
			{
				if (anims_[i].Sys?.Name == name)
					return anims_[i];
			}

			return null;
		}

		public void Add(Animation a)
		{
			bool replaced = false;

			for (int i = 0; i < anims_.Count; ++i)
			{
				if (anims_[i].Type == a.Type)
				{
					anims_.RemoveAt(i);
					replaced = true;
					break;
				}
			}

			if (replaced)
				log_.Info($"{a} (replaced)");
			else
				log_.Info($"{a}");

			anims_.Add(a);
		}
	}


	public class Personality : EnumValueManager
	{
		private readonly string name_;
		private string origin_;
		private Person person_ = null;

		private Expression[] exps_ = new Expression[0];
		private Sensitivities sensitivities_;
		private Dictionary<string, IEventData> events_ = new Dictionary<string, IEventData>();
		private Voice voiceProto_ = null;
		private Pose pose_ = Pose.CreateDefault();
		private AnimationFactory anims_;

		public Personality(string name)
			: base(new PS())
		{
			name_ = name;
			sensitivities_ = new Sensitivities();
			anims_ = new AnimationFactory(this);
		}

		public string Origin
		{
			get { return origin_; }
			set { origin_ = value; }
		}

		public Pose Pose
		{
			get { return pose_; }
			set { pose_ = value; }
		}

		public Personality Clone(string newName, Person p)
		{
			var ps = new Personality(newName ?? name_);
			ps.CopyFrom(this, p);
			return ps;
		}

		private void CopyFrom(Personality ps, Person p)
		{
			base.CopyFrom(ps);

			person_ = p;
			origin_ = ps.origin_;

			exps_ = new Expression[ps.exps_.Length];
			for (int i = 0; i < ps.exps_.Length; i++)
				exps_[i] = ps.exps_[i].Clone();

			sensitivities_ = ps.sensitivities_.Clone(person_);

			events_.Clear();
			foreach (var kv in ps.events_)
				events_[kv.Key] = kv.Value.Clone();

			voiceProto_ = ps.voiceProto_?.Clone();
			pose_ = ps.pose_.Clone();
			anims_ = ps.anims_.Clone(this);
		}

		public void Init()
		{
			sensitivities_.Init();
		}

		public void Destroy()
		{
			for (int i = 0; i < exps_.Length; ++i)
				exps_[i].Reset();
		}

		public void SetExpressions(Expression[] exps)
		{
			exps_ = exps;
		}

		public void SetEventData(string name, IEventData d)
		{
			events_[name] = d;
		}

		public void LoadVoice(JSONClass o, bool inherited)
		{
			if (voiceProto_ == null)
				voiceProto_ = new Voice(o);
			else
				voiceProto_.Load(o, inherited);
		}

		public IEventData CloneEventData(string name)
		{
			IEventData d;
			if (events_.TryGetValue(name, out d))
				return d.Clone();

			return null;
		}

		public void Load(JSONClass o)
		{
			// no-op
		}

		public JSONNode ToJSON()
		{
			var o = new JSONClass();

			if (name_ != Resources.DefaultPersonality)
				o.Add("name", name_);

			var v = new JSONClass();
			o.Add("voice", v);

			return o;
		}

		public string Name
		{
			get { return name_; }
		}

		public Sensitivities Sensitivities
		{
			get { return sensitivities_; }
		}

		public AnimationFactory Animations
		{
			get { return anims_; }
		}

		public Expression[] GetExpressions()
		{
			var e = new Expression[exps_.Length];
			exps_.CopyTo(e, 0);
			return e;
		}

		public Voice CreateVoice()
		{
			if (voiceProto_ == null)
				return null;

			return voiceProto_.Clone();
		}

		public Color GetFlushBaseColor()
		{
			return new Color(
				Get(PS.FlushBaseColorRed),
				Get(PS.FlushBaseColorGreen),
				Get(PS.FlushBaseColorBlue),
				1.0f);
		}

		public override string ToString()
		{
			return $"{Name}";
		}
	}
}
