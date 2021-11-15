using System;

namespace Cue
{
	class ExcitementBodyPart
	{
		private int bodyPart_;
		private Sys.TriggerInfo[] lastTriggers_ = null;
		private float[] lastMods_ = new float[3]; // some initial count
		private float value_ = 0;
		private float specificModifier_ = 0;
		private float fromPenisValue_ = 0;

		public ExcitementBodyPart(int bodyPart)
		{
			bodyPart_ = bodyPart;
		}

		public float Value
		{
			get { return value_; }
			set { value_ = value; }
		}

		public float SpecificModifier
		{
			get { return specificModifier_; }
			set { specificModifier_ = value; }
		}

		public float FromPenisValue
		{
			get { return fromPenisValue_; }
			set { fromPenisValue_ = value; }
		}

		public void Update(float s, Person p, Sys.TriggerInfo[] ts)
		{
			specificModifier_ = 0;
			fromPenisValue_ = 0;
			lastTriggers_ = ts;

			if (ts == null || ts.Length == 0)
			{
				// todo, decay
				if (value_ > 0)
					value_ = Math.Max(value_ - s, 0);

				return;
			}


			var ps = p.Personality;

			value_ = 0;
			bool sawUnknown = false;

			if (lastMods_ == null || lastMods_.Length < ts.Length)
				lastMods_ = new float[ts.Length];

			for (int j = 0; j < ts.Length; ++j)
			{
				if (!ts[j].IsPerson())
				{
					if (sawUnknown)
						continue;
					else
						sawUnknown = true;
				}

				if (ts[j].sourcePartIndex == BP.Penis)
					fromPenisValue_ += ts[j].value;

				float mod = ps.GetSpecificModifier(
					ts[j].personIndex, ts[j].sourcePartIndex,
					p.PersonIndex, bodyPart_);

				value_ += ts[j].value;
				specificModifier_ += mod;
				lastMods_[j] = mod;
			}
		}

		public string Debug()
		{
			string s = null;

			if (lastTriggers_ == null && value_ > 0)
			{
				s = $"{value_:0.0} (decaying)";
			}
			else if (lastTriggers_ != null && lastTriggers_.Length > 0)
			{
				s = "";

				for (int i = 0; i < lastTriggers_.Length; ++i)
				{
					var t = lastTriggers_[i];

					if (s != "")
						s += " ";

					s += $"+{t}@{t.value:0.0}";

					if (lastMods_[i] > 0)
						s += $"*{lastMods_[i]}";
				}
			}

			if (s == null)
				return null;

			return $"{BP.ToString(bodyPart_)}: {s}";
		}
	}
}
