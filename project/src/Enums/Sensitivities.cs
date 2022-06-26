// auto generated from SensitivitiesEnums.tt

using System.Collections.Generic;

namespace Cue
{
	public class SS
	{
		public static readonly ZoneTypes None = ZoneTypes.CreateInternal(-1);
		public static readonly ZoneTypes Penetration = ZoneTypes.CreateInternal(0);
		public static readonly ZoneTypes Mouth = ZoneTypes.CreateInternal(1);
		public static readonly ZoneTypes Breasts = ZoneTypes.CreateInternal(2);
		public static readonly ZoneTypes Genitals = ZoneTypes.CreateInternal(3);
		public static readonly ZoneTypes OthersExcitement = ZoneTypes.CreateInternal(4);

		public const int Count = 5;
		public int GetCount() { return 5; }
	}


	public struct ZoneTypes
	{

		private static ZoneTypes[] values_ = new ZoneTypes[]
		{
			ZoneTypes.CreateInternal(0),
			ZoneTypes.CreateInternal(1),
			ZoneTypes.CreateInternal(2),
			ZoneTypes.CreateInternal(3),
			ZoneTypes.CreateInternal(4),
		};

		public static ZoneTypes[] Values
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

		public static ZoneTypes FromString(string s)
		{
			for (int i = 0; i<names_.Length; ++i)
			{
				if (names_[i] == s)
					return ZoneTypes.CreateInternal(i);
			}

			return CreateInternal(-1);
		}

		public static ZoneTypes[] FromStringMany(string s)
		{
			var list = new List<ZoneTypes>();
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

		public string GetName(ZoneTypes i)
		{
			return ToString(i);
		}

		public static string ToString(ZoneTypes i)
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

		private ZoneTypes(int value)
		{
			v_ = value;
		}

		public static ZoneTypes CreateInternal(int value)
		{
			return new ZoneTypes(value);
		}

		public int Int
		{
			get { return v_; }
		}

		public static bool operator==(ZoneTypes a, ZoneTypes b)
		{
			return (a.v_ == b.v_);
		}

		public static bool operator!=(ZoneTypes a, ZoneTypes b)
		{
			return (a.v_ != b.v_);
		}

		public override bool Equals(object o)
		{
			return (o is ZoneTypes) && (((ZoneTypes)o).v_ == v_);
		}

		public override int GetHashCode()
		{
			return v_;
		}
	}
}
