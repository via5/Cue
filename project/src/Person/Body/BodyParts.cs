namespace Cue
{
	class BodyParts
	{
		public const float CloseToDistance = 0.05f;

		private static BodyPartType[] breasts_ = new BodyPartType[]
		{
			BP.LeftBreast,
			BP.RightBreast
		};

		public static BodyPartType[] BreastParts
		{
			get { return breasts_; }
		}


		private static BodyPartType[] genitals_ = new BodyPartType[]
		{
			BP.Labia, BP.Vagina, BP.DeepVagina, BP.DeeperVagina
		};

		public static BodyPartType[] GenitalParts
		{
			get { return genitals_; }
		}


		private static BodyPartType[] personalSpace_ = new BodyPartType[]
		{
			BP.LeftHand, BP.RightHand,
			BP.Head, BP.Chest, BP.Hips,
			BP.Labia, BP.Penis,
			BP.LeftFoot, BP.RightFoot
		};

		public static BodyPartType[] PersonalSpaceParts
		{
			get { return personalSpace_; }
		}


		private static BodyPartType[] groped_ = new BodyPartType[]
		{
			BP.Head, BP.LeftBreast, BP.RightBreast, BP.Labia, BP.Penis
		};

		public static BodyPartType[] GropedParts
		{
			get { return groped_; }
		}


		private static BodyPartType[] gropedBy_ = new BodyPartType[]
		{
			BP.LeftHand, BP.RightHand, BP.LeftFoot, BP.RightFoot
		};

		public static BodyPartType[] GropedByParts
		{
			get { return gropedBy_; }
		}


		private static BodyPartType[] penetrated_ = new BodyPartType[]
		{
			BP.Labia, BP.Vagina, BP.DeepVagina, BP.DeeperVagina, BP.Anus
		};

		public static BodyPartType[] PenetratedParts
		{
			get { return penetrated_; }
		}


		private static BodyPartType[] penetratedBy_ = new BodyPartType[]
		{
			BP.Penis
		};

		public static BodyPartType[] PenetratedByParts
		{
			get { return penetratedBy_; }
		}
	}
}
