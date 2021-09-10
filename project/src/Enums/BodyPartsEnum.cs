// auto generated from BodyPartsEnums.tt

namespace Cue
{
	class BodyPartsEnum
	{
		public const int None = -1;
		public const int Head = 0;
		public const int Lips = 1;
		public const int Mouth = 2;
		public const int LeftBreast = 3;
		public const int RightBreast = 4;
		public const int Labia = 5;
		public const int Vagina = 6;
		public const int DeepVagina = 7;
		public const int DeeperVagina = 8;
		public const int Anus = 9;
		public const int Chest = 10;
		public const int Belly = 11;
		public const int Hips = 12;
		public const int LeftGlute = 13;
		public const int RightGlute = 14;
		public const int LeftShoulder = 15;
		public const int LeftArm = 16;
		public const int LeftForearm = 17;
		public const int LeftHand = 18;
		public const int RightShoulder = 19;
		public const int RightArm = 20;
		public const int RightForearm = 21;
		public const int RightHand = 22;
		public const int LeftThigh = 23;
		public const int LeftShin = 24;
		public const int LeftFoot = 25;
		public const int RightThigh = 26;
		public const int RightShin = 27;
		public const int RightFoot = 28;
		public const int Eyes = 29;
		public const int Pectorals = 30;
		public const int Penis = 31;

		public const int Count = 32;
		public int GetCount() { return 32; }


		private static string[] names_ = new string[]
		{
			"head",
			"lips",
			"mouth",
			"leftBreast",
			"rightBreast",
			"labia",
			"vagina",
			"deepVagina",
			"deeperVagina",
			"anus",
			"chest",
			"belly",
			"hips",
			"leftGlute",
			"rightGlute",
			"leftShoulder",
			"leftArm",
			"leftForearm",
			"leftHand",
			"rightShoulder",
			"rightArm",
			"rightForearm",
			"rightHand",
			"leftThigh",
			"leftShin",
			"leftFoot",
			"rightThigh",
			"rightShin",
			"rightFoot",
			"eyes",
			"pectorals",
			"penis",
		};

		public static int FromString(string s)
		{
			for (int i = 0; i<names_.Length; ++i)
			{
				if (names_[i] == s)
					return i;
			}

			return -1;
		}

		public string GetName(int i)
		{
			return ToString(i);
		}

		public static string ToString(int i)
		{
			if (i >= 0 && i < names_.Length)
				return names_[i];
			else
				return $"?{i}";
		}

		public static string[] Names
		{
			get { return names_; }
		}
	}
}
