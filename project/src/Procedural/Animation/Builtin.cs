﻿using System.Collections.Generic;

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
			var a = new ProcAnimation("backToNeutral");

			var s = a.AddStep();
			s.AddTarget(new Controller("headControl", new Vector3(0, 1.6f, 0), new Vector3(0, 0, 0)));
			s.AddTarget(new Controller("chestControl", new Vector3(0, 1.4f, 0), new Vector3(20, 0, 0)));
			s.AddTarget(new Controller("hipControl", new Vector3(0, 1.1f, 0), new Vector3(340, 10, 0)));
			s.AddTarget(new Controller("lHandControl", new Vector3(-0.2f, 0.9f, 0), new Vector3(0, 10, 90)));
			s.AddTarget(new Controller("rHandControl", new Vector3(0.2f, 0.9f, 0), new Vector3(0, 0, 270)));
			s.AddTarget(new Controller("lFootControl", new Vector3(-0.1f, 0, 0), new Vector3(20, 10, 0)));
			s.AddTarget(new Controller("rFootControl", new Vector3(0.1f, 0, -0.1f), new Vector3(20, 10, 0)));

			return new Animation(
				Animation.TransitionType,
				from, PersonState.Standing,
				PersonState.None, Sexes.Any, a);
		}

		private static Animation StandIdle()
		{
			var a = new ProcAnimation("standIdle", true);

			var s = a.AddStep();

			var forceMin = new Vector3(-50, 0, -50);
			var forceMax = new Vector3(50, 50, 50);
			float torque = 5;
			Duration d = new Duration(1, 6);
			Duration delay = new Duration(0, 6);

			var forceAndTorque = new Pair<string, int>[]
			{
				new Pair<string, int>("hip", BodyParts.Hips),
				new Pair<string, int>("chest", BodyParts.Chest),
				new Pair<string, int>("lHand", BodyParts.LeftHand),
				new Pair<string, int>("rHand", BodyParts.RightHand)
			};


			foreach (var p in forceAndTorque)
			{
				s.AddTarget(new Force(
					p.second, p.first, forceMin, forceMax,
					new Duration(d), new Duration(delay)));

				s.AddTarget(new Torque(
					p.second, p.first,
					new Vector3(-torque, -torque, -torque),
					new Vector3(torque, torque, torque),
					new Duration(d), new Duration(delay)));
			}


			var headForceMin = new Vector3(-20, 0, -20);
			var headForceMax = new Vector3(20, 20, 20);

			s.AddTarget(new Force(
				BodyParts.Head, "head", headForceMin, headForceMax,
				new Duration(d), new Duration(delay)));

			s.AddTarget(new Morph(
				BodyParts.RightHand, "Right Fingers Fist", 0.1f, 0.4f,
				new Duration(d), new Duration(delay)));

			return new Animation(
				Animation.IdleType, PersonState.None, PersonState.None,
				PersonState.Standing, Sexes.Any, a);
		}
	}
}
