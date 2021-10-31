﻿namespace Cue
{
	class ClockwiseSilverBlowjob : IBlowjob
	{
		private Person person_;
		private Logger log_;
		private Person target_ = null;

		private Sys.Vam.BoolParameter enabled_ = null;
		private Sys.Vam.BoolParameter active_ = null;
		private Sys.Vam.BoolParameterRO running_ = null;
		private Sys.Vam.StringChooserParameter male_ = null;
		private Sys.Vam.FloatParameter sfxVolume_ = null;
		private Sys.Vam.FloatParameter moanVolume_ = null;
		private Sys.Vam.FloatParameter volumeScaling_ = null;
		private Sys.Vam.FloatParameter headX_ = null;
		private Sys.Vam.FloatParameter headY_ = null;
		private Sys.Vam.FloatParameter headZ_ = null;

		private Sys.Vam.FloatParameter overallSpeed_ = null;
		private Sys.Vam.FloatParameter speedMin_ = null;
		private Sys.Vam.FloatParameter speedMax_ = null;
		private Sys.Vam.FloatParameter topOnlyChance_ = null;
		private Sys.Vam.FloatParameter mouthOpenMax_ = null;

		private bool wasActive_ = false;

		public ClockwiseSilverBlowjob(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Integration, p, "ClockwiseBJ");
			enabled_ = new Sys.Vam.BoolParameter(p, "ClockwiseSilver.BJ", "enabled");
			active_ = new Sys.Vam.BoolParameter(p, "ClockwiseSilver.BJ", "isActive");
			running_ = new Sys.Vam.BoolParameterRO(p, "ClockwiseSilver.BJ", "isBJRoutine");
			male_ = new Sys.Vam.StringChooserParameter(p, "ClockwiseSilver.BJ", "Atom");
			sfxVolume_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "SFX Volume");
			moanVolume_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Moan Volume");
			volumeScaling_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Volume Scaling");
			headX_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Head Side/Side");
			headY_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Head Up/Down");
			headZ_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Head Fwd/Bkwd");

			overallSpeed_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Overall Speed");
			speedMin_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Speed Min");
			speedMax_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.BJ", "Speed Max");
			topOnlyChance_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Top Only Chance");
			mouthOpenMax_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Mouth Open Max");

			active_.Value = false;
			sfxVolume_.Value = 0;
			moanVolume_.Value = 0;
			volumeScaling_.Value = 0;
			headX_.Value = 0;
			headY_.Value = 0;
			headZ_.Value = 0;
			topOnlyChance_.Value = 0.1f;
		}

		public bool Active
		{
			get { return running_.Value; }
		}

		public Person Target
		{
			get
			{
				if (Active)
					return target_;

				return null;
			}
		}

		public bool Start(Person target)
		{
			if (target.Atom.IsMale)
			{
				male_.Value = target.ID;
			}
			else if (target.Body.HasPenis)
			{
				var s = target.Body.Get(BP.Penis).Sys as Sys.Vam.StraponBodyPart;
				male_.Value = s.Dildo.ID;
			}
			else
			{
				log_.Error("can't start, target is not male and has no dildo");
				return false;
			}

			enabled_.Value = true;
			active_.Value = true;
			target_ = target;

			return true;
		}

		public void Stop()
		{
			active_.Value = false;
			target_ = null;
		}

		public void Update(float s)
		{
			wasActive_ = Active;
			if (!wasActive_)
				return;

			// speed
			{
				var minRange = speedMin_.Maximum - speedMin_.DefaultValue;
				var maxRange = speedMax_.Maximum - speedMax_.DefaultValue;
				var range = overallSpeed_.Maximum - overallSpeed_.DefaultValue;
				var p = person_.Mood.MovementEnergy;

				speedMin_.Value = speedMin_.DefaultValue + minRange * p;
				speedMax_.Value = speedMax_.DefaultValue + maxRange * p;
				overallSpeed_.Value = overallSpeed_.DefaultValue + range * p;
			}
		}

		public void OnPluginState(bool b)
		{
		}

		public override string ToString()
		{
			return
				$"ClockwiseBJ: " +
				$"running={running_} " +
				$"active={active_}";
		}
	}
}
