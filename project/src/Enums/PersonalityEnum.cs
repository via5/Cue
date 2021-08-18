// auto generated from PersonalityEnums.tt

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
		public const int IdleNaturalRandomWeight = 7;
		public const int NaturalRandomWeight = 8;
		public const int NaturalOtherEyesWeight = 9;
		public const int BusyOtherEyesWeight = 10;
		public const int MaxTirednessForRandomGaze = 11;
		public const int OtherEyesExcitementWeight = 12;
		public const int OtherEyesOrgasmWeight = 13;
		public const int BlowjobEyesWeight = 14;
		public const int BlowjobGenitalsWeight = 15;
		public const int HandjobEyesWeight = 16;
		public const int HandjobGenitalsWeight = 17;
		public const int PenetrationEyesWeight = 18;
		public const int PenetrationGenitalsWeight = 19;
		public const int GropedEyesWeight = 20;
		public const int GropedTargetWeight = 21;
		public const int OtherBlowjobEyesWeight = 22;
		public const int OtherBlowjobTargetEyesWeight = 23;
		public const int OtherBlowjobTargetGenitalsWeight = 24;
		public const int OtherHandjobEyesWeight = 25;
		public const int OtherHandjobTargetEyesWeight = 26;
		public const int OtherHandjobTargetGenitalsWeight = 27;
		public const int OtherPenetrationEyesWeight = 28;
		public const int OtherPenetrationSourceEyesWeight = 29;
		public const int OtherPenetrationSourceGenitalsWeight = 30;
		public const int OtherGropedEyesWeight = 31;
		public const int OtherGropedSourceEyesWeight = 32;
		public const int OtherGropedTargetWeight = 33;
		public const int OtherSexExcitementRateFactor = 34;
		public const int MaxOtherSexExcitement = 35;
		public const int KissSpeedEnergyFactor = 36;
		public const int IdleMaxExcitement = 37;
		public const int GazeEnergyTirednessFactor = 38;
		public const int GazeTirednessFactor = 39;
		public const int MovementEnergyTirednessFactor = 40;
		public const int MovementTirednessFactor = 41;
		public const int ExpressionExcitementFactor = 42;
		public const int ExpressionTirednessFactor = 43;
		public const int FloatCount = 44;
		public int GetFloatCount() { return 44; }

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
			return slidingDurationNames_[i];
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
			return boolNames_[i];
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
			"penetrationEyesWeight",
			"penetrationGenitalsWeight",
			"gropedEyesWeight",
			"gropedTargetWeight",
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
			return floatNames_[i];
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
			return stringNames_[i];
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
			"penetrationEyesWeight",
			"penetrationGenitalsWeight",
			"gropedEyesWeight",
			"gropedTargetWeight",
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
