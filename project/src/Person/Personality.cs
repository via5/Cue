using SimpleJSON;
using System.Collections.Generic;

namespace Cue
{
	public class Voice
	{
		public class Dataset
		{
			private string name_;
			private float pitch_;

			public Dataset(string name, float pitch)
			{
				name_ = name;
				pitch_ = pitch;
			}

			public Dataset(Dataset d)
			{
				name_ = d.name_;
				pitch_ = d.pitch_;
			}

			public string Name
			{
				get { return name_; }
			}

			public float GetPitch(Person p)
			{
				if (pitch_ < 0)
				{
					float neutral = p.Personality.Get(PS.NeutralVoicePitch);
					float scale = p.Atom.Scale;
					SetPitch(neutral + (1 - scale));
				}

				return pitch_;
			}

			public void SetPitch(float f)
			{
				pitch_ = U.Clamp(f, 0, 1);
			}
		}

		public class DatasetForIntensity
		{
			public Dataset dataset;
			public float intensityMin;
			public float intensityMax;

			public DatasetForIntensity(Dataset ds, float intensityMin, float intensityMax)
			{
				this.dataset = ds;
				this.intensityMin = intensityMin;
				this.intensityMax = intensityMax;
			}

			public DatasetForIntensity(DatasetForIntensity d)
			{
				dataset = new Dataset(d.dataset);
				intensityMin = d.intensityMin;
				intensityMax = d.intensityMax;
			}
		}


		private Person person_ = null;
		private DatasetForIntensity[] datasets_ = new DatasetForIntensity[0];
		private Dataset orgasm_ = new Dataset("", -1);
		private Dataset dummy_ = new Dataset("", -1);
		private bool warned_ = false;
		private float forcedPitch_ = -1;


		public Logger Log
		{
			get { return person_.Log; }
		}

		public void Set(List<DatasetForIntensity> dss, Dataset orgasm)
		{
			if (dss != null)
				datasets_ = dss.ToArray();

			if (orgasm != null)
				orgasm_ = orgasm;
		}

		public Voice Clone(Person p)
		{
			var v = new Voice();
			v.CopyFrom(this, p);
			return v;
		}

		private void CopyFrom(Voice v, Person p)
		{
			person_ = p;

			datasets_ = new DatasetForIntensity[v.datasets_.Length];
			for (int i = 0; i < datasets_.Length; ++i)
				datasets_[i] = new DatasetForIntensity(v.datasets_[i]);

			orgasm_ = new Dataset(v.orgasm_);
		}

		public DatasetForIntensity[] Datasets
		{
			get { return datasets_; }
		}

		public Dataset OrgasmDataset
		{
			get { return orgasm_; }
		}

		public float ForcedPitch
		{
			get { return forcedPitch_; }
		}

		public float GetNormalPitch()
		{
			return GetDatasetForIntensity(0).GetPitch(person_);
		}

		public void ForcePitch(float f)
		{
			forcedPitch_ = f;

			foreach (var d in datasets_)
				d.dataset.SetPitch(f);

			orgasm_.SetPitch(f);
		}

		public Dataset GetDatasetForIntensity(float e)
		{
			for (int i = 0; i < datasets_.Length; ++i)
			{
				if (e >= datasets_[i].intensityMin &&
					e <= datasets_[i].intensityMax)
				{
					return datasets_[i].dataset;
				}
			}

			if (!warned_)
			{
				Log.Error(
					$"personality missing voice for excitement " +
					$"{e}");

				warned_ = true;
			}

			if (datasets_.Length == 0)
				return dummy_;
			else
				return datasets_[0].dataset;
		}
	}


	public class SensitivityModifier
	{
		public const int Unresolved = -1;
		public const int Any = -2;
		public const int Player = -3;
		public const int Self = -4;

		private string sourceName_;
		private int sourceIndex_ = Unresolved;
		private int sourceBodyPart_;
		private float modifier_;

		public SensitivityModifier(
			string source, int sourceBodyPart, float modifier)
		{
			sourceName_ = source;
			sourceBodyPart_ = sourceBodyPart;
			modifier_ = modifier;
		}

