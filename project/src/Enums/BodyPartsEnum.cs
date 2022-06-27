// auto generated from BodyPartsEnums.tt

using System.Collections.Generic;

namespace Cue
{
	public class BP
	{
		public static readonly BodyPartType None = BodyPartType.CreateInternal(-1);
		public static readonly BodyPartType Head = BodyPartType.CreateInternal(0);
		public static readonly BodyPartType Lips = BodyPartType.CreateInternal(1);
		public static readonly BodyPartType Mouth = BodyPartType.CreateInternal(2);
		public static readonly BodyPartType Neck = BodyPartType.CreateInternal(3);
		public static readonly BodyPartType LeftBreast = BodyPartType.CreateInternal(4);
		public static readonly BodyPartType RightBreast = BodyPartType.CreateInternal(5);
		public static readonly BodyPartType Labia = BodyPartType.CreateInternal(6);
		public static readonly BodyPartType Vagina = BodyPartType.CreateInternal(7);
		public static readonly BodyPartType DeepVagina = BodyPartType.CreateInternal(8);
		public static readonly BodyPartType DeeperVagina = BodyPartType.CreateInternal(9);
		public static readonly BodyPartType Penis = BodyPartType.CreateInternal(10);
		public static readonly BodyPartType Anus = BodyPartType.CreateInternal(11);
		public static readonly BodyPartType Chest = BodyPartType.CreateInternal(12);
		public static readonly BodyPartType Belly = BodyPartType.CreateInternal(13);
		public static readonly BodyPartType Hips = BodyPartType.CreateInternal(14);
		public static readonly BodyPartType LeftGlute = BodyPartType.CreateInternal(15);
		public static readonly BodyPartType RightGlute = BodyPartType.CreateInternal(16);
		public static readonly BodyPartType LeftShoulder = BodyPartType.CreateInternal(17);
		public static readonly BodyPartType LeftElbow = BodyPartType.CreateInternal(18);
		public static readonly BodyPartType LeftHand = BodyPartType.CreateInternal(19);
		public static readonly BodyPartType RightShoulder = BodyPartType.CreateInternal(20);
		public static readonly BodyPartType RightElbow = BodyPartType.CreateInternal(21);
		public static readonly BodyPartType RightHand = BodyPartType.CreateInternal(22);
		public static readonly BodyPartType LeftThigh = BodyPartType.CreateInternal(23);
		public static readonly BodyPartType LeftShin = BodyPartType.CreateInternal(24);
		public static readonly BodyPartType LeftFoot = BodyPartType.CreateInternal(25);
		public static readonly BodyPartType RightThigh = BodyPartType.CreateInternal(26);
		public static readonly BodyPartType RightShin = BodyPartType.CreateInternal(27);
		public static readonly BodyPartType RightFoot = BodyPartType.CreateInternal(28);
		public static readonly BodyPartType Eyes = BodyPartType.CreateInternal(29);

		public const int Count = 30;
		public int GetCount() { return 30; }
	}


	public struct BodyPartType
	{

		private static BodyPartType[] values_ = new BodyPartType[]
		{
			BodyPartType.CreateInternal(0),
			BodyPartType.CreateInternal(1),
			BodyPartType.CreateInternal(2),
			BodyPartType.CreateInternal(3),
			BodyPartType.CreateInternal(4),
			BodyPartType.CreateInternal(5),
			BodyPartType.CreateInternal(6),
			BodyPartType.CreateInternal(7),
			BodyPartType.CreateInternal(8),
			BodyPartType.CreateInternal(9),
			BodyPartType.CreateInternal(10),
			BodyPartType.CreateInternal(11),
			BodyPartType.CreateInternal(12),
			BodyPartType.CreateInternal(13),
			BodyPartType.CreateInternal(14),
			BodyPartType.CreateInternal(15),
			BodyPartType.CreateInternal(16),
			BodyPartType.CreateInternal(17),
			BodyPartType.CreateInternal(18),
			BodyPartType.CreateInternal(19),
			BodyPartType.CreateInternal(20),
			BodyPartType.CreateInternal(21),
			BodyPartType.CreateInternal(22),
			BodyPartType.CreateInternal(23),
			BodyPartType.CreateInternal(24),
			BodyPartType.CreateInternal(25),
			BodyPartType.CreateInternal(26),
			BodyPartType.CreateInternal(27),
			BodyPartType.CreateInternal(28),
			BodyPartType.CreateInternal(29),
		};

		public static BodyPartType[] Values
		{
			get { return values_; }
		}

		private static string[] names_ = new string[]
		{
			"head",
			"lips",
			"mouth",
			"neck",
			"leftBreast",
			"rightBreast",
			"labia",
			"vagina",
			"deepVagina",
			"deeperVagina",
			"penis",
			"anus",
			"chest",
			"belly",
			"hips",
			"leftGlute",
			"rightGlute",
			"leftShoulder",
			"leftElbow",
			"leftHand",
			"rightShoulder",
			"rightElbow",
			"rightHand",
			"leftThigh",
			"leftShin",
			"leftFoot",
			"rightThigh",
			"rightShin",
			"rightFoot",
			"eyes",
		};

		public static BodyPartType FromString(string s)
		{
			for (int i = 0; i<names_.Length; ++i)
			{
				if (names_[i] == s)
					return BodyPartType.CreateInternal(i);
			}

			return CreateInternal(-1);
		}

		public static BodyPartType[] FromStringMany(string s)
		{
			var list = new List<BodyPartType>();
			var ss = s.Split(' ');

			foreach (string p in ss)
			{
				string tp = p.Trim();
				if (tp == "")
					continue;

				var i = FromString(tp);
				if (i != CreateInternal(-1))
					list.Add(i);
			}

			return list.ToArray();
		}

		public string GetName(BodyPartType i)
		{
			return ToString(i);
		}

		public static string ToString(BodyPartType i)
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

		private BodyPartType(int value)
		{
			v_ = value;
		}

		public static BodyPartType CreateInternal(int value)
		{
			return new BodyPartType(value);
		}

		public int Int
		{
			get { return v_; }
		}

		public override string ToString()
		{
			return ToString(this);
		}

		public static bool operator==(BodyPartType a, BodyPartType b)
		{
			return (a.v_ == b.v_);
		}

		public static bool operator!=(BodyPartType a, BodyPartType b)
		{
			return (a.v_ != b.v_);
		}

		public override bool Equals(object o)
		{
			return (o is BodyPartType) && (((BodyPartType)o).v_ == v_);
		}

		public override int GetHashCode()
		{
			return v_;
		}
	}
}
