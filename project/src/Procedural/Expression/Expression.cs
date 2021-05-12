using System.Collections.Generic;

namespace Cue.Proc
{
	using BE = BuiltinExpressions;

	class ExpressionType
	{
		private readonly int type_;
		private float intensity_ = 0;
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

		public float Intensity
		{
			get { return intensity_; }
			set { intensity_ = value; }
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

		public void Update(float s)
		{
			for (int i = 0; i < groups_.Count; ++i)
				groups_[i].Update(s);

			for (int i = 0; i < groups_.Count; ++i)
				groups_[i].Set(intensity_);
		}

		public override string ToString()
		{
			return
				Expressions.ToString(type_) + " " +
				"intensity=" + intensity_.ToString();
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

		public void Set(int type, float intensity, bool resetOthers = false)
		{
			Set(
				new ExpressionIntensity[]
				{
					new ExpressionIntensity(type, intensity)
				},
				resetOthers);
		}

		public void Set(ExpressionIntensity[] intensities, bool resetOthers = false)
		{
			// todo: let morphs go back to normal

			for (int i = 0; i < expressions_.Count; ++i)
			{
				bool found = false;

				for (int j = 0; j < intensities.Length; ++j)
				{
					if (intensities[j].type == expressions_[i].Type)
					{
						expressions_[i].Intensity = intensities[j].intensity;
						found = true;
						break;
					}
				}

				if (!found && resetOthers)
					expressions_[i].Intensity = 0;
			}
		}

		public bool Enabled
		{
			get
			{
				return enabled_;
			}

			set
			{
				enabled_ = value;
			}
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

		public void Update(float s)
		{
			if (!enabled_)
				return;

			for (int i = 0; i < expressions_.Count; ++i)
				expressions_[i].Update(s);
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
