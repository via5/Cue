﻿namespace Cue.Proc
{
	class BuiltinExpressions
	{
		/*public static IProceduralMorphGroup CornerSmile(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("cornerSmile");
			g.Add(new MorphTarget(p, BP.Mouth, "Mouth Smile Simple Left", 0, 1, 1, 5, 2, 2));
			return g;
		}*/

		//public static Expression EyesRollBack(Person p)
		//{
		//	return new Expression("eyesRollback", new MorphGroup(
		//		p, "eyesRollback", BP.Mouth, new MorphGroup.MorphInfo[]
		//		{
		//			new MorphGroup.MorphInfo("Eye Roll Back_DD", 1, BP.None)
		//		}));
		//}

		public static Expression Smile(Person p)
		{
			return new Expression("smile", new MorphGroup(
				p, "smile", BP.Mouth, new MorphGroup.MorphInfo[]
				{
					new MorphGroup.MorphInfo("Smile Open Full Face", 1, BP.None)
				}));
		}

		public static Expression Pleasure(Person p)
		{
			return new Expression("pleasure", new MorphGroup(
				p, "pleasure", new int[] { BP.Mouth, BP.Eyes },
				new MorphGroup.MorphInfo[]
				{
					new MorphGroup.MorphInfo("CTRLTongueRaise-Lower", 0.13f, BP.None),
					new MorphGroup.MorphInfo("CTRLBrowInnerUp", 0.7f, BP.None),
					new MorphGroup.MorphInfo("PHMMouthSmile", 0.46f, BP.None),
					new MorphGroup.MorphInfo("PHMMouthOpenWide", 1.7f, BP.None),
					new MorphGroup.MorphInfo("PHMMouthCornerUpDown", -0.37f, BP.None),
					new MorphGroup.MorphInfo("CTRLEyesClosed", 0.66f, BP.None),
					new MorphGroup.MorphInfo("CTRLTongueIn-Out", 0.012f, BP.None)
				}));
		}

		public static Expression Pain(Person p)
		{
			return new Expression("pain", new MorphGroup(
				p, "pain", new int[] { BP.Mouth, BP.Eyes },
				new MorphGroup.MorphInfo[]
				{
					new MorphGroup.MorphInfo("Pain", 1, BP.None)
				}));
		}

		public static Expression Shock(Person p)
		{
			return new Expression("shock", new MorphGroup(
				p, "shock", BP.Mouth, new MorphGroup.MorphInfo[]
				{
					new MorphGroup.MorphInfo("Shock", 1, BP.None)
				}));
		}

		public static Expression Scream(Person p)
		{
			return new Expression("scream", new MorphGroup(
				p, "scream", new int[] { BP.Mouth, BP.Eyes },
				new MorphGroup.MorphInfo[]
				{
					new MorphGroup.MorphInfo("Scream", 1, BP.None)
				}));
		}

		public static Expression Angry(Person p)
		{
			return new Expression("angry", new MorphGroup(
				p, "angry", BP.Mouth, new MorphGroup.MorphInfo[]
				{
					new MorphGroup.MorphInfo("Angry", 1, BP.None)
				}));
		}

		public static Expression EyesClosed(Person p)
		{
			return new Expression("eyesClosed", new MorphGroup(
				p, "eyesClosed", BP.Eyes, new MorphGroup.MorphInfo[]
				{
					new MorphGroup.MorphInfo("Eyes Closed", 1, BP.None)
				}));
		}
		/*
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
		}*/
	}
}
