namespace Cue.Proc
{
	class BuiltinExpressions
	{
		public static IProceduralMorphGroup Smile(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();
			g.Add(new Morph(p, "Smile Open Full Face", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup CornerSmile(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();
			g.Add(new Morph(p, "Mouth Smile Simple Left", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Pleasure(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();

			g.Add(new Morph(p, "07-Extreme Pleasure", 0, 1, 1, 5, 2, 2));
			g.Add(new Morph(p, "Pain", 0, 0.5f, 1, 5, 2, 2));
			g.Add(new Morph(p, "Shock", 0, 1, 1, 5, 2, 2));
			g.Add(new Morph(p, "Scream", 0, 1, 1, 5, 2, 2));
			g.Add(new Morph(p, "Angry", 0, 0.3f, 1, 5, 2, 2));

			return g;
		}

		public static IProceduralMorphGroup EyesRollBack(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();

			if (p.Sex == Sexes.Female)
				g.Add(new Morph(p, "Eye Roll Back_DD", 0, 0.5f, 1, 5, 2, 2));

			return g;
		}

		public static IProceduralMorphGroup EyesClosed(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();

			var eyesClosed = new Morph(p, "Eyes Closed", 0, 1, 0.5f, 5, 2, 2);
			eyesClosed.DisableBlinkAbove = 0.5f;
			g.Add(eyesClosed);

			return g;
		}

		public static IProceduralMorphGroup Swallow(Person p)
		{
			var g = new SequentialProceduralMorphGroup(
				new Duration(40, 60));


			var m = new Morph(p, "Mouth Open",
				-0.1f, -0.1f, 0.3f, 0.3f, 0, 0, true);
			m.Easing = new SineOutEasing();

			g.Add(m);


			m = new Morph(p, "deepthroat",
				0.1f, 0.1f, 0.2f, 0.2f, 0, 0, true);

			g.Add(m);


			m = new Morph(p, "Mouth Open",
				0.1f, 0.2f, 0.3f, 0.3f, 0, 0, true);
			m.Easing = new SineOutEasing();

			g.Add(m);

			return g;
		}

		public static IProceduralMorphGroup Frown(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();
			g.Add(new Morph(p, "Brow Down", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Squint(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();
			g.Add(new Morph(p, "Eyes Squint", 0, 1, 1, 5, 2, 2));
			g.Add(new Morph(p, "Nose Wrinkle", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup MouthFrown(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup();
			g.Add(new Morph(p, "Mouth Corner Up-Down", 0, -0.5f, 1, 5, 2, 2));
			g.Add(new Morph(p, "Lip Top Up", 0, 0.3f, 1, 5, 2, 2));
			return g;
		}
	}
}
