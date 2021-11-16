﻿// auto generated from BodyPartsEnums.tt

using System.Collections.Generic;

namespace Cue
{
	class BP
	{
		public const int None = -1;
		public const int Head = 0;
		public const int Lips = 1;
		public const int Mouth = 2;
		public const int LeftBreast = 3;
		public const int RightBreast = 4;
		public const int Labia = 5;
		public const int Vagina = 6;
		public const int DeepVagina = 7;
		public const int DeeperVagina = 8;
		public const int Penis = 9;
		public const int Anus = 10;
		public const int Chest = 11;
		public const int Belly = 12;
		public const int Hips = 13;
		public const int LeftGlute = 14;
		public const int RightGlute = 15;
		public const int LeftShoulder = 16;
		public const int LeftArm = 17;
		public const int LeftForearm = 18;
		public const int LeftHand = 19;
		public const int RightShoulder = 20;
		public const int RightArm = 21;
		public const int RightForearm = 22;
		public const int RightHand = 23;
		public const int LeftThigh = 24;
		public const int LeftShin = 25;
		public const int LeftFoot = 26;
		public const int RightThigh = 27;
		public const int RightShin = 28;
		public const int RightFoot = 29;
		public const int Eyes = 30;

		public const int Count = 31;
		public int GetCount() { return 31; }


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

		public static int FromString(string s)
		{
			for (int i = 0; i<names_.Length; ++i)
			{
				if (names_[i] == s)
					return i;
			}

			return -1;
		}

		public static int[] FromStringMany(string s)
		{
			var list = new List<int>();
			var ss = s.Split(' ');

			foreach (string p in ss)
			{
				string tp = p.Trim();
				if (tp == "")
					continue;

				var i = FromString(tp);
				if (i != -1)
					list.Add(i);
			}

			return list.ToArray();
		}

		public string GetName(int i)
		{
			return ToString(i);
		}

		public static string ToString(int i)
		{
			if (i >= 0 && i < names_.Length)
				return names_[i];
			else
				return $"?{i}";
		}

		public static string[] Names
		{
			get { return names_; }
		}
	}
}
