using System;

namespace Cue
{
	interface IExcitementReason
	{
		string Name { get; }
		bool Physical { get; }

		float Value { get; }
		float GlobalSensitivityRate { get; }
		float SpecificSensitivityModifier { get; }
		float DampenModifier { get; }
		float Rate { get; }
		float MaximumExcitement { get; }

		void UpdateValue(Person p, ExcitementBodyPart[] parts);
		void UpdateRate(Person p, bool isPenetrated);
		string Debug(Person p, ExcitementBodyPart[] parts);
	}


	abstract class BasicExcitementReason : IExcitementReason
	{
		private BasicEnumValues.FloatIndex rateIndex_;
		private BasicEnumValues.FloatIndex maxIndex_;
		private bool physical_;
		private string name_;
		protected float value_ = 0;
		protected float specificSensitivityModifier_ = 0;
		private float globalSensitivityRate_ = 0;
		private float dampen_ = 0;
		private float sensitivityMax_ = 0;
		private float rate_ = 0;

		protected BasicExcitementReason(
			string name,
			BasicEnumValues.FloatIndex rateIndex,
			BasicEnumValues.FloatIndex maxIndex,
			bool physical)
		{
			name_ = name;
			rateIndex_ = rateIndex;
			maxIndex_ = maxIndex;
			physical_ = physical;
		}

		public string Name
		{
			get { return name_; }
		}

		public bool Physical
		{
			get { return physical_; }
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

		public float DampenModifier
		{
			get { return dampen_; }
			set { dampen_ = value; }
		}

		public float MaximumExcitement
		{
			get { return sensitivityMax_; }
			set { sensitivityMax_ = value; }
		}

		public void UpdateValue(Person p, ExcitementBodyPart[] parts)
		{
			value_ = 0;
			specificSensitivityModifier_ = 0;

			DoUpdateValue(p, parts);
		}

		protected abstract void DoUpdateValue(Person p, ExcitementBodyPart[] parts);

		public void UpdateRate(Person p, bool isPenetrated)
		{
			GlobalSensitivityRate = p.Personality.Get(rateIndex_);
			MaximumExcitement = p.Personality.Get(maxIndex_);
			DampenModifier = 1;

			DoUpdateRate(p, isPenetrated);

			Rate =
				Value *
				GlobalSensitivityRate *
				SpecificSensitivityModifier *
				DampenModifier;
		}

		protected abstract void DoUpdateRate(Person p, bool isPenetrated);

		protected abstract string DoDebug(Person p, ExcitementBodyPart[] parts);

		public string Debug(Person p, ExcitementBodyPart[] parts)
		{
			string s = DoDebug(p, parts);

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


	class BodyPartExcitementReason : BasicExcitementReason
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

		private Source[] sources_;
		private bool ignoreFromPenis_;
		private bool dampenedByPenetration_;

		public BodyPartExcitementReason(
			string name, Source[] sources,
			BasicEnumValues.FloatIndex rateIndex,
			BasicEnumValues.FloatIndex maxIndex,
			bool ignoreFromPenis, bool dampenedByPenetration)
				: base(name, rateIndex, maxIndex, true)
		{
			sources_ = sources;
			ignoreFromPenis_ = ignoreFromPenis;
			dampenedByPenetration_ = dampenedByPenetration;
		}

		protected override void DoUpdateValue(Person p, ExcitementBodyPart[] parts)
		{
			for (int i = 0; i < sources_.Length; ++i)
			{
				value_ += GetValue(p, parts, sources_[i]);
				specificSensitivityModifier_ += parts[sources_[i].bodyPart].SpecificModifier;
			}

			if (specificSensitivityModifier_ == 0)
				specificSensitivityModifier_ = 1;
		}

		protected override void DoUpdateRate(Person p, bool isPenetrated)
		{
			if (dampenedByPenetration_)
			{
				if (isPenetrated)
					DampenModifier = p.Personality.Get(PS.PenetrationDamper);
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

				if (ignoreFromPenis_ && value > 0)
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

		protected override string DoDebug(Person p, ExcitementBodyPart[] parts)
		{
			string s = null;

			for (int i = 0; i < sources_.Length; ++i)
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

			return s;
		}
	}


	class OtherSexExcitementReason : BasicExcitementReason
	{
		public OtherSexExcitementReason()
			: base("otherSex", PS.OtherSexExcitementRateFactor, PS.MaxOtherSexExcitement, false)
		{
		}

		protected override void DoUpdateValue(Person p, ExcitementBodyPart[] parts)
		{
			value_ = 0;
			specificSensitivityModifier_ = 1;

			foreach (var op in Cue.Instance.ActivePersons)
			{
				if (op == p || op.Status.PenetratedBy(p) || p.Status.PenetratedBy(op))
					continue;

				// todo, missing in debug
				//if (op.Excitement.physicalRate_.Value > 0)
				//	debug_.Add($"  other physical: {p.ID}@{p.Excitement.physicalRate_.Value}");

				value_ += op.Excitement.PhysicalRate;
			}
		}

		protected override void DoUpdateRate(Person p, bool isPenetrated)
		{
			// no-op
		}

		protected override string DoDebug(Person p, ExcitementBodyPart[] parts)
		{
			return null;
		}
	}
}
