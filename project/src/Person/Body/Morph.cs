using System;
using System.Collections.Generic;

namespace Cue
{
	public class Morph
	{
		private Person person_;
		private Sys.IMorph m_;
		private BodyPartType[] bodyParts_ = null;

		public Morph(Person p, string id, BodyPartType bodyPart)
			: this(p, p.Atom.GetMorph(id), bodyPart)
		{
		}

		public Morph(Person p, Sys.IMorph m, BodyPartType bodyPart)
		{
			person_ = p;
			m_ = m;

			if (bodyPart != BP.None)
				bodyParts_ = new BodyPartType[] { bodyPart };
		}

		public bool Init()
		{
			return (m_ != null) && m_.Valid;
		}

		public string Name
		{
			get { return m_?.Name ?? "?"; }
		}

		public float Value
		{
			get { return m_?.Value ?? 0; }
			set { SetValue(value); }
		}

		public Sys.IMorph Sys
		{
			get { return m_; }
		}

		private void SetValue(float originalValue)
		{
			if (m_ == null)
				return;

			float use = Math.Abs(originalValue - m_.DefaultValue);
			float available = person_.Body.UseMorphs(bodyParts_, use);

			float fixedValue;
			if (originalValue < m_.DefaultValue)
				fixedValue = m_.DefaultValue  - available;
			else
				fixedValue = m_.DefaultValue + available;

			m_.Value = fixedValue;
		}

		public float DefaultValue
		{
			get { return m_?.DefaultValue ?? 0; }
		}

		public bool LimiterEnabled
		{
			set
			{
				if (m_ != null)
					m_.LimiterEnabled = value;
			}
		}

		public void Reset()
		{
			m_?.Reset();
		}

		public override string ToString()
		{
			return m_?.ToString() ?? "no morph";
		}
	}


	public class MorphGroup
	{
		public class MorphInfo
		{
			private string id_;
			private float min_, max_;
			private BodyPartType bodyPart_;
			private Morph m_ = null;

			public MorphInfo(string id, float min, float max, BodyPartType bodyPart)
			{
				id_ = id;
				min_ = min;
				max_ = max;
				bodyPart_ = bodyPart;
			}

			public Morph Morph
			{
				get { return m_; }
			}

			public MorphInfo Clone()
			{
				return new MorphInfo(id_, min_, max_, bodyPart_);
			}

			public bool Init(Person p)
			{
				m_ = new Morph(p, id_, bodyPart_);
				return m_.Init();
			}

			public void Set(float v)
			{
				if (m_.Sys != null)
				{
					float range = max_ - min_;
					m_.Sys.Value = min_ + v * range;
				}
			}

			public void Reset()
			{
				m_?.Reset();
			}
		}


		private Person person_ = null;
		private string name_;
		private MorphInfo[] morphs_;
		private BodyPartType[] bodyParts_ = null;
		private float value_ = 0;

		public MorphGroup(string name, BodyPartType bodyPart, MorphInfo[] morphs)
			: this(name, new BodyPartType[] { bodyPart }, morphs)
		{
		}

		public MorphGroup(string name, BodyPartType[] bodyParts, MorphInfo[] morphs)
			: this(name)
		{
			morphs_ = morphs;
			bodyParts_ = FixedBodyParts(bodyParts);
		}

		private MorphGroup(string name)
		{
			name_ = name;
		}

		public MorphGroup Clone()
		{
			var g = new MorphGroup(name_);
			g.CopyFrom(this);
			return g;
		}

		private void CopyFrom(MorphGroup g)
		{
			morphs_ = new MorphInfo[g.morphs_.Length];
			for (int i = 0; i < g.morphs_.Length; ++i)
				morphs_[i] = g.morphs_[i].Clone();

			bodyParts_ = new BodyPartType[g.bodyParts_.Length];
			g.bodyParts_.CopyTo(bodyParts_, 0);
		}

		public bool Init(Person p)
		{
			person_ = p;

			for (int i = 0; i < morphs_.Length; ++i)
			{
				if (!morphs_[i].Init(p))
					return false;
			}

			return true;
		}

		public bool AffectsAnyBodyPart(BodyPartType[] bodyParts)
		{
			for (int i = 0; i < bodyParts.Length; ++i)
			{
				for (int j = 0; j < bodyParts_.Length; ++j)
				{
					if (bodyParts[i] == bodyParts_[j])
						return true;
				}
			}

			return false;
		}

		public string Name
		{
			get { return name_; }
		}

		public float Value
		{
			get { return value_; }
			set { SetValue(value); }
		}

		public BodyPartType[] BodyParts
		{
			get { return bodyParts_; }
		}

		private void SetValue(float requestedValue)
		{
			// assume default of 0
			float use = Math.Abs(requestedValue);
			float available = person_.Body.UseMorphs(bodyParts_, use);

			float allowedValue;
			if (requestedValue < 0)
				allowedValue = -available;
			else
				allowedValue = available;

			value_ = allowedValue;

			for (int i = 0; i < morphs_.Length; ++i)
				morphs_[i].Set(value_);
		}

		public void Reset()
		{
			for (int i = 0; i < morphs_.Length; ++i)
				morphs_[i].Reset();
		}

		public override string ToString()
		{
			return name_;
		}

		private static BodyPartType[] FixedBodyParts(BodyPartType[] bodyParts)
		{
			if (bodyParts == null)
				return null;

			List<BodyPartType> fixedBodyParts = null;

			for (int i = 0; i < bodyParts.Length; ++i)
			{
				if (bodyParts[i] != BP.None)
				{
					if (fixedBodyParts == null)
						fixedBodyParts = new List<BodyPartType>();

					fixedBodyParts.Add(bodyParts[i]);
				}
			}

			if (fixedBodyParts == null)
				return null;

			return fixedBodyParts.ToArray();
		}
	}
}
