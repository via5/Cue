namespace Cue
{
	class BodyParts
	{
		public const int None = -1;

		public const int Head = 0;
		public const int Lips = 1;
		public const int Mouth = 2;

		// female
		public const int LeftBreast = 3;
		public const int RightBreast = 4;
		public const int Labia = 5;
		public const int Vagina = 6;
		public const int DeepVagina = 7;
		public const int DeeperVagina = 8;

		// all
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

		// male
		public const int Pectorals = 30;
		public const int Penis = 31;


		public const int Count = 32;


		private static int[] breasts_ = new int[] { LeftBreast, RightBreast };
		public static int[] BreastParts { get { return breasts_; } }

		private static int[] genitals_ = new int[] {
			Labia, Vagina, DeepVagina, DeeperVagina };
		public static int[] GenitalParts { get { return genitals_; } }
		public static bool IsGenitalPart(int i)
		{
			return
				(i == Labia) ||
				(i == Vagina) ||
				(i == DeepVagina) ||
				(i == DeeperVagina);
		}

		private static int[] personalSpace_ = new[]{
			LeftHand, RightHand, Head, Chest, Hips, Labia, Penis,
			LeftFoot, RightFoot};
		public static int[] PersonalSpaceParts { get { return personalSpace_; } }

		private static int[] groped_ = new[] {
			Head, LeftBreast, RightBreast, Labia, Penis };
		public static int[] GropedParts { get { return groped_; } }

		private static int[] gropedBy_ = new[] {
			Head, LeftHand, RightHand, LeftFoot, RightFoot };
		public static int[] GropedByParts { get { return gropedBy_; } }

		private static int[] penetrated_ = new[]{
			Labia, Vagina, DeepVagina, DeeperVagina, Anus };
		public static int[] PenetratedParts { get { return penetrated_; } }

		private static int[] penetratedBy_ = new[]{
			Penis };
		public static int[] PenetratedByParts { get { return penetratedBy_; } }


		public static bool IsHandPart(int i)
		{
			return (i == LeftHand) || (i == RightHand);
		}


		private static string[] names_ = new string[]
		{
			"head", "lips", "mouth", "leftbreast", "rightbreast",
			"labia", "vagina", "deepvagina", "deepervagina", "anus",

			"chest", "belly", "hips", "leftglute", "rightglute",

			"leftshoulder", "leftarm", "leftforearm", "lefthand",
			"rightshoulder", "rightarm", "rightforearm", "righthand",

			"leftthigh", "leftshin", "leftfoot",
			"rightthigh", "rightshin", "rightfoot",

			"eyes", "genitals", "pectorals", "penis"
		};

		public static int FromString(string s)
		{
			for (int i = 0; i < names_.Length; ++i)
			{
				if (names_[i] == s)
					return i;
			}

			if (s == "unknown")
				return -1;

			return None;
		}

		public static string ToString(int t)
		{
			if (t >= 0 && t < names_.Length)
				return names_[t];

			return $"?{t}";
		}
	}
}
