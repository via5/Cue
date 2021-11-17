using SimpleJSON;

namespace Cue.Proc
{
	public interface ISync
	{
		ITarget Target { get; set; }

		ISync Clone();

		int State{ get; }
		int UpdateResult { get; }

		bool Finished { get; }
		float Magnitude { get; }
		float Energy { set; }

		void RequestStop();
		void Reset();
		int FixedUpdate(float s);
		string ToDetailedString();
	}


	public abstract class BasicSync : ISync
	{
		private const int NoState = 0;
		public const int ForwardsState = 1;
		public const int ForwardsDelayState = 2;
		public const int BackwardsState = 3;
		public const int BackwardsDelayState = 4;
		public const int FinishedState = 5;

		public const int Working = 1;
		public const int DurationFinished = 2;
		public const int Delaying = 3;
		public const int DelayFinished = 4;
		public const int Looping = 5;
		public const int SyncFinished = 6;

		private ITarget target_ = null;
		private int state_ = ForwardsState;
		private int nextState_ = NoState;
		private int updateResult_ = Working;


		public static ISync Create(JSONClass o)
		{
			if (o == null)
				throw new LoadFailed("sync null object");

			if (!o.HasKey("type"))
				throw new LoadFailed("sync missing type");

			var type = o["type"].Value;
			switch (type)
			{
				case "duration":
					return DurationSync.Create(o);

				case "nosync":
					return NoSync.Create(o);

				case "parent":
					return ParentTargetSync.Create(o);

				case "elapsed":
					return ElapsedSync.Create(o);

				default:
					throw new LoadFailed($"bad sync type '{type}'");
			}
		}

		public abstract ISync Clone();

		public ITarget Target
		{
			get { return target_; }
			set { target_ = value; }
		}

		public virtual int State
		{
			get { return state_; }
		}

		public int NextState
		{
			get { return nextState_; }
		}

		public int UpdateResult
		{
			get { return updateResult_; }
		}

		public abstract float Magnitude { get; }
		public abstract float Energy { set; }
		public abstract bool Finished { get; }

		public virtual void RequestStop()
		{
			// no-op
		}

		public virtual void Reset()
		{
			state_ = ForwardsState;
			nextState_ = NoState;
			updateResult_ = Working;
		}

		public abstract string ToDetailedString();

		public int FixedUpdate(float s)
		{
			if (nextState_ != NoState)
			{
				state_ = nextState_;
				nextState_ = NoState;
			}

			updateResult_ = DoFixedUpdate(s);
			return updateResult_;
		}

		protected abstract int DoFixedUpdate(float s);

		protected void SetState(int s)
		{
			nextState_ = s;
		}

		protected void SetStateNow(int s)
		{
			state_ = s;
			nextState_ = NoState;
		}
	}


	public class NoSync : BasicSync
	{
		public override float Magnitude { get { return 0; } }
		public override float Energy { set { } }
		public override bool Finished { get { return true; } }

		public new static NoSync Create(JSONClass o)
		{
			return new NoSync();
		}

		public override ISync Clone()
		{
			return new NoSync();
		}

		protected override int DoFixedUpdate(float s)
		{
			return SyncFinished;
		}

		public override string ToDetailedString()
		{
			return "nosync";
		}
	}


	public class ParentTargetSync : BasicSync
	{
		public ParentTargetSync()
		{
		}

		public override int State
		{
			get { return Target?.Parent?.Sync.State ?? base.State; }
		}

		public override float Magnitude
		{
			get { return Target?.Parent?.Sync.Magnitude ?? 1; }
		}

		public override float Energy
		{
			set { }
		}

		public override bool Finished
		{
			get { return Target?.Parent?.Sync.Finished ?? true; }
		}

		public new static ParentTargetSync Create(JSONClass o)
		{
			return new ParentTargetSync();
		}

		public override ISync Clone()
		{
			return new ParentTargetSync();
		}

