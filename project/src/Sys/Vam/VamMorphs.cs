using System;
using System.Collections.Generic;

namespace Cue.Sys.Vam
{
	static class VamMorphManager
	{
		public class MorphInfo
		{
			private string id_;
			private DAZMorph m_;
			private List<MorphInfo> subMorphs_ = new List<MorphInfo>();
			private bool free_ = true;
			private int freeFrame_ = -1;
			private float multiplier_ = 1;

			public MorphInfo(VamAtom atom, string morphId, DAZMorph m)
			{
				id_ = morphId;
				m_ = m;

				if (m_ != null && (m_.deltas == null || m_.deltas.Length == 0))
				{
					foreach (var sm in m_.formulas)
					{
						if (sm.targetType == DAZMorphFormulaTargetType.MorphValue)
						{
							var smm = Get(atom, sm.target, m_.morphBank);
							smm.multiplier_ = sm.multiplier;
							subMorphs_.Add(smm);
						}
						else
						{
							subMorphs_.Clear();
							break;
						}
					}

					if (subMorphs_.Count > 0)
						m_.Reset();
				}
			}

			public override string ToString()
			{
				string s = id_ + " ";

				if (m_ == null)
					s += "notfound";
				else
					s += $"v={m_.morphValue:0.00} sub={subMorphs_.Count != 0} f={free_} ff={freeFrame_}";

				return s;
			}

			public string ID
			{
				get { return id_; }
			}

			public float Value
			{
				get { return m_?.morphValue ?? -1; }
			}

			public float DefaultValue
			{
				get { return m_?.startValue ?? 0; }
			}

			public bool Set(float f)
			{
				if (m_ == null)
					return false;

				if (free_ || freeFrame_ != Cue.Instance.Frame)
				{
					if (subMorphs_.Count == 0)
					{
						if (f > m_.morphValue)
							m_.morphValue = Math.Min(m_.morphValue + 0.02f, f);
						else
							m_.morphValue = Math.Max(m_.morphValue - 0.02f, f);
					}
					else
					{
						for (int i = 0; i < subMorphs_.Count; ++i)
						{
							float smf = f * subMorphs_[i].multiplier_;
							subMorphs_[i].Set(smf);
						}
					}

					free_ = false;
					freeFrame_ = Cue.Instance.Frame;

					return true;
				}

				return false;
			}

			public void Reset()
			{
				if (m_ != null)
					m_.morphValue = m_.startValue;
			}
		}

		private static Dictionary<string, MorphInfo> map_ =
			new Dictionary<string, MorphInfo>();

		public static MorphInfo Get(VamAtom atom, string morphId, DAZMorphBank bank = null)
		{
			string key = atom.ID + "/" + morphId;

			MorphInfo mi;
			if (map_.TryGetValue(key, out mi))
				return mi;

			DAZMorph m;

			if (bank == null)
				m = Cue.Instance.VamSys.FindMorph(atom.Atom, morphId);
			else
				m = bank.GetMorph(morphId);

			if (m == null)
				Cue.LogError($"{atom.ID}: morph '{morphId}' not found");

			mi = new MorphInfo(atom, morphId, m);
			map_.Add(key, mi);

			return mi;
		}
	}


	class VamMorph : IMorph
	{
		private VamAtom atom_;
		private string name_;
		private VamMorphManager.MorphInfo morph_ = null;
		private bool inited_ = false;

		public VamMorph(VamAtom a, string name)
		{
			atom_ = a;
			name_ = name;
		}

		public string Name
		{
			get { return name_; }
		}

		public float Value
		{
			get
			{
				GetMorph();
				return morph_?.Value ?? 0;
			}

			set
			{
				GetMorph();
				if (morph_ != null)
					morph_.Set(value);
			}
		}

		public float DefaultValue
		{
			get
			{
				GetMorph();
				return morph_?.DefaultValue ?? 0;
			}
		}

		public void Reset()
		{
			GetMorph();
			morph_?.Reset();
		}

		private void GetMorph()
		{
			if (inited_)
				return;

			morph_ = VamMorphManager.Get(atom_, name_);
			if (morph_ == null)
				atom_.Log.Error($"no morph '{name_}'");

			inited_ = true;
		}

		public override string ToString()
		{
			return $"{morph_}";
		}
	}
}