		public SensitivityModifier Clone()
		{
			return new SensitivityModifier(
				sourceName_, sourceBodyPart_, modifier_);
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

		public bool AppliesTo(
			Person self, int sourcePersonIndex, int sourceBodyPart)
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


			if (sourceBodyPart_ != BP.None && sourceBodyPart_ != sourceBodyPart)
				return false;


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
			return $"{ToString(sourceIndex_, sourceBodyPart_)} {modifier_}";
		}

		private string ToString(int index, int bodyPart)
		{
			string s = IndexToString(index) + ".";

			if (bodyPart == BP.None)
				s += "any";
			else
				s += BP.ToString(bodyPart);

			return s;
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
		private float rate_;
		private float max_;
		private SensitivityModifier[] modifiers_ = new SensitivityModifier[0];


		public Sensitivity(int type, float rate, float max, SensitivityModifier[] mods)
		{
			type_ = type;
			rate_ = rate;
			max_ = max;
			modifiers_ = mods ?? new SensitivityModifier[0];
		}

		public int Type
		{
			get { return type_; }
		}

		public float Rate
		{
			get { return rate_; }
		}

		public float Maximum
		{
			get { return max_; }
		}

		public SensitivityModifier[] Modifiers
		{
			get { return modifiers_; }
		}

		public Sensitivity Clone()
		{
			var s = new Sensitivity(type_, rate_, max_, null);
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

		public float GetModifier(
			Person self, int sourcePersonIndex, int sourceBodyPart)
		{
			for (int i = 0; i < modifiers_.Length; ++i)
			{
				var m = modifiers_[i];

				if (m.AppliesTo(self, sourcePersonIndex, sourceBodyPart))
					return m.Modifier;
			}

			return 0;
		}

		public override string ToString()
		{
			return $"{SS.ToString(type_)} rate={rate_:0.00000} max={max_:0.00}";
		}
	}


	public class Sensitivities
	{
		private Person person_ = null;
		private Sensitivity[] s_ = new Sensitivity[SS.Count];

		public Sensitivities()
		{
			for (int i = 0; i < s_.Length; ++i)
				s_[i] = new Sensitivity(i, 0, 0, null);
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

		public float GetModifier(
			int type, int sourcePersonIndex, int sourceBodyPart)
		{
			return Get(type).GetModifier(
				person_, sourcePersonIndex, sourceBodyPart);
		}

		public Sensitivity Get(int type)
		{
			return s_[type];
		}
	}


	public class Personality : EnumValueManager
	{
		private readonly string name_;
		private Person person_ = null;

		private Voice voice_;
		private Expression[] exps_ = new Expression[0];
		private Sensitivities sensitivities_;

		public Personality(string name)
			: base(new PS())
		{
			name_ = name;
			voice_ = new Voice();
			sensitivities_ = new Sensitivities();
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

			exps_ = new Expression[ps.exps_.Length];
			for (int i = 0; i < ps.exps_.Length; i++)
				exps_[i] = ps.exps_[i].Clone();

			voice_ = ps.voice_.Clone(person_);
			sensitivities_ = ps.sensitivities_.Clone(person_);
		}

		public void Init()
		{
			sensitivities_.Init();
		}

		public void SetExpressions(Expression[] exps)
		{
			exps_ = exps;
		}

		public void Load(JSONClass o)
		{
			if (o.HasKey("voice"))
			{
				var vo = o["voice"].AsObject;

				if (vo.HasKey("pitch"))
					voice_.ForcePitch(vo["pitch"].AsFloat);
			}
		}

		public JSONNode ToJSON()
		{
			var o = new JSONClass();

			if (name_ != Resources.DefaultPersonality)
				o.Add("name", name_);

			if (voice_.ForcedPitch >= 0)
			{
				var v = new JSONClass();
				v.Add("pitch", new JSONData(voice_.ForcedPitch));
				o.Add("voice", v);
			}

			return o;
		}

		public string Name
		{
			get { return name_; }
		}

		public Voice Voice
		{
			get { return voice_; }
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

		public override string ToString()
		{
			return $"{Name}";
		}
	}
}
