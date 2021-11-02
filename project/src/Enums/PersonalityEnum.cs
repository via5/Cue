// auto generated from PersonalityEnums.tt

using System.Collections.Generic;

namespace Cue
{
	class PS : IEnumValues
	{
		// sliding durations
		public const int GazeDuration = 0;

		public const int SlidingDurationCount = 1;
		public int GetSlidingDurationCount() { return 1; }

		// bools

		public const int BoolCount = 0;
		public int GetBoolCount() { return 0; }

		// floats
		public const int GazeRandomIntervalMinimum = 0;
		public const int GazeRandomIntervalMaximum = 1;
		public const int EmergencyGazeDuration = 2;
		public const int AvoidGazePlayer = 3;
		public const int AvoidGazePlayerInsidePersonalSpace = 4;
		public const int AvoidGazePlayerDuringSex = 5;
		public const int AvoidGazePlayerDelayAfterOrgasm = 6;
		public const int AvoidGazePlayerWeight = 7;
		public const int AvoidGazeOthers = 8;
		public const int AvoidGazeOthersInsidePersonalSpace = 9;
		public const int AvoidGazeOthersDuringSex = 10;
		public const int AvoidGazeOthersDelayAfterOrgasm = 11;
		public const int AvoidGazeOthersWeight = 12;
		public const int AvoidGazeUninvolvedHavingSex = 13;
		public const int LookAboveMaxWeight = 14;
		public const int LookAboveMaxWeightOrgasm = 15;
		public const int LookAboveMinExcitement = 16;
		public const int LookAboveMinPhysicalRate = 17;
		public const int IdleNaturalRandomWeight = 18;
		public const int IdleEmptyRandomWeight = 19;
		public const int NaturalRandomWeight = 20;
		public const int NaturalOtherEyesWeight = 21;
		public const int BusyOtherEyesWeight = 22;
		public const int NaturalPlayerEyesWeight = 23;
		public const int BusyPlayerEyesWeight = 24;
		public const int MaxTirednessForRandomGaze = 25;
		public const int OtherEyesExcitementWeight = 26;
		public const int OtherEyesOrgasmWeight = 27;
		public const int BlowjobEyesWeight = 28;
		public const int BlowjobGenitalsWeight = 29;
		public const int HandjobEyesWeight = 30;
		public const int HandjobGenitalsWeight = 31;
		public const int PenetratedEyesWeight = 32;
		public const int PenetratedGenitalsWeight = 33;
		public const int PenetratingEyesWeight = 34;
		public const int PenetratingGenitalsWeight = 35;
		public const int GropedEyesWeight = 36;
		public const int GropedTargetWeight = 37;
		public const int GropingEyesWeight = 38;
		public const int GropingTargetWeight = 39;
		public const int OtherBlowjobEyesWeight = 40;
		public const int OtherBlowjobTargetEyesWeight = 41;
		public const int OtherBlowjobTargetGenitalsWeight = 42;
		public const int OtherHandjobEyesWeight = 43;
		public const int OtherHandjobTargetEyesWeight = 44;
		public const int OtherHandjobTargetGenitalsWeight = 45;
		public const int OtherPenetrationEyesWeight = 46;
		public const int OtherPenetrationSourceEyesWeight = 47;
		public const int OtherPenetrationSourceGenitalsWeight = 48;
		public const int OtherGropedEyesWeight = 49;
		public const int OtherGropedSourceEyesWeight = 50;
		public const int OtherGropedTargetWeight = 51;
		public const int OtherSexExcitementRateFactor = 52;
		public const int MaxOtherSexExcitement = 53;
		public const int KissSpeedEnergyFactor = 54;
		public const int IdleMaxExcitement = 55;
		public const int TirednessExcitementRateFactor = 56;
		public const int GazeEnergyTirednessFactor = 57;
		public const int GazeTirednessFactor = 58;
		public const int MovementEnergyTirednessFactor = 59;
		public const int ExpressionTirednessFactor = 60;
		public const int MovementEnergyRampUpAfterOrgasm = 61;
		public const int AvoidGazeAnger = 62;
		public const int AngerWhenPlayerInteracts = 63;
		public const int AngerMaxExcitementForAnger = 64;
		public const int AngerMaxExcitementForHappiness = 65;
		public const int AngerExcitementFactorForAnger = 66;
		public const int AngerExcitementFactorForHappiness = 67;
		public const int MaxHappiness = 68;
		public const int MaxSweat = 69;
		public const int MaxFlush = 70;
		public const int TemperatureExcitementMax = 71;
		public const int TemperatureExcitementRate = 72;
		public const int TemperatureDecayRate = 73;
		public const int TirednessRateDuringPostOrgasm = 74;
		public const int TirednessBaseDecayRate = 75;
		public const int TirednessBackToBaseRate = 76;
		public const int DelayAfterOrgasmUntilTirednessDecay = 77;
		public const int TirednessMaxExcitementForBaseDecay = 78;
		public const int OrgasmBaseTirednessIncrease = 79;
		public const int NeutralVoicePitch = 80;
		public const int MouthRate = 81;
		public const int MouthMax = 82;
		public const int LipsFactor = 83;
		public const int MouthFactor = 84;
		public const int BreastsRate = 85;
		public const int BreastsMax = 86;
		public const int LeftBreastFactor = 87;
		public const int RightBreastFactor = 88;
		public const int GenitalsRate = 89;
		public const int GenitalsMax = 90;
		public const int LabiaFactor = 91;
		public const int PenetrationRate = 92;
		public const int PenetrationMax = 93;
		public const int VaginaFactor = 94;
		public const int DeepVaginaFactor = 95;
		public const int DeeperVaginaFactor = 96;
		public const int ExcitementDecayRate = 97;
		public const int ExcitementPostOrgasm = 98;
		public const int OrgasmTime = 99;
		public const int PostOrgasmTime = 100;
		public const int RateAdjustment = 101;

