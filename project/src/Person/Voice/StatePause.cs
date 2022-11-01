using SimpleJSON;
using System;

namespace Cue
{
	public class VoiceStateRandomPause : VoiceState
	{
		private float minExcitement_ = 0;
		private float cooldown_ = 0;
		private RandomRange timePausedRange_ = new RandomRange();
		private RandomRange timeHighRange_ = new RandomRange();
		private float chance_ = 0;
		private IRandom rng_ = new UniformRandom();

		private float time_ = 0;
		private float elapsed_ = 0;
		private float cooldownElapsed_ = 0;
		private float lastRng_ = 0;
		private bool inPause_ = false;


		private VoiceStateRandomPause()
		{
		}

		public VoiceStateRandomPause(JSONClass vo)
		{
			Load(vo, false);
		}

		public override void Load(JSONClass vo, bool inherited)
		{
			if (vo.HasKey("pauseState"))
			{
				var o = J.ReqObject(vo, "pauseState");

				if (o.HasKey("minExcitement"))
					minExcitement_ = J.ReqFloat(o, "minExcitement");
				else if (!inherited)
					throw new LoadFailed("missing minExcitement");

				if (o.HasKey("cooldown"))
					cooldown_ = J.ReqFloat(o, "cooldown");
				else if (!inherited)
					throw new LoadFailed("missing cooldown");

				if (o.HasKey("timePaused"))
					timePausedRange_ = RandomRange.Create(o, "timePaused");
				else if (!inherited)
					throw new LoadFailed("missing timePaused");

				if (o.HasKey("timeHigh"))
					timeHighRange_ = RandomRange.Create(o, "timeHigh");
				else if (!inherited)
					throw new LoadFailed("missing timeHigh");

				if (o.HasKey("chance"))
				{
					var co = J.ReqObject(o, "chance");
					chance_ = J.ReqFloat(co, "value");

					if (co.HasKey("rng"))
					{
						rng_ = BasicRandom.FromJSON(co["rng"]);
						if (rng_ == null)
							throw new LoadFailed("bad pause rng");
					}
				}
				else if (!inherited)
				{
					throw new LoadFailed("missing chance");
				}
			}
			else if (!inherited)
			{
				throw new LoadFailed("missing pauseState");
			}
		}

		public override string Name
		{
			get { return "pause"; }
		}

		public override IVoiceState Clone()
		{
			var s = new VoiceStateRandomPause();
			s.CopyFrom(this);
			return s;
		}

		private void CopyFrom(VoiceStateRandomPause o)
		{
			minExcitement_ = o.minExcitement_;
			cooldown_ = o.cooldown_;
			timePausedRange_ = o.timePausedRange_.Clone();
			timeHighRange_ = o.timeHighRange_.Clone();
			chance_ = o.chance_;
			rng_ = o.rng_.Clone();
		}

		protected override void DoStart()
		{
			elapsed_ = 0;
			time_ = timePausedRange_.RandomFloat(v_.MaxIntensity);
			inPause_ = true;

			v_.Provider.SetSilent();
		}

		protected override void DoEarlyUpdate(float s)
		{
			cooldownElapsed_ = Math.Min(cooldownElapsed_ + s, cooldown_);
		}

		protected override void DoUpdate(float s)
		{
			elapsed_ += s;

			if (inPause_)
			{
				if (elapsed_ >= time_)
				{
					elapsed_ = 0;
					v_.Provider.SetMoaning(1.0f);
					time_ = timeHighRange_.RandomFloat(v_.MaxIntensity);
					inPause_ = false;
				}
			}
			else
			{
				if (elapsed_ >= time_)
				{
					cooldownElapsed_ = 0;
					SetDone();
				}
			}
		}

		protected override int DoCanRun()
		{
			if (cooldownElapsed_ < cooldown_)
			{
				SetLastState($"cooldown {cooldownElapsed_: 0.00}/{cooldown_: 0.00}");
				return CannotRun;
			}

			if (v_.MaxIntensity < minExcitement_)
			{
				SetLastState("excitement too low");
				return CannotRun;
			}

			lastRng_ = rng_.RandomFloat(0, 1, v_.MaxIntensity);
			if (lastRng_ >= chance_)
			{
				SetLastState($"rng failed, {lastRng_:0.00} >= {chance_:0.00}");
				return CannotRun;
			}

			SetLastState("ok");
			return HighPriority;
		}

		protected override void DoDebug(DebugLines debug)
		{
			debug.Add("minExcitement", $"{minExcitement_:0.00}");
			debug.Add("timePausedRange", $"{timePausedRange_}");
			debug.Add("timeHighRange", $"{timeHighRange_}");
			debug.Add("chance", $"{chance_:0.00}");
			debug.Add("rng", $"{rng_}");
			debug.Add("lastRng", $"{lastRng_}");
			debug.Add("elapsed", $"{elapsed_:0.00}/{time_:0.00}");
			debug.Add("cooldown", $"{cooldownElapsed_:0.00}/{cooldown_:0.00}");
		}
	}
}
