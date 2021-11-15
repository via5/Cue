using System;

namespace Cue
{
	class ExcitementReason
	{
		public class Source
		{
			public int bodyPart;
			public BasicEnumValues.FloatIndex ps;

			public float lastValue = 0;
			public float lastMod = 0;
			public bool allIgnored = false;
			public bool someIgnored = false;
			public bool guess = false;

			public Source(int bodyPart, BasicEnumValues.FloatIndex ps)
			{
				this.bodyPart = bodyPart;
				this.ps = ps;
			}
		}

		public const int Mouth = 0;
		public const int Breasts = 1;
		public const int Genitals = 2;
		public const int Penetration = 3;
		public const int OtherSex = 4;
		public const int Count = 5;

		private int type_;
		private Source[] sources_;
		private string name_;
		private float value_ = 0;
		private float rate_ = 0;
		private float specificSensitivityModifier_ = 0;
		private float globalSensitivityRate_ = 0;
		private float sensitivityMax_ = 0;

		private static string[] names_ = new string[]
		{
			"mouth", "breasts", "genitals", "penetration", "others"
		};

		public ExcitementReason(int type, Source[] sources)
		{
			type_ = type;
			sources_ = sources;
			name_ = ToString(type);
		}

		private static string ToString(int type)
		{
			return names_[type];
		}

		public string Name
		{
			get { return name_; }
		}

		public bool Physical
		{
			get
			{
				// todo
				return (type_ != OtherSex);
			}
		}

		public float Value
		{
			get { return value_; }
			set { value_ = value; }
		}

		public float Rate
		{
			get { return rate_; }
			set { rate_ = value; }
		}

		public float SpecificSensitivityModifier
		{
			get { return specificSensitivityModifier_; }
			set { specificSensitivityModifier_ = value; }
		}

		public float GlobalSensitivityRate
		{
			get { return globalSensitivityRate_; }
			set { globalSensitivityRate_ = value; }
		}

		public float SensitivityMax
		{
			get { return sensitivityMax_; }
			set { sensitivityMax_ = value; }
		}

		public void Update(Person p, ExcitementBodyPart[] parts)
		{
			if (type_ == OtherSex)
			{
				value_ = 0;
				specificSensitivityModifier_ = 1;

				foreach (var op in Cue.Instance.ActivePersons)
				{
					if (op == p || op.Body.HavingSexWith(p))
						continue;

					// todo, missing in debug
					//if (op.Excitement.physicalRate_.Value > 0)
					//	debug_.Add($"  other physical: {p.ID}@{p.Excitement.physicalRate_.Value}");

					value_ += op.Excitement.PhysicalRate;
				}
			}
			else
			{
				value_ = 0;
				specificSensitivityModifier_ = 0;

				for (int i = 0; i < sources_.Length; ++i)
				{
					value_ += GetValue(p, parts, sources_[i]);
					specificSensitivityModifier_ += parts[sources_[i].bodyPart].SpecificModifier;
				}

				if (specificSensitivityModifier_ == 0)
					specificSensitivityModifier_ = 1;
			}
		}

		private float GetValue(Person p, ExcitementBodyPart[] parts, Source src)
		{
			src.lastValue = 0;
			src.lastMod = 0;
			src.allIgnored = false;
			src.someIgnored = false;
			src.guess = false;

			if (src.bodyPart == BP.Vagina)
			{
				return GetFixedPenetrationValue(p, parts, src);
			}
			else
			{
				float value = parts[src.bodyPart].Value;

				if (type_ == Genitals && value > 0)
				{
					if (parts[src.bodyPart].FromPenisValue > 0)
					{
						value -= parts[src.bodyPart].FromPenisValue;

						if (Math.Abs(value) < 0.001f)
						{
							src.allIgnored = true;
							value = 0;
						}
						else
						{
							src.someIgnored = true;
						}
					}
				}

				float mod = p.Personality.Get(src.ps);

				src.lastValue = value;
				src.lastMod = mod;

				return value * mod;
			}
		}

		private float GetFixedPenetrationValue(
			Person p, ExcitementBodyPart[] parts, Source src)
		{
			var ps = p.Personality;

			if (parts[BP.Vagina].Value > 0)
			{
				src.lastValue = parts[BP.Vagina].Value;
				src.lastMod = ps.Get(PS.VaginaFactor);

				return src.lastValue * src.lastMod;
			}

			if (parts[BP.Penis].Value > 0)
			{
				// todo
				src.lastValue = parts[BP.Penis].Value;
				src.lastMod = ps.Get(PS.VaginaFactor);

				return src.lastValue * src.lastMod;
			}

			// if penetration is shallow, the vagina trigger doesn't activate
			// at all, it seems rather deep
			//
			// if the labia is triggered by a genital, assume penetration
			if (parts[BP.Labia].FromPenisValue > 0)
			{
				src.lastValue = parts[BP.Labia].Value;
				src.lastMod = ps.Get(PS.VaginaFactor);
				src.guess = true;

				return src.lastValue * src.lastMod;
			}

			return 0;
		}

		public string Debug(Person p, ExcitementBodyPart[] parts)
		{
			string s = null;

			for (int i=0; i<sources_.Length;++i)
			{
				var src = sources_[i];

				if (parts[src.bodyPart].Value > 0)
				{
					if (s == null)
						s = "";
					else
						s += " ";

					var f = p.Personality.Get(src.ps);
					float sm = parts[src.bodyPart].SpecificModifier;
					if (sm == 0)
						sm = 1;

					s +=
						$"{BP.ToString(src.bodyPart)}=" +
						$"{src.lastValue:0.0}*" +
						$"{src.lastMod}*" +
						$"{sm}";

					if (src.allIgnored)
						s += "(ignored)";
					else if (src.someIgnored)
						s += "(some ignored)";

					if (src.guess)
						s += "(guess)";
				}
			}

			if (s == null)
				return null;

			return $"{Name}: " + s;
		}

		public override string ToString()
		{
			if (rate_ == 0)
				return "0";

			return
				$"{value_:0.00}=" +
				$"{rate_:0.000000}";
		}
	}
}