		protected override int DoFixedUpdate(float s)
		{
			return Target?.Parent?.Sync.UpdateResult ?? SyncFinished;
		}

		public override string ToDetailedString()
		{
			return "parent";
		}
	}


	public class SyncOther : BasicSync
	{
		private BasicSync other_;
		private bool waiting_ = true;
		private float mag_ = 0;

		public SyncOther(ISync sync)
		{
			other_ = sync as BasicSync;
		}

		public override int State
		{
			get { return other_.State; }
		}

		public override float Magnitude
		{
			get { return mag_; }
		}

		public override float Energy
		{
			set { }
		}

		public override bool Finished
		{
			get { return other_.Finished; }
		}

		public override ISync Clone()
		{
			return null;
		}

		protected override int DoFixedUpdate(float s)
		{
			if (waiting_)
			{
				if (other_.UpdateResult == Looping && other_.NextState == ForwardsState)
				{
					waiting_ = false;
					mag_ = 0;
					return Working;
				}
			}

			if (waiting_)
			{
				return Working;
			}
			else
			{
				mag_ = other_.Magnitude;
				return other_.UpdateResult;
			}
		}

		public override string ToDetailedString()
		{
			return $"sync other {other_}";
		}
	}



	public class ElapsedSync : BasicSync
	{
		public const int NoFlags = 0x00;
		public const int Loop = 0x01;

		private float duration_;
		private int flags_;
		private float elapsed_ = 0;
		private IEasing easing_ = new SinusoidalEasing();

		public ElapsedSync(float duration, int flags = NoFlags)
		{
			duration_ = duration;
			flags_ = flags;
		}

		public override float Magnitude
		{
			get
			{
				if (duration_ <= 0)
					return 1;

				float p = U.Clamp(elapsed_ / duration_, 0, 1);
				return easing_.Magnitude(p);
			}
		}

		public override float Energy { set { } }
		public override bool Finished
		{
			get { return (elapsed_ >= duration_); }
		}

		public new static ElapsedSync Create(JSONClass o)
		{
			return new ElapsedSync(o["duration"].AsFloat);
		}

		public override ISync Clone()
		{
			return new ElapsedSync(duration_, flags_);
		}

		public override void Reset()
		{
			base.Reset();
			elapsed_ = 0;
		}

		protected override int DoFixedUpdate(float s)
		{
			elapsed_ += s;

			if (elapsed_ >= duration_)
			{
				if (Bits.IsSet(flags_, Loop))
				{
					elapsed_ = 0;
					return Looping;
				}
				else
				{
					return SyncFinished;
				}
			}

			return Working;
		}

		public override string ToDetailedString()
		{
			return $"elapsed\n{elapsed_}/{duration_}";
		}
	}



	public class DurationSync : BasicSync
	{
		public const int NoFlags = 0x00;
		public const int Loop = 0x01;
		public const int ResetBetween = 0x02;
		public const int StartFast = 0x04;

		private const float StopTime = 1;

		private Duration fwdDuration_, bwdDuration_;
		private Duration fwdDelay_, bwdDelay_;
		private IEasing fwdEasing_ = new LinearEasing();
		private IEasing bwdEasing_ = new LinearEasing();
		private int flags_;
		private bool stopping_ = false;
		private float stoppingElapsed_ = 0;
		private float energy_ = 0;
		private bool needsRestart_ = false;

		public DurationSync(
			Duration fwdDuration, Duration bwdDuration,
			Duration fwdDelay, Duration bwdDelay, int flags)
		{
			fwdDuration_ = fwdDuration;
			bwdDuration_ = bwdDuration;
			fwdDelay_ = fwdDelay;
			bwdDelay_ = bwdDelay;
			flags_ = flags;
		}

