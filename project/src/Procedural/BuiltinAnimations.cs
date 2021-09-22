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
			list.Add(Sex());
			list.Add(Smoke());
			list.Add(Suck());
			list.Add(Penetrated());

			return list;
		}

		private static Animation Stand(int from)
		{
			var a = new ProcAnimation("backToNeutral");

			var s = new ElapsedSync(1);

			a.AddTarget(new Controller("headControl", new Vector3(0, 1.6f, 0), new Vector3(0, 0, 0), s.Clone()));
			a.AddTarget(new Controller("chestControl", new Vector3(0, 1.4f, 0), new Vector3(20, 0, 0), s.Clone()));
			a.AddTarget(new Controller("hipControl", new Vector3(0, 1.1f, 0), new Vector3(340, 10, 0), s.Clone()));
			a.AddTarget(new Controller("lHandControl", new Vector3(-0.2f, 0.9f, 0), new Vector3(0, 10, 90), s.Clone()));
			a.AddTarget(new Controller("rHandControl", new Vector3(0.2f, 0.9f, 0), new Vector3(0, 0, 270), s.Clone()));
			a.AddTarget(new Controller("lFootControl", new Vector3(-0.1f, 0, 0), new Vector3(20, 10, 0), s.Clone()));
			a.AddTarget(new Controller("rFootControl", new Vector3(0.1f, 0, -0.1f), new Vector3(20, 10, 0), s.Clone()));

			return new Animation(
				Animations.Transition,
				from, PersonState.Standing,
				PersonState.None, MovementStyles.Any, a);
		}

		private static Animation Sex()
		{
			var a = new SexProcAnimation();

			return new Animation(
				Animations.Sex,
				PersonState.None, PersonState.None,
				PersonState.None, MovementStyles.Any, a);
		}

		private static Animation Smoke()
		{
			var a = new SmokeProcAnimation();

			return new Animation(
				Animations.Smoke,
				PersonState.None, PersonState.None,
				PersonState.None, MovementStyles.Any, a);
		}

		private static Animation Suck()
		{
			var a = new SuckProcAnimation();

			return new Animation(
				Animations.Suck,
				PersonState.None, PersonState.None,
				PersonState.None, MovementStyles.Any, a);
		}

		private static Animation Penetrated()
		{
			var a = new PenetratedAnimation();

			return new Animation(
				Animations.Penetrated,
				PersonState.None, PersonState.None,
				PersonState.None, MovementStyles.Any, a);
		}
	}
}
