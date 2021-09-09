﻿namespace Cue.Proc
{
	class BuiltinExpressions
	{
		public static IProceduralMorphGroup Smile(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("smile");
			g.Add(new MorphTarget(p, BodyParts.Mouth, "Smile Open Full Face", 0, 1, 0.3f, 3, 2, 1));
			return g;
		}

		public static IProceduralMorphGroup CornerSmile(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("cornerSmile");
			g.Add(new MorphTarget(p, BodyParts.Mouth, "Mouth Smile Simple Left", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Pleasure(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("pleasure");

			g.Add(new MorphTarget(p, BodyParts.Mouth, "07-Extreme Pleasure", 0, 1, 1, 5, 2, 2));
			g.Add(new MorphTarget(p, BodyParts.Mouth, "Pain", 0, 0.5f, 1, 5, 2, 2));
			g.Add(new MorphTarget(p, BodyParts.Mouth, "Shock", 0, 1, 1, 5, 2, 2));
			g.Add(new MorphTarget(p, BodyParts.Mouth, "Scream", 0, 1, 1, 5, 2, 2));
			g.Add(new MorphTarget(p, BodyParts.Mouth, "Angry", 0, 0.3f, 1, 5, 2, 2));

			return g;
		}

		public static IProceduralMorphGroup EyesRollBack(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("eyesRollback");

			if (p.MovementStyle == MovementStyles.Feminine)
				g.Add(new MorphTarget(p, BodyParts.Eyes, "Eye Roll Back_DD", 0, 0.4f, 1, 5, 2, 2));

			return g;
		}

		public static IProceduralMorphGroup EyesClosed(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("eyesClosed");

			var eyesClosed = new MorphTarget(p, BodyParts.Eyes, "Eyes Closed", 0, 1, 0.5f, 5, 2, 2);
			eyesClosed.DisableBlinkAbove = 0.5f;
			g.Add(eyesClosed);

			return g;
		}

		public static IProceduralMorphGroup Swallow(Person p)
		{
			var g = new SequentialProceduralMorphGroup(
				"swallow", new Duration(40, 60));


			var m = new MorphTarget(p, BodyParts.Mouth, "Mouth Open",
				-0.1f, -0.1f, 0.2f, 0.2f, 0, 0, true);
			m.Easing = new SineOutEasing();

			g.Add(m);


			m = new MorphTarget(p, BodyParts.None, "deepthroat",
				0.1f, 0.1f, 0.2f, 0.2f, 0, 0, true);

			g.Add(m);


			m = new MorphTarget(p, BodyParts.Mouth, "Mouth Open",
				0.05f, 0.1f, 0.2f, 0.2f, 0, 0, true);
			m.Easing = new SineOutEasing();

			g.Add(m);

			return g;
		}

		public static IProceduralMorphGroup Frown(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("frown");
			g.Add(new MorphTarget(p, BodyParts.None, "Brow Down", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Squint(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("squint");
			g.Add(new MorphTarget(p, BodyParts.None, "Eyes Squint", 0, 1, 1, 5, 2, 2));
			g.Add(new MorphTarget(p, BodyParts.None, "Nose Wrinkle", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup MouthFrown(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("mouthFrown");
			g.Add(new MorphTarget(p, BodyParts.None, "Mouth Corner Up-Down", 0, -0.5f, 1, 5, 2, 2));
			g.Add(new MorphTarget(p, BodyParts.None, "Lip Top Up", 0, 0.3f, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Drooling(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("drooling");
			g.Add(new MorphTarget(p, BodyParts.Mouth, "Mouth Open", 0, 1, 2, 5, 3, 3));
			return g;
		}

		public static IProceduralMorphGroup EyesClosedTired(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("eyesClosedTired");

			var eyesClosed = new MorphTarget(p, BodyParts.Eyes, "Eyes Closed", 0.2f, 1, 2, 5, 3, 3);
			eyesClosed.DisableBlinkAbove = 0.5f;
			g.Add(eyesClosed);

			return g;
		}
	}
}
