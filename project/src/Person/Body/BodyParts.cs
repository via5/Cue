namespace Cue
{
	class BodyParts
	{
		public const float CloseToDistance = 0.05f;

		private static BodyPartTypes[] breasts_ = new BodyPartTypes[]
		{
			BP.LeftBreast,
			BP.RightBreast
		};

		public static BodyPartTypes[] BreastParts
		{
			get { return breasts_; }
		}


		private static BodyPartTypes[] genitals_ = new BodyPartTypes[]
		{
			BP.Labia, BP.Vagina, BP.DeepVagina, BP.DeeperVagina
		};

		public static BodyPartTypes[] GenitalParts
		{
			get { return genitals_; }
		}


		private static BodyPartTypes[] personalSpace_ = new BodyPartTypes[]
		{
			BP.LeftHand, BP.RightHand,
			BP.Head, BP.Chest, BP.Hips,
			BP.Labia, BP.Penis,
			BP.LeftFoot, BP.RightFoot
		};

		public static BodyPartTypes[] PersonalSpaceParts
		{
			get { return personalSpace_; }
		}


		private static BodyPartTypes[] groped_ = new BodyPartTypes[]
		{
			BP.Head, BP.LeftBreast, BP.RightBreast, BP.Labia, BP.Penis
		};

		public static BodyPartTypes[] GropedParts
		{
			get { return groped_; }
		}


		private static BodyPartTypes[] gropedBy_ = new BodyPartTypes[]
		{
			BP.LeftHand, BP.RightHand, BP.LeftFoot, BP.RightFoot
		};

		public static BodyPartTypes[] GropedByParts
		{
			get { return gropedBy_; }
		}


		private static BodyPartTypes[] penetrated_ = new BodyPartTypes[]
		{
			BP.Labia, BP.Vagina, BP.DeepVagina, BP.DeeperVagina, BP.Anus
		};

		public static BodyPartTypes[] PenetratedParts
		{
			get { return penetrated_; }
		}


		private static BodyPartTypes[] penetratedBy_ = new BodyPartTypes[]
		{
			BP.Penis
		};

		public static BodyPartTypes[] PenetratedByParts
		{
			get { return penetratedBy_; }
		}
	}
}
