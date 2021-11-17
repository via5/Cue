using System;
using System.Collections.Generic;

namespace Cue
{
	public class Morph
	{
		private Person person_;
		private Sys.IMorph m_;
		private int[] bodyParts_ = null;

		public Morph(Person p, string id, int bodyPart)
			: this(p, p.Atom.GetMorph(id), bodyPart)
		{
		}

		public Morph(Person p, Sys.IMorph m, int bodyPart)
		{
			person_ = p;
			m_ = m;

			if (bodyPart != BP.None)
				bodyParts_ = new int[] { bodyPart };
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
			private float multiplier_;
			private int bodyPart_;
			private Morph m_ = null;

			public MorphInfo(string id, float multiplier, int bodyPart)
			{
				id_ = id;
				multiplier_ = multiplier;
				bodyPart_ = bodyPart;
			}

			public MorphInfo Clone()
			{
				return new MorphInfo(id_, multiplier_, bodyPart_);
			}

			public void Init(Person p)
			{
				m_ = new Morph(p, id_, bodyPart_);
			}

			public void Set(float v)
			{
				if (m_.Sys != null)
					m_.Sys.Value = v * multiplier_;
			}

			public void Reset()
			{
				m_?.Reset();
			}
		}


		private Person person_ = null;
		private string name_;
		private MorphInfo[] morphs_;
		private int[] bodyParts_ = null;
		private float value_ = 0;

		public MorphGroup(string name, int bodyPart, MorphInfo[] morphs)
			: this(name, new int[] { bodyPart }, morphs)
		{
		}

		public MorphGroup(string name, int[] bodyParts, MorphInfo[] morphs)
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

			bodyParts_ = new int[g.bodyParts_.Length];
			g.bodyParts_.CopyTo(bodyParts_, 0);
		}

		public void Init(Person p)
		{
			person_ = p;

			for (int i = 0; i < morphs_.Length; ++i)
				morphs_[i].Init(p);
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

		private static int[] FixedBodyParts(int[] bodyParts)
		{
			if (bodyParts == null)
				return null;

			List<int> fixedBodyParts = null;

			for (int i = 0; i < bodyParts.Length; ++i)
			{
				if (bodyParts[i] != BP.None)
				{
					if (fixedBodyParts == null)
						fixedBodyParts = new List<int>();

					fixedBodyParts.Add(bodyParts[i]);
				}
			}

			if (fixedBodyParts == null)
				return null;

			return fixedBodyParts.ToArray();
		}
	}
}
