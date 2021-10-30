namespace Cue.Proc
{
	class BuiltinExpressions
	{
		public static Expression[] All(Person p)
		{
			return new Expression[]
			{
				Smile(p),
				//CornerSmile(p),

				Pleasure(p),
				Pain(p),
				Shock(p),
				Scream(p),
				Angry(p),
				//EyesRollBack(p),
				EyesClosed(p),

				Frown(p),
				Squint(p),
				MouthFrown(p),

				Drooling(p),
			};
		}

		public static Expression Smile(Person p)
		{
			return new Expression("smile",
				Moods.Happy,
				new MorphGroup(
					p, "smile", BP.Mouth, new MorphGroup.MorphInfo[]
					{
						new MorphGroup.MorphInfo("Smile Open Full Face", 1, BP.None)
					}));
		}

		public static Expression CornerSmile(Person p)
		{
			return new Expression("cornerSmile",
				Moods.Happy,
				new MorphGroup(
					p, "cornerSmile", BP.Mouth, new MorphGroup.MorphInfo[]
					{
						new MorphGroup.MorphInfo("Smile Open Full Face", 0.3f, BP.None),
						new MorphGroup.MorphInfo("Mouth Smile Simple Left", 1.2f, BP.None),
						new MorphGroup.MorphInfo("Eyes Squint", 0.15f, BP.None)
					}));
		}

		public static Expression Pleasure(Person p)
		{
			return new Expression("pleasure",
				Moods.Excited,
				new MorphGroup(
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
			return new Expression("pain",
				Moods.Excited,
				new MorphGroup(
					p, "pain", new int[] { BP.Mouth, BP.Eyes },
					new MorphGroup.MorphInfo[]
					{
						new MorphGroup.MorphInfo("Pain", 1, BP.None)
					}));
		}

		public static Expression Shock(Person p)
		{
			return new Expression("shock",
				Moods.Excited,
				new MorphGroup(
					p, "shock", BP.Mouth, new MorphGroup.MorphInfo[]
					{
						new MorphGroup.MorphInfo("Shock", 1, BP.None)
					}));
		}

		public static Expression Scream(Person p)
		{
			return new Expression("scream",
				Moods.Excited,
				new MorphGroup(
					p, "scream", new int[] { BP.Mouth, BP.Eyes },
					new MorphGroup.MorphInfo[]
					{
						new MorphGroup.MorphInfo("Scream", 1, BP.None)
					}));
		}

		public static Expression Angry(Person p)
		{
			return new Expression("angry",
				new int[] { Moods.Excited, Moods.Angry },
				new MorphGroup(
					p, "angry", BP.Mouth, new MorphGroup.MorphInfo[]
					{
						new MorphGroup.MorphInfo("Angry", 1, BP.None)
					}));
		}

		public static Expression EyesClosed(Person p)
		{
			return new Expression("eyesClosed",
				new int[] { Moods.Excited, Moods.Tired },
				new MorphGroup(
					p, "eyesClosed", BP.Eyes, new MorphGroup.MorphInfo[]
					{
						new MorphGroup.MorphInfo("Eyes Closed", 1, BP.None)
					}));
		}

		public static Expression Drooling(Person p)
		{
			return new Expression("drooling",
				Moods.Tired,
				new MorphGroup(
					p, "drooling", BP.Mouth, new MorphGroup.MorphInfo[]
					{
						new MorphGroup.MorphInfo("Mouth Open", 1, BP.None)
					}));
		}

		public static Expression Frown(Person p)
		{
			return new Expression("frown",
				Moods.Angry,
				new MorphGroup(
					p, "frown", BP.Eyes, new MorphGroup.MorphInfo[]
					{
						new MorphGroup.MorphInfo("Brow Down", 1, BP.None)
					}));
		}

		public static Expression Squint(Person p)
		{
			return new Expression("squint",
				Moods.Angry,
				new MorphGroup(
					p, "squint", BP.Eyes, new MorphGroup.MorphInfo[]
					{
						new MorphGroup.MorphInfo("Eyes Squint", 1, BP.None),
						new MorphGroup.MorphInfo("Nose Wrinkle", 1, BP.None)
					}));
		}

		public static Expression MouthFrown(Person p)
		{
			return new Expression("mouthFrown",
				Moods.Angry,
				new MorphGroup(
					p, "mouthFrown", BP.Mouth, new MorphGroup.MorphInfo[]
					{
						new MorphGroup.MorphInfo("Mouth Corner Up-Down", -0.5f, BP.None),
						new MorphGroup.MorphInfo("Lip Top Up", 0.3f, BP.None)
					}));
		}

		//public static Expression EyesRollBack(Person p)
		//{
		//	return new Expression("eyesRollback", new MorphGroup(
		//		p, "eyesRollback", BP.Mouth, new MorphGroup.MorphInfo[]
		//		{
		//			new MorphGroup.MorphInfo("Eye Roll Back_DD", 1, BP.None)
		//		}));
		//}

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
		*/
	}
}
