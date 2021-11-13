using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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


	interface IRandom
	{
		IRandom Clone();
		float RandomFloat(float first, float last, float magnitude);
	}


	abstract class BasicRandom : IRandom
	{
		public static IRandom FromJSON(JSONClass o)
		{
			if (!o.HasKey("type"))
				throw new LoadFailed("missing 'type'");

			var type = o["type"].Value;

			if (type == "uniform")
				return UniformRandom.FromJSON(o);
			else if (type == "normal")
				return NormalRandom.FromJSON(o);
			else
				throw new LoadFailed($"unknown type '{type}'");
		}

		public abstract IRandom Clone();
		public abstract float RandomFloat(float first, float last, float magnitude);
	}


	class UniformRandom : BasicRandom
	{
		public new static UniformRandom FromJSON(JSONClass o)
		{
			return new UniformRandom();
		}

		public override IRandom Clone()
		{
			return new UniformRandom();
		}

		public override float RandomFloat(float first, float last, float magnitude)
		{
			return U.RandomFloat(first, last);
		}

		public override string ToString()
		{
			return "uniform";
		}
	}


	class NormalRandom : BasicRandom
	{
		private readonly float centerMin_;
		private readonly float centerMax_;
		private readonly float widthMin_;
		private readonly float widthMax_;

		public NormalRandom(float centerMin, float centerMax, float widthMin, float widthMax)
		{
			centerMin_ = centerMin;
			centerMax_ = centerMax;
			widthMin_ = widthMin;
			widthMax_ = widthMax;
		}

		public override string ToString()
		{
			return $"normal:c=({centerMin_},{centerMax_}),w=({widthMin_},{widthMax_})";
		}

		public new static NormalRandom FromJSON(JSONClass o)
		{
			float centerMin = 0.5f;
			if (o.HasKey("centerMin"))
			{
				if (!float.TryParse(o["centerMin"].Value, out centerMin))
					throw new LoadFailed($"centerMin is not a float");
			}

			float centerMax = 0.5f;
			if (o.HasKey("centerMax"))
			{
				if (!float.TryParse(o["centerMax"].Value, out centerMax))
					throw new LoadFailed($"centerMax is not a float");
			}

			float widthMin = 1.0f;
			if (o.HasKey("widthMin"))
			{
				if (!float.TryParse(o["widthMin"].Value, out widthMin))
					throw new LoadFailed($"widthMin is not a float");
			}

			float widthMax = 1.0f;
			if (o.HasKey("widthMax"))
			{
				if (!float.TryParse(o["widthMax"].Value, out widthMax))
					throw new LoadFailed($"widthMax is not a float");
			}

			return new NormalRandom(centerMin, centerMax, widthMin, widthMax);
		}

		public override IRandom Clone()
		{
			return new NormalRandom(centerMin_, centerMax_, widthMin_, widthMax_);
		}

		public override float RandomFloat(float first, float last, float magnitude)
		{
			float center;

			if (centerMin_ < centerMax_)
				center = centerMin_ + (centerMax_ - centerMin_) * magnitude;
			else
				center = centerMax_ + (centerMin_ - centerMax_) * magnitude;

			float width;

			if (widthMin_ < widthMax_)
				width = widthMin_ + (widthMax_ - widthMin_) * magnitude;
			else
				width = widthMax_ + (widthMin_ - widthMax_) * magnitude;

			return U.RandomNormal(first, last, center, width);
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
			return Cue.Instance.Sys.RandomInt(first, last);
		}

		// [first, last]
		//
		public static float RandomFloat(float first, float last)
		{
			return Cue.Instance.Sys.RandomFloat(first, last);
		}

		// inclusive
		//
		public static float RandomFloat(Pair<float, float> p)
		{
			return RandomFloat(p.first, p.second);
		}

		public static float RandomNormal(
			float first, float last, float center = 0.5f, float width = 1.0f)
		{
			float u, v, S;
			int tries = 0;

			do
			{
				u = 2.0f * RandomFloat(0, 1) - 1.0f;
				v = 2.0f * RandomFloat(0, 1) - 1.0f;
				S = u * u + v * v;

				++tries;
				if (tries > 20)
					return RandomFloat(first, last);
			}
			while (S >= 1.0f);

			// Standard Normal Distribution
			float std = (float)(u * Math.Sqrt(-2.0f * Math.Log(S) / S));

			// Normal Distribution centered between the min and max value
			// and clamped following the "three-sigma rule"
			float mean = (first + last) * center;
			float sigma = (last - mean) / (3.0f / width);
			return Clamp(std * sigma + mean, first, last);
		}

		public static bool RandomBool()
		{
			return RandomBool(0.5f);
		}

		public static bool RandomBool(float trueChance)
		{
			return (RandomFloat(0, 1) <= trueChance);
		}

		public static void NatSort(List<string> list)
		{
			list.Sort(new NaturalStringComparer());
		}

		public static void NatSort<T>(List<T> list, Func<T, string> stringify = null)
		{
			list.Sort(new GenericNaturalStringComparer<T>(stringify));
		}

		public static string BearingToString(float b)
		{
			if (b == BasicObject.NoBearing)
				return "(none)";
			else
				return b.ToString("0.0");
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


	// from https://stackoverflow.com/questions/248603
	public class NaturalStringComparer : IComparer<string>
	{
		private static readonly Regex _re =
			new Regex(@"(?<=\D)(?=\d)|(?<=\d)(?=\D)", RegexOptions.Compiled);

		public int Compare(string x, string y)
		{
			x = x.ToLower();
			y = y.ToLower();
			if (string.Compare(x, 0, y, 0, Math.Min(x.Length, y.Length)) == 0)
			{
				if (x.Length == y.Length) return 0;
				return x.Length < y.Length ? -1 : 1;
			}
			var a = _re.Split(x);
			var b = _re.Split(y);
			int i = 0;
			while (true)
			{
				int r = PartCompare(a[i], b[i]);
				if (r != 0)
					return r;

				if (a[i] != b[i])
					return string.Compare(a[i], b[i]);

				++i;
			}
		}

		private static int PartCompare(string x, string y)
		{
			int a, b;
			if (int.TryParse(x, out a) && int.TryParse(y, out b))
				return a.CompareTo(b);
			return x.CompareTo(y);
		}
	}


	// from https://stackoverflow.com/questions/248603
	public class GenericNaturalStringComparer<T> : IComparer<T>
	{
		private static readonly Regex _re =
			new Regex(@"(?<=\D)(?=\d)|(?<=\d)(?=\D)", RegexOptions.Compiled);

		private Func<T, string> stringify_;

		public GenericNaturalStringComparer(Func<T, string> stringify = null)
		{
			stringify_ = stringify;
		}

		public int Compare(T xt, T yt)
		{
			string x, y;

			if (stringify_ == null)
			{
				x = xt.ToString();
				y = yt.ToString();
			}
			else
			{
				x = stringify_(xt);
				y = stringify_(yt);
			}

			x = x.ToLower();
			y = y.ToLower();
			if (string.Compare(x, 0, y, 0, Math.Min(x.Length, y.Length)) == 0)
			{
				if (x.Length == y.Length) return 0;
				return x.Length < y.Length ? -1 : 1;
			}
			var a = _re.Split(x);
			var b = _re.Split(y);
			int i = 0;
			while (true)
			{
				int r = PartCompare(a[i], b[i]);
				if (r != 0)
					return r;

				if (a[i] != b[i])
					return string.Compare(a[i], b[i]);

				++i;
			}
		}

		private static int PartCompare(string x, string y)
		{
			int a, b;
			if (int.TryParse(x, out a) && int.TryParse(y, out b))
				return a.CompareTo(b);
			return x.CompareTo(y);
		}
	}


	class Logger
	{
		public const int Animation   = 0x001;
		public const int Action      = 0x002;
		public const int Event       = 0x004;
		public const int AI          = 0x008;
		public const int Command     = 0x010;
		public const int Integration = 0x020;
		public const int Object      = 0x040;
		public const int Slots       = 0x080;
		public const int Sys         = 0x100;
		public const int Clothing    = 0x200;
		public const int Resources   = 0x400;

		public const int All         = int.MaxValue;

		private static int enabled_ =
			Action | Event | AI | Command |
			Object | Animation;

		private int type_;
		private Func<string> prefix_;

		public Logger(int type, string prefix)
		{
			type_ = type;
			prefix_ = () => prefix;
		}

		public Logger(int type, Func<string> prefix)
		{
			type_ = type;
			prefix_ = prefix;
		}

		public Logger(int type, IObject o, string prefix)
		{
			type_ = type;
			prefix_ = () => o.ID + (prefix == "" ? "" : " " + prefix);
		}

		public Logger(int type, Sys.IAtom a, string prefix)
		{
			type_ = type;
			prefix_ = () => a.ID + (prefix == "" ? "" : " " + prefix);
		}

		public static int Enabled
		{
			get { return enabled_; }
			set { enabled_ = value; }
		}

		public static string[] Names
		{
			get
			{
				return new string[]
				{
					"animation",
					"action",
					"event",
					"ai",
					"command",
					"integration",
					"object",
					"slots",
					"sys",
					"clothing",
					"resources"
				};
			}
		}

		public string Prefix
		{
			get { return prefix_(); }
		}

		public void Verbose(string s)
		{
			if (IsEnabled() && Cue.LogVerboseEnabled)
				Cue.LogVerbose($"{Prefix}: {s}");
		}

		public void Info(string s)
		{
			if (IsEnabled())
				Cue.LogInfo($"{Prefix}: {s}");
		}

		public void Warning(string s)
		{
			Cue.LogWarning($"{Prefix}: {s}");
		}

		public void Error(string s)
		{
			Cue.LogError($"{Prefix}: {s}");
		}

		public void ErrorST(string s)
		{
			Cue.LogErrorST($"{Prefix}: {s}");
		}

		private bool IsEnabled()
		{
			return Bits.IsSet(enabled_, type_);
		}
	}




	class RandomRange
	{
		private float min_, max_;

		public RandomRange(float min, float max)
		{
			min_ = min;
			max_ = max;
		}

		public void SetRange(Pair<float, float> r)
		{
			min_ = r.first;
			max_ = r.second;
		}

		public float Next()
		{
			return U.RandomFloat(min_, max_);
		}
	}


	class InterpolatedRandomRange
	{
		private Pair<float, float> valuesRange_;
		private Pair<float, float> changeIntervalRange_;
		private Pair<float, float> interpolateTimeRange_;

		private float nextElapsed_ = 0;
		private float nextInterval_ = 0;

		private float nextValue_ = 0;
		private float currentValue_ = 0;
		private float valueElapsed_ = 0;
		private float valueTime_ = 0;
		private float lastValue_ = 0;

		private IEasing easing_ = new SinusoidalEasing();

		public InterpolatedRandomRange(
			Pair<float, float> values,
			Pair<float, float> changeInterval,
			Pair<float, float> interpolateTime)
		{
			valuesRange_ = values;
			changeIntervalRange_ = changeInterval;
			interpolateTimeRange_ = interpolateTime;
		}

		public float Value
		{
			get { return currentValue_; }
		}

		public void Reset()
		{
			nextElapsed_ = 0;
			nextInterval_ = NextInterval();

			nextValue_ = NextValue();
			currentValue_ = 0;
			valueElapsed_ = 0;
			valueTime_ = ValueTime();
			lastValue_ = 0;
		}

		public bool Update(float s)
		{
			nextElapsed_ += s;

			if (nextElapsed_ >= nextInterval_)
			{
				lastValue_ = currentValue_;
				nextValue_ = NextValue();
				nextElapsed_ = 0;
				valueElapsed_ = 0;
				nextInterval_ = NextInterval();
				valueTime_ = ValueTime();
			}
			else if (valueElapsed_ < valueTime_)
			{
				valueElapsed_ = U.Clamp(valueElapsed_ + s, 0, valueTime_);
				currentValue_ = Interpolate(lastValue_, nextValue_, valueElapsed_ / valueTime_);

				return true;
			}

			return false;
		}

		private float NextValue()
		{
			return U.RandomFloat(valuesRange_.first, valuesRange_.second);
		}

		private float NextInterval()
		{
			return U.RandomFloat(changeIntervalRange_.first, changeIntervalRange_.second);
		}

		private float ValueTime()
		{
			return U.RandomFloat(interpolateTimeRange_.first, interpolateTimeRange_.second);
		}

		private float Interpolate(float start, float end, float f)
		{
			return start + (end - start) * easing_.Magnitude(f);
		}
	}


	class ForceableFloat
	{
		private float value_;
		private float forced_;
		private bool isForced_;

		public ForceableFloat()
		{
			value_ = 0;
			forced_ = 0;
			isForced_ = false;
		}

		public float Value
		{
			get
			{
				if (isForced_)
					return forced_;
				else
					return value_;
			}

			set
			{
				value_ = value;
			}
		}

		public bool IsForced
		{
			get { return isForced_; }
		}

		public float UnforcedValue
		{
			get { return value_; }
		}

		public void SetForced(float f)
		{
			isForced_ = true;
			forced_ = f;
		}

		public void UnsetForced()
		{
			isForced_ = false;
		}

		public override string ToString()
		{
			if (isForced_)
				return $"{forced_:0.000000} (forced)";
			else
				return $"{value_:0.000000}";
		}
	}


	// [0, 1], starts at 0
	//
	class DampedFloat : ForceableFloat
	{
		private float target_;
		private float up_;
		private float down_;

		private Action<float> set_;

		public DampedFloat(Action<float> set=null, float upFactor = 0.1f, float downFactor = 0.1f)
		{
			target_ = 0;
			set_ = set;
			up_ = upFactor;
			down_ = downFactor;
		}

		public float Target
		{
			get { return target_; }
			set { target_ = value; }
		}

		public new float Value
		{
			get { return base.Value; }
		}

		public float UpRate
		{
			get { return up_; }
			set { up_ = value; }
		}

		public float DownRate
		{
			get { return down_; }
			set { down_ = value; }
		}

		public float CurrentRate
		{
			get
			{
				if (target_ > Value)
					return up_;
				else
					return -down_;
			}
		}

		public void Update(float s)
		{
			if (target_ > Value)
				base.Value = U.Clamp(Value + s * up_, 0, target_);
			else
				base.Value = U.Clamp(Value - s * down_, target_, 1);

			set_?.Invoke(Value);
		}

		public override string ToString()
		{
			if (IsForced)
				return base.ToString();
			else if (Math.Abs(target_ - Value) > 0.0001f)
				return $"{Value:0.000}=>{target_:0.000}@{CurrentRate:0.00000}";
			else
				return $"{Value:0.000}";
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
