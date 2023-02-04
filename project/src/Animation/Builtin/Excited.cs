using System.Collections.Generic;

namespace Cue.Proc
{
	public class Excited : BuiltinAnimation
	{
		class Action
		{
			private const float Interval = 5;
			private const float StopTime = 1;
			private const float MinExcitement = 0.25f;

			private const int NoState = 0;
			private const int UpState = 1;
			private const int DelayState = 2;
			private const int DownState = 3;
			private const int StoppingState = 4;
			private const int StoppedState = 5;

			private readonly MorphGroup g_;
			private readonly IEasing easing_ = new SinusoidalEasing();
			private readonly IRandom startRng_;
			private readonly IRandom timeRng_;
			private readonly IRandom delayRng_;
			private Person p_ = null;

			private int state_ = NoState;
			private float elapsed_ = 0;
			private float timeUp_ = 0;
			private float timeDown_ = 0;
			private float timeDelay_ = 0;
			private float intervalElapsed_ = 0;
			private float stopValue_ = 0;
			private float lastStartRng_ = 0;

			private BodyPartLock[] locks_ = null;
			private string lastStartResult_ = "";

			public Action(MorphGroup g)
			{
				g_ = g;
				startRng_ = new NormalRandom(0.2f, 0.3f, 1, 1);
				timeRng_ = new UniformRandom();
				delayRng_ = new NormalRandom();
			}

			public void Init(Person p)
			{
				p_ = p;
				g_.Init(p);
				g_.Reset();
			}

			private bool Start()
			{
				locks_ = BodyPartLock.LockMany(
					p_, g_.BodyParts, BodyPartLock.Morph,
					"excited anim", BodyPartLock.Weak);

				if (locks_ == null)
				{
					lastStartResult_ = "lock failed";
					return false;
				}

				state_ = UpState;
				elapsed_ = 0;
				timeUp_ = timeRng_.RandomFloat(0.1f, 1, 1);
				timeDown_ = timeRng_.RandomFloat(0.1f, 1, 1);
				timeDelay_ = delayRng_.RandomFloat(0, 8, 1);

				return true;
			}

			public void Stop()
			{
				state_ = StoppingState;
				elapsed_ = 0;
				stopValue_ = g_.Value;
			}

			private void Stopped()
			{
				if (locks_ != null)
				{
					for (int i = 0; i < locks_.Length; ++i)
						locks_[i].Unlock();

					locks_ = null;
				}
			}

			public bool Done
			{
				get
				{
					return (state_ == StoppedState);
				}
			}

			public void FixedUpdate(float s)
			{
				switch (state_)
				{
					case UpState:
					{
						if (MustStop())
						{
							Stop();
							break;
						}

						elapsed_ += s;

						float f = U.Clamp(elapsed_ / timeUp_, 0, 1);

						if (elapsed_ >= timeUp_)
						{
							elapsed_ = 0;
							state_ = DelayState;
						}

						g_.Value = easing_.Magnitude(f);

						break;
					}

					case DelayState:
					{
						if (MustStop())
						{
							Stop();
							break;
						}

						elapsed_ += s;

						if (elapsed_ >= timeDelay_)
						{
							elapsed_ = 0;
							state_ = DownState;
						}

						break;
					}

					case DownState:
					{
						if (MustStop())
						{
							Stop();
							break;
						}

						elapsed_ += s;

						float f = U.Clamp(1 - elapsed_ / timeDown_, 0, 1);

						if (elapsed_ >= timeDown_)
						{
							elapsed_ = 0;
							state_ = NoState;
							Stopped();
						}

						g_.Value = easing_.Magnitude(f);

						break;
					}

					case StoppingState:
					{
						elapsed_ += s;

						float t = U.Clamp(elapsed_ / StopTime, 0, 1);
						float v = U.Lerp(stopValue_, 0, t);
						g_.Value = easing_.Magnitude(v);

						if (t >= 1)
						{
							state_ = StoppedState;
							Stopped();
						}

						break;
					}

					case StoppedState:
					{
						state_ = NoState;
						break;
					}
				}
			}

