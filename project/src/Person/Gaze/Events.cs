using System.Collections.Generic;

namespace Cue
{
	public interface IGazeEvent
	{
		int Check(int flags);
		bool HasEmergency(float s);
	}


	abstract class BasicGazeEvent : IGazeEvent
	{
		public const int Continue = 0x00;
		public const int NoGazer = 0x01;
		public const int NoRandom = 0x02;
		public const int Busy = 0x04;

		protected Person person_;
		protected Gaze g_;
		protected GazeTargets targets_;
		private InstrumentationType inst_;

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
				new GazeOtherPersons(p)
			}.ToArray();
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

		public bool HasEmergency(float s)
		{
			return DoHasEmergency(s);
		}

		protected virtual bool DoHasEmergency(float s)
		{
			return false;
		}

		public override abstract string ToString();
	}
}
