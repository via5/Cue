namespace Cue.Proc
{
	class Expressions
	{
		public const int Happy = 0x01;
		public const int Excited = 0x02;
		public const int Angry = 0x04;
		public const int Tired = 0x08;

		//private static string[] names_ = new string[Count]
		//{
		//	"happy", "excited", "angry", "tired"
		//};

		//public static int FromString(string s)
		//{
		//	for (int i = 0; i < names_.Length; ++i)
		//	{
		//		if (names_[i] == s)
		//			return i;
		//	}
		//
		//	return -1;
		//}

		public static string ToString(int i)
		{
			string s = "";

			if (Bits.IsSet(i, Happy))
			{
				if (s != "") s += "|";
				s += "happy";
			}

			if (Bits.IsSet(i, Excited))
			{
				if (s != "") s += "|";
				s += "excited";
			}

			if (Bits.IsSet(i, Angry))
			{
				if (s != "") s += "|";
				s += "angry";
			}

			if (Bits.IsSet(i, Tired))
			{
				if (s != "") s += "|";
				s += "tired";
			}

			return s;
		}
	}


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

			public override string ToString()
			{
				if (!valid)
					return "-";

				return $"{value:0.00}";
			}
		}

		private string name_;
		private int type_;
		private MorphGroup g_;
		private TargetInfo target_;
		private IEasing easing_ = new SinusoidalEasing();
		private float value_ = 0;
		private float autoRange_ = 0.1f;

		public Expression(string name, int type, MorphGroup g)
		{
			name_ = name;
			type_ = type;
			g_ = g;
			target_.valid = false;
		}

		public override string ToString()
		{
			return $"{name_} {Expressions.ToString(type_)} {target_}";
		}

		public string Name
		{
			get { return name_; }
		}

		public float Target
		{
			get { return target_.value; }
		}

		public float AutoRange
		{
			get { return autoRange_; }
			set { autoRange_ = value; }
		}

		public bool Finished
		{
			get { return (!target_.valid || target_.auto || target_.elapsed >= target_.time); }
		}

		public bool IsType(int t)
		{
			return Bits.IsSet(type_, t);
		}

		public void SetTarget(float t, float time)
		{
			target_.start = g_.Value;
			target_.value = t;
			target_.time = time;
			target_.elapsed = 0;
			target_.valid = true;
			target_.auto = false;
		}

		public void Reset()
		{
			g_.Reset();
		}

		public void FixedUpdate(float s)
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
			if (autoRange_ <= 0)
				return;

			float v = U.RandomFloat(value_ - autoRange_, value_ + autoRange_);
			v = U.Clamp(v, 0, 1);

			float t = U.RandomFloat(0.5f, 2.0f);

			SetTarget(v, t);
			target_.auto = true;
		}
	}
	/*

	class Expression : IExpression
	{
		private bool enabled_ = true;


		public Expression(Person p)
		{
		}

		public List<ExpressionType> All
		{
			get { return expressions_; }
		}

		public float Max
		{
			get { return MaxMorphs; }
		}


		public void SetMaximum(int type, float max)
		{
			expressions_[type].Maximum = max;
		}

		public void SetIntensity(int type, float intensity)
		{
			expressions_[type].Intensity = intensity;
		}

		public void SetDampen(int type, float intensity)
		{
			expressions_[type].Dampen = intensity;
		}

		public bool Enabled
		{
			get { return enabled_; }
			set { enabled_ = value; }
		}

		public void MakeNeutral()
		{
			Reset();
		}

		public void FixedUpdate(float s)
		{
			if (!enabled_)
				return;

		}

		public void ForceChange()
		{
			for (int i = 0; i < expressions_.Count; ++i)
				expressions_[i].ForceChange();
		}

		public void OnPluginState(bool b)
		{
			Reset();
		}

		public void DumpActive()
		{
			var mui = Sys.Vam.U.GetMUI(person_.VamAtom.Atom);

			foreach (var m in mui.GetMorphs())
			{
				if (m.morphValue != m.startValue)
					Cue.LogInfo($"name='{m.morphName}' dname='{m.displayName}'");
			}
		}
	}*/
}
