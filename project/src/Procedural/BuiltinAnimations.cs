using System.Collections.Generic;

namespace Cue.Proc
{
	class BuiltinAnimations
	{
		public static List<Animation> Get()
		{
			var list = new List<Animation>();

			list.Add(Sex());
			list.Add(Smoke());
			list.Add(Suck());
			//list.Add(Penetrated());
			list.Add(LeftFinger());
			list.Add(RightFinger());

			return list;
		}

		private static Animation Sex()
		{
			var a = new SexProcAnimation();
			return new Animation(Animations.Sex, MovementStyles.Any, a);
		}

		private static Animation Smoke()
		{
			var a = new SmokeProcAnimation();
			return new Animation(Animations.Smoke, MovementStyles.Any, a);
		}

		private static Animation Suck()
		{
			var a = new SuckProcAnimation();
			return new Animation(Animations.Suck, MovementStyles.Any, a);
		}

		//private static Animation Penetrated()
		//{
		//	var a = new PenetratedAnimation();
		//
		//	return new Animation(
		//		Animations.Penetrated,
		//		PersonState.None, PersonState.None,
		//		PersonState.None, MovementStyles.Any, a);
		//}

		private static Animation LeftFinger()
		{
			var a = new LeftFingerProcAnimation();
			return new Animation(Animations.LeftFinger, MovementStyles.Any, a);
		}

		private static Animation RightFinger()
		{
			var a = new RightFingerProcAnimation();
			return new Animation(Animations.RightFinger, MovementStyles.Any, a);
		}
	}
}
