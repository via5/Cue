using System.Collections.Generic;

namespace Cue
{
	public interface IGazeEvent
	{
		int Check(int flags);
		bool HasEmergency(float s);
		void ResetBeforeCheck();
		string DebugLine();
	}


	abstract class BasicGazeEvent : IGazeEvent
	{
		public const int Continue = 0x00;
		public const int NoGazer = 0x01;
		public const int NoRandom = 0x02;
		public const int Busy = 0x04;
		public const int Stop = 0x08;

		protected readonly Person person_;
		protected readonly Gaze g_;
		protected readonly GazeTargets targets_;
		private InstrumentationType inst_;
		private string lastResult_ = "";

		protected BasicGazeEvent(Person p, InstrumentationType inst)
		{
			person_ = p;
			g_ = p.Gaze;
			targets_ = p.Gaze.Targets;
			inst_ = inst;
		}

		public static IGazeEvent[] All(Person p)
		{
			return new List<IGazeEvent>()
			{
				new GazeAbove(p),
				new GazeFront(p),
				new GazeDown(p),
				new GazeGrabbed(p),
				new GazeZapped(p),
				new GazeKissing(p),
				new GazeMouth(p),
				new GazeHands(p),
				new GazeInteractions(p),
				new GazeRandom(p),
				new GazeOtherPersons(p),
				new GazeCustom(p)
			}.ToArray();
		}

		public void ResetBeforeCheck()
		{
			lastResult_ = "not checked";
		}

		public int Check(int flags)
		{
			int r;

			Instrumentation.Start(inst_);
			{
				r = DoCheck(flags);
			}
			Instrumentation.End();

			return r;
		}

		protected virtual int DoCheck(int flags)
		{
			return Continue;
		}

		protected void SetLastResult(string s)
		{
			lastResult_ = s;
		}

		public bool HasEmergency(float s)
		{
			bool b = DoHasEmergency(s);

			if (b)
			{
				if (lastResult_ != "")
					lastResult_ += " ";

				lastResult_ += " (emergency)";
			}

			return b;
		}

		protected virtual bool DoHasEmergency(float s)
		{
			return false;
		}

		public string DebugLine()
		{
			return $"{this}: {lastResult_}";
		}

		public override abstract string ToString();
	}
}
