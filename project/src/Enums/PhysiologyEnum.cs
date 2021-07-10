// auto generated from PhysiologyEnums.tt

namespace Cue
{
	class PE_Enum
	{
		// sliding durations
		public const int SlidingDurationCount = 0;

		// bools
		public const int BoolCount = 0;

		// floats
		public const int MaxSweat = 0;
		public const int MaxFlush = 1;
		public const int TemperatureExcitementRate = 2;
		public const int TemperatureDecayRate = 3;
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

		// states
		public const int StateCount = 0;


		private static string[] slidingDurationNames_ = new string[]
		{
		};

		public static int SlidingDurationFromString(string s)
		{
			for (int i = 0; i<slidingDurationNames_.Length; ++i)
			{
				if (slidingDurationNames_[i] == s)
					return i;
			}

			return -1;
		}

		public static string SlidingDurationToString(int i)
		{
			return slidingDurationNames_[i];
		}

		public static string[] SlidingDurationNames
		{
			get { return slidingDurationNames_; }
		}

		private static string[] boolNames_ = new string[]
		{
		};

		public static int BoolFromString(string s)
		{
			for (int i = 0; i<boolNames_.Length; ++i)
			{
				if (boolNames_[i] == s)
					return i;
			}

			return -1;
		}

		public static string BoolToString(int i)
		{
			return boolNames_[i];
		}

		public static string[] BoolNames
		{
			get { return boolNames_; }
		}

		private static string[] floatNames_ = new string[]
		{
			"maxSweat",
			"maxFlush",
			"temperatureExcitementRate",
			"temperatureDecayRate",
			"temperatureExcitementMax",
			"tirednessRateDuringPostOrgasm",
			"tirednessBaseDecayRate",
			"tirednessBackToBaseRate",
			"delayAfterOrgasmUntilTirednessDecay",
			"tirednessMaxExcitementForBaseDecay",
			"orgasmBaseTirednessIncrease",
			"neutralVoicePitch",
			"voicePitch",
			"mouthRate",
			"mouthMax",
			"breastsRate",
			"breastsMax",
			"genitalsRate",
			"genitalsMax",
			"penetrationRate",
			"penetrationMax",
			"decayPerSecond",
			"excitementPostOrgasm",
			"orgasmTime",
			"postOrgasmTime",
			"rateAdjustment",
		};

		public static int FloatFromString(string s)
		{
			for (int i = 0; i<floatNames_.Length; ++i)
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

		public static string[] FloatNames
		{
			get { return floatNames_; }
		}

		private static string[] stringNames_ = new string[]
		{
			"voice",
		};

		public static int StringFromString(string s)
		{
			for (int i = 0; i<stringNames_.Length; ++i)
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

		public static string[] StringNames
		{
			get { return stringNames_; }
		}

		private static string[] stateNames_ = new string[]
		{
		};

		public static int StateFromString(string s)
		{
			for (int i = 0; i<stateNames_.Length; ++i)
			{
				if (stateNames_[i] == s)
					return i;
			}

			return -1;
		}

		public static string StateToString(int i)
		{
			return stateNames_[i];
		}

		public static string[] StateNames
		{
			get { return stateNames_; }
		}
	}
}
