using SimpleJSON;
using System.Collections.Generic;

namespace Cue
{
	public class SensitivityModifier
	{
		public const int Unresolved = -1;
		public const int Any = -2;
		public const int Player = -3;
		public const int Self = -4;

		private string sourceName_;
		private int sourceIndex_ = Unresolved;
		private int sourcePart_;
		private float modifier_;

		public SensitivityModifier(string source, int sourcePart, float modifier)
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

		public bool AppliesTo(Person self, int sourcePersonIndex, int sourcePart)
		{
			if (sourceIndex_ == Player)
			{
				if (sourcePersonIndex != Cue.Instance.Player.PersonIndex)
					return false;
			}
			else if (sourceIndex_ == Self)
			{
				if (sourcePersonIndex != self.PersonIndex)
					return false;
			}
			else if (sourceIndex_ != Any)
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

			Person p = Cue.Instance.FindPerson(s);
			if (p == null)
			{
				Cue.LogError($"specific modifier: cannot resolve '{s}'");
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
				s += BP.ToString(sourcePart_);

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
			else
				return Cue.Instance.GetPerson(i)?.ID ?? "?";
		}
	}


	public class Sensitivity
	{
		private int type_;
		private float physicalRate_;
		private float physicalMax_;
		private float nonPhysicalRate_;
		private float nonPhysicalMax_;
		private SensitivityModifier[] modifiers_ = new SensitivityModifier[0];


		public Sensitivity(
			int type,
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

		public int Type
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

		public float GetModifier(Person self, int sourcePersonIndex, int sourcePart)
		{
			for (int i = 0; i < modifiers_.Length; ++i)
			{
				var m = modifiers_[i];

				if (m.AppliesTo(self, sourcePersonIndex, sourcePart))
					return m.Modifier;
			}

			return 1;
		}

		public override string ToString()
		{
			return $"{SS.ToString(type_)}";
		}
	}


	public class Sensitivities
	{
		private Person person_ = null;
		private Sensitivity[] s_ = new Sensitivity[SS.Count];

		public Sensitivities()
		{
			for (int i = 0; i < s_.Length; ++i)
				s_[i] = new Sensitivity(i, 0, 0, 0, 0, null);
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

		public Sensitivity Get(int type)
		{
			return s_[type];
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

		public Personality(string name)
			: base(new PS())
		{
			name_ = name;
			sensitivities_ = new Sensitivities();
		}

		public string Origin
		{
			get { return origin_; }
			set { origin_ = value; }
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

		public void SetVoice(Voice v)
		{
			voiceProto_ = v;
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

		public override string ToString()
		{
			return $"{Name}";
		}
	}
}
