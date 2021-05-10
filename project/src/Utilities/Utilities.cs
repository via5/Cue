using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Cue
{
	class Bits
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


	public class Strings
	{
		public static string Get(string s, params object[] ps)
		{
			if (ps.Length > 0)
				return string.Format(s, ps);
			else
				return s;
		}
	}


	class Sexes
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


	class U
	{
		private static float lastErrorTime_ = 0;
		private static int errorCount_ = 0;

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
					if (errorCount_ > 5)
					{
						Cue.LogError(
							"more than 5 errors in the last second, " +
							"disabling plugin");

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

		// [begin, end]
		//
		public static int RandomInt(int first, int last)
		{
			return Cue.Instance.Sys.RandomInt(first, last);
		}

		// [begin, end]
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

		public Logger(int type, W.IAtom a, string prefix)
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
}
