namespace Cue
{
	class BodyParts
	{
		public const float PersonalSpaceDistance = 0.25f;

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


		private static BodyPartType[] genitals_ = new BodyPartType[]
		{
			BP.Vagina, BP.Penis, BP.Anus
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


		private static BodyPartType[] groping_ = new BodyPartType[]
		{
			BP.LeftHand, BP.RightHand, BP.LeftFoot, BP.RightFoot
		};

		public static BodyPartType[] GropingParts
		{
			get { return groping_; }
		}
	}
}
