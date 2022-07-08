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

			w_.Reset();
			w_.Start();

			gcStart_ = GC.GetTotalMemory(false);
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

		public long MemoryChange
		{
			get { return lastGc_; }
		}

		public override string ToString()
		{
			return
				$"n={Calls,3} " +
				$"avg={AverageMs,5:##0.00} " +
				$"peak={PeakMS,5:##0.00} " +
				$"gc={BytesToString(MemoryChange)} " +
				$"gcTotal={BytesToString(gcTotal_)}";
		}

		private string BytesToString(long bytes)
		{
			string[] sizes = { "B", "KB", "MB", "GB", "TB" };
			double len = (double)bytes;
			int order = 0;
			while (len >= 1024 && order < sizes.Length - 1)
			{
				order++;
				len = len / 1024;
			}

			// Adjust the format string to your preferences. For example "{0:0.#}{1}" would
			// show a single decimal place, and no space.
			return string.Format($"{len,7:###0.00} {sizes[order],-2}");
		}
	}


	class Instrumentation
	{
		public const bool AlwaysActive = false;

		private Ticker[] tickers_ = new Ticker[I.Count];
		private int[] depth_ = new int[I.Count]
		{
			0, 1, 1, 2, 2, 2, 3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 2, 1, 0, 0
		};

		private int[] stack_ = new int[4];
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
				Cue.LogErrorST($"bad current {current_}");
				Cue.Instance.DisablePlugin();
			}

			if (i.Int < 0 || i.Int >= tickers_.Length)
			{
				Cue.LogErrorST($"bad index {i}");
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

		public int Depth(int i)
		{
			return depth_[i];
		}

		public string Name(int i)
		{
			return tickers_[i].Name;
		}

		public Ticker Get(int i)
		{
			return tickers_[i];
		}

		private void DoUpdateTickers(float s)
		{
			if (!Enabled)
				return;

			for (int i = 0; i < tickers_.Length; ++i)
				tickers_[i].Update(s);
		}
	}


	/*static class I
	{
		public const int Update = 0;
		public const int UpdateInput = 1;
		public const int UpdateObjects = 2;
		public const int UpdateObjectsAtoms = 3;
		public const int UpdatePersonAnimator = 4;
		public const int UpdatePersonGaze = 5;
		public const int UpdatePersonVoice = 6;
		public const int UpdatePersonExcitement = 7;
		public const int UpdatePersonMood = 8;
		public const int UpdatePersonBody = 9;
		public const int UpdatePersonHoming = 10;
		public const int UpdatePersonStatus = 11;
		public const int UpdatePersonAI = 12;
		public const int UpdateUi = 13;
		public const int FixedUpdate = 14;
		public const int LateUpdate = 15;
		public const int UpdateGazeEmergency = 16;
		public const int UpdateGazePicker = 17;
		public const int UpdateGazeTargets = 18;
		public const int UpdateGazePostTarget = 19;
		public const int TickerCount = 20;



		public static void Start(int i)
		{
			instance_.Start(i);
		}

		public static void End()
		{
			instance_.End();
		}
	}*/
}
