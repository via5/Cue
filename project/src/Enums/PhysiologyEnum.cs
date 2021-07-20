﻿// auto generated from PhysiologyEnums.tt

namespace Cue
{
	class PE : IEnumValues
	{
		// sliding durations
		public const int SlidingDurationCount = 0;
		public int GetSlidingDurationCount() { return 0; }

		// bools
		public const int BoolCount = 0;
		public int GetBoolCount() { return 0; }

		// floats
		public const int MaxSweat = 0;
		public const int MaxFlush = 1;
		public const int TemperatureExcitementMax = 2;
		public const int TemperatureExcitementRate = 3;
		public const int TemperatureDecayRate = 4;
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
		public const int LipsFactor = 15;
		public const int MouthFactor = 16;
		public const int BreastsRate = 17;
		public const int BreastsMax = 18;
		public const int LeftBreastFactor = 19;
		public const int RightBreastFactor = 20;
		public const int GenitalsRate = 21;
		public const int GenitalsMax = 22;
		public const int LabiaFactor = 23;
		public const int PenetrationRate = 24;
		public const int PenetrationMax = 25;
		public const int VaginaFactor = 26;
		public const int DeepVaginaFactor = 27;
		public const int DeeperVaginaFactor = 28;
		public const int ExcitementDecayRate = 29;
		public const int ExcitementPostOrgasm = 30;
		public const int OrgasmTime = 31;
		public const int PostOrgasmTime = 32;
		public const int RateAdjustment = 33;
		public const int FloatCount = 34;
		public int GetFloatCount() { return 34; }

		// strings
		public const int Voice = 0;
		public const int StringCount = 1;
		public int GetStringCount() { return 1; }


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

		public string GetSlidingDurationName(int i)
		{
			return SlidingDurationToString(i);
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

		public string GetBoolName(int i)
		{
			return BoolToString(i);
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
			"temperatureExcitementMax",
			"temperatureExcitementRate",
			"temperatureDecayRate",
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
			"lipsFactor",
			"mouthFactor",
			"breastsRate",
			"breastsMax",
			"leftBreastFactor",
			"rightBreastFactor",
			"genitalsRate",
			"genitalsMax",
			"labiaFactor",
			"penetrationRate",
			"penetrationMax",
			"vaginaFactor",
			"deepVaginaFactor",
			"deeperVaginaFactor",
			"excitementDecayRate",
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

		public string GetFloatName(int i)
		{
			return FloatToString(i);
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

		public string GetStringName(int i)
		{
			return StringToString(i);
		}

		public static string StringToString(int i)
		{
			return stringNames_[i];
		}

		public static string[] StringNames
		{
			get { return stringNames_; }
		}


		private static string[] allNames_ = new string[] {
			"maxSweat",
			"maxFlush",
			"temperatureExcitementMax",
			"temperatureExcitementRate",
			"temperatureDecayRate",
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
			"lipsFactor",
			"mouthFactor",
			"breastsRate",
			"breastsMax",
			"leftBreastFactor",
			"rightBreastFactor",
			"genitalsRate",
			"genitalsMax",
			"labiaFactor",
			"penetrationRate",
			"penetrationMax",
			"vaginaFactor",
			"deepVaginaFactor",
			"deeperVaginaFactor",
			"excitementDecayRate",
			"excitementPostOrgasm",
			"orgasmTime",
			"postOrgasmTime",
			"rateAdjustment",
			"voice",
		};

		public static string[] AllNames
		{
			get { return allNames_; }
		}

		public string[] GetAllNames()
		{
			return AllNames;
		}
	}
}
