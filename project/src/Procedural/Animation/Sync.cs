using SimpleJSON;

namespace Cue.Proc
{
	interface ISync
	{
		ISync Clone();

		int State{ get; }
		bool Finished { get; }
		float Magnitude { get; }
		float Excitement { set; }

		void Reset();
		int FixedUpdate(float s);
		string ToDetailedString();
	}


	abstract class BasicSync : ISync
	{
		public const int ForwardsState = 1;
		public const int ForwardsDelayState = 2;
		public const int BackwardsState = 3;
		public const int BackwardsDelayState = 4;
		public const int FinishedState = 5;

		public const int Working = 1;
		public const int DurationFinished = 2;
		public const int DelayFinished = 3;
		public const int Looping = 4;
		public const int SyncFinished = 5;

		private int state_ = ForwardsState;

		public static ISync Create(JSONClass o)
		{
			SlidingDuration fwd, bwd;

			if (o.HasKey("duration"))
			{
				fwd = SlidingDuration.FromJSON(o, "duration");
				bwd = null;
			}
			else
			{
				fwd = SlidingDuration.FromJSON(o, "fwdDuration");
				bwd = SlidingDuration.FromJSON(o, "bwdDuration");
			}

			var fwdD = Duration.FromJSON(o, "fwdDelay");
			var bwdD = Duration.FromJSON(o, "bwdDelay");

			int flags = SlidingDurationSync.NoFlags;

			if (o["loop"].AsBool)
				flags |= SlidingDurationSync.Loop;

			if (o["resetBetween"].AsBool)
				flags |= SlidingDurationSync.ResetBetween;

			return new SlidingDurationSync(fwd, bwd, fwdD, bwdD, flags);
		}

		public abstract ISync Clone();

		public int State
		{
			get { return state_; }
		}

		public abstract float Magnitude { get; }
		public abstract float Excitement { set; }
		public abstract bool Finished { get; }

		public virtual void Reset()
		{
			state_ = ForwardsState;
		}

		public abstract string ToDetailedString();
		public abstract int FixedUpdate(float s);

		protected void SetState(int s)
		{
			state_ = s;
		}
	}


	class SlidingDurationSync : BasicSync
	{
		public const int NoFlags = 0x00;
		public const int Loop = 0x01;
		public const int ResetBetween = 0x02;

		private SlidingDuration fwdDuration_, bwdDuration_;
		private Duration fwdDelay_, bwdDelay_;
		private IEasing fwdEasing_ = new SinusoidalEasing();
		private IEasing bwdEasing_ = new SinusoidalEasing();
		private int flags_;

		public SlidingDurationSync(
			SlidingDuration fwdDuration, SlidingDuration bwdDuration,
			Duration fwdDelay, Duration bwdDelay, int flags)
		{
			fwdDuration_ = fwdDuration;
			bwdDuration_ = bwdDuration;
			fwdDelay_ = fwdDelay;
			bwdDelay_ = bwdDelay;
			flags_ = flags;
		}

		public override ISync Clone()
		{
			return new SlidingDurationSync(
				new SlidingDuration(fwdDuration_),
				bwdDuration_ == null ? null : new SlidingDuration(bwdDuration_),
				new Duration(fwdDelay_),
				new Duration(bwdDelay_),
				flags_);
		}

		public override bool Finished
		{
			get { return CurrentDuration().Finished; }
		}

		public override float Excitement
		{
			set
			{
				CurrentDuration().Excitement = value;
			}
		}

		public override float Magnitude
		{
			get
			{
				var p = CurrentDuration().Progress;
				return CurrentEasing()?.Magnitude(p) ?? 0;
			}
		}

		private IDuration CurrentDuration()
		{
			switch (State)
			{
				case ForwardsState:
				case ForwardsDelayState:
					return fwdDuration_;

				case BackwardsState:
				case BackwardsDelayState:
					return bwdDuration_ == null ? fwdDuration_ : bwdDuration_;

				default:
					Cue.LogError("??");
					return fwdDuration_;
			}
		}

		private IEasing CurrentEasing()
		{
			switch (State)
			{
				case ForwardsState:
				case ForwardsDelayState:
					return fwdEasing_;

				case BackwardsState:
				case BackwardsDelayState:
					return bwdEasing_;

				default:
					Cue.LogError("??");
					return null;
			}
		}

		public override void Reset()
		{
			base.Reset();
			fwdDuration_.Reset();
			bwdDuration_?.Reset();
			fwdDelay_.Reset();
			bwdDelay_.Reset();
		}

		public override int FixedUpdate(float s)
		{
			switch (State)
			{
				case ForwardsState:
					return DoForwards(s);

				case ForwardsDelayState:
					return DoForwardsDelay(s);

				case BackwardsState:
					return DoBackwards(s);

				case BackwardsDelayState:
					return DoBackwardsDelay(s);

				default:
					return Working;
			}
		}

		public override string ToDetailedString()
		{
			return
				$"fdur={fwdDuration_}\n" +
				$"bdur={bwdDuration_}\n" +
				$"fdel={fwdDelay_} bdel={bwdDelay_}\n" +
				$"p={CurrentDuration().Progress:0.00} mag={Magnitude:0.00}\n" +
				$"state={State}";
		}

		private int DoForwards(float s)
		{
			fwdDuration_.Update(s);

			if (fwdDuration_.Finished)
			{
				if (bwdDuration_ == null)
					fwdDuration_.Restart();

				if (fwdDelay_.Enabled)
				{
					SetState(ForwardsDelayState);
					return DurationFinished;
				}
				else if (Bits.IsSet(flags_, ResetBetween))
				{
					SetState(BackwardsState);
					return DurationFinished;
				}
				else if (Bits.IsSet(flags_, Loop))
				{
					return Looping;
				}
				else
				{
					SetState(FinishedState);
					return SyncFinished;
				}
			}

			return Working;
		}

		private int DoForwardsDelay(float s)
		{
			fwdDelay_.Update(s);

			if (fwdDelay_.Finished)
			{
				if (Bits.IsSet(flags_, ResetBetween))
				{
					SetState(BackwardsState);
					return DelayFinished;
				}
				else if (Bits.IsSet(flags_, Loop))
				{
					SetState(ForwardsState);
					return Looping;
				}
				else
				{
					SetState(FinishedState);
					return SyncFinished;
				}
			}

			return Working;
		}

		private int DoBackwards(float s)
		{
			var d = CurrentDuration();

			d.Update(s);

			if (d.Finished)
			{
				if (bwdDelay_.Enabled)
				{
					SetState(BackwardsDelayState);
					return DurationFinished;
				}
				else if (Bits.IsSet(flags_, Loop))
				{
					SetState(ForwardsState);
					return Looping;
				}
				else
				{
					SetState(FinishedState);
					return SyncFinished;
				}
			}

			return Working;
		}

		private int DoBackwardsDelay(float s)
		{
			bwdDelay_.Update(s);

			if (bwdDelay_.Finished)
			{
				if (Bits.IsSet(flags_, Loop))
				{
					SetState(ForwardsState);
					return DelayFinished;
				}
				else
				{
					SetState(FinishedState);
					return SyncFinished;
				}
			}

			return Working;
		}
	}
}
