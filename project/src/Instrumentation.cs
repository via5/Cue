using System;
using System.Diagnostics;

namespace Cue
{
	class Ticker
	{
		private readonly string name_;
		private Stopwatch w_ = new Stopwatch();
		private long freq_ = Stopwatch.Frequency;
		private long ticks_ = 0;
		private long calls_ = 0;
		private long gcStart_ = 0;
		private long gc_ = 0;

		private float elapsed_ = 0;
		private long avg_ = 0;
		private long peak_ = 0;

		private long lastTotal_ = 0;
		private long lastPeak_ = 0;
		private long lastCalls_ = 0;
		private long lastGc_ = 0;
		private long gcTotal_ = 0;
		private bool updated_ = false;

		public Ticker(string name = "")
		{
			name_ = name;
		}

		public string Name
		{
			get { return name_; }
		}

		public void Start()
		{
			updated_ = false;

			gcStart_ = GC.GetTotalMemory(false);

			w_.Reset();
			w_.Start();
		}

		public void End()
		{
			w_.Stop();

			++calls_;
			ticks_ += w_.ElapsedTicks;
			peak_ = Math.Max(peak_, w_.ElapsedTicks);

			gc_ += GC.GetTotalMemory(false) - gcStart_;
		}

		public void Update(float s)
		{
			elapsed_ += s;
			if (elapsed_ >= 1)
			{
				if (calls_ <= 0)
					avg_ = 0;
				else
					avg_ = ticks_ / calls_;

				lastTotal_ = ticks_;
				lastPeak_ = peak_;
				lastCalls_ = calls_;
				lastGc_ = gc_;

				if (gc_ > 0)
					gcTotal_ += gc_;

				ticks_ = 0;
				calls_ = 0;
				elapsed_ = 0;
				peak_ = 0;
				gc_ = 0;
				updated_ = true;
			}
		}

		public bool Updated
		{
			get { return updated_; }
		}

		public float TotalMs
		{
			get { return ToMs(lastTotal_); }
		}

		public float AverageMs
		{
			get { return ToMs(avg_); }
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

		public long MemoryChange
		{
			get { return lastGc_; }
		}

		public override string ToString()
		{
			return
				$"n={Calls,5} " +
				$"tot={TotalMs,6:##0.00} " +
				$"avg={AverageMs,6:##0.00} " +
				$"peak={PeakMS,6:##0.00} " +
				$"gc={BytesToString(MemoryChange)} " +
				$"gct={BytesToString(gcTotal_)}";
		}

		private string BytesToString(long bytes)
		{
			string[] sizes = { "B", "K", "M", "G", "T" };
			double len = (double)bytes;
			int order = 0;
			while (len >= 1024 && order < sizes.Length - 1)
			{
				order++;
				len = len / 1024;
			}

			// Adjust the format string to your preferences. For example "{0:0.#}{1}" would
			// show a single decimal place, and no space.
			return string.Format($"{len,6:###0.0} {sizes[order]}");
		}
	}


	class Instrumentation
	{
		public const bool AlwaysActive = false;

		private Ticker[] tickers_ = new Ticker[I.Count];
		private int[] depth_ = new int[I.Count]
		{
			0,
				1, 1,
					2, 2, 2,
						3, 3,
							4, 4,
						3,
							4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
						3,
					2, 2, 2, 2,
						3, 3, 3,
							4, 4, 4,
						3, 3,
					2, 2, 2,
				1, 1, 1,
			0,
				1, 1,
					2, 2, 2, 2,
			0, 0,
				1, 1, 1, 1
		};

		private int[] stack_ = new int[6];
		private int current_ = 0;
		private bool enabled_ = false;
		private static Instrumentation instance_ = new Instrumentation();


		public Instrumentation()
		{
			instance_ = this;

			foreach (var i in InstrumentationType.Values)
				tickers_[i.Int] = new Ticker(i.ToString());
		}

		public static Instrumentation Instance
		{
			get { return instance_; }
		}

		public bool Updated
		{
			get { return tickers_[I.Update.Int].Updated; }
		}

		public bool Enabled
		{
			get { return enabled_ || AlwaysActive; }
			set { enabled_ = value; }
		}

		public static void Reset()
		{
			instance_.DoReset();
		}

		public static void Start(InstrumentationType i)
		{
			instance_.DoStart(i);
		}

		public static void End()
		{
			instance_.DoEnd();
		}

		public static void UpdateTickers(float s)
		{
			instance_.DoUpdateTickers(s);
		}

		private void DoStart(InstrumentationType i)
		{
			if (!Enabled)
				return;

			if (current_ < 0 || current_ >= stack_.Length)
			{
				Logger.Global.ErrorST($"bad current {current_}");
				Cue.Instance.DisablePlugin();
			}

			if (i.Int < 0 || i.Int >= tickers_.Length)
			{
				Logger.Global.ErrorST($"bad index {i}");
				Cue.Instance.DisablePlugin();
			}

			stack_[current_] = i.Int;
			++current_;

			tickers_[i.Int].Start();
		}

		private void DoEnd()
		{
			if (!Enabled)
				return;

			--current_;

			int i = stack_[current_];
			stack_[current_] = -1;

			tickers_[i].End();
		}

		private void DoReset()
		{
			current_ = 0;
		}

		public int Depth(InstrumentationType i)
		{
			return depth_[i.Int];
		}

		public string Name(InstrumentationType i)
		{
			return tickers_[i.Int].Name;
		}

		public Ticker Get(InstrumentationType i)
		{
			return tickers_[i.Int];
		}

		private void DoUpdateTickers(float s)
		{
			if (!Enabled)
				return;

			for (int i = 0; i < tickers_.Length; ++i)
				tickers_[i].Update(s);
		}
	}
}
