using SimpleJSON;

namespace Cue
{
	public class VoiceStatePause : VoiceState
	{
		private float minExcitement_ = 0;
		private RandomRange timeRange_ = new RandomRange();
		private float chance_ = 0;
		private IRandom rng_ = new UniformRandom();

		private float time_ = 0;
		private float elapsed_ = 0;
		private float lastRng_ = 0;
		private bool inPause_ = false;


		private VoiceStatePause()
		{
		}

		public VoiceStatePause(JSONClass vo)
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

				if (o.HasKey("time"))
					timeRange_ = RandomRange.Create(o, "time");
				else if (!inherited)
					throw new LoadFailed("missing time");

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
			var s = new VoiceStatePause();
			s.CopyFrom(this);
			return s;
		}

		private void CopyFrom(VoiceStatePause o)
		{
			minExcitement_ = o.minExcitement_;
			timeRange_ = o.timeRange_.Clone();
			chance_ = o.chance_;
			rng_ = o.rng_.Clone();
		}

		protected override void DoStart()
		{
			elapsed_ = 0;
			time_ = timeRange_.RandomFloat(v_.MaxIntensity);
			inPause_ = true;

			v_.Provider.SetSilent();
		}

		protected override void DoUpdate(float s)
		{
			elapsed_ += s;

			if (inPause_)
			{
				if (elapsed_ >= time_)
				{
					v_.Provider.SetMoaning(1.0f);
					inPause_ = false;
				}
			}
			else
			{
				if (elapsed_ >= 3)
				{
					SetDone();
				}
			}
		}

		public override int CanRun()
		{
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

		public string SettingsToString()
		{
			return
				$"minEx={minExcitement_:0.00} timeRange={timeRange_} " +
				$"chance={chance_:0.00};rng={rng_}";
		}

		public string LiveToString()
		{
			return
				$"time={time_:0.00} elapsed={elapsed_:0.00} " +
				$"lastrng={lastRng_:0.00}";
		}

		protected override void DoDebug(DebugLines debug)
		{
			debug.Add("pause settings", SettingsToString());
			debug.Add("pause", LiveToString());
		}
	}
}
