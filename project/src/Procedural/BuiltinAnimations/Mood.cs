using System.Collections.Generic;

namespace Cue.Proc
{
	using BE = BuiltinExpressions;

	class Moods
	{
		public const int Common = 0;
		public const int Happy = 1;
		public const int Mischievous = 2;
		public const int Excited = 3;
		public const int Angry = 4;
		public const int Tired = 5;
		public const int Count = 6;

		private static string[] names_ = new string[]
		{
			"common", "happy", "mischievous", "excited", "angry", "tired"
		};

		public static int FromString(string s)
		{
			for (int i = 0; i < names_.Length; ++i)
			{
				if (names_[i] == s)
					return i;
			}

			return -1;
		}

		public static string ToString(int i)
		{
			return names_[i];
		}
	}


	class Mood
	{
		private readonly int type_;
		private float max_ = 0;
		private float intensity_ = 0;
		private float dampen_ = 0;
		private readonly List<Expression> exps_ = new List<Expression>();
		private SlidingDuration wait_;

		public Mood(Person p, int type)
		{
			type_ = type;
			wait_ = new SlidingDuration(0.5f, 4);
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
			get
			{
				return intensity_;
			}

			set
			{
				if (intensity_ != value)
				{
					intensity_ = value;

					for (int i = 0; i < exps_.Count; ++i)
						exps_[i].AutoRange = intensity_ * 0.05f;
				}
			}
		}

		public float Dampen
		{
			get { return dampen_; }
			set { dampen_ = value; }
		}

		public void Add(Expression e)
		{
			e.AutoRange = 0;
			exps_.Add(e);
		}

		public void Reset()
		{
			for (int i = 0; i < exps_.Count; ++i)
				exps_[i].Reset();
		}

		public void FixedUpdate(float s)
		{
			wait_.Update(s);

			if (wait_.Finished)
			{
				//float intensity = max_ * intensity_ * (1 - dampen_);
				float target = U.RandomFloat(0, 1) * intensity_;
				for (int i = 0; i < exps_.Count; ++i)
					exps_[i].SetTarget(target, U.RandomFloat(0.4f, 2));
			}

			for (int i = 0; i < exps_.Count; ++i)
				exps_[i].FixedUpdate(s);
		}

		public override string ToString()
		{
			return
				$"{Moods.ToString(type_)} " +
				$"max={max_} int ={intensity_} damp={dampen_}";
		}
	}


	class MoodProcAnimation : BasicProcAnimation
	{
		private readonly List<Mood> moods_ = new List<Mood>();

		public MoodProcAnimation()
			: base("procMood", false)
		{
		}

		public override BasicProcAnimation Clone()
		{
			var a = new MoodProcAnimation();
			a.CopyFrom(this);
			return a;
		}

		public override bool Start(Person p, AnimationContext cx)
		{
			if (!base.Start(p, cx))
				return false;

			moods_.Add(CreateCommon(p));
			moods_.Add(CreateHappy(p));
			moods_.Add(CreateMischievous(p));
			moods_.Add(CreateExcited(p));
			moods_.Add(CreateAngry(p));
			moods_.Add(CreateTired(p));

			for (int i = 0; i < moods_.Count; ++i)
			{
				if (moods_[i].Type != i)
					Cue.LogError("bad mood type/index");
			}

			if (moods_.Count != Moods.Count)
				Cue.LogError("bad mood count");


			moods_[Moods.Common].Intensity = 1;
			moods_[Moods.Happy].Intensity = 0;
			moods_[Moods.Mischievous].Intensity = 1;
			moods_[Moods.Angry].Intensity = 0;

			return true;
		}

		public override void Reset()
		{
			base.Reset();

			for (int i = 0; i < moods_.Count; ++i)
				moods_[i].Reset();
		}

		public override void FixedUpdate(float s)
		{
			moods_[Moods.Excited].Intensity = person_.Mood.ExpressionExcitement;

			for (int i = 0; i < moods_.Count; ++i)
				moods_[i].FixedUpdate(s);
		}

		private Mood CreateCommon(Person p)
		{
			var e = new Mood(p, Moods.Common);
			e.Intensity = 1;

			//e.Groups.Add(BE.Swallow(p));

			return e;
		}

		private Mood CreateHappy(Person p)
		{
			var e = new Mood(p, Moods.Happy);

			e.Add(BE.Smile(p));

			return e;
		}

		private Mood CreateMischievous(Person p)
		{
			var e = new Mood(p, Moods.Mischievous);

			e.Add(BE.CornerSmile(p));

			return e;
		}

		private Mood CreateExcited(Person p)
		{
			var e = new Mood(p, Moods.Excited);

			e.Add(BE.Pleasure(p));
			e.Add(BE.Pain(p));
			e.Add(BE.Shock(p));
			e.Add(BE.Scream(p));
			e.Add(BE.Angry(p));
			//e.Add(BE.EyesRollBack(p));
			e.Add(BE.EyesClosed(p));

			return e;
		}

		private Mood CreateAngry(Person p)
		{
			var e = new Mood(p, Moods.Angry);

			//e.Add(BE.Frown(p));
			//e.Add(BE.Squint(p));
			//e.Add(BE.MouthFrown(p));

			return e;
		}

		private Mood CreateTired(Person p)
		{
			var e = new Mood(p, Moods.Tired);

			//e.Add(BE.Drooling(p));
			//e.Add(BE.EyesRollBack(p));
			//e.Add(BE.EyesClosedTired(p));

			return e;
		}
	}
}
