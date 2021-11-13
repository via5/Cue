using System;
using System.Collections.Generic;

namespace Cue
{
	static class Bits
	{
		public static bool IsSet(int flag, int bits)
		{
			return ((flag & bits) == bits);
		}

		public static bool IsAnySet(int flag, int bits)
		{
			return ((flag & bits) != 0);
		}

		public static int Bit(int pos)
		{
			return (1 << pos);
		}
	}


	static class Strings
	{
		public static string Get(string s, params object[] ps)
		{
			if (ps.Length > 0)
				return string.Format(s, ps);
			else
				return s;
		}
	}


	static class MovementStyles
	{
		public const int Any = 0;
		public const int Masculine = 1;
		public const int Feminine = 2;

		public static int FromString(string os)
		{
			var s = os.ToLower();

			if (s == "masculine" || s == "male")
				return Masculine;
			else if (s == "feminine" || s == "female")
				return Feminine;
			else if (s == "")
				return Any;

			Cue.LogError("bad style value '" + os + "'");
			return Any;
		}

		public static string ToString(int i)
		{
			switch (i)
			{
				case Masculine:
					return "masculine";

				case Feminine:
					return "feminine";

				default:
					return "any";
			}
		}

		public static bool Match(int a, int b)
		{
			if (a == Any || b == Any)
				return true;

			return (a == b);
		}
	}


	struct Pair<First, Second>
	{
		public First first;
		public Second second;

		public Pair(First f, Second s)
		{
			first = f;
			second = s;
		}

		public override string ToString()
		{
			return $"{first},{second}";
		}
	}


	static class HashHelper
	{
		public static int GetHashCode<T1, T2>(T1 arg1, T2 arg2)
		{
			unchecked
			{
				return 31 * arg1.GetHashCode() + arg2.GetHashCode();
			}
		}

		public static int GetHashCode<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
		{
			unchecked
			{
				int hash = arg1.GetHashCode();
				hash = 31 * hash + arg2.GetHashCode();
				return 31 * hash + arg3.GetHashCode();
			}
		}

		public static int GetHashCode<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3,
			T4 arg4)
		{
			unchecked
			{
				int hash = arg1.GetHashCode();
				hash = 31 * hash + arg2.GetHashCode();
				hash = 31 * hash + arg3.GetHashCode();
				return 31 * hash + arg4.GetHashCode();
			}
		}
	}


	class U
	{
		public static float Clamp(float val, float min, float max)
		{
			if (val < min)
				return min;
			else if (val > max)
				return max;
			else
				return val;
		}

		public static float Lerp(float min, float max, float t)
		{
			t = Clamp(t, 0, 1);

			if (min < max)
				return min + (max - min) * t;
			else
				return min - (min - max) * t;
		}

		// [first, last]
		//
		public static int RandomInt(int first, int last)
		{
			return BasicRandom.RandomInt(first, last);
		}

		// [first, last]
		//
		public static float RandomFloat(float first, float last)
		{
			return BasicRandom.RandomFloat(first, last);
		}

		// inclusive
		//
		public static float RandomFloat(Pair<float, float> p)
		{
			return BasicRandom.RandomFloat(p.first, p.second);
		}

		public static float RandomNormal(
			float first, float last, float center = 0.5f, float width = 1.0f)
		{
			return BasicRandom.RandomNormal(first, last, center, width);
		}

		public static bool RandomBool()
		{
			return BasicRandom.RandomBool(0.5f);
		}

		public static bool RandomBool(float trueChance)
		{
			return BasicRandom.RandomBool(trueChance);
		}

		public static void NatSort(List<string> list)
		{
			list.Sort(new NaturalStringComparer());
		}

		public static void NatSort<T>(List<T> list, Func<T, string> stringify = null)
		{
			list.Sort(new GenericNaturalStringComparer<T>(stringify));
		}

		public static void Shuffle<T>(T[] list)
		{
			int n = list.Length;

			while (n > 1)
			{
				n--;
				int k = RandomInt(0, n);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}

		public static void Shuffle<T>(IList<T> list)
		{
			int n = list.Count;

			while (n > 1)
			{
				n--;
				int k = RandomInt(0, n);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}
	}


	class CircularIndex<T>
		where T : class
	{
		private int i_ = -1;
		private IList<T> e_;
		private Func<T, bool> valid_;

		public CircularIndex(IList<T> e, Func<T, bool> valid = null)
		{
			e_ = e;
			valid_ = valid;
			Next(+1);
		}

		public int Index
		{
			get
			{
				return i_;
			}

			set
			{
				i_ = value;
			}
		}

		public bool HasIndex
		{
			get { return i_ >= 0 && i_ < e_.Count; }
		}

		public T Value
		{
			get
			{
				if (HasIndex)
					return e_[i_];
				else
					return null;
			}
		}

		public void Reset()
		{
			i_ = -1;
		}

		public void Next(int dir)
		{
			if (dir == 0)
				return;

			dir = (dir < 0) ? -1 : 1;

			int count = e_.Count;

			if (count == 0)
			{
				i_ = -1;
				return;
			}

			var start = i_;
			if (start < 0)
				start = 0;

			bool wentAround = false;

			for (; ; )
			{
				i_ += dir;

				if (i_ < 0)
				{
					i_ = count - 1;
					wentAround = true;
				}
				else if (i_ >= count)
				{
					i_ = 0;
					wentAround = true;
				}

				if (valid_ == null || valid_(e_[i_]))
					break;

				if (wentAround && i_ == start)
				{
					i_ = -1;
					break;
				}
			}
		}
	}
}
