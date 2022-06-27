// auto generated from MoodsEnums.tt

using System.Collections.Generic;

namespace Cue
{
	public struct MoodType
	{
		public static readonly MoodType None = MoodType.CreateInternal(-1);
		public static readonly MoodType Happy = MoodType.CreateInternal(0);
		public static readonly MoodType Playful = MoodType.CreateInternal(1);
		public static readonly MoodType Excited = MoodType.CreateInternal(2);
		public static readonly MoodType Angry = MoodType.CreateInternal(3);
		public static readonly MoodType Tired = MoodType.CreateInternal(4);
		public static readonly MoodType Orgasm = MoodType.CreateInternal(5);

		public const int Count = 6;
		public int GetCount() { return 6; }


		private static MoodType[] values_ = new MoodType[]
		{
			MoodType.CreateInternal(0),
			MoodType.CreateInternal(1),
			MoodType.CreateInternal(2),
			MoodType.CreateInternal(3),
			MoodType.CreateInternal(4),
			MoodType.CreateInternal(5),
		};

		public static MoodType[] Values
		{
			get { return values_; }
		}

		private static string[] names_ = new string[]
		{
			"happy",
			"playful",
			"excited",
			"angry",
			"tired",
			"orgasm",
		};

		public static MoodType FromString(string s)
		{
			for (int i = 0; i<names_.Length; ++i)
			{
				if (names_[i] == s)
					return MoodType.CreateInternal(i);
			}

			return None;
		}

		public static MoodType[] FromStringMany(string s)
		{
			var list = new List<MoodType>();
			var ss = s.Split(' ');

			foreach (string p in ss)
			{
				string tp = p.Trim();
				if (tp == "")
					continue;

				var i = FromString(tp);
				if (i != None)
					list.Add(i);
			}

			return list.ToArray();
		}

		public string GetName(MoodType i)
		{
			return ToString(i);
		}

		public static string ToString(MoodType i)
		{
			if (i.v_ >= 0 && i.v_ < names_.Length)
				return names_[i.v_];
			else
				return $"?{i.v_}";
		}

		public static string[] Names
		{
			get { return names_; }
		}



		private int v_;

		private MoodType(int value)
		{
			v_ = value;
		}

		public static MoodType CreateInternal(int value)
		{
			return new MoodType(value);
		}

		public int Int
		{
			get { return v_; }
		}

		public override string ToString()
		{
			return ToString(this);
		}

		public static bool operator==(MoodType a, MoodType b)
		{
			return (a.v_ == b.v_);
		}

		public static bool operator!=(MoodType a, MoodType b)
		{
			return (a.v_ != b.v_);
		}

		public override bool Equals(object o)
		{
			return (o is MoodType) && (((MoodType)o).v_ == v_);
		}

		public override int GetHashCode()
		{
			return v_;
		}
	}
}
