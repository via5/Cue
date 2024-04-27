using System;
using System.Collections.Generic;

namespace Cue
{
	public class Morph
	{
		public const float NoEyesClosed = -9999;

		public static bool HasEyesClosed(float f)
		{
			return (f > -1000);
		}


		private Person person_;
		private Sys.IMorph m_;
		private BodyPartType[] bodyParts_ = null;

		public Morph(Person p, string id, BodyPartType bodyPart, float eyesClosed = NoEyesClosed)
			: this(p, p.Atom.GetMorph(id, eyesClosed), bodyPart)
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
			if (m_ == null)
				return "no morph";

			return m_.ToString();
		}
	}


	public class MorphGroup
	{
		private const float NoValue = -9999;

		public class MorphInfo
		{
			private string id_;
			private float min_, max_;
			private BodyPartType bodyPart_;
			private float eyesClosed_;
			private Morph m_ = null;

			public MorphInfo(string id, float min, float max)
				: this(id, min, max, BP.None, Morph.NoEyesClosed)
			{
			}

			public MorphInfo(string id, float min, float max, BodyPartType bodyPart, float eyesClosed)
			{
				id_ = id;
				min_ = min;
				max_ = max;
				bodyPart_ = bodyPart;
				eyesClosed_ = eyesClosed;
			}

			public Morph Morph
			{
				get { return m_; }
			}

			public MorphInfo Clone()
			{
				return new MorphInfo(id_, min_, max_, bodyPart_, eyesClosed_);
			}

			public bool Init(Person p)
			{
				m_ = new Morph(p, id_, bodyPart_, eyesClosed_);
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

			public void MoveTowards(float start, float end, float t)
			{
				if (m_.Sys == null)
					return;

				float range = max_ - min_;
				float realStart, realEnd;

				if (start == NoValue)
					realStart = m_.Sys.DefaultValue;
				else
					realStart = min_ + start * range;

				if (end == NoValue)
					realEnd = m_.Sys.DefaultValue;
				else
					realEnd = min_ + end * range;

				float v = U.Lerp(realStart, realEnd, t);
				m_.Sys.Value = v;
			}

			public bool MoveTowardsReset()
			{
				const float MaxMove = 0.02f;
				const float Tolerance = 0.01f;

				if (m_.Sys == null)
					return true;

				float def = m_.DefaultValue;
				float val = m_.Sys.Value;

				if (val > def)
				{
					float m = Math.Min(val - def, MaxMove);
					val -= m;
				}
				else if (val < def)
				{
					float m = Math.Min(def - val, MaxMove);
					val += m;
				}

				m_.Sys.Value = val;

				float d = Math.Abs(m_.Sys.Value - m_.Sys.DefaultValue);

				return (d <= Tolerance);

			}

			public void Reset()
			{
				m_?.Reset();
			}

			public override string ToString()
			{
				if (m_ == null)
					return id_ + " (nomorph)";
				else
					return m_.ToString();
			}
		}


		private Person person_ = null;
		private string name_;
		private MorphInfo[] morphs_;
		private BodyPartType[] bodyParts_ = null;
		private float value_ = NoValue;

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
			get
			{
				return value_;
			}

			set
			{
				SetValue(value);
			}
		}

		public void MoveTowards(float start, float end, float t)
		{
			// todo: assumes default of 0
			float estimated;
			if (start == NoValue && end == NoValue)
				estimated = 0;
			else if (start == NoValue)
				estimated = end;
			else if (end == NoValue)
				estimated = 0;
			else
				estimated = U.Lerp(start, end, t);

			value_ = UseValue(estimated);

			for (int i = 0; i < morphs_.Length; ++i)
				morphs_[i].MoveTowards(start, value_, t);
		}

		public bool MoveTowardsReset()
		{
			value_ = NoValue;

			bool finished = true;
			for (int i = 0; i < morphs_.Length; ++i)
			{
				if (!morphs_[i].MoveTowardsReset())
					finished = false;
			}

			return finished;
		}

		private void SetValue(float requestedValue)
		{
			value_ = UseValue(requestedValue);

			for (int i = 0; i < morphs_.Length; ++i)
				morphs_[i].Set(value_);
		}

		private float UseValue(float requestedValue)
		{
			// todo: assumes default of 0
			float use = Math.Abs(requestedValue);
			float available = person_.Body.UseMorphs(bodyParts_, use);

			if (requestedValue < 0)
				return -available;
			else
				return available;
		}

		public BodyPartType[] BodyParts
		{
			get { return bodyParts_; }
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

		public void Debug(DebugLines debug)
		{
			debug.Add(ToDetailedString());

			for (int i = 0; i < morphs_.Length; ++i)
				debug.Add("    " + morphs_[i].ToString());
		}

		public string ToDetailedString()
		{
			string s = "";

			for (int i = 0; i < bodyParts_.Length; ++i)
			{
				if (s.Length > 0)
					s += ",";

				s += bodyParts_[i].ToString();
			}

			if (s == "")
				s = "none";

			return name_ + ", bp:" + s;
		}

		private static BodyPartType[] FixedBodyParts(BodyPartType[] bodyParts)
		{
			if (bodyParts != null)
			{
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

				if (fixedBodyParts != null)
					return fixedBodyParts.ToArray();
			}

			return new BodyPartType[0];
		}
	}
}
