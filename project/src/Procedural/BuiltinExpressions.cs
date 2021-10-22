namespace Cue.Proc
{
	class Expression
	{
		struct TargetInfo
		{
			public float value;
			public float time;
			public float start;
			public float elapsed;
			public bool valid;
			public bool auto;
		}

		private string name_;
		private MorphGroup g_;
		private TargetInfo target_;
		private IEasing easing_ = new SinusoidalEasing();
		private float value_ = 0;

		public Expression(string name, MorphGroup g)
		{
			name_ = name;
			g_ = g;
		}

		public float Target
		{
			get { return target_.value; }
		}

		public void SetTarget(float t, float time, bool auto = false)
		{
			target_.start = g_.Value;
			target_.value = t;
			target_.time = time;
			target_.elapsed = 0;
			target_.valid = true;
			target_.auto = auto;
		}

		public void Update(float s)
		{
			if (target_.valid)
			{
				target_.elapsed += s;

				float p = U.Clamp(target_.elapsed / target_.time, 0, 1);
				float t = easing_.Magnitude(p);
				float v = U.Lerp(target_.start, target_.value, t);

				g_.Value = v;

				if (!target_.auto)
					value_ = v;

				if (p >= 1)
					NextAuto();
			}
			else
			{
				NextAuto();
			}
		}

		private void NextAuto()
		{
			float v = U.RandomFloat(value_ - 0.1f, value_ + 0.1f);
			v = U.Clamp(v, 0, 1);

			float t = U.RandomFloat(0.5f, 2.0f);

			SetTarget(v, t, true);
		}
	}

	class BuiltinExpressions
	{
		//public static List<IProceduralMorphGroup> All(Person p)
		//{
		//	var list = new List<IProceduralMorphGroup>();
		//
		//	list.Add(Smile(p));
		//	list.Add(CornerSmile(p));
		//	list.Add(Pleasure(p));
		//	list.Add(EyesRollBack(p));
		//	list.Add(EyesClosed(p));
		//	list.Add(Swallow(p));
		//	list.Add(Frown(p));
		//	list.Add(Squint(p));
		//	list.Add(MouthFrown(p));
		//	list.Add(Drooling(p));
		//	list.Add(EyesClosedTired(p));
		//
		//	return list;
		//}


		public static Expression Smile(Person p)
		{
			return new Expression("smile", new MorphGroup(
				p, "smile", BP.Mouth, new MorphGroup.MorphInfo[]
				{
					new MorphGroup.MorphInfo("Smile Open Full Face", 1, BP.None)
				}));
		}

		/*public static IProceduralMorphGroup CornerSmile(Person p)
		{
			var g = new ConcurrentProceduralMorphGroup("cornerSmile");
			g.Add(new MorphTarget(p, BP.Mouth, "Mouth Smile Simple Left", 0, 1, 1, 5, 2, 2));
			return g;
		}*/

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
		/*
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
		}*/
	}
}
