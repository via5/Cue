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
		public static readonly InstrumentationType PickerGeo = InstrumentationType.CreateInternal(8);
		public static readonly InstrumentationType PickerCanLook = InstrumentationType.CreateInternal(9);
		public static readonly InstrumentationType GazeEvents = InstrumentationType.CreateInternal(10);
		public static readonly InstrumentationType GazeAbove = InstrumentationType.CreateInternal(11);
		public static readonly InstrumentationType GazeFront = InstrumentationType.CreateInternal(12);
		public static readonly InstrumentationType GazeGrabbed = InstrumentationType.CreateInternal(13);
		public static readonly InstrumentationType GazeZapped = InstrumentationType.CreateInternal(14);
		public static readonly InstrumentationType GazeKissing = InstrumentationType.CreateInternal(15);
		public static readonly InstrumentationType GazeMouth = InstrumentationType.CreateInternal(16);
		public static readonly InstrumentationType GazeHands = InstrumentationType.CreateInternal(17);
		public static readonly InstrumentationType GazeInteractions = InstrumentationType.CreateInternal(18);
		public static readonly InstrumentationType GazeRandom = InstrumentationType.CreateInternal(19);
		public static readonly InstrumentationType GazeOtherPersons = InstrumentationType.CreateInternal(20);
		public static readonly InstrumentationType GazePostTarget = InstrumentationType.CreateInternal(21);
		public static readonly InstrumentationType Voice = InstrumentationType.CreateInternal(22);
		public static readonly InstrumentationType Excitement = InstrumentationType.CreateInternal(23);
		public static readonly InstrumentationType Mood = InstrumentationType.CreateInternal(24);
		public static readonly InstrumentationType Body = InstrumentationType.CreateInternal(25);
		public static readonly InstrumentationType BodyZap = InstrumentationType.CreateInternal(26);
		public static readonly InstrumentationType BodyParts = InstrumentationType.CreateInternal(27);
		public static readonly InstrumentationType BodyZones = InstrumentationType.CreateInternal(28);
		public static readonly InstrumentationType ZoneDecay = InstrumentationType.CreateInternal(29);
		public static readonly InstrumentationType ZoneUpdate = InstrumentationType.CreateInternal(30);
		public static readonly InstrumentationType ZoneIgnore = InstrumentationType.CreateInternal(31);
		public static readonly InstrumentationType BodyTemperature = InstrumentationType.CreateInternal(32);
		public static readonly InstrumentationType BodyVoice = InstrumentationType.CreateInternal(33);
		public static readonly InstrumentationType Homing = InstrumentationType.CreateInternal(34);
		public static readonly InstrumentationType Status = InstrumentationType.CreateInternal(35);
		public static readonly InstrumentationType AI = InstrumentationType.CreateInternal(36);
		public static readonly InstrumentationType Triggers = InstrumentationType.CreateInternal(37);
		public static readonly InstrumentationType UI = InstrumentationType.CreateInternal(38);
		public static readonly InstrumentationType FixedUpdate = InstrumentationType.CreateInternal(39);
		public static readonly InstrumentationType FUSys = InstrumentationType.CreateInternal(40);
		public static readonly InstrumentationType FUObjects = InstrumentationType.CreateInternal(41);
		public static readonly InstrumentationType FUBody = InstrumentationType.CreateInternal(42);
		public static readonly InstrumentationType FUAnimator = InstrumentationType.CreateInternal(43);
		public static readonly InstrumentationType FUAI = InstrumentationType.CreateInternal(44);
		public static readonly InstrumentationType FUExpressions = InstrumentationType.CreateInternal(45);
		public static readonly InstrumentationType LateUpdate = InstrumentationType.CreateInternal(46);
		public static readonly InstrumentationType Collisions = InstrumentationType.CreateInternal(47);
		public static readonly InstrumentationType ColWithThis = InstrumentationType.CreateInternal(48);
		public static readonly InstrumentationType ColGetBP = InstrumentationType.CreateInternal(49);
		public static readonly InstrumentationType ColExternal = InstrumentationType.CreateInternal(50);
		public static readonly InstrumentationType ColPerson = InstrumentationType.CreateInternal(51);

		public const int Count = 52;
		public int GetCount() { return 52; }
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
			InstrumentationType.CreateInternal(20),
			InstrumentationType.CreateInternal(21),
			InstrumentationType.CreateInternal(22),
			InstrumentationType.CreateInternal(23),
			InstrumentationType.CreateInternal(24),
			InstrumentationType.CreateInternal(25),
			InstrumentationType.CreateInternal(26),
			InstrumentationType.CreateInternal(27),
			InstrumentationType.CreateInternal(28),
			InstrumentationType.CreateInternal(29),
			InstrumentationType.CreateInternal(30),
			InstrumentationType.CreateInternal(31),
			InstrumentationType.CreateInternal(32),
			InstrumentationType.CreateInternal(33),
			InstrumentationType.CreateInternal(34),
			InstrumentationType.CreateInternal(35),
			InstrumentationType.CreateInternal(36),
			InstrumentationType.CreateInternal(37),
			InstrumentationType.CreateInternal(38),
			InstrumentationType.CreateInternal(39),
			InstrumentationType.CreateInternal(40),
			InstrumentationType.CreateInternal(41),
			InstrumentationType.CreateInternal(42),
			InstrumentationType.CreateInternal(43),
			InstrumentationType.CreateInternal(44),
			InstrumentationType.CreateInternal(45),
			InstrumentationType.CreateInternal(46),
			InstrumentationType.CreateInternal(47),
			InstrumentationType.CreateInternal(48),
			InstrumentationType.CreateInternal(49),
			InstrumentationType.CreateInternal(50),
			InstrumentationType.CreateInternal(51),
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
			"pickerGeo",
			"pickerCanLook",
			"gazeEvents",
			"gazeAbove",
			"gazeFront",
			"gazeGrabbed",
			"gazeZapped",
			"gazeKissing",
			"gazeMouth",
			"gazeHands",
			"gazeInteractions",
			"gazeRandom",
			"gazeOtherPersons",
			"gazePostTarget",
			"voice",
			"excitement",
			"mood",
			"body",
			"bodyZap",
			"bodyParts",
			"bodyZones",
			"zoneDecay",
			"zoneUpdate",
			"zoneIgnore",
			"bodyTemperature",
			"bodyVoice",
			"homing",
			"status",
			"AI",
			"triggers",
			"UI",
			"fixedUpdate",
			"fUSys",
			"fUObjects",
			"fUBody",
			"fUAnimator",
			"FUAI",
			"fUExpressions",
			"lateUpdate",
			"collisions",
			"colWithThis",
			"colGetBP",
			"colExternal",
			"colPerson",
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