		public new static DurationSync Create(JSONClass o)
		{
			Duration fwd, bwd;

			if (o.HasKey("duration"))
			{
				fwd = Duration.FromJSON(o, "duration");
				bwd = null;
			}
			else
			{
				fwd = Duration.FromJSON(o, "fwdDuration");
				bwd = Duration.FromJSON(o, "bwdDuration");
			}

			var fwdD = Duration.FromJSON(o, "fwdDelay");
			var bwdD = Duration.FromJSON(o, "bwdDelay");

			int flags = NoFlags;

			if (o["loop"].AsBool)
				flags |= Loop;

			if (o["resetBetween"].AsBool)
				flags |= ResetBetween;

			return new DurationSync(fwd, bwd, fwdD, bwdD, flags);
		}

		public override ISync Clone()
		{
			return new DurationSync(
				fwdDuration_.Clone(),
				bwdDuration_?.Clone(),
				fwdDelay_?.Clone(),
				bwdDelay_?.Clone(),
				flags_);
		}

		public override bool Finished
		{
			get
			{
				return
					(State == FinishedState) ||
					CurrentDuration().Finished ||
					(stopping_ && stoppingElapsed_ >= StopTime);
			}
		}

		public override float Energy
		{
			set { energy_ = value; }
		}

		public override float Magnitude
		{
			get
			{
				if (stopping_)
					return U.Clamp(stoppingElapsed_ / StopTime, 0, 1);

				var p = CurrentDuration().Progress;

				if (State == BackwardsState && !Bits.IsSet(flags_, ResetBetween))
					p = 1 - p;

				return CurrentEasing()?.Magnitude(p) ?? 0;
			}
		}

		public Duration CurrentDuration()
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
					return null;
			}
		}

		public override void Reset()
		{
			base.Reset();

			stopping_ = false;
			SetStateNow(ForwardsState);
			fwdDuration_?.Reset(energy_, Bits.IsSet(flags_, StartFast));
			bwdDuration_?.Reset(energy_);
			fwdDelay_?.Reset(energy_);
			bwdDelay_?.Reset(energy_);
			needsRestart_ = false;
		}

		public override void RequestStop()
		{
			stopping_ = true;
			stoppingElapsed_ = 0;
		}

		protected override int DoFixedUpdate(float s)
		{
			if (stopping_)
			{
				stoppingElapsed_ += s;

				if (stoppingElapsed_ >= StopTime)
					return SyncFinished;
				else
					return Working;
			}

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
				$"sliding\n" +
				$"fdur={fwdDuration_?.ToLiveString()}\n" +
				$"bdur={bwdDuration_?.ToLiveString()}\n" +
				$"fdel={fwdDelay_?.ToLiveString()}\n" +
				$"bdel={bwdDelay_?.ToLiveString()}\n" +
				$"p={CurrentDuration()?.Progress:0.00} mag={Magnitude:0.00}\n" +
				$"state={State} finished={Finished} stopping={stopping_} stopelapsed={stoppingElapsed_:0.00}";
		}

		private int DoForwards(float s)
		{
			if (needsRestart_)
			{
				fwdDuration_.Restart();
				needsRestart_ = false;
			}

			fwdDuration_.Update(s, energy_);

			if (fwdDuration_.Finished)
			{
				if (bwdDuration_ == null)
					needsRestart_ = true;

				if (fwdDelay_?.Enabled ?? false)
				{
					SetState(ForwardsDelayState);
					return Delaying;
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
			fwdDelay_.Update(s, energy_);

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

			return Delaying;
		}

		private int DoBackwards(float s)
		{
			var d = CurrentDuration();

			d.Update(s, energy_);

			if (d.Finished)
			{
				if (bwdDelay_?.Enabled ?? false)
				{
					SetState(BackwardsDelayState);
					return Delaying;
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
			bwdDelay_.Update(s, energy_);

			if (bwdDelay_.Finished)
			{
				if (Bits.IsSet(flags_, Loop))
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

			return Delaying;
		}
	}
}
