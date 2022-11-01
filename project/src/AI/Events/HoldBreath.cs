using SimpleJSON;
using System;
using UnityThreading;

namespace Cue
{
	class HoldBreathEventData : IEventData
	{
		public float minExcitement = 0;
		public float cooldown = 0;
		public RandomRange holdTime = new RandomRange();
		public float chance = 0;
		public IRandom rng = new UniformRandom();

		public IEventData Clone()
		{
			var d = new HoldBreathEventData();
			d.CopyFrom(this);
			return d;
		}

		private void CopyFrom(HoldBreathEventData d)
		{
			minExcitement = d.minExcitement;
			cooldown = d.cooldown;
			holdTime = d.holdTime.Clone();
			chance = d.chance;
			rng = d.rng.Clone();
		}
	}

	class HoldBreathEvent : BasicEvent
	{
		private HoldBreathEventData d_ = null;
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

		protected override IEventData DoParseEventData(JSONClass o)
		{
			var d = new HoldBreathEventData();

			d.minExcitement = J.ReqFloat(o, "minExcitement");
			d.cooldown = J.ReqFloat(o, "cooldown");
			d.holdTime = RandomRange.Create(o, "holdTime");

			d.chance = J.ReqFloat(o, "chance");
			d.rng = BasicRandom.FromJSON(o["chanceRng"]);

			return d;
		}

		public override bool Active
		{
			get { return active_; }
			set { active_ = value; }
		}

		public override bool CanToggle { get { return true; } }
		public override bool CanDisable { get { return true; } }

		protected override void DoInit()
		{
			OnPersonalityChanged();
			person_.PersonalityChanged += OnPersonalityChanged;
		}

		private void OnPersonalityChanged()
		{
			d_ = person_.Personality.CloneEventData(Name) as HoldBreathEventData;
		}

		public override void Debug(DebugLines debug)
		{
			debug.Add("active", $"{active_}");
			debug.Add("elapsed", $"{elapsed_:0.00}");
			debug.Add("minExcitement", $"{d_.minExcitement:0.00}");
			debug.Add("holdTime", $"{d_.holdTime}");
			debug.Add("chance", $"{d_.chance:0.00}");
			debug.Add("rng", $"{d_.rng}");
			debug.Add("lastRng", $"{lastRng_}");
			debug.Add("elapsed", $"{elapsed_:0.00}/{time_:0.00}");
			debug.Add("cooldown", $"{cooldownElapsed_:0.00}/{d_.cooldown:0.00}");
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
				cooldownElapsed_ = Math.Min(cooldownElapsed_ + s, d_.cooldown);

				if (CheckRun())
					Start();
			}
		}

		private bool CheckRun()
		{
			if (cooldownElapsed_ < d_.cooldown)
			{
				lastState_ = $"cooldown {cooldownElapsed_: 0.00}/{d_.cooldown: 0.00}";
				return false;
			}

			if (person_.Mood.Get(MoodType.Excited) < d_.minExcitement)
			{
				lastState_ = "excitement too low";
				return false;
			}

			lastRng_ = d_.rng.RandomFloat(0, 1, person_.Mood.MovementEnergy);
			if (lastRng_ >= d_.chance)
			{
				lastState_ = $"rng failed, {lastRng_:0.00} >= {d_.chance:0.00}";
				return false;
			}

			lastState_ = "ok";
			return true;
		}

		private void Start()
		{
			person_.Body.Breathing = false;

			elapsed_ = 0;
			time_ = d_.holdTime.RandomFloat(person_.Mood.MovementEnergy);
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
