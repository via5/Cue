using SimpleJSON;
using System.Collections.Generic;

namespace Cue
{
	class Voice
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


	class Personality : EnumValueManager
	{
		public struct SpecificModifier
		{
			public int bodyPart;
			public int sourceBodyPart;
			public float modifier;

			public override string ToString()
			{
				return
					$"{BP.ToString(bodyPart)}=>" +
					$"{BP.ToString(sourceBodyPart)}   " +
					$"{modifier}";
			}
		}

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
				specificModifiers_[i] = ps.specificModifiers_[i];
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

		public float GetSpecificModifier(int part, int sourcePart)
		{
			for (int i = 0; i < specificModifiers_.Length; ++i)
			{
				var sm = specificModifiers_[i];
				if (sm.bodyPart == part && sm.sourceBodyPart == sourcePart)
					return sm.modifier;
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
