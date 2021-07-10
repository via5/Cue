namespace Cue
{
	class PE : PE_Enum
	{
	}


	class Physiology
	{
		public struct SpecificModifier
		{
			public int bodyPart;
			public int sourceBodyPart;
			public float modifier;
		}


		private string name_;
		private float[] floats_ = new float[PE.FloatCount];
		private string[] strings_ = new string[PE.StringCount];
		private SpecificModifier[] specificModifiers_ = new SpecificModifier[0];

		public Physiology(string name)
		{
			name_ = name;
		}

		public string Name
		{
			get { return name_; }
		}

		public Physiology Clone(Person p)
		{
			var pp = new Physiology(name_);

			for (int i = 0; i < floats_.Length; ++i)
				pp.floats_[i] = floats_[i];

			for (int i = 0; i < strings_.Length; ++i)
				pp.strings_[i] = strings_[i];

			for (int i = 0; i < specificModifiers_.Length; ++i)
				pp.specificModifiers_[i] = specificModifiers_[i];

			pp.Init(p);

			return pp;
		}

		private void Init(Person p)
		{
			if (floats_[PE.VoicePitch] < 0)
			{
				floats_[PE.VoicePitch] = U.Clamp(
					Get(PE.NeutralVoicePitch) + (1 - p.Atom.Body.Scale),
					0, 1);
			}
		}

		public void Set(float[] fs, string[] ss, SpecificModifier[] sms)
		{
			floats_ = fs;
			strings_ = ss;
			specificModifiers_ = sms;
		}

		public float Get(int i)
		{
			return floats_[i];
		}

		public string GetString(int i)
		{
			return strings_[i];
		}

		public float GetSpecificModifier(int part, Sys.TriggerInfo t)
		{
			for (int i = 0; i < specificModifiers_.Length; ++i)
			{
				var sm = specificModifiers_[i];
				if (sm.bodyPart == part && sm.sourceBodyPart == t.sourcePartIndex)
					return sm.modifier;
			}

			return 1;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
