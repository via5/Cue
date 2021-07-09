namespace Cue
{
	static class PE
	{
		// floats
		public const int MaxSweat = 0;
		public const int MaxFlush = 1;

		public const int TemperatureExcitementRate = 2;
		public const int TemperatureDecayRate = 3;

		// excitement at which temperature is at max
		//
		public const int TemperatureExcitementMax = 4;

		public const int TirednessRateDuringPostOrgasm = 5;
		public const int TirednessBaseDecayRate = 6;
		public const int TirednessBackToBaseRate = 7;
		public const int DelayAfterOrgasmUntilTirednessDecay = 8;
		public const int TirednessMaxExcitementForBaseDecay = 9;
		public const int OrgasmBaseTirednessIncrease = 10;

		public const int NeutralVoicePitch = 11;
		public const int VoicePitch = 12;

		public const int MouthRate = 13;
		public const int MouthMax = 14;

		public const int BreastsRate = 15;
		public const int BreastsMax = 16;

		public const int GenitalsRate = 17;
		public const int GenitalsMax = 18;

		public const int PenetrationRate = 19;
		public const int PenetrationMax = 20;

		public const int DecayPerSecond = 21;
		public const int ExcitementPostOrgasm = 22;
		public const int OrgasmTime = 23;
		public const int PostOrgasmTime = 24;
		public const int RateAdjustment = 25;

		public const int FloatCount = 26;


		// strings
		public const int Voice = 0;

		public const int StringCount = 1;


		private static string[] floatNames_ = new string[]
		{
			"maxSweat", "maxFlush",

			"temperatureExcitementRate", "temperatureDecayRate",
			"temperatureExcitementMax",

			"tirednessRateDuringPostOrgasm", "tirednessBaseDecayRate",
			"tirednessBackToBaseRate", "delayAfterOrgasmUntilTirednessDecay",
			"tirednessMaxExcitementForBaseDecay", "orgasmBaseTirednessIncrease",

			"neutralVoicePitch", "voicePitch",


			"mouthRate", "mouthMax",
			"breastsRate", "breastsMax",
			"genitalsRate", "genitalsMax",
			"penetrationRate", "penetrationMax",

			"decayPerSecond", "excitementPostOrgasm", "orgasmTime",
			"postOrgasmTime", "rateAdjustment",
		};

		public static int FloatFromString(string s)
		{
			for (int i = 0; i < floatNames_.Length; ++i)
			{
				if (floatNames_[i] == s)
					return i;
			}

			return -1;
		}

		public static string FloatToString(int i)
		{
			return floatNames_[i];
		}


		private static string[] stringNames_ = new string[]
		{
			"voice"
		};

		public static int StringFromString(string s)
		{
			for (int i = 0; i < stringNames_.Length; ++i)
			{
				if (stringNames_[i] == s)
					return i;
			}

			return -1;
		}

		public static string StringToString(int i)
		{
			return stringNames_[i];
		}
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
	}
}
