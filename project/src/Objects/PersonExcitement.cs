using System;

namespace Cue
{
	class Excitement
	{
		private Person person_;
		private float[] parts_ = new float[BodyParts.Count];
		private float decay_ = 1;

		private float flatExcitement_ = 0;
		private float forcedExcitement_ = -1;
		private float mouthRate_ = 0;
		private float breastsRate_ = 0;
		private float genitalsRate_ = 0;
		private float penetrationRate_ = 0;
		private float totalRate_ = 0;
		private float max_ = 0;
		private bool postOrgasm_ = false;
		private float postOrgasmElapsed_ = 0;

		private IEasing easing_ = new CubicOutEasing();


		public Excitement(Person p)
		{
			person_ = p;
		}

		public string StateString
		{
			get
			{
				if (postOrgasm_)
					return "post orgasm";
				else
					return "none";
			}
		}

		public float Value
		{
			get
			{
				if (forcedExcitement_ >= 0)
					return forcedExcitement_;
				else
					return easing_.Magnitude(flatExcitement_);
			}
		}

		public void ForceValue(float s)
		{
			forcedExcitement_ = s;
		}

		public void Update(float s)
		{
			var ss = person_.Personality.Sensitivity;

			if (postOrgasm_)
			{
				postOrgasmElapsed_ += s;
				if (postOrgasmElapsed_ < ss.DelayPostOrgasm)
					return;

				postOrgasm_ = false;
				postOrgasmElapsed_ = 0;
				person_.Animator.StopType(Animation.OrgasmType);
			}

			UpdateParts(s);
			UpdateRates(s);
			UpdateMax(s);
			UpdateValue(s);
			Apply(s);
		}

		private void UpdateParts(float s)
		{
			for (int i = 0; i < BodyParts.Count; ++i)
			{
				var t = person_.Body.Get(i).Trigger;

				if (t > 0)
					parts_[i] = t;
				else
					parts_[i] = Math.Max(parts_[i] - s * decay_, 0);
			}
		}

		private void UpdateRates(float s)
		{
			var ss = person_.Personality.Sensitivity;

			totalRate_ = 0;

			if (flatExcitement_ < ss.MouthMax)
				mouthRate_ = Mouth * ss.MouthRate * s;
			else
				mouthRate_ = 0;

			if (flatExcitement_ < ss.BreastsMax)
				breastsRate_ = Breasts * ss.BreastsRate * s;
			else
				breastsRate_ = 0;

			if (flatExcitement_ < ss.GenitalsMax)
				genitalsRate_ = Genitals * ss.GenitalsRate * s;
			else
				genitalsRate_ = 0;

			if (flatExcitement_ < ss.PenetrationMax)
				penetrationRate_ = Penetration * ss.PenetrationRate * s;
			else
				penetrationRate_ = 0;


			totalRate_ += mouthRate_ + breastsRate_ + genitalsRate_ + penetrationRate_;
			if (totalRate_ == 0)
				totalRate_ = ss.DecayPerSecond * s;
		}

		private void UpdateMax(float s)
		{
			var ss = person_.Personality.Sensitivity;

			max_ = 0;

			if (Mouth > 0)
				max_ = Math.Max(max_, ss.MouthMax);

			if (Breasts > 0)
				max_ = Math.Max(max_, ss.BreastsMax);

			if (Genitals > 0)
				max_ = Math.Max(max_, ss.GenitalsMax);

			if (Penetration > 0)
				max_ = Math.Max(max_, ss.PenetrationMax);
		}

		private void UpdateValue(float s)
		{
			var ss = person_.Personality.Sensitivity;

			if (flatExcitement_ > max_)
			{
				flatExcitement_ =
					Math.Max(flatExcitement_ + ss.DecayPerSecond * s, max_);
			}
			else
			{
				flatExcitement_ =
					U.Clamp(flatExcitement_ + totalRate_, 0, max_);
			}
		}

		private void Apply(float s)
		{
			var ss = person_.Personality.Sensitivity;

			person_.Breathing.Intensity = Value;
			person_.Body.Sweat = Value;
			person_.Body.Flush = Value;
			person_.Expression.Set(Expressions.Pleasure, Value);
			person_.Hair.Loose = Value;

			if (Value >= 1)
			{
				person_.Log.Info("orgasm");
				person_.Orgasmer.Orgasm();
				person_.Animator.PlayType(Animation.OrgasmType);
				flatExcitement_ = ss.ExcitementPostOrgasm;
				postOrgasm_ = true;
				postOrgasmElapsed_ = 0;
			}
		}


		public float Mouth
		{
			get
			{
				return
					parts_[BodyParts.Lips] * 0.1f +
					parts_[BodyParts.Mouth] * 0.9f;
			}
		}

		public float MouthRate
		{
			get { return mouthRate_; }
		}

		public float Breasts
		{
			get
			{
				return
					parts_[BodyParts.LeftBreast] * 0.5f +
					parts_[BodyParts.RightBreast] * 0.5f;
			}
		}

		public float BreastsRate
		{
			get { return breastsRate_; }
		}

		public float Genitals
		{
			get
			{
				return Math.Min(1,
					parts_[BodyParts.Labia]);
			}
		}

		public float GenitalsRate
		{
			get { return genitalsRate_; }
		}

		public float Penetration
		{
			get
			{
				return Math.Min(1,
					parts_[BodyParts.Vagina] * 0.3f +
					parts_[BodyParts.DeepVagina] * 1 +
					parts_[BodyParts.DeeperVagina] * 1);
			}
		}

		public float PenetrationRate
		{
			get { return penetrationRate_; }
		}

		public float Rate
		{
			get { return totalRate_; }
		}

		public float Max
		{
			get { return max_; }
		}

		public override string ToString()
		{
			string s =
				$"{Value:0.000000} " +
				$"(flat {flatExcitement_:0.000000}, max {max_:0.000000})";

			if (forcedExcitement_ >= 0)
				s += $" forced {forcedExcitement_:0.000000})";

			return s;
		}
	}
}
