using System;

namespace Cue
{
	class ClockwiseSilverKiss : IKisser
	{
		public const float Cooldown = 10;

		private Person person_;
		private W.VamBoolParameter enabled_ = null;
		private W.VamBoolParameter kissingRunning_ = null;
		private W.VamBoolParameter activate_ = null;
		private W.VamStringChooserParameter atom_ = null;
		private W.VamStringChooserParameter target_ = null;
		private W.VamBoolParameter trackPos_ = null;
		private W.VamBoolParameter trackRot_ = null;
		private W.VamFloatParameter headAngleX_ = null;
		private W.VamFloatParameter headAngleY_ = null;
		private W.VamFloatParameter headAngleZ_ = null;
		private bool wasKissing_ = false;
		private float elapsed_ = 0;
		private float cooldownRemaining_ = 0;

		public ClockwiseSilverKiss(Person p)
		{
			person_ = p;

			enabled_ = new W.VamBoolParameter(
				p, "ClockwiseSilver.Kiss", "enabled");

			kissingRunning_ = new W.VamBoolParameter(
				p, "ClockwiseSilver.Kiss", "Is Kissing");

			activate_ = new W.VamBoolParameter(
				p, "ClockwiseSilver.Kiss", "isActive");

			atom_ = new W.VamStringChooserParameter(
				p, "ClockwiseSilver.Kiss", "atom");

			target_ = new W.VamStringChooserParameter(
				p, "ClockwiseSilver.Kiss", "kissTargetJSON");

			trackPos_ = new W.VamBoolParameter(
				p, "ClockwiseSilver.Kiss", "trackPosition");

			trackRot_ = new W.VamBoolParameter(
				p, "ClockwiseSilver.Kiss", "trackRotation");

			headAngleX_ = new W.VamFloatParameter(
				p, "ClockwiseSilver.Kiss", "Head Angle X");

			headAngleY_ = new W.VamFloatParameter(
				p, "ClockwiseSilver.Kiss", "Head Angle Y");

			headAngleZ_ = new W.VamFloatParameter(
				p, "ClockwiseSilver.Kiss", "Head Angle Z");

			activate_.SetValue(false);
		}

		public bool Active
		{
			get { return wasKissing_; }
		}

		public float Elapsed
		{
			get { return elapsed_; }
		}

		public bool OnCooldown
		{
			get { return cooldownRemaining_ > 0; }
		}

		public Person Target
		{
			get
			{
				if (!Active)
					return null;

				var tid = atom_.GetValue();
				if (tid == "")
					return null;

				return Cue.Instance.FindPerson(tid);
			}
		}

		public void Update(float s)
		{
			cooldownRemaining_ = Math.Max(cooldownRemaining_ - s, 0);

			var k = kissingRunning_.GetValue();
			if (wasKissing_ != k)
				SetActive(k);

			if (k)
				elapsed_ += s;
		}

		public void Stop()
		{
			StopSelf();

			var target = GetTarget()?.Kisser;
			if (target != null)
				target.StopSelf();
		}

		public void StopSelf()
		{
			if (activate_.GetValue())
			{
				Cue.LogError($"Clockwise {person_}: stopping");
				activate_.SetValue(false);
				cooldownRemaining_ = Cooldown;
			}
		}

		public void Start(Person target)
		{
			if (Active)
			{
				Cue.LogError($"Clockwise {person_}: can't start, already active");
				return;
			}

			DoKiss(target, true);
		}

		public void StartReciprocal(Person target)
		{
			var t = target.Kisser as ClockwiseSilverKiss;
			if (t == null)
			{
				Cue.LogError($"Clockwise {person_}: can't kiss, {target} is not using clockwise");
				return;
			}

			if (Active)
			{
				Cue.LogError($"Clockwise {person_}: can't start reciprocal, already active");
				return;
			}

			if (t.Active)
			{
				Cue.LogError($"Clockwise {t.person_}: can't start reciprocal, already active");
				return;
			}

			Cue.LogInfo($"Clockwise {person_}: starting reciprocal with {target}");

			bool thisPos = true;
			bool targetPos = false;

			if (person_ == Cue.Instance.Player)
			{
				thisPos = false;
				targetPos = true;
			}

			DoKiss(target, thisPos);
			t.DoKiss(person_, targetPos);
		}

		public void OnPluginState(bool b)
		{
		}

