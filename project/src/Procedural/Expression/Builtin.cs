namespace Cue.Proc
{
	class BuiltinExpressions
	{
		public static IProceduralMorphGroup Smile(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();
			g.Add(new ClampableMorph(p, "Smile Open Full Face", 0, 1, 0.3f, 3, 2, 1));
			return g;
		}

		public static IProceduralMorphGroup CornerSmile(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();
			g.Add(new ClampableMorph(p, "Mouth Smile Simple Left", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Pleasure(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();

			g.Add(new ClampableMorph(p, "07-Extreme Pleasure", 0, 1, 1, 5, 2, 2));
			g.Add(new ClampableMorph(p, "Pain", 0, 0.5f, 1, 5, 2, 2));
			g.Add(new ClampableMorph(p, "Shock", 0, 1, 1, 5, 2, 2));
			g.Add(new ClampableMorph(p, "Scream", 0, 1, 1, 5, 2, 2));
			g.Add(new ClampableMorph(p, "Angry", 0, 0.3f, 1, 5, 2, 2));

			return g;
		}

		public static IProceduralMorphGroup EyesRollBack(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();

			if (p.Sex == Sexes.Female)
				g.Add(new ClampableMorph(p, "Eye Roll Back_DD", 0, 0.5f, 1, 5, 2, 2));

			return g;
		}

		public static IProceduralMorphGroup EyesClosed(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();

			var eyesClosed = new ClampableMorph(p, "Eyes Closed", 0, 1, 0.5f, 5, 2, 2);
			eyesClosed.DisableBlinkAbove = 0.5f;
			g.Add(eyesClosed);

			return g;
		}

		public static IProceduralMorphGroup Swallow(Person p)
		{
			var g = new SequentialProceduralMorphGroup(
				new Duration(40, 60));


			var m = new ClampableMorph(p, "Mouth Open",
				-0.1f, -0.1f, 0.3f, 0.3f, 0, 0, true);
			m.Easing = new SineOutEasing();

			g.Add(m);


			m = new ClampableMorph(p, "deepthroat",
				0.1f, 0.1f, 0.2f, 0.2f, 0, 0, true);

			g.Add(m);


			m = new ClampableMorph(p, "Mouth Open",
				0.1f, 0.2f, 0.3f, 0.3f, 0, 0, true);
			m.Easing = new SineOutEasing();

			g.Add(m);

			return g;
		}

		public static IProceduralMorphGroup Frown(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();
			g.Add(new ClampableMorph(p, "Brow Down", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Squint(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();
			g.Add(new ClampableMorph(p, "Eyes Squint", 0, 1, 1, 5, 2, 2));
			g.Add(new ClampableMorph(p, "Nose Wrinkle", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup MouthFrown(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();
			g.Add(new ClampableMorph(p, "Mouth Corner Up-Down", 0, -0.5f, 1, 5, 2, 2));
			g.Add(new ClampableMorph(p, "Lip Top Up", 0, 0.3f, 1, 5, 2, 2));
			return g;
		}
	}
}
