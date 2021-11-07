// auto generated from PersonalityEnums.tt

using System.Collections.Generic;

namespace Cue
{
	class PS : IEnumValues
	{
		// durations
		public const int GazeDuration = 0;
		public const int GazeRandomInterval = 1;
		public const int EmergencyGazeDuration = 2;

		public const int DurationCount = 3;
		public int GetDurationCount() { return 3; }

		// bools

		public const int BoolCount = 0;
		public int GetBoolCount() { return 0; }

		// floats
		public const int AvoidGazePlayer = 0;
		public const int AvoidGazePlayerInsidePersonalSpace = 1;
		public const int AvoidGazePlayerDuringSex = 2;
		public const int AvoidGazePlayerDelayAfterOrgasm = 3;
		public const int AvoidGazePlayerWeight = 4;
		public const int AvoidGazeOthers = 5;
		public const int AvoidGazeOthersInsidePersonalSpace = 6;
		public const int AvoidGazeOthersDuringSex = 7;
		public const int AvoidGazeOthersDelayAfterOrgasm = 8;
		public const int AvoidGazeOthersWeight = 9;
		public const int AvoidGazeUninvolvedHavingSex = 10;
		public const int LookAboveMaxWeight = 11;
		public const int LookAboveMaxWeightOrgasm = 12;
		public const int LookAboveMinExcitement = 13;
		public const int LookAboveMinPhysicalRate = 14;
		public const int IdleNaturalRandomWeight = 15;
		public const int IdleEmptyRandomWeight = 16;
		public const int NaturalRandomWeight = 17;
		public const int NaturalOtherEyesWeight = 18;
		public const int BusyOtherEyesWeight = 19;
		public const int NaturalPlayerEyesWeight = 20;
		public const int BusyPlayerEyesWeight = 21;
		public const int MaxTirednessForRandomGaze = 22;
		public const int OtherEyesExcitementWeight = 23;
		public const int OtherEyesOrgasmWeight = 24;
		public const int BlowjobEyesWeight = 25;
		public const int BlowjobGenitalsWeight = 26;
		public const int HandjobEyesWeight = 27;
		public const int HandjobGenitalsWeight = 28;
		public const int PenetratedEyesWeight = 29;
		public const int PenetratedGenitalsWeight = 30;
		public const int PenetratingEyesWeight = 31;
		public const int PenetratingGenitalsWeight = 32;
		public const int GropedEyesWeight = 33;
		public const int GropedTargetWeight = 34;
		public const int GropingEyesWeight = 35;
		public const int GropingTargetWeight = 36;
		public const int OtherBlowjobEyesWeight = 37;
		public const int OtherBlowjobTargetEyesWeight = 38;
		public const int OtherBlowjobTargetGenitalsWeight = 39;
		public const int OtherHandjobEyesWeight = 40;
		public const int OtherHandjobTargetEyesWeight = 41;
		public const int OtherHandjobTargetGenitalsWeight = 42;
		public const int OtherPenetrationEyesWeight = 43;
		public const int OtherPenetrationSourceEyesWeight = 44;
		public const int OtherPenetrationSourceGenitalsWeight = 45;
		public const int OtherGropedEyesWeight = 46;
		public const int OtherGropedSourceEyesWeight = 47;
		public const int OtherGropedTargetWeight = 48;
		public const int OtherSexExcitementRateFactor = 49;
		public const int MaxOtherSexExcitement = 50;
		public const int KissSpeedEnergyFactor = 51;
		public const int IdleMaxExcitement = 52;
		public const int TirednessExcitementRateFactor = 53;
		public const int GazeEnergyTirednessFactor = 54;
		public const int GazeTirednessFactor = 55;
		public const int MovementEnergyTirednessFactor = 56;
		public const int ExpressionTirednessFactor = 57;
		public const int MovementEnergyRampUpAfterOrgasm = 58;
		public const int AvoidGazeAnger = 59;
		public const int AngerWhenPlayerInteracts = 60;
		public const int AngerMaxExcitementForAnger = 61;
		public const int AngerMaxExcitementForHappiness = 62;
		public const int AngerExcitementFactorForAnger = 63;
		public const int AngerExcitementFactorForHappiness = 64;
		public const int MaxHappiness = 65;
		public const int MaxSweat = 66;
		public const int MaxFlush = 67;
		public const int TemperatureExcitementMax = 68;
		public const int TemperatureExcitementRate = 69;
		public const int TemperatureDecayRate = 70;
		public const int TirednessRateDuringPostOrgasm = 71;
		public const int TirednessBaseDecayRate = 72;
		public const int TirednessBackToBaseRate = 73;
		public const int DelayAfterOrgasmUntilTirednessDecay = 74;
		public const int TirednessMaxExcitementForBaseDecay = 75;
		public const int OrgasmBaseTirednessIncrease = 76;
		public const int NeutralVoicePitch = 77;
		public const int MouthRate = 78;
		public const int MouthMax = 79;
		public const int LipsFactor = 80;
		public const int MouthFactor = 81;
		public const int BreastsRate = 82;
		public const int BreastsMax = 83;
		public const int LeftBreastFactor = 84;
		public const int RightBreastFactor = 85;
		public const int GenitalsRate = 86;
		public const int GenitalsMax = 87;
		public const int LabiaFactor = 88;
		public const int PenetrationRate = 89;
		public const int PenetrationMax = 90;
		public const int VaginaFactor = 91;
		public const int DeepVaginaFactor = 92;
		public const int DeeperVaginaFactor = 93;
		public const int ExcitementDecayRate = 94;
		public const int ExcitementPostOrgasm = 95;
		public const int OrgasmTime = 96;
		public const int PostOrgasmTime = 97;
		public const int RateAdjustment = 98;

		public const int FloatCount = 99;
		public int GetFloatCount() { return 99; }

		// strings

		public const int StringCount = 0;
		public int GetStringCount() { return 0; }


		private static string[] durationNames_ = new string[]
		{
			"gazeDuration",
			"gazeRandomInterval",
			"emergencyGazeDuration",
		};

		public static int DurationFromString(string s)
		{
			for (int i = 0; i<durationNames_.Length; ++i)
			{
				if (durationNames_[i] == s)
					return i;
			}

			return -1;
		}

		public static int[] DurationFromStringMany(string s)
		{
			var list = new List<int>();
			var ss = s.Split(' ');

			foreach (string p in ss)
			{
				string tp = p.Trim();
				if (tp == "")
					continue;

				var i = DurationFromString(tp);
				if (i != -1)
					list.Add(i);
			}

			return list.ToArray();
		}

		public string GetDurationName(int i)
		{
			return DurationToString(i);
		}

		public static string DurationToString(int i)
		{
			if (i >= 0 && i < durationNames_.Length)
				return durationNames_[i];
			else
				return $"?{i}";
		}

		public static string[] DurationNames
		{
			get { return durationNames_; }
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
			"gazeRandomInterval",
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
