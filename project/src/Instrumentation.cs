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
				$"calls={Calls,-3} " +
				$"avg={AverageMs:00.000} " +
				$"peak={PeakMS:00.000} " +
				$"gc={MemoryChange,-9} " +
				$"gcTotal={gcTotal_,-9}";
		}
	}


	class Instrumentation
	{
		public const bool AlwaysActive = false;

		private Ticker[] tickers_ = new Ticker[I.TickerCount];
		private int[] depth_ = new int[I.TickerCount]
		{
			0, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1
		};
		private int[] stack_ = new int[4];
		private int current_ = 0;

		private bool enabled_ = false;

		public Instrumentation()
		{
			tickers_[I.Update] = new Ticker("Update");
			tickers_[I.UpdateInput] = new Ticker("Input");
			tickers_[I.UpdateObjects] = new Ticker("Objects");
			tickers_[I.UpdateObjectsAtoms] = new Ticker("Atoms");
			tickers_[I.UpdatePersonAnimator] = new Ticker("Animator");
			tickers_[I.UpdatePersonGaze] = new Ticker("Gaze");
			tickers_[I.UpdatePersonExcitement] = new Ticker("Excitement");
			tickers_[I.UpdatePersonMood] = new Ticker("Mood");
			tickers_[I.UpdatePersonBody] = new Ticker("Body");
			tickers_[I.UpdatePersonStatus] = new Ticker("Status");
			tickers_[I.UpdatePersonAI] = new Ticker("AI");
			tickers_[I.UpdateUi] = new Ticker("UI");
			tickers_[I.FixedUpdate] = new Ticker("Fixed update");
		}

		public bool Updated
		{
			get { return tickers_[I.Update].Updated; }
		}

		public bool Enabled
		{
			get { return enabled_ || AlwaysActive; }
			set { enabled_ = value; }
		}

		public void Start(int i)
		{
			if (!Enabled)
				return;

			if (current_ < 0 || current_ >= stack_.Length)
			{
				Cue.LogErrorST($"bad current {current_}");
				Cue.Instance.DisablePlugin();
			}

			if (i < 0 || i >= tickers_.Length)
			{
				Cue.LogErrorST($"bad index {i}");
				Cue.Instance.DisablePlugin();
			}

			stack_[current_] = i;
			++current_;

			tickers_[i].Start();
		}

		public void End()
		{
			if (!Enabled)
				return;

			--current_;

			int i = stack_[current_];
			stack_[current_] = -1;

			tickers_[i].End();
		}

		public void Reset()
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

		public void UpdateTickers(float s)
		{
			if (!Enabled)
				return;

			for (int i = 0; i < tickers_.Length; ++i)
				tickers_[i].Update(s);
		}
	}


	static class I
	{
		public const int Update = 0;
		public const int UpdateInput = 1;
		public const int UpdateObjects = 2;
		public const int UpdateObjectsAtoms = 3;
		public const int UpdatePersonAnimator = 4;
		public const int UpdatePersonGaze = 5;
		public const int UpdatePersonExcitement = 6;
		public const int UpdatePersonMood = 7;
		public const int UpdatePersonBody = 8;
		public const int UpdatePersonStatus = 9;
		public const int UpdatePersonAI = 10;
		public const int UpdateUi = 11;
		public const int FixedUpdate = 12;
		public const int TickerCount = 13;



		private static Instrumentation instance_ = new Instrumentation();

		public static Instrumentation Instance
		{
			get { return instance_; }
		}

		public static void Start(int i)
		{
			instance_.Start(i);
		}

		public static void End()
		{
			instance_.End();
		}
	}
}