		public const int FloatCount = 102;
		public int GetFloatCount() { return 102; }

		// strings

		public const int StringCount = 0;
		public int GetStringCount() { return 0; }


		private static string[] slidingDurationNames_ = new string[]
		{
			"gazeDuration",
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

		public static int[] SlidingDurationFromStringMany(string s)
		{
			var list = new List<int>();
			var ss = s.Split(' ');

			foreach (string p in ss)
			{
				string tp = p.Trim();
				if (tp == "")
					continue;

				var i = SlidingDurationFromString(tp);
				if (i != -1)
					list.Add(i);
			}

			return list.ToArray();
		}

		public string GetSlidingDurationName(int i)
		{
			return SlidingDurationToString(i);
		}

		public static string SlidingDurationToString(int i)
		{
			if (i >= 0 && i < slidingDurationNames_.Length)
				return slidingDurationNames_[i];
			else
				return $"?{i}";
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

		public static int[] BoolFromStringMany(string s)
		{
			var list = new List<int>();
			var ss = s.Split(' ');

			foreach (string p in ss)
			{
				string tp = p.Trim();
				if (tp == "")
					continue;

				var i = BoolFromString(tp);
				if (i != -1)
					list.Add(i);
			}

			return list.ToArray();
		}

		public string GetBoolName(int i)
		{
			return BoolToString(i);
		}

		public static string BoolToString(int i)
		{
			if (i >= 0 && i < boolNames_.Length)
				return boolNames_[i];
			else
				return $"?{i}";
		}

		public static string[] BoolNames
		{
			get { return boolNames_; }
		}

		private static string[] floatNames_ = new string[]
		{
			"gazeRandomIntervalMinimum",
			"gazeRandomIntervalMaximum",
			"emergencyGazeDuration",
			"avoidGazePlayer",
			"avoidGazePlayerInsidePersonalSpace",
			"avoidGazePlayerDuringSex",
			"avoidGazePlayerDelayAfterOrgasm",
			"avoidGazePlayerWeight",
			"avoidGazeOthers",
			"avoidGazeOthersInsidePersonalSpace",
			"avoidGazeOthersDuringSex",
			"avoidGazeOthersDelayAfterOrgasm",
			"avoidGazeOthersWeight",
			"avoidGazeUninvolvedHavingSex",
			"lookAboveMaxWeight",
			"lookAboveMaxWeightOrgasm",
			"lookAboveMinExcitement",
			"lookAboveMinPhysicalRate",
			"idleNaturalRandomWeight",
			"idleEmptyRandomWeight",
			"naturalRandomWeight",
			"naturalOtherEyesWeight",
			"busyOtherEyesWeight",
			"naturalPlayerEyesWeight",
			"busyPlayerEyesWeight",
			"maxTirednessForRandomGaze",
			"otherEyesExcitementWeight",
			"otherEyesOrgasmWeight",
			"blowjobEyesWeight",
			"blowjobGenitalsWeight",
			"handjobEyesWeight",
			"handjobGenitalsWeight",
			"penetratedEyesWeight",
			"penetratedGenitalsWeight",
			"penetratingEyesWeight",
			"penetratingGenitalsWeight",
			"gropedEyesWeight",
			"gropedTargetWeight",
			"gropingEyesWeight",
			"gropingTargetWeight",
			"otherBlowjobEyesWeight",
			"otherBlowjobTargetEyesWeight",
			"otherBlowjobTargetGenitalsWeight",
			"otherHandjobEyesWeight",
			"otherHandjobTargetEyesWeight",
			"otherHandjobTargetGenitalsWeight",
			"otherPenetrationEyesWeight",
			"otherPenetrationSourceEyesWeight",
			"otherPenetrationSourceGenitalsWeight",
			"otherGropedEyesWeight",
			"otherGropedSourceEyesWeight",
			"otherGropedTargetWeight",
			"otherSexExcitementRateFactor",
			"maxOtherSexExcitement",
			"kissSpeedEnergyFactor",
			"idleMaxExcitement",
			"tirednessExcitementRateFactor",
			"gazeEnergyTirednessFactor",
			"gazeTirednessFactor",
			"movementEnergyTirednessFactor",
			"expressionTirednessFactor",
			"movementEnergyRampUpAfterOrgasm",
			"avoidGazeAnger",
			"angerWhenPlayerInteracts",
			"angerMaxExcitementForAnger",
			"angerMaxExcitementForHappiness",
			"angerExcitementFactorForAnger",
			"angerExcitementFactorForHappiness",
			"maxHappiness",
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

		public static int[] FloatFromStringMany(string s)
		{
			var list = new List<int>();
			var ss = s.Split(' ');

			foreach (string p in ss)
			{
				string tp = p.Trim();
				if (tp == "")
					continue;

				var i = FloatFromString(tp);
				if (i != -1)
					list.Add(i);
			}

			return list.ToArray();
		}

		public string GetFloatName(int i)
		{
			return FloatToString(i);
		}

		public static string FloatToString(int i)
		{
			if (i >= 0 && i < floatNames_.Length)
				return floatNames_[i];
			else
				return $"?{i}";
		}

		public static string[] FloatNames
		{
			get { return floatNames_; }
		}

		private static string[] stringNames_ = new string[]
		{
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

		public static int[] StringFromStringMany(string s)
		{
			var list = new List<int>();
			var ss = s.Split(' ');

			foreach (string p in ss)
			{
				string tp = p.Trim();
				if (tp == "")
					continue;

				var i = StringFromString(tp);
				if (i != -1)
					list.Add(i);
			}

			return list.ToArray();
		}

		public string GetStringName(int i)
		{
			return StringToString(i);
		}

		public static string StringToString(int i)
		{
			if (i >= 0 && i < stringNames_.Length)
				return stringNames_[i];
			else
				return $"?{i}";
		}

		public static string[] StringNames
		{
			get { return stringNames_; }
		}


		private static string[] allNames_ = new string[] {
			"gazeDuration",
			"gazeRandomIntervalMinimum",
			"gazeRandomIntervalMaximum",
			"emergencyGazeDuration",
			"avoidGazePlayer",
			"avoidGazePlayerInsidePersonalSpace",
			"avoidGazePlayerDuringSex",
			"avoidGazePlayerDelayAfterOrgasm",
			"avoidGazePlayerWeight",
			"avoidGazeOthers",
			"avoidGazeOthersInsidePersonalSpace",
			"avoidGazeOthersDuringSex",
			"avoidGazeOthersDelayAfterOrgasm",
			"avoidGazeOthersWeight",
			"avoidGazeUninvolvedHavingSex",
			"lookAboveMaxWeight",
			"lookAboveMaxWeightOrgasm",
			"lookAboveMinExcitement",
			"lookAboveMinPhysicalRate",
			"idleNaturalRandomWeight",
			"idleEmptyRandomWeight",
			"naturalRandomWeight",
			"naturalOtherEyesWeight",
			"busyOtherEyesWeight",
			"naturalPlayerEyesWeight",
			"busyPlayerEyesWeight",
			"maxTirednessForRandomGaze",
			"otherEyesExcitementWeight",
			"otherEyesOrgasmWeight",
			"blowjobEyesWeight",
			"blowjobGenitalsWeight",
			"handjobEyesWeight",
			"handjobGenitalsWeight",
			"penetratedEyesWeight",
			"penetratedGenitalsWeight",
			"penetratingEyesWeight",
			"penetratingGenitalsWeight",
			"gropedEyesWeight",
			"gropedTargetWeight",
			"gropingEyesWeight",
			"gropingTargetWeight",
			"otherBlowjobEyesWeight",
			"otherBlowjobTargetEyesWeight",
			"otherBlowjobTargetGenitalsWeight",
			"otherHandjobEyesWeight",
			"otherHandjobTargetEyesWeight",
			"otherHandjobTargetGenitalsWeight",
			"otherPenetrationEyesWeight",
			"otherPenetrationSourceEyesWeight",
			"otherPenetrationSourceGenitalsWeight",
			"otherGropedEyesWeight",
			"otherGropedSourceEyesWeight",
			"otherGropedTargetWeight",
			"otherSexExcitementRateFactor",
			"maxOtherSexExcitement",
			"kissSpeedEnergyFactor",
			"idleMaxExcitement",
			"tirednessExcitementRateFactor",
			"gazeEnergyTirednessFactor",
			"gazeTirednessFactor",
			"movementEnergyTirednessFactor",
			"expressionTirednessFactor",
			"movementEnergyRampUpAfterOrgasm",
			"avoidGazeAnger",
			"angerWhenPlayerInteracts",
			"angerMaxExcitementForAnger",
			"angerMaxExcitementForHappiness",
			"angerExcitementFactorForAnger",
			"angerExcitementFactorForHappiness",
			"maxHappiness",
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
