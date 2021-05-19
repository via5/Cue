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
			//list.Add(Orgasm());

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

		private static Animation Orgasm()
		{
			var a = new ProcAnimation("orgasm", true);

			var seq = new SequentialTargetGroup("seq");

			{
				var min = new Vector3(-30, 0, 100);
				var max = new Vector3( 30, 0, 150);
				var d = new Duration(0.05f, 0.1f);
				var delay = new Duration(0, 0.5f);

				var g = new ConcurrentTargetGroup(
					"fast", new Duration(0, 0), new Duration(4, 4), false);

				g.AddTarget(new Force(
					Force.RelativeForce, BodyParts.Hips, "hip",
					min, max,
					d, Duration.Zero,
					Duration.Zero, delay,
					Force.Loop | Force.ResetBetween));

				g.AddTarget(new Force(
					Force.RelativeForce, BodyParts.Chest, "chest",
					min, max,
					d, Duration.Zero,
					Duration.Zero, delay,
					Force.Loop | Force.ResetBetween));

				seq.AddTarget(g);
			}

			{
				var min = new Vector3(-20, 0, 50);
				var max = new Vector3(20, 0, 100);
				var d = new Duration(0.05f, 0.1f);
				var delay = new Duration(0, 1);

				var g = new ConcurrentTargetGroup(
					"slow", new Duration(0, 0), new Duration(4, 4), false);

				g.AddTarget(new Force(
					Force.RelativeForce, BodyParts.Hips, "hip",
					min, max,
					d, Duration.Zero,
					Duration.Zero, delay,
					Force.Loop | Force.ResetBetween));

				g.AddTarget(new Force(
					Force.RelativeForce, BodyParts.Chest, "chest",
					min, max,
					d, Duration.Zero,
					Duration.Zero, delay,
					Force.Loop | Force.ResetBetween));

				seq.AddTarget(g);
			}

			a.AddTarget(seq);

			return new Animation(
				Animation.OrgasmType,
				PersonState.None, PersonState.None, PersonState.None,
				Sexes.Female, a);
		}

		private static Animation StandIdle()
		{
			var a = new ProcAnimation("standIdle", true);

			ConcurrentTargetGroup g = new ConcurrentTargetGroup("sway");

			var forceMin = new Vector3(-50, 0, -50);
			var forceMax = new Vector3(50, 0, 50);
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
				g.AddTarget(new Force(
					Force.RelativeForce, p.second, p.first,
					forceMin, forceMax,
					d, d, delay, delay, Force.Loop));

				g.AddTarget(new Force(
					Force.RelativeTorque, p.second, p.first,
					new Vector3(-torque, -torque, -torque),
					new Vector3(torque, torque, torque),
					d, d, delay, delay, Force.Loop));
			}


			var headForceMin = new Vector3(-20, 0, -20);
			var headForceMax = new Vector3(20, 0, 20);

			g.AddTarget(new Force(
				Force.RelativeForce, BodyParts.Head, "head",
				headForceMin, headForceMax, d, d, delay, delay, Force.Loop));

			g.AddTarget(new Morph(
				BodyParts.RightHand, "Right Fingers Fist", 0.1f, 0.4f,
				d, delay));

			a.AddTarget(g);



			/*var fg = new SequentialTargetGroup("footup", new Duration(1, 1));


			{
				g = new ConcurrentTargetGroup("footup1", new Duration(0, 0), false);

				d = new Duration(0.7f, 0.7f);
				float f = 50;
				float ff = 75;

				g.AddTarget(new Force(
					BodyParts.RightFoot, "rFoot",
					new Vector3(0, ff, -ff), new Vector3(0, ff, -ff),
					new Duration(d), new Duration(0, 0), false));

				g.AddTarget(new Torque(
					BodyParts.RightFoot, "rFoot",
					new Vector3(10, 0, 0), new Vector3(10, 0, 0),
					new Duration(d), new Duration(0, 0), false));

				g.AddTarget(new Force(
					BodyParts.Hips, "hip",
					new Vector3(-f, 0, 0), new Vector3(-f, 0, 0),
					new Duration(d), new Duration(0, 0), false));

				g.AddTarget(new Force(
					BodyParts.Hips, "chest",
					new Vector3(-f, 0, 0), new Vector3(-f, 0, 0),
					new Duration(d), new Duration(0, 0), false));

				g.AddTarget(new Force(
					BodyParts.Hips, "head",
					new Vector3(-f, 0, 0), new Vector3(-f, 0, 0),
					new Duration(d), new Duration(0, 0), false));

				g.AddTarget(new Force(
					BodyParts.RightThigh, "rThigh",
					new Vector3(0, 0, f), new Vector3(0, 0, f),
					new Duration(d), new Duration(0, 0), false));

				fg.AddTarget(g);
			}

			{
				g = new ConcurrentTargetGroup("footup2", new Duration(0, 0), false);

				d = new Duration(0.7f, 0.7f);
				float f = 50;
				float ff = 75;

				g.AddTarget(new Force(
					BodyParts.RightFoot, "lFoot",
					new Vector3(0, ff, -ff), new Vector3(0, ff, -ff),
					new Duration(d), new Duration(0, 0), false));

				g.AddTarget(new Torque(
					BodyParts.RightFoot, "lFoot",
					new Vector3(10, 0, 0), new Vector3(10, 0, 0),
					new Duration(d), new Duration(0, 0), false));

				g.AddTarget(new Force(
					BodyParts.Hips, "hip",
					new Vector3(f, 0, 0), new Vector3(f, 0, 0),
					new Duration(d), new Duration(0, 0), false));

				g.AddTarget(new Force(
					BodyParts.Hips, "chest",
					new Vector3(f, 0, 0), new Vector3(f, 0, 0),
					new Duration(d), new Duration(0, 0), false));

				g.AddTarget(new Force(
					BodyParts.Hips, "head",
					new Vector3(f, 0, 0), new Vector3(f, 0, 0),
					new Duration(d), new Duration(0, 0), false));

				g.AddTarget(new Force(
					BodyParts.RightThigh, "lThigh",
					new Vector3(0, 0, f), new Vector3(0, 0, f),
					new Duration(d), new Duration(0, 0), false));

				fg.AddTarget(g);
			}

			a.AddTarget(fg);*/

			return new Animation(
				Animation.IdleType, PersonState.None, PersonState.None,
				PersonState.Standing, Sexes.Any, a);
		}
	}
}
