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


		private Person person_;
		private DatasetForIntensity[] datasets_ = new DatasetForIntensity[0];
		private Dataset orgasm_ = new Dataset("", -1);
		private Dataset dummy_ = new Dataset("", -1);
		private bool warned_ = false;
		private float forcedPitch_ = -1;


		public Voice(Person p)
		{
			person_ = p;
		}

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

		public void CopyFrom(Voice v)
		{
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


	public class SpecificModifier
	{
		public const int Unresolved = -1;
		public const int Any = -2;
		public const int Player = -3;
		public const int Self = -4;

		private string source_;
		private int sourceIndex_ = Unresolved;
		private int sourceBodyPart_;

		private string target_;
		private int targetIndex_ = Unresolved;
		private int targetBodyPart_;

		private float modifier_;


		public SpecificModifier(
			string source, int sourceBodyPart,
			string target, int targetBodyPart,
			float modifier)
		{
			source_ = source;
			sourceBodyPart_ = sourceBodyPart;
			target_ = target;
			targetBodyPart_ = targetBodyPart;
			modifier_ = modifier;
		}

		public float Modifier
		{
			get { return modifier_; }
		}

		public SpecificModifier Clone()
		{
			return new SpecificModifier(
				source_, sourceBodyPart_, target_, targetBodyPart_, modifier_);
		}

		public bool AppliesTo(
			Person self,
			int sourcePersonIndex, int sourceBodyPart,
			int targetPersonIndex, int targetBodyPart)
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


			if (targetIndex_ == Player)
			{
				if (targetPersonIndex != Cue.Instance.Player.PersonIndex)
					return false;
			}
			else if (targetIndex_ == Self)
			{
				if (targetPersonIndex != self.PersonIndex)
					return false;
			}
			else if (targetIndex_ != Any)
			{
				if (targetIndex_ != targetPersonIndex)
					return false;
			}


			if (sourceBodyPart_ != BP.None && sourceBodyPart_ != sourceBodyPart)
				return false;

			if (targetBodyPart_ != BP.None && targetBodyPart_ != targetBodyPart)
				return false;


			return true;
		}

		public override string ToString()
		{
			return
				$"{ToString(sourceIndex_, sourceBodyPart_)}=>" +
				$"{ToString(targetIndex_, targetBodyPart_)}    {modifier_}";
		}

		public void Resolve()
		{
			if (sourceIndex_ == Unresolved)
				sourceIndex_ = Resolve(source_);

			if (targetIndex_ == Unresolved)
				targetIndex_ = Resolve(target_);
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


	public class Personality : EnumValueManager
	{
		private readonly string name_;
		private Person person_;

		private Voice voice_;
		private Expression[] exps_ = new Expression[0];
		private SpecificModifier[] specificModifiers_ = new SpecificModifier[0];

		public Personality(string name, Person p = null)
			: base(new PS())
		{
			voice_ = new Voice(p);
			name_ = name;
			person_ = p;
		}

		public Personality Clone(string newName, Person p)
		{
			var ps = new Personality(newName ?? name_, p);
			ps.CopyFrom(this);
			return ps;
		}

		private void CopyFrom(Personality ps)
		{
			base.CopyFrom(ps);

			exps_ = new Expression[ps.exps_.Length];
			for (int i = 0; i < ps.exps_.Length; i++)
				exps_[i] = ps.exps_[i].Clone();

			voice_.CopyFrom(ps.voice_);

			specificModifiers_ = new SpecificModifier[ps.specificModifiers_.Length];
			for (int i = 0; i < ps.specificModifiers_.Length; ++i)
				specificModifiers_[i] = ps.specificModifiers_[i].Clone();
		}

		public void Init()
		{
			for (int i = 0; i < specificModifiers_.Length; ++i)
				specificModifiers_[i].Resolve();
		}

		public void SetExpressions(Expression[] exps)
		{
			exps_ = exps;
		}

		public void SetSpecificModifiers(SpecificModifier[] sms)
		{
			specificModifiers_ = sms;
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

		public Expression[] GetExpressions()
		{
			var e = new Expression[exps_.Length];
			exps_.CopyTo(e, 0);
			return e;
		}

		public float GetSpecificModifier(
			int sourcePersonIndex, int sourceBodyPart,
			int targetPersonIndex, int targetBodyPart)
		{
			for (int i = 0; i < specificModifiers_.Length; ++i)
			{
				var sm = specificModifiers_[i];

				if (sm.AppliesTo(
						person_,
						sourcePersonIndex, sourceBodyPart,
						targetPersonIndex, targetBodyPart))
				{
					return sm.Modifier;
				}
			}

			return 0;
		}

		public SpecificModifier[] SpecificModifiers
		{
			get { return specificModifiers_; }
		}

		public override string ToString()
		{
			return $"{Name}";
		}
	}
}
