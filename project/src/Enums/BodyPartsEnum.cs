// auto generated from BodyPartsEnums.tt

using System.Collections.Generic;

namespace Cue
{
	public class BP
	{
		public static readonly BodyPartTypes None = BodyPartTypes.CreateInternal(-1);
		public static readonly BodyPartTypes Head = BodyPartTypes.CreateInternal(0);
		public static readonly BodyPartTypes Lips = BodyPartTypes.CreateInternal(1);
		public static readonly BodyPartTypes Mouth = BodyPartTypes.CreateInternal(2);
		public static readonly BodyPartTypes LeftBreast = BodyPartTypes.CreateInternal(3);
		public static readonly BodyPartTypes RightBreast = BodyPartTypes.CreateInternal(4);
		public static readonly BodyPartTypes Labia = BodyPartTypes.CreateInternal(5);
		public static readonly BodyPartTypes Vagina = BodyPartTypes.CreateInternal(6);
		public static readonly BodyPartTypes DeepVagina = BodyPartTypes.CreateInternal(7);
		public static readonly BodyPartTypes DeeperVagina = BodyPartTypes.CreateInternal(8);
		public static readonly BodyPartTypes Penis = BodyPartTypes.CreateInternal(9);
		public static readonly BodyPartTypes Anus = BodyPartTypes.CreateInternal(10);
		public static readonly BodyPartTypes Chest = BodyPartTypes.CreateInternal(11);
		public static readonly BodyPartTypes Belly = BodyPartTypes.CreateInternal(12);
		public static readonly BodyPartTypes Hips = BodyPartTypes.CreateInternal(13);
		public static readonly BodyPartTypes LeftGlute = BodyPartTypes.CreateInternal(14);
		public static readonly BodyPartTypes RightGlute = BodyPartTypes.CreateInternal(15);
		public static readonly BodyPartTypes LeftShoulder = BodyPartTypes.CreateInternal(16);
		public static readonly BodyPartTypes LeftArm = BodyPartTypes.CreateInternal(17);
		public static readonly BodyPartTypes LeftForearm = BodyPartTypes.CreateInternal(18);
		public static readonly BodyPartTypes LeftHand = BodyPartTypes.CreateInternal(19);
		public static readonly BodyPartTypes RightShoulder = BodyPartTypes.CreateInternal(20);
		public static readonly BodyPartTypes RightArm = BodyPartTypes.CreateInternal(21);
		public static readonly BodyPartTypes RightForearm = BodyPartTypes.CreateInternal(22);
		public static readonly BodyPartTypes RightHand = BodyPartTypes.CreateInternal(23);
		public static readonly BodyPartTypes LeftThigh = BodyPartTypes.CreateInternal(24);
		public static readonly BodyPartTypes LeftShin = BodyPartTypes.CreateInternal(25);
		public static readonly BodyPartTypes LeftFoot = BodyPartTypes.CreateInternal(26);
		public static readonly BodyPartTypes RightThigh = BodyPartTypes.CreateInternal(27);
		public static readonly BodyPartTypes RightShin = BodyPartTypes.CreateInternal(28);
		public static readonly BodyPartTypes RightFoot = BodyPartTypes.CreateInternal(29);
		public static readonly BodyPartTypes Eyes = BodyPartTypes.CreateInternal(30);

		public const int Count = 31;
		public int GetCount() { return 31; }
	}


	public struct BodyPartTypes
	{

		private static BodyPartTypes[] values_ = new BodyPartTypes[]
		{
			BodyPartTypes.CreateInternal(0),
			BodyPartTypes.CreateInternal(1),
			BodyPartTypes.CreateInternal(2),
			BodyPartTypes.CreateInternal(3),
			BodyPartTypes.CreateInternal(4),
			BodyPartTypes.CreateInternal(5),
			BodyPartTypes.CreateInternal(6),
			BodyPartTypes.CreateInternal(7),
			BodyPartTypes.CreateInternal(8),
			BodyPartTypes.CreateInternal(9),
			BodyPartTypes.CreateInternal(10),
			BodyPartTypes.CreateInternal(11),
			BodyPartTypes.CreateInternal(12),
			BodyPartTypes.CreateInternal(13),
			BodyPartTypes.CreateInternal(14),
			BodyPartTypes.CreateInternal(15),
			BodyPartTypes.CreateInternal(16),
			BodyPartTypes.CreateInternal(17),
			BodyPartTypes.CreateInternal(18),
			BodyPartTypes.CreateInternal(19),
			BodyPartTypes.CreateInternal(20),
			BodyPartTypes.CreateInternal(21),
			BodyPartTypes.CreateInternal(22),
			BodyPartTypes.CreateInternal(23),
			BodyPartTypes.CreateInternal(24),
			BodyPartTypes.CreateInternal(25),
			BodyPartTypes.CreateInternal(26),
			BodyPartTypes.CreateInternal(27),
			BodyPartTypes.CreateInternal(28),
			BodyPartTypes.CreateInternal(29),
			BodyPartTypes.CreateInternal(30),
		};

		public static BodyPartTypes[] Values
		{
			get { return values_; }
		}

		private static string[] names_ = new string[]
		{
			"head",
			"lips",
			"mouth",
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
			"leftArm",
			"leftForearm",
			"leftHand",
			"rightShoulder",
			"rightArm",
			"rightForearm",
			"rightHand",
			"leftThigh",
			"leftShin",
			"leftFoot",
			"rightThigh",
			"rightShin",
			"rightFoot",
			"eyes",
		};

		public static BodyPartTypes FromString(string s)
		{
			for (int i = 0; i<names_.Length; ++i)
			{
				if (names_[i] == s)
					return BodyPartTypes.CreateInternal(i);
			}

			return CreateInternal(-1);
		}

		public static BodyPartTypes[] FromStringMany(string s)
		{
			var list = new List<BodyPartTypes>();
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

		public string GetName(BodyPartTypes i)
		{
			return ToString(i);
		}

		public static string ToString(BodyPartTypes i)
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

		private BodyPartTypes(int value)
		{
			v_ = value;
		}

		public static BodyPartTypes CreateInternal(int value)
		{
			return new BodyPartTypes(value);
		}

		public int Int
		{
			get { return v_; }
		}

		public static bool operator==(BodyPartTypes a, BodyPartTypes b)
		{
			return (a.v_ == b.v_);
		}

		public static bool operator!=(BodyPartTypes a, BodyPartTypes b)
		{
			return (a.v_ != b.v_);
		}

		public override bool Equals(object o)
		{
			return (o is BodyPartTypes) && (((BodyPartTypes)o).v_ == v_);
		}

		public override int GetHashCode()
		{
			return v_;
		}
	}
}
