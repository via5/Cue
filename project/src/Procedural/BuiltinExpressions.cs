namespace Cue.Proc
{
	class BuiltinExpressions
	{
		public static IProceduralMorphGroup Smile(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("smile");
			g.Add(new ClampableMorph(p, BodyParts.None, "Smile Open Full Face", 0, 1, 0.3f, 3, 2, 1));
			return g;
		}

		public static IProceduralMorphGroup CornerSmile(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("cornerSmile");
			g.Add(new ClampableMorph(p, BodyParts.None, "Mouth Smile Simple Left", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Pleasure(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("pleasure");

			g.Add(new ClampableMorph(p, BodyParts.None, "07-Extreme Pleasure", 0, 1, 1, 5, 2, 2));
			g.Add(new ClampableMorph(p, BodyParts.None, "Pain", 0, 0.5f, 1, 5, 2, 2));
			g.Add(new ClampableMorph(p, BodyParts.None, "Shock", 0, 1, 1, 5, 2, 2));
			g.Add(new ClampableMorph(p, BodyParts.None, "Scream", 0, 1, 1, 5, 2, 2));
			g.Add(new ClampableMorph(p, BodyParts.None, "Angry", 0, 0.3f, 1, 5, 2, 2));

			return g;
		}

		public static IProceduralMorphGroup EyesRollBack(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("eyesRollback");

			if (p.Sex == Sexes.Female)
				g.Add(new ClampableMorph(p, BodyParts.None, "Eye Roll Back_DD", 0, 0.4f, 1, 5, 2, 2));

			return g;
		}

		public static IProceduralMorphGroup EyesClosed(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("eyesClosed");

			var eyesClosed = new ClampableMorph(p, BodyParts.None, "Eyes Closed", 0, 1, 0.5f, 5, 2, 2);
			eyesClosed.DisableBlinkAbove = 0.5f;
			g.Add(eyesClosed);

			return g;
		}

		public static IProceduralMorphGroup Swallow(Person p)
		{
			var g = new SequentialProceduralMorphGroup(
				"swallow", new Duration(40, 60));


			var m = new ClampableMorph(p, BodyParts.None, "Mouth Open",
				-0.1f, -0.1f, 0.3f, 0.3f, 0, 0, true);
			m.Easing = new SineOutEasing();

			g.Add(m);


			m = new ClampableMorph(p, BodyParts.None, "deepthroat",
				0.1f, 0.1f, 0.2f, 0.2f, 0, 0, true);

			g.Add(m);


			m = new ClampableMorph(p, BodyParts.None, "Mouth Open",
				0.1f, 0.2f, 0.3f, 0.3f, 0, 0, true);
			m.Easing = new SineOutEasing();

			g.Add(m);

			return g;
		}

		public static IProceduralMorphGroup Frown(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("frown");
			g.Add(new ClampableMorph(p, BodyParts.None, "Brow Down", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Squint(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("squint");
			g.Add(new ClampableMorph(p, BodyParts.None, "Eyes Squint", 0, 1, 1, 5, 2, 2));
			g.Add(new ClampableMorph(p, BodyParts.None, "Nose Wrinkle", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup MouthFrown(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("mouthFrown");
			g.Add(new ClampableMorph(p, BodyParts.None, "Mouth Corner Up-Down", 0, -0.5f, 1, 5, 2, 2));
			g.Add(new ClampableMorph(p, BodyParts.None, "Lip Top Up", 0, 0.3f, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Drooling(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("drooling");
			g.Add(new ClampableMorph(p, BodyParts.None, "Mouth Open", 0, 1, 2, 5, 3, 3));
			return g;
		}

		public static IProceduralMorphGroup EyesClosedTired(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("eyesClosedTired");

			var eyesClosed = new ClampableMorph(p, BodyParts.None, "Eyes Closed", 0.2f, 1, 2, 5, 3, 3);
			eyesClosed.DisableBlinkAbove = 0.5f;
			g.Add(eyesClosed);

			return g;
		}
	}
}
