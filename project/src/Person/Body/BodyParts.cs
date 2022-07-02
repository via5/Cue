namespace Cue
{
	class BodyParts
	{
		public const float CloseToDistance = 0.05f;

		private static BodyPartType[] fullLeftArm_ = new BodyPartType[]
		{
			BP.LeftShoulder,
			BP.LeftArm,
			BP.LeftElbow,
			BP.LeftForearm,
			BP.LeftHand,
		};

		public static BodyPartType[] FullLeftArm
		{
			get { return fullLeftArm_ ; }
		}


		private static BodyPartType[] fullRightArm_ = new BodyPartType[]
		{
			BP.RightShoulder,
			BP.RightArm,
			BP.RightElbow,
			BP.RightForearm,
			BP.RightHand,
		};

		public static BodyPartType[] FullRightArm
		{
			get { return fullRightArm_; }
		}


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
			BP.Vagina, BP.DeepVagina
		};

		public static BodyPartType[] GenitalParts
		{
			get { return genitals_; }
		}


		private static BodyPartType[] personalSpace_ = new BodyPartType[]
		{
			BP.LeftHand, BP.RightHand,
			BP.Head, BP.Chest, BP.Hips,
			BP.Vagina, BP.Penis,
			BP.LeftFoot, BP.RightFoot
		};

		public static BodyPartType[] PersonalSpaceParts
		{
			get { return personalSpace_; }
		}


		private static BodyPartType[] groped_ = new BodyPartType[]
		{
			BP.Head, BP.LeftBreast, BP.RightBreast, BP.Vagina, BP.Penis
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
			BP.Vagina, BP.DeepVagina, BP.Anus
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
