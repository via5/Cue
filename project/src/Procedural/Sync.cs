using SimpleJSON;

namespace Cue.Proc
{
	interface ISync
	{
		ITarget Target { get; set; }

		ISync Clone();

		int State{ get; }
		int UpdateResult { get; }

		bool Finished { get; }
		float Magnitude { get; }
		float Energy { set; }

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
		public const int Delaying = 3;
		public const int DelayFinished = 4;
		public const int Looping = 5;
		public const int SyncFinished = 6;

		private ITarget target_ = null;
		private int state_ = ForwardsState;
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
				//case "duration":
				//	return DurationSync.Create(o);

				case "sliding":
					return SlidingDurationSync.Create(o);

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

		public int State
		{
			get { return state_; }
		}

		public int UpdateResult
		{
			get { return updateResult_; }
		}

		public abstract float Magnitude { get; }
		public abstract float Energy { set; }
		public abstract bool Finished { get; }

		public virtual void Reset()
		{
			state_ = ForwardsState;
		}

		public abstract string ToDetailedString();

		public int FixedUpdate(float s)
		{
			updateResult_ = DoFixedUpdate(s);
			return updateResult_;
		}

		protected abstract int DoFixedUpdate(float s);

		protected void SetState(int s)
		{
			state_ = s;
		}
	}


	class NoSync : BasicSync
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


	class ParentTargetSync : BasicSync
	{
		public ParentTargetSync()
		{
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


	class ElapsedSync : BasicSync
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

		public new static SlidingDurationSync Create(JSONClass o)
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

			int flags = NoFlags;

			if (o["loop"].AsBool)
				flags |= Loop;

			if (o["resetBetween"].AsBool)
				flags |= ResetBetween;

			return new SlidingDurationSync(fwd, bwd, fwdD, bwdD, flags);
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
			get
			{
				return (State == FinishedState) || CurrentDuration().Finished;
			}
		}

		public override float Energy
		{
			set
			{
				CurrentDuration().Energy = value;
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
					//Cue.LogError($"??{State}");
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
					//Cue.LogError($"??{State}");
					return null;
			}
		}

		public override void Reset()
		{
			base.Reset();
			SetState(ForwardsState);
			fwdDuration_.Reset();
			bwdDuration_?.Reset();
			fwdDelay_.Reset();
			bwdDelay_.Reset();
		}

		protected override int DoFixedUpdate(float s)
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
				$"sliding\n" +
				$"fdur={fwdDuration_.ToLiveString()}\n" +
				$"bdur={bwdDuration_.ToLiveString()}\n" +
				$"fdel={fwdDelay_.ToLiveString()} bdel={bwdDelay_.ToLiveString()}\n" +
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
			bwdDelay_.Update(s);

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

			return Working;
		}
	}
}
