using System.Collections.Generic;

namespace Cue.Proc
{
	using BE = BuiltinExpressions;

	class ExpressionType
	{
		private readonly int type_;
		private float max_ = 0;
		private float intensity_ = 0;
		private float dampen_ = 0;
		private readonly List<IProceduralMorphGroup> groups_ =
			new List<IProceduralMorphGroup>();

		public ExpressionType(Person p, int type)
		{
			type_ = type;
		}

		public int Type
		{
			get { return type_; }
		}

		public float Maximum
		{
			get { return max_; }
			set { max_ = value; }
		}

		public float Intensity
		{
			get { return intensity_; }
			set { intensity_ = value; }
		}

		public float Dampen
		{
			get { return dampen_; }
			set { dampen_ = value; }
		}

		public List<IProceduralMorphGroup> Groups
		{
			get { return groups_; }
		}

		public void Reset()
		{
			for (int i = 0; i < groups_.Count; ++i)
				groups_[i].Reset();
		}

		public void FixedUpdate(float s)
		{
			for (int i = 0; i < groups_.Count; ++i)
				groups_[i].FixedUpdate(s, max_ * intensity_ * (1 - dampen_));

			for (int i = 0; i < groups_.Count; ++i)
				groups_[i].Set();
		}

		public void ForceChange()
		{
			for (int i = 0; i < groups_.Count; ++i)
				groups_[i].ForceChange();
		}

		public override string ToString()
		{
			return
				$"{Expressions.ToString(type_)} " +
				$"max={max_} int ={intensity_} damp={dampen_}";
		}
	}


	class Expression : IExpression
	{
		private Person person_;
		private bool enabled_ = true;

		private readonly List<ExpressionType> expressions_ =
			new List<ExpressionType>();


		public Expression(Person p)
		{
			person_ = p;
			expressions_.Add(CreateCommon(p));
			expressions_.Add(CreateHappy(p));
			expressions_.Add(CreateMischievous(p));
			expressions_.Add(CreatePleasure(p));
			expressions_.Add(CreateAngry(p));
			expressions_.Add(CreateTired(p));

			for (int i = 0; i < expressions_.Count; ++i)
			{
				if (expressions_[i].Type != i)
					Cue.LogError("bad expression type/index");
			}

			if (expressions_.Count != Expressions.Count)
				Cue.LogError("bad expression count");
		}

		public List<ExpressionType> All
		{
			get { return expressions_; }
		}

		private ExpressionType CreateCommon(Person p)
		{
			var e = new ExpressionType(p, Expressions.Common);
			e.Intensity = 1;

			e.Groups.Add(BE.Swallow(p));

			return e;
		}

		private ExpressionType CreateHappy(Person p)
		{
			var e = new ExpressionType(p, Expressions.Happy);

			e.Groups.Add(BE.Smile(p));

			return e;
		}

		private ExpressionType CreateMischievous(Person p)
		{
			var e = new ExpressionType(p, Expressions.Mischievous);

			e.Groups.Add(BE.CornerSmile(p));

			return e;
		}

		private ExpressionType CreatePleasure(Person p)
		{
			var e = new ExpressionType(p, Expressions.Pleasure);

			e.Groups.Add(BE.Pleasure(p));
			e.Groups.Add(BE.EyesRollBack(p));
			e.Groups.Add(BE.EyesClosed(p));

			return e;
		}

		private ExpressionType CreateAngry(Person p)
		{
			var e = new ExpressionType(p, Expressions.Angry);

			e.Groups.Add(BE.Frown(p));
			e.Groups.Add(BE.Squint(p));
			e.Groups.Add(BE.MouthFrown(p));

			return e;
		}

		private ExpressionType CreateTired(Person p)
		{
			var e = new ExpressionType(p, Expressions.Tired);

			e.Groups.Add(BE.Drooling(p));
			e.Groups.Add(BE.EyesRollBack(p));
			e.Groups.Add(BE.EyesClosedTired(p));

			return e;
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

		public void Reset()
		{
			for (int i = 0; i < expressions_.Count; ++i)
				expressions_[i].Reset();
		}

		public void FixedUpdate(float s)
		{
			if (!enabled_)
				return;

			for (int i = 0; i < expressions_.Count; ++i)
				expressions_[i].FixedUpdate(s);
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
			var mui = Cue.Instance.VamSys.GetMUI(person_.VamAtom.Atom);

			foreach (var m in mui.GetMorphs())
			{
				if (m.morphValue != m.startValue)
					Cue.LogInfo($"name='{m.morphName}' dname='{m.displayName}'");
			}
		}
	}
}