			public bool MustStop()
			{
				if (locks_ != null)
				{
					for (int i = 0; i < locks_.Length; ++i)
					{
						if (locks_[i].Expired)
							return true;
					}
				}

				return false;
			}

			public void Update(float s)
			{
				if (state_ == NoState)
				{
					intervalElapsed_ += s;

					if (intervalElapsed_ >= Interval)
					{
						intervalElapsed_ = 0;
						lastStartResult_ = "";

						var ex = p_.Mood.Get(MoodType.Excited);
						if (ex >= MinExcitement)
						{
							lastStartRng_ = startRng_.RandomFloat(0, 1, ex);

							if (lastStartRng_ >= 0)//0.5f)
								Start();
						}
						else
						{
							lastStartResult_ = "excitement too low";
						}
					}
				}
			}

			public void Debug(DebugLines debug)
			{
				debug.Add(g_.Name);
				debug.Add($"rng: s={startRng_} t={timeRng_} d={delayRng_}");
				debug.Add($"state={state_} lastStart={lastStartRng_:0.00} stopValue={stopValue_:0.00} intElapsed={intervalElapsed_:0.00}");
				debug.Add($"elapsed={elapsed_:0.00} timeup={timeUp_:0.00} timedelay={timeDelay_:0.00} timedown={timeDown_:0.00}");
				debug.Add($"value={g_.Value:0.00} lastStart={lastStartResult_}");
			}
		}


		private readonly Action[] actions_;
		private bool done_ = false;

		public Excited()
			: base("cueExcited")
		{
			var list = new List<Action>();

			list.Add(new Action(new MorphGroup(
				"left hand fist", BP.LeftHand, new MorphGroup.MorphInfo[]
				{
					MI("Left Thumb Bend",          0,   0.210f),
					MI("Left Thumb Grasp",         0,   0.375f),
					MI("Left Thumb In-Out",        0,  -0.375f),
					MI("Left Fingers Grasp",       0,   1.0f),
					MI("Left Fingers Straighten",  0,  -1.0f)
				})));

			list.Add(new Action(new MorphGroup(
				"right hand fist", BP.RightHand, new MorphGroup.MorphInfo[]
				{
					MI("Right Thumb Bend",          0,   0.210f),
					MI("Right Thumb Grasp",         0,   0.375f),
					MI("Right Thumb In-Out",        0,  -0.375f),
					MI("Right Fingers Grasp",       0,   1.0f),
					MI("Right Fingers Straighten",  0,  -1.0f)
				})));

			actions_ = list.ToArray();
		}

		private MorphGroup.MorphInfo MI(string name, float min, float max)
		{
			return new MorphGroup.MorphInfo(name, min, max);
		}

		public override BuiltinAnimation Clone()
		{
			var a = new Excited();
			a.CopyFrom(this);
			return a;
		}

		public override bool Done
		{
			get { return done_; }
		}

		public override void RequestStop(int stopFlags)
		{
			for (int i = 0; i < actions_.Length; ++i)
				actions_[i].Stop();
		}

		public override bool Start(Person p, AnimationContext cx)
		{
			if (!base.Start(p, cx))
				return false;

			for (int i = 0; i < actions_.Length; ++i)
				actions_[i].Init(p);

			return true;
		}

		public override void FixedUpdate(float s)
		{
			base.FixedUpdate(s);

			for (int i = 0; i < actions_.Length; ++i)
				actions_[i].FixedUpdate(s);
		}

		public override void Update(float s)
		{
			done_ = true;
			for (int i = 0; i < actions_.Length; ++i)
			{
				actions_[i].Update(s);
				if (!actions_[i].Done)
					done_ = false;
			}
		}

		public override void Debug(DebugLines debug)
		{
			for (int i = 0; i < actions_.Length; ++i)
			{
				if (i > 0)
					debug.Add("");

				actions_[i].Debug(debug);
			}
		}
	}
}
