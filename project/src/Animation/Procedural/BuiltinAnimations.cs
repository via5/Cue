using System.Collections.Generic;

namespace Cue.Proc
{
	class BuiltinAnimations
	{
		public static List<Animation> Get()
		{
			var list = new List<Animation>();

			list.Add(Create<SexProcAnimation>(Animations.Sex));
			list.Add(Create<SmokeProcAnimation>(Animations.Smoke));
			list.Add(Create<SuckProcAnimation>(Animations.Suck));
			list.Add(Create<PenetratedProcAnimation>(Animations.Penetrated));
			list.Add(Create<LeftFingerProcAnimation>(Animations.LeftFinger));
			list.Add(Create<RightFingerProcAnimation>(Animations.RightFinger));

			list.Add(Create<ClockwiseKiss>(Animations.Kiss));
			list.Add(Create<ClockwiseBJ>(Animations.BJ));
			list.Add(Create<ClockwiseHJBoth>(Animations.HJBoth));
			list.Add(Create<ClockwiseHJLeft>(Animations.HJLeft));
			list.Add(Create<ClockwiseHJRight>(Animations.HJRight));

			return list;
		}

		private static Animation Create<T>(int type)
			where T : BasicProcAnimation, new()
		{
			var a = new T();
			return new Animation(type, MovementStyles.Any, a);
		}
	}
}
