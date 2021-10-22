using System;
using System.Collections.Generic;

namespace Cue
{
	class Morph
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

		private void SetValue(float value)
		{
			if (m_ == null)
				return;

			float use = Math.Abs(value - m_.DefaultValue);
			float available = person_.Body.UseMorphs(bodyParts_, use);

			if (value < 0)
				value = -available;
			else
				value = available;

			m_.Value = value;
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


	class MorphGroup
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

			public void Start(Person p)
			{
				m_ = new Morph(p, id_, bodyPart_);
			}

			public void Set(float v)
			{
				m_.Value = v * multiplier_;
			}

			public void Reset()
			{
				m_?.Reset();
			}
		}


		private Person person_;
		private string name_;
		private MorphInfo[] morphs_;
		private int[] bodyParts_ = null;
		private float value_ = 0;

		public MorphGroup(Person p, string name, int bodyPart, MorphInfo[] morphs)
			: this(p, name, new int[] { bodyPart }, morphs)
		{
		}

		public MorphGroup(Person p, string name, int[] bodyParts, MorphInfo[] morphs)
		{
			person_ = p;
			name_ = name;
			morphs_ = morphs;
			bodyParts_ = FixedBodyParts(bodyParts);

			for (int i = 0; i < morphs_.Length; ++i)
				morphs_[i].Start(p);
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

		private void SetValue(float value)
		{
			// assume default of 0
			float use = Math.Abs(value);
			float available = person_.Body.UseMorphs(bodyParts_, use);

			if (value < 0)
				value = -available;
			else
				value = available;

			value_ = value;

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
