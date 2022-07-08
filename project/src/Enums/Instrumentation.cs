// auto generated from InstrumentationEnums.tt

using System.Collections.Generic;

namespace Cue
{
	public class I
	{
		public static readonly InstrumentationType Update = InstrumentationType.CreateInternal(0);
		public static readonly InstrumentationType Input = InstrumentationType.CreateInternal(1);
		public static readonly InstrumentationType Objects = InstrumentationType.CreateInternal(2);
		public static readonly InstrumentationType Atoms = InstrumentationType.CreateInternal(3);
		public static readonly InstrumentationType Animator = InstrumentationType.CreateInternal(4);
		public static readonly InstrumentationType Gaze = InstrumentationType.CreateInternal(5);
		public static readonly InstrumentationType GazeEmergency = InstrumentationType.CreateInternal(6);
		public static readonly InstrumentationType GazePicker = InstrumentationType.CreateInternal(7);
		public static readonly InstrumentationType GazeTargets = InstrumentationType.CreateInternal(8);
		public static readonly InstrumentationType GazePostTarget = InstrumentationType.CreateInternal(9);
		public static readonly InstrumentationType Voice = InstrumentationType.CreateInternal(10);
		public static readonly InstrumentationType Excitement = InstrumentationType.CreateInternal(11);
		public static readonly InstrumentationType Mood = InstrumentationType.CreateInternal(12);
		public static readonly InstrumentationType Body = InstrumentationType.CreateInternal(13);
		public static readonly InstrumentationType Homing = InstrumentationType.CreateInternal(14);
		public static readonly InstrumentationType Status = InstrumentationType.CreateInternal(15);
		public static readonly InstrumentationType AI = InstrumentationType.CreateInternal(16);
		public static readonly InstrumentationType UI = InstrumentationType.CreateInternal(17);
		public static readonly InstrumentationType FixedUpdate = InstrumentationType.CreateInternal(18);
		public static readonly InstrumentationType LateUpdate = InstrumentationType.CreateInternal(19);

		public const int Count = 20;
		public int GetCount() { return 20; }
	}


	public struct InstrumentationType
	{

		private static InstrumentationType[] values_ = new InstrumentationType[]
		{
			InstrumentationType.CreateInternal(0),
			InstrumentationType.CreateInternal(1),
			InstrumentationType.CreateInternal(2),
			InstrumentationType.CreateInternal(3),
			InstrumentationType.CreateInternal(4),
			InstrumentationType.CreateInternal(5),
			InstrumentationType.CreateInternal(6),
			InstrumentationType.CreateInternal(7),
			InstrumentationType.CreateInternal(8),
			InstrumentationType.CreateInternal(9),
			InstrumentationType.CreateInternal(10),
			InstrumentationType.CreateInternal(11),
			InstrumentationType.CreateInternal(12),
			InstrumentationType.CreateInternal(13),
			InstrumentationType.CreateInternal(14),
			InstrumentationType.CreateInternal(15),
			InstrumentationType.CreateInternal(16),
			InstrumentationType.CreateInternal(17),
			InstrumentationType.CreateInternal(18),
			InstrumentationType.CreateInternal(19),
		};

		public static InstrumentationType[] Values
		{
			get { return values_; }
		}

		private static string[] names_ = new string[]
		{
			"update",
			"input",
			"objects",
			"atoms",
			"animator",
			"gaze",
			"gazeEmergency",
			"gazePicker",
			"gazeTargets",
			"gazePostTarget",
			"voice",
			"excitement",
			"mood",
			"body",
			"homing",
			"status",
			"AI",
			"UI",
			"fixedUpdate",
			"lateUpdate",
		};

		public static InstrumentationType FromString(string s)
		{
			for (int i = 0; i<names_.Length; ++i)
			{
				if (names_[i] == s)
					return InstrumentationType.CreateInternal(i);
			}

			return CreateInternal(-1);
		}

		public static InstrumentationType[] FromStringMany(string s)
		{
			var list = new List<InstrumentationType>();
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

		public string GetName(InstrumentationType i)
		{
			return ToString(i);
		}

		public static string ToString(InstrumentationType i)
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

		private InstrumentationType(int value)
		{
			v_ = value;
		}

		public static InstrumentationType CreateInternal(int value)
		{
			return new InstrumentationType(value);
		}

		public int Int
		{
			get { return v_; }
		}

		public override string ToString()
		{
			return ToString(this);
		}

		public static bool operator==(InstrumentationType a, InstrumentationType b)
		{
			return (a.v_ == b.v_);
		}

		public static bool operator!=(InstrumentationType a, InstrumentationType b)
		{
			return (a.v_ != b.v_);
		}

		public override bool Equals(object o)
		{
			return (o is InstrumentationType) && (((InstrumentationType)o).v_ == v_);
		}

		public override int GetHashCode()
		{
			return v_;
		}
	}
}
