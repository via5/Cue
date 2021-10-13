﻿// auto generated from PersonalityEnums.tt

namespace Cue
{
	class PSE : IEnumValues
	{
		// sliding durations
		public const int GazeDuration = 0;

		public const int SlidingDurationCount = 1;
		public int GetSlidingDurationCount() { return 1; }

		// bools
		public const int AvoidGazePlayer = 0;
		public const int AvoidGazeInsidePersonalSpace = 1;
		public const int AvoidGazeDuringSex = 2;
		public const int AvoidGazeDuringSexOthers = 3;

		public const int BoolCount = 4;
		public int GetBoolCount() { return 4; }

		// floats
		public const int GazeRandomIntervalMinimum = 0;
		public const int GazeRandomIntervalMaximum = 1;
		public const int EmergencyGazeDuration = 2;
		public const int MaxExcitementForAvoid = 3;
		public const int AvoidDelayAfterOrgasm = 4;
		public const int LookAboveMaxWeight = 5;
		public const int LookAboveMaxWeightOrgasm = 6;
		public const int LookAboveMinExcitement = 7;
		public const int LookAboveMinPhysicalRate = 8;
		public const int IdleNaturalRandomWeight = 9;
		public const int NaturalRandomWeight = 10;
		public const int NaturalOtherEyesWeight = 11;
		public const int BusyOtherEyesWeight = 12;
		public const int MaxTirednessForRandomGaze = 13;
		public const int OtherEyesExcitementWeight = 14;
		public const int OtherEyesOrgasmWeight = 15;
		public const int BlowjobEyesWeight = 16;
		public const int BlowjobGenitalsWeight = 17;
		public const int HandjobEyesWeight = 18;
		public const int HandjobGenitalsWeight = 19;
		public const int PenetratedEyesWeight = 20;
		public const int PenetratedGenitalsWeight = 21;
		public const int PenetratingEyesWeight = 22;
		public const int PenetratingGenitalsWeight = 23;
		public const int GropedEyesWeight = 24;
		public const int GropedTargetWeight = 25;
		public const int GropingEyesWeight = 26;
		public const int GropingTargetWeight = 27;
		public const int OtherBlowjobEyesWeight = 28;
		public const int OtherBlowjobTargetEyesWeight = 29;
		public const int OtherBlowjobTargetGenitalsWeight = 30;
		public const int OtherHandjobEyesWeight = 31;
		public const int OtherHandjobTargetEyesWeight = 32;
		public const int OtherHandjobTargetGenitalsWeight = 33;
		public const int OtherPenetrationEyesWeight = 34;
		public const int OtherPenetrationSourceEyesWeight = 35;
		public const int OtherPenetrationSourceGenitalsWeight = 36;
		public const int OtherGropedEyesWeight = 37;
		public const int OtherGropedSourceEyesWeight = 38;
		public const int OtherGropedTargetWeight = 39;
		public const int OtherSexExcitementRateFactor = 40;
		public const int MaxOtherSexExcitement = 41;
		public const int KissSpeedEnergyFactor = 42;
		public const int IdleMaxExcitement = 43;
		public const int TirednessExcitementRateFactor = 44;
		public const int GazeEnergyTirednessFactor = 45;
		public const int GazeTirednessFactor = 46;
		public const int MovementEnergyTirednessFactor = 47;
		public const int MovementTirednessFactor = 48;
		public const int ExpressionExcitementFactor = 49;
		public const int ExpressionTirednessFactor = 50;

		public const int FloatCount = 51;
		public int GetFloatCount() { return 51; }

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
			"avoidGazePlayer",
			"avoidGazeInsidePersonalSpace",
			"avoidGazeDuringSex",
			"avoidGazeDuringSexOthers",
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
			"maxExcitementForAvoid",
			"avoidDelayAfterOrgasm",
			"lookAboveMaxWeight",
			"lookAboveMaxWeightOrgasm",
			"lookAboveMinExcitement",
			"lookAboveMinPhysicalRate",
			"idleNaturalRandomWeight",
			"naturalRandomWeight",
			"naturalOtherEyesWeight",
			"busyOtherEyesWeight",
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
			"movementTirednessFactor",
			"expressionExcitementFactor",
			"expressionTirednessFactor",
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
			"avoidGazePlayer",
			"avoidGazeInsidePersonalSpace",
			"avoidGazeDuringSex",
			"avoidGazeDuringSexOthers",
			"gazeRandomIntervalMinimum",
			"gazeRandomIntervalMaximum",
			"emergencyGazeDuration",
			"maxExcitementForAvoid",
			"avoidDelayAfterOrgasm",
			"lookAboveMaxWeight",
			"lookAboveMaxWeightOrgasm",
			"lookAboveMinExcitement",
			"lookAboveMinPhysicalRate",
			"idleNaturalRandomWeight",
			"naturalRandomWeight",
			"naturalOtherEyesWeight",
			"busyOtherEyesWeight",
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
			"movementTirednessFactor",
			"expressionExcitementFactor",
			"expressionTirednessFactor",
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