		private void DoKiss(Person target, bool pos)
		{
			enabled_.SetValue(true);

			// force reset
			atom_.SetValue("");
			target_.SetValue("");
			atom_.SetValue(target.ID);
			target_.SetValue("LipTrigger");

			headAngleX_.SetValue(-10);
			headAngleZ_.SetValue(-40);

			trackPos_.SetValue(pos);
			trackRot_.SetValue(true);

			activate_.SetValue(true);

			elapsed_ = 0;
		}

		private void SetActive(bool b)
		{
			if (b)
			{
				Cue.LogInfo($"Clockwise {person_}: kiss got activated");

				var target = GetTarget();
				if (target != null)
				{
					Cue.LogInfo($"Clockwise {person_}: now kissing {target}");
					person_.Gaze.LookAt(target, false);
				}
			}
			else
			{
				Cue.LogInfo($"Clockwise {person_}: kiss stopped");
				person_.Gaze.LookAtDefault();
			}

			wasKissing_ = b;
		}

		private Person GetTarget()
		{
			var atom = atom_.GetValue();
			if (atom != "")
				return Cue.Instance.FindPerson(atom);

			return null;
		}

		public override string ToString()
		{
			return
				$"Clockwise: " +
				$"running={kissingRunning_.GetValue()} " +
				$"active={activate_.GetValue()}";
		}
	}


	class ClockwiseSilverHandjob : IHandjob
	{
		private Person person_;
		private W.VamBoolParameter enabled_ = null;
		private W.VamBoolParameter active_ = null;
		private W.VamStringChooserParameter male_ = null;
		private W.VamStringChooserParameter hand_ = null;

		public ClockwiseSilverHandjob(Person p)
		{
			person_ = p;
			enabled_ = new W.VamBoolParameter(p, "ClockwiseSilver.HJ", "enabled");
			active_ = new W.VamBoolParameter(p, "ClockwiseSilver.HJ", "isActive");
			male_ = new W.VamStringChooserParameter(p, "ClockwiseSilver.HJ", "Atom");
			hand_ = new W.VamStringChooserParameter(p, "ClockwiseSilver.HJ", "handedness");

			active_.SetValue(false);
		}

		public bool Active
		{
			get { return active_.GetValue(); }
		}

		public void Start(Person target)
		{
			enabled_.SetValue(true);

			if (target != null)
				male_.SetValue(target.ID);

			// todo
			hand_.SetValue("Right");

			active_.SetValue(true);
		}

		public void Stop()
		{
			active_.SetValue(false);
		}

		public void Update(float s)
		{
		}

		public void OnPluginState(bool b)
		{
		}

		public override string ToString()
		{
			string s = $"Clockwise: active={active_.GetValue()} target=";

			//if (target_ == null)
			//	s += "(none)";
			//else
			//	s += target_.ID;

			return s;
		}
	}


	class ClockwiseSilverBlowjob : IBlowjob
	{
		private Person person_;
		private W.VamBoolParameter enabled_ = null;
		private W.VamBoolParameter active_ = null;
		private W.VamStringChooserParameter male_ = null;
		private W.VamFloatParameter sfxVolume_ = null;
		private W.VamFloatParameter moanVolume_ = null;
		private W.VamFloatParameter volumeScaling_ = null;

		public ClockwiseSilverBlowjob(Person p)
		{
			person_ = p;

			enabled_ = new W.VamBoolParameter(p, "ClockwiseSilver.BJ", "enabled");
			active_ = new W.VamBoolParameter(p, "ClockwiseSilver.BJ", "isActive");
			male_ = new W.VamStringChooserParameter(p, "ClockwiseSilver.BJ", "Atom");
			sfxVolume_ = new W.VamFloatParameter(p, "ClockwiseSilver.BJ", "SFX Volume");
			moanVolume_ = new W.VamFloatParameter(p, "ClockwiseSilver.BJ", "Moan Volume");
			volumeScaling_ = new W.VamFloatParameter(p, "ClockwiseSilver.BJ", "Volume Scaling");

			active_.SetValue(false);
			sfxVolume_.SetValue(0);
			moanVolume_.SetValue(0);
			volumeScaling_.SetValue(0);
		}

		public bool Active
		{
			get { return active_.GetValue(); }
		}

		public void Start(Person target)
		{
			enabled_.SetValue(true);

			if (target != null)
				male_.SetValue(target.ID);

			active_.SetValue(true);
		}

		public void Stop()
		{
			active_.SetValue(false);
		}

		public void Update(float s)
		{
		}

		public void OnPluginState(bool b)
		{
		}

		public override string ToString()
		{
			string s = $"Clockwise: active={active_.GetValue()} target=";

			//if (target_ == null)
			//	s += "(none)";
			//else
			//	s += target_.ID;

			return s;
		}
	}
}
