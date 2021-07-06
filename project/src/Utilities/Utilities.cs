﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Cue
{
	static class Bits
	{
		public static bool IsSet(int flag, int bits)
		{
			return ((flag & bits) == bits);
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


	static class Sexes
	{
		public const int Any = 0;
		public const int Male = 1;
		public const int Female = 2;

		public static int FromString(string os)
		{
			var s = os.ToLower();

			if (s == "male")
				return Male;
			else if (s == "female")
				return Female;
			else if (s == "")
				return Any;

			Cue.LogError("bad sex value '" + os + "'");
			return Any;
		}

		public static string ToString(int i)
		{
			switch (i)
			{
				case Male:
					return "male";

				case Female:
					return "female";

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
		private static float lastErrorTime_ = 0;
		private static int errorCount_ = 0;
		private const int MaxErrors = 3;

		public static void Safe(Action a)
		{
			try
			{
				a();
			}
			catch (Exception e)
			{
				Cue.LogError(e.ToString());

				var now = Cue.Instance.Sys.RealtimeSinceStartup;

				if (now - lastErrorTime_ < 1)
				{
					++errorCount_;
					if (errorCount_ > MaxErrors)
					{
						Cue.LogError(
							$"more than {MaxErrors} errors in the last " +
							"second, disabling plugin");

						Cue.Instance.DisablePlugin();
					}
				}
				else
				{
					errorCount_ = 0;
				}

				lastErrorTime_ = now;
			}
		}

		public static T Clamp<T>(T val, T min, T max)
			where T : IComparable<T>
		{
			if (val.CompareTo(min) < 0)
				return min;
			else if (val.CompareTo(max) > 0)
				return max;
			else
				return val;
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

		public static void NatSort(List<string> list)
		{
			list.Sort(new NaturalStringComparer());
		}

		public static void NatSort<T>(List<T> list)
		{
			list.Sort(new GenericNaturalStringComparer<T>());
		}

		public static string BearingToString(float b)
		{
			if (b == BasicObject.NoBearing)
				return "(none)";
			else
				return b.ToString("0.0");
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

		public int Compare(T xt, T yt)
		{
			string x = xt.ToString();
			string y = yt.ToString();

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


	class Ticker
	{
		private Stopwatch w_ = new Stopwatch();
		private long freq_ = Stopwatch.Frequency;
		private long ticks_ = 0;
		private long calls_ = 0;

		private float elapsed_ = 0;
		private long avg_ = 0;
		private long peak_ = 0;

		private long lastPeak_ = 0;
		private long lastCalls_ = 0;
		private bool updated_ = false;

		public void Do(float s, Action f)
		{
			updated_ = false;

			w_.Reset();
			w_.Start();
			f();
			w_.Stop();

			++calls_;
			ticks_ += w_.ElapsedTicks;
			peak_ = Math.Max(peak_, w_.ElapsedTicks);

			elapsed_ += s;
			if (elapsed_ >= 1)
			{
				if (calls_ <= 0)
					avg_ = 0;
				else
					avg_ = ticks_ / calls_;

				lastPeak_ = peak_;
				lastCalls_ = calls_;

				ticks_ = 0;
				calls_ = 0;
				elapsed_ = 0;
				peak_ = 0;
				updated_ = true;
			}
		}

		public bool Updated
		{
			get { return updated_; }
		}

		public float AverageMs
		{
			get
			{
				return ToMs(avg_);
			}
		}

		public float PeakMS
		{
			get { return ToMs(lastPeak_); }
		}

		private float ToMs(long ticks)
		{
			return (float)((((double)ticks) / freq_) * 1000);
		}

		public long Calls
		{
			get { return lastCalls_; }
		}

		public override string ToString()
		{
			return $"calls={Calls} avg={AverageMs:0.000} peak={PeakMS:0.000}";
		}
	}


	public class IgnoreFlag
	{
		private bool ignore_ = false;

		public static implicit operator bool(IgnoreFlag f)
		{
			return f.ignore_;
		}

		public void Do(Action a)
		{
			try
			{
				ignore_ = true;
				a();
			}
			finally
			{
				ignore_ = false;
			}
		}
	}

	class Logger
	{
		public const int Animation   = 0x001;
		public const int Action      = 0x002;
		public const int Interaction = 0x004;
		public const int AI          = 0x008;
		public const int Event       = 0x010;
		public const int Integration = 0x020;
		public const int Object      = 0x040;
		public const int Slots       = 0x080;
		public const int Sys         = 0x100;
		public const int Clothing    = 0x200;
		public const int Resources   = 0x400;

		public const int All         = int.MaxValue;

		private static int enabled_ =
			Action | Interaction | AI | Event | Integration | Object | Animation;

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

		public string Prefix
		{
			get { return prefix_(); }
		}

		public void Verbose(string s)
		{
			if (IsEnabled())
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


	// [0, 1], starts at 0
	//
	struct DampedFloat
	{
		private float current_;
		private float target_;
		private float up_;
		private float down_;

		private Action<float> set_;

		public DampedFloat(Action<float> set, float upFactor = 0.025f, float downFactor = 0.08f)
		{
			current_ = 0;
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

		public float Current
		{
			get { return current_; }
		}

		public void Update(float s)
		{
			if (target_ > current_)
				current_ = U.Clamp(current_ + s * up_, 0, target_);
			else
				current_ = U.Clamp(current_ - s * down_, target_, 1);

			set_(current_);
		}

		public override string ToString()
		{
			return $"{target_} ({current_})";
		}
	}


	class CircularIndex
	{
		private int i_ = -1;
		protected IList e_;

		public CircularIndex(IList e)
		{
			e_ = e;
		}

		public virtual int Index
		{
			get { return i_; }
		}

		public bool HasIndex
		{
			get { return i_ >= 0 && i_ < e_.Count; }
		}

		public void Reset()
		{
			i_ = -1;
		}

		public virtual void Next(Func<int, bool> f)
		{
			int count = e_.Count;

			if (count == 0)
			{
				i_ = -1;
			}
			else
			{
				if (i_ == -1)
				{
					i_ = 0;
				}
				else
				{
					++i_;
					if (i_ >= count)
					{
						WentAround();
						i_ = 0;
					}
				}


				var start = i_;
				bool wentAround = false;

				for (; ; )
				{
					if (f(i_))
						break;

					++i_;
					if (i_ >= count)
					{
						i_ = 0;
						WentAround();
						wentAround = true;
					}

					if (i_ >= start && wentAround)
					{
						i_ = -1;
						break;
					}
				}
			}
		}

		protected virtual void WentAround()
		{
			// no-op
		}
	}


	class ShuffledIndex : CircularIndex
	{
		private List<int> order_ = new List<int>();

		public ShuffledIndex(IList e)
			: base(e)
		{
		}

		public override int Index
		{
			get
			{
				if (HasIndex)
					return order_[base.Index];
				else
					return -1;
			}
		}

		public override void Next(Func<int, bool> f)
		{
			if (order_.Count != e_.Count)
				Shuffle();

			base.Next((i) => f(order_[i]));
		}

		public static void Shuffle(List<int> list, int count)
		{
			if (count == 0)
			{
				list.Clear();
				return;
			}

			var last = -1;
			if (list.Count > 0)
				last = list[list.Count - 1];

			list.Clear();
			for (int i = 0; i < count; ++i)
				list.Add(i);

			U.Shuffle(list);

			if (list[0] == last)
			{
				var mid = list.Count / 2;
				list[0] = list[mid];
				list[mid] = last;
			}
		}

		public void Shuffle()
		{
			Shuffle(order_, e_.Count);
		}

		protected override void WentAround()
		{
			Shuffle();
		}
	}
}
