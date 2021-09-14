using System.Collections.Generic;

namespace Cue.Proc
{
	class BuiltinExpressions
	{
		public static List<IProceduralMorphGroup> All(Person p)
		{
			var list = new List<IProceduralMorphGroup>();

			list.Add(Smile(p));
			list.Add(CornerSmile(p));
			list.Add(Pleasure(p));
			list.Add(EyesRollBack(p));
			list.Add(EyesClosed(p));
			list.Add(Swallow(p));
			list.Add(Frown(p));
			list.Add(Squint(p));
			list.Add(MouthFrown(p));
			list.Add(Drooling(p));
			list.Add(EyesClosedTired(p));

			return list;
		}


		public static IProceduralMorphGroup Smile(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("smile");
			g.Add(new MorphTarget(p, BP.Mouth, "Smile Open Full Face", 0, 1, 0.3f, 3, 2, 1));
			return g;
		}

		public static IProceduralMorphGroup CornerSmile(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("cornerSmile");
			g.Add(new MorphTarget(p, BP.Mouth, "Mouth Smile Simple Left", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Pleasure(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("pleasure");
			g.Add(new MorphTarget(p, BP.Mouth, "07-Extreme Pleasure", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Pain(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("pain");
			g.Add(new MorphTarget(p, BP.Mouth, "Pain", 0, 0.5f, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Shock(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("shock");
			g.Add(new MorphTarget(p, BP.Mouth, "Shock", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Scream(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("scream");
			g.Add(new MorphTarget(p, BP.Mouth, "Scream", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Angry(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("angry");
			g.Add(new MorphTarget(p, BP.Mouth, "Angry", 0, 0.3f, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup EyesRollBack(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("eyesRollback");

			if (p.MovementStyle == MovementStyles.Feminine)
				g.Add(new MorphTarget(p, BP.Eyes, "Eye Roll Back_DD", 0, 0.4f, 1, 5, 2, 2));

			return g;
		}

		public static IProceduralMorphGroup EyesClosed(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("eyesClosed");
			g.Add(new MorphTarget(p, BP.Eyes, "Eyes Closed", 0, 1, 0.5f, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Swallow(Person p)
		{
			var g = new SequentialProceduralMorphGroup(
				"swallow", new Duration(40, 60));


			var m = new MorphTarget(p, BP.Mouth, "Mouth Open",
				-0.1f, -0.1f, 0.2f, 0.2f, 0, 0, true);
			m.Easing = new SineOutEasing();

			g.Add(m);


			m = new MorphTarget(p, BP.None, "deepthroat",
				0.1f, 0.1f, 0.2f, 0.2f, 0, 0, true);

			g.Add(m);


			m = new MorphTarget(p, BP.Mouth, "Mouth Open",
				0.05f, 0.1f, 0.2f, 0.2f, 0, 0, true);
			m.Easing = new SineOutEasing();

			g.Add(m);

			return g;
		}

		public static IProceduralMorphGroup Frown(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("frown");
			g.Add(new MorphTarget(p, BP.None, "Brow Down", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Squint(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("squint");
			g.Add(new MorphTarget(p, BP.None, "Eyes Squint", 0, 1, 1, 5, 2, 2));
			g.Add(new MorphTarget(p, BP.None, "Nose Wrinkle", 0, 1, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup MouthFrown(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("mouthFrown");
			g.Add(new MorphTarget(p, BP.None, "Mouth Corner Up-Down", 0, -0.5f, 1, 5, 2, 2));
			g.Add(new MorphTarget(p, BP.None, "Lip Top Up", 0, 0.3f, 1, 5, 2, 2));
			return g;
		}

		public static IProceduralMorphGroup Drooling(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("drooling");
			g.Add(new MorphTarget(p, BP.Mouth, "Mouth Open", 0, 1, 2, 5, 3, 3));
			return g;
		}

		public static IProceduralMorphGroup EyesClosedTired(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("eyesClosedTired");
			g.Add(new MorphTarget(p, BP.Eyes, "Eyes Closed", 0.2f, 1, 2, 5, 3, 3));
			return g;
		}
	}
}
