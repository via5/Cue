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

			return list;
		}

		private static Animation Stand(int from)
		{
			var a = new ProcAnimation("backToNeutral");

			a.AddTarget(new Controller("headControl", new Vector3(0, 1.6f, 0), new Vector3(0, 0, 0)));
			a.AddTarget(new Controller("chestControl", new Vector3(0, 1.4f, 0), new Vector3(20, 0, 0)));
			a.AddTarget(new Controller("hipControl", new Vector3(0, 1.1f, 0), new Vector3(340, 10, 0)));
			a.AddTarget(new Controller("lHandControl", new Vector3(-0.2f, 0.9f, 0), new Vector3(0, 10, 90)));
			a.AddTarget(new Controller("rHandControl", new Vector3(0.2f, 0.9f, 0), new Vector3(0, 0, 270)));
			a.AddTarget(new Controller("lFootControl", new Vector3(-0.1f, 0, 0), new Vector3(20, 10, 0)));
			a.AddTarget(new Controller("rFootControl", new Vector3(0.1f, 0, -0.1f), new Vector3(20, 10, 0)));

			return new Animation(
				Animation.TransitionType,
				from, PersonState.Standing,
				PersonState.None, Sexes.Any, a);
		}
	}
}
