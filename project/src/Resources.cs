using System.Collections.Generic;

namespace Cue.Resources
{
	class Animations
	{
		private static IAnimation walk_ = null;
		private static IAnimation sit_ = null;
		private static List<IAnimation> sitIdle_ = new List<IAnimation>();
		private static List<IAnimation> standIdle_ = new List<IAnimation>();

		public static IAnimation Walk()
		{
			if (walk_ == null)
			{
				walk_ = new BVH.Animation(
					"Custom\\Scripts\\VAMDeluxe\\Synthia Movement System\\Animations\\StandToWalk.bvh",
					true, false, false, 67, 150);
			}

			return walk_;
		}

		public static IAnimation Sit()
		{
			if (sit_ == null)
			{
				sit_ = new BVH.Animation(
					"Custom\\Animations\\bvh_files\\avatar_sit_female.bvh",
					false, true, true, 0, 30);
			}

			return sit_;
		}

		public static List<IAnimation> SitIdles()
		{
			if (sitIdle_.Count == 0)
			{
				sitIdle_ = new List<IAnimation>()
				{
					new BVH.Animation("Custom/Animations/Cassandra AO/Cassandra sit.bvh"),
					new BVH.Animation("Custom/Animations/V3_BVH_Ambient_Motions/sitting_gesturing_ambient_1.bvh"),
					new BVH.Animation("Custom/Animations/V3_BVH_Ambient_Motions/sitting_gesturing_ambient_3.bvh"),
					new BVH.Animation("Custom/Animations/V3_BVH_Ambient_Motions/sitting_gesturing_ambient_4.bvh"),
					new BVH.Animation("Custom/Animations/V3_BVH_Ambient_Motions/sitting_gesturing_ambient_5.bvh")
				};
			}

			return sitIdle_;
		}

		public static List<IAnimation> StandIdles()
		{
			if (standIdle_.Count == 0)
			{
				standIdle_ = new List<IAnimation>()
				{
					new BVH.Animation("Custom/Animations/bvh_files/avatar_stand_1.bvh", false, true, true),
					new BVH.Animation("Custom/Animations/bvh_files/avatar_stand_2.bvh", false, true, true),
					new BVH.Animation("Custom/Animations/bvh_files/avatar_stand_3.bvh", false, true, true),
					new BVH.Animation("Custom/Animations/bvh_files/avatar_stand_4.bvh", false, true, true)
				};
			}

			return standIdle_;
		}
	}
}
