namespace Cue
{
	class BodyParts : BodyPartsEnum
	{
		private static int[] breasts_ = new int[]
		{
			LeftBreast,
			RightBreast
		};

		public static int[] BreastParts
		{
			get { return breasts_; }
		}


		private static int[] genitals_ = new int[]
		{
			Labia, Vagina, DeepVagina, DeeperVagina
		};

		public static int[] GenitalParts
		{
			get { return genitals_; }
		}


		private static int[] personalSpace_ = new[]
		{
			LeftHand, RightHand,
			Head, Chest, Hips,
			Labia, Penis,
			LeftFoot, RightFoot
		};

		public static int[] PersonalSpaceParts
		{
			get { return personalSpace_; }
		}


		private static int[] groped_ = new[]
		{
			Head, LeftBreast, RightBreast, Labia, Penis
		};

		public static int[] GropedParts
		{
			get { return groped_; }
		}


		private static int[] gropedBy_ = new[]
		{
			Head, LeftHand, RightHand, LeftFoot, RightFoot
		};

		public static int[] GropedByParts
		{
			get { return gropedBy_; }
		}


		private static int[] penetrated_ = new[]
		{
			Labia, Vagina, DeepVagina, DeeperVagina, Anus
		};

		public static int[] PenetratedParts
		{
			get { return penetrated_; }
		}


		private static int[] penetratedBy_ = new[]
		{
			Penis
		};

		public static int[] PenetratedByParts
		{
			get { return penetratedBy_; }
		}
	}
}
