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
		private float autoRange_ = 0.1f;

		public Expression(string name, MorphGroup g)
		{
			name_ = name;
			g_ = g;
			target_.valid = false;
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

		public void SetTarget(float t, float time, bool auto = false)
		{
			target_.start = g_.Value;
			target_.value = t;
			target_.time = time;
			target_.elapsed = 0;
			target_.valid = true;
			target_.auto = auto;
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

			SetTarget(v, t, true);
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
