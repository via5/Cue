// auto generated from PersonalityEnums.tt

namespace Cue
{
	class PSE_Enum
	{
		// sliding durations
		public const int GazeDuration = 0;
		public const int SlidingDurationCount = 1;

		// bools
		public const int AvoidGazePlayer = 0;
		public const int AvoidGazeInsidePersonalSpace = 1;
		public const int AvoidGazeDuringSex = 2;
		public const int AvoidGazeDuringSexOthers = 3;
		public const int BoolCount = 4;

		// floats
		public const int GazeRandomIntervalMinimum = 0;
		public const int GazeRandomIntervalMaximum = 1;
		public const int MaxExcitementForAvoid = 2;
		public const int AvoidDelayAfterOrgasm = 3;
		public const int LookAboveMaxWeight = 4;
		public const int LookAboveMaxWeightOrgasm = 5;
		public const int NaturalRandomWeight = 6;
		public const int NaturalOtherEyesWeight = 7;
		public const int BusyOtherEyesWeight = 8;
		public const int MaxTirednessForRandomGaze = 9;
		public const int OtherEyesExcitementWeight = 10;
		public const int OtherEyesOrgasmWeight = 11;
		public const int BlowjobEyesWeight = 12;
		public const int BlowjobGenitalsWeight = 13;
		public const int HandjobEyesWeight = 14;
		public const int HandjobGenitalsWeight = 15;
		public const int PenetrationEyesWeight = 16;
		public const int PenetrationGenitalsWeight = 17;
		public const int GropedEyesWeight = 18;
		public const int GropedTargetWeight = 19;
		public const int OtherBlowjobEyesWeight = 20;
		public const int OtherBlowjobTargetEyesWeight = 21;
		public const int OtherBlowjobTargetGenitalsWeight = 22;
		public const int OtherHandjobEyesWeight = 23;
		public const int OtherHandjobTargetEyesWeight = 24;
		public const int OtherHandjobTargetGenitalsWeight = 25;
		public const int OtherPenetrationEyesWeight = 26;
		public const int OtherPenetrationSourceEyesWeight = 27;
		public const int OtherPenetrationSourceGenitalsWeight = 28;
		public const int OtherGropedEyesWeight = 29;
		public const int OtherGropedSourceEyesWeight = 30;
		public const int OtherGropedTargetWeight = 31;
		public const int OtherSexExcitementRateFactor = 32;
		public const int MaxOtherSexExcitement = 33;
		public const int EnergyTirednessFactor = 34;
		public const int FloatCount = 35;

		// strings
		public const int StringCount = 0;

		// states
		public const int IdleState = 0;
		public const int CloseState = 1;
		public const int StateCount = 2;


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
			"maxExcitementForAvoid",
			"avoidDelayAfterOrgasm",
			"lookAboveMaxWeight",
			"lookAboveMaxWeightOrgasm",
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
			"energyTirednessFactor",
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
			"idleState",
			"closeState",
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


		private static string[] allNames_ = new string[] {
			"gazeDuration",
			"avoidGazePlayer",
			"avoidGazeInsidePersonalSpace",
			"avoidGazeDuringSex",
			"avoidGazeDuringSexOthers",
			"gazeRandomIntervalMinimum",
			"gazeRandomIntervalMaximum",
			"maxExcitementForAvoid",
			"avoidDelayAfterOrgasm",
			"lookAboveMaxWeight",
			"lookAboveMaxWeightOrgasm",
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
			"energyTirednessFactor",
			"idleState",
			"closeState",
		};

		public static string[] AllNames
		{
			get { return allNames_; }
		}
	}
}
