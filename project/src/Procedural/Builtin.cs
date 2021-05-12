using System.Collections.Generic;

namespace Cue.Proc
{
	class BuiltinAnimations
	{
		public static List<Animation> Get()
		{
			var list = new List<Animation>();

			list.Add(Stand(PersonState.Walking));
			list.Add(Stand(PersonState.Standing));
			list.Add(StandIdle());

			return list;
		}

		private static Animation Stand(int from)
		{
			var a = new ProcAnimation("stand");

			var neutral = a.AddStep();
			neutral.AddController("headControl", new Vector3(0, 1.6f, 0), new Vector3(0, 0, 0));
			neutral.AddController("chestControl", new Vector3(0, 1.4f, 0), new Vector3(20, 0, 0));
			neutral.AddController("hipControl", new Vector3(0, 1.1f, 0), new Vector3(340, 10, 0));
			neutral.AddController("lHandControl", new Vector3(-0.2f, 0.9f, 0), new Vector3(0, 10, 90));
			neutral.AddController("rHandControl", new Vector3(0.2f, 0.9f, 0), new Vector3(0, 0, 270));
			neutral.AddController("lFootControl", new Vector3(-0.1f, 0, 0), new Vector3(20, 10, 0));
			neutral.AddController("rFootControl", new Vector3(0.1f, 0, -0.1f), new Vector3(20, 10, 0));

			return new Animation(
				Animation.TransitionType,
				from, PersonState.Standing,
				PersonState.None, Sexes.Any, a);
		}

		private static Animation StandIdle()
		{
			var a = new ProcAnimation("standIdle");

			var neutral = a.AddStep();

			neutral.AddController("headControl", new Vector3(0, 1.6f, 0), new Vector3(0, 0, 0));
			neutral.AddController("chestControl", new Vector3(0, 1.4f, 0), new Vector3(20, 0, 0));
			neutral.AddController("hipControl", new Vector3(0, 1.1f, 0), new Vector3(340, 10, 0));
			neutral.AddController("lHandControl", new Vector3(-0.2f, 0.9f, 0), new Vector3(0, 10, 90));
			neutral.AddController("rHandControl", new Vector3(0.2f, 0.9f, 0), new Vector3(0, 0, 270));
			neutral.AddController("lFootControl", new Vector3(-0.1f, 0, 0), new Vector3(20, 10, 0));
			neutral.AddController("rFootControl", new Vector3(0.1f, 0, -0.1f), new Vector3(20, 10, 0));

			return new Animation(
				Animation.IdleType, PersonState.None, PersonState.None,
				PersonState.Standing, Sexes.Any, a);
		}
	}
}
