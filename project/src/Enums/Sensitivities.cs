// auto generated from SensitivitiesEnums.tt

using System.Collections.Generic;

namespace Cue
{
	public class SS
	{
		public static readonly ZoneType None = ZoneType.CreateInternal(-1);
		public static readonly ZoneType Penetration = ZoneType.CreateInternal(0);
		public static readonly ZoneType Mouth = ZoneType.CreateInternal(1);
		public static readonly ZoneType Breasts = ZoneType.CreateInternal(2);
		public static readonly ZoneType Genitals = ZoneType.CreateInternal(3);
		public static readonly ZoneType OthersExcitement = ZoneType.CreateInternal(4);

		public const int Count = 5;
		public int GetCount() { return 5; }
	}


	public struct ZoneType
	{

		private static ZoneType[] values_ = new ZoneType[]
		{
			ZoneType.CreateInternal(0),
			ZoneType.CreateInternal(1),
			ZoneType.CreateInternal(2),
			ZoneType.CreateInternal(3),
			ZoneType.CreateInternal(4),
		};

		public static ZoneType[] Values
		{
			get { return values_; }
		}

		private static string[] names_ = new string[]
		{
			"penetration",
			"mouth",
			"breasts",
			"genitals",
			"othersExcitement",
		};

		public static ZoneType FromString(string s)
		{
			for (int i = 0; i<names_.Length; ++i)
			{
				if (names_[i] == s)
					return ZoneType.CreateInternal(i);
			}

			return CreateInternal(-1);
		}

		public static ZoneType[] FromStringMany(string s)
		{
			var list = new List<ZoneType>();
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

		public string GetName(ZoneType i)
		{
			return ToString(i);
		}

		public static string ToString(ZoneType i)
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

		private ZoneType(int value)
		{
			v_ = value;
		}

		public static ZoneType CreateInternal(int value)
		{
			return new ZoneType(value);
		}

		public int Int
		{
			get { return v_; }
		}

		public override string ToString()
		{
			return ToString(this);
		}

		public static bool operator==(ZoneType a, ZoneType b)
		{
			return (a.v_ == b.v_);
		}

		public static bool operator!=(ZoneType a, ZoneType b)
		{
			return (a.v_ != b.v_);
		}

		public override bool Equals(object o)
		{
			return (o is ZoneType) && (((ZoneType)o).v_ == v_);
		}

		public override int GetHashCode()
		{
			return v_;
		}
	}
}
