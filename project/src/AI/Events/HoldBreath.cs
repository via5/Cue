using SimpleJSON;
using System;

namespace Cue
{
	class HoldBreathEventData : BasicEventData
	{
		public float minExcitement = 0;
		public float cooldown = 0;
		public RandomRange holdTime = new RandomRange();
		public float chance = 0;
		public IRandom rng = new UniformRandom();

		public override BasicEventData Clone()
		{
			var d = new HoldBreathEventData();
			d.CopyFrom(this);
			return d;
		}

		private void CopyFrom(HoldBreathEventData d)
		{
			base.CopyFrom(d);
			minExcitement = d.minExcitement;
			cooldown = d.cooldown;
			holdTime = d.holdTime.Clone();
			chance = d.chance;
			rng = d.rng.Clone();
		}
	}

	class HoldBreathEvent : BasicEvent<HoldBreathEventData>
	{
		private bool active_ = false;
		private float time_ = 0;
		private float elapsed_ = 0;
		private float cooldownElapsed_ = 0;
		private float lastRng_ = 0;
		private string lastState_ = "";

		public HoldBreathEvent()
			: base("HoldBreath")
		{
		}

		protected override void DoParseEventData(JSONClass o, HoldBreathEventData d)
		{
			d.minExcitement = J.ReqFloat(o, "minExcitement");
			d.cooldown = J.ReqFloat(o, "cooldown");
			d.holdTime = RandomRange.Create(o, "holdTime");

			d.chance = J.ReqFloat(o, "chance");
			d.rng = BasicRandom.FromJSON(o["chanceRng"]);
		}

		public override bool Active
		{
			get { return active_; }
			set { active_ = Enabled && value; }
		}

		public override bool CanToggle { get { return true; } }
		public override bool CanDisable { get { return true; } }

		protected override void DoDebug(DebugLines debug)
		{
			var d = Data;

			debug.Add("active", $"{active_}");
			debug.Add("elapsed", $"{elapsed_:0.00}");
			debug.Add("minExcitement", $"{d.minExcitement:0.00}");
			debug.Add("holdTime", $"{d.holdTime}");
			debug.Add("chance", $"{d.chance:0.00}");
			debug.Add("rng", $"{d.rng}");
			debug.Add("lastRng", $"{lastRng_}");
			debug.Add("elapsed", $"{elapsed_:0.00}/{time_:0.00}");
			debug.Add("cooldown", $"{cooldownElapsed_:0.00}/{d.cooldown:0.00}");
			debug.Add("last state", lastState_);
		}

		protected override void DoUpdate(float s)
		{
			if (!Enabled)
			{
				Stop();
				return;
			}

			elapsed_ += s;

			if (active_)
			{
				if (elapsed_ >= time_)
				{
					elapsed_ = 0;
					Stop();
				}
			}
			else
			{
				cooldownElapsed_ = Math.Min(cooldownElapsed_ + s, Data.cooldown);

				if (CheckRun())
					Start();
			}
		}

		private bool CheckRun()
		{
			if (cooldownElapsed_ < Data.cooldown)
			{
				lastState_ = $"cooldown {cooldownElapsed_: 0.00}/{Data.cooldown: 0.00}";
				return false;
			}

			if (person_.Mood.Get(MoodType.Excited) < Data.minExcitement)
			{
				lastState_ = "excitement too low";
				return false;
			}

			lastRng_ = Data.rng.RandomFloat(0, 1, person_.Mood.MovementEnergy);
			if (lastRng_ >= Data.chance)
			{
				lastState_ = $"rng failed, {lastRng_:0.00} >= {Data.chance:0.00}";
				return false;
			}

			lastState_ = "ok";
			return true;
		}

		private void Start()
		{
			person_.Body.Breathing = false;

			elapsed_ = 0;
			time_ = Data.holdTime.RandomFloat(person_.Mood.MovementEnergy);
			active_ = true;
		}

		private void Stop()
		{
			person_.Body.Breathing = true;
			active_ = false;
			elapsed_ = 0;
			cooldownElapsed_ = 0;
		}
	}
}
