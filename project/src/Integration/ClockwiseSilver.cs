using System;

namespace Cue
{
	class ClockwiseSilverKiss : IKisser
	{
		public const float Cooldown = 10;

		private Logger log_;
		private Person person_;
		private W.VamBoolParameter enabled_ = null;
		private W.VamBoolParameter active_ = null;
		private W.VamBoolParameterRO running_ = null;
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
			log_ = new Logger(() => $"cwkiss {person_}");

			person_ = p;

			enabled_ = new W.VamBoolParameter(
				p, "ClockwiseSilver.Kiss", "enabled");

			active_ = new W.VamBoolParameter(
				p, "ClockwiseSilver.Kiss", "isActive");

			running_ = new W.VamBoolParameterRO(
				p, "ClockwiseSilver.Kiss", "Is Kissing");

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

			active_.Value = false;
		}

		public bool Active
		{
			get { return running_.Value; }
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

				var tid = atom_.Value;
				if (tid == "")
					return null;

				return Cue.Instance.FindPerson(tid);
			}
		}

		public void Update(float s)
		{
			cooldownRemaining_ = Math.Max(cooldownRemaining_ - s, 0);

			var k = running_.Value;
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
			if (active_.Value)
			{
				log_.Error("stopping");
				active_.Value = false;
				cooldownRemaining_ = Cooldown;
			}
		}

		public void Start(Person target)
		{
			if (Active)
			{
				log_.Error("can't start, already active");
				return;
			}

			DoKiss(target, true);
		}

		public void StartReciprocal(Person target)
		{
			log_.Info($"starting reciprocal with {target}");

			if (Active)
			{
				log_.Error($"can't start reciprocal, already active");
				return;
			}

			var t = target.Kisser;
			if (t != null && t.Active)
			{
				log_.Error($"can't start reciprocal, already active on {target}");
				return;
			}

			bool thisPos = true;
			bool targetPos = false;

			if (person_ == Cue.Instance.Player)
			{
				thisPos = false;
				targetPos = true;
			}

			DoKiss(target, thisPos);

			if (target.Kisser == null)
			{
				log_.Info($"{target} doesn't know how to kiss, won't be reciprocal");
			}
			else
			{
				var tcw = target.Kisser as ClockwiseSilverKiss;
				if (tcw == null)
				{
					log_.Warning($"{target} is not using CW");
					target.Kisser.Start(person_);
				}
				else
				{
					tcw.DoKiss(person_, targetPos);
				}
			}
		}

		public void OnPluginState(bool b)
		{
		}

		private void DoKiss(Person target, bool pos)
		{
			enabled_.Value = true;

			// force reset
			atom_.Value = "";
			target_.Value = "";
			atom_.Value = target.ID;
			target_.Value = "LipTrigger";

			headAngleX_.Value = -10;
			headAngleZ_.Value = -40;

			trackPos_.Value = pos;
			trackRot_.Value = true;

			active_.Value = true;

			elapsed_ = 0;
		}

		private void SetActive(bool b)
		{
			if (b)
			{
				log_.Info("kiss got activated");

				var target = GetTarget();
				if (target != null)
				{
					log_.Info($"now kissing {target}");
					person_.Gaze.LookAt(target, false);
				}
			}
			else
			{
				log_.Info($"kiss stopped");
				person_.Gaze.LookAtDefault();
			}

			wasKissing_ = b;
		}

		private Person GetTarget()
		{
			var atom = atom_.Value;
			if (atom != "")
				return Cue.Instance.FindPerson(atom);

			return null;
		}

		public override string ToString()
		{
			return
				$"ClockwiseKiss: " +
				$"running={running_} " +
				$"active={active_}";
		}
	}


	class ClockwiseSilverHandjob : IHandjob
	{
		private Logger log_;
		private Person person_;
		private W.VamBoolParameter enabled_ = null;
		private W.VamBoolParameter active_ = null;
		private W.VamBoolParameterRO running_ = null;
		private W.VamStringChooserParameter male_ = null;
		private W.VamStringChooserParameter hand_ = null;
		private W.VamFloatParameter handX_ = null;
		private W.VamFloatParameter handY_ = null;
		private W.VamFloatParameter handZ_ = null;
		private W.VamFloatParameter closeMax_ = null;
		private float elapsed_ = 0;
		private bool closedHand_ = false;

		public ClockwiseSilverHandjob(Person p)
		{
			log_ = new Logger(() => $"cwhj {person_}");
			person_ = p;
			enabled_ = new W.VamBoolParameter(p, "ClockwiseSilver.HJ", "enabled");
			active_ = new W.VamBoolParameter(p, "ClockwiseSilver.HJ", "isActive");
			running_ = new W.VamBoolParameterRO(p, "ClockwiseSilver.HJ", "isHJRoutine");
			male_ = new W.VamStringChooserParameter(p, "ClockwiseSilver.HJ", "Atom");
			hand_ = new W.VamStringChooserParameter(p, "ClockwiseSilver.HJ", "handedness");
			handX_ = new W.VamFloatParameter(p, "ClockwiseSilver.HJ", "Hand Side/Side");
			handY_ = new W.VamFloatParameter(p, "ClockwiseSilver.HJ", "Hand Fwd/Bkwd");
			handZ_ = new W.VamFloatParameter(p, "ClockwiseSilver.HJ", "Hand Shift Up/Down");
			closeMax_ = new W.VamFloatParameter(p, "ClockwiseSilver.HJ", "Hand Close Max");

			active_.Value = false;
		}

		public bool Active
		{
			get { return running_.Value; }
		}

		public void Start(Person target)
		{
			enabled_.Value = true;

			if (target != null)
				male_.Value = target.ID;

			// todo
			hand_.Value = "Right";
			handX_.Value = 0.03f;
			handY_.Value = -0.08f;
			handZ_.Value = 0.00f;
			closeMax_.Value = 0.5f;

			elapsed_ = 0;
			closedHand_ = false;

			active_.Value = true;
		}

		public void Stop()
		{
			active_.Value = false;
		}

		public void Update(float s)
		{
			if (!closedHand_)
			{
				elapsed_ += s;
				if (elapsed_ >= 2)
				{
					closedHand_ = true;
					closeMax_.Value = 0.75f;
				}
			}
		}

		public void OnPluginState(bool b)
		{
		}

		public override string ToString()
		{
			return
				$"ClockwiseHJ: " +
				$"running={running_} " +
				$"active={active_}";
		}
	}


	class ClockwiseSilverBlowjob : IBlowjob
	{
		private Logger log_;
		private Person person_;
		private W.VamBoolParameter enabled_ = null;
		private W.VamBoolParameter active_ = null;
		private W.VamBoolParameterRO running_ = null;
		private W.VamStringChooserParameter male_ = null;
		private W.VamFloatParameter sfxVolume_ = null;
		private W.VamFloatParameter moanVolume_ = null;
		private W.VamFloatParameter volumeScaling_ = null;
		private W.VamFloatParameter headX_ = null;
		private W.VamFloatParameter headY_ = null;
		private W.VamFloatParameter headZ_ = null;

		public ClockwiseSilverBlowjob(Person p)
		{
			log_ = new Logger(() => $"cwbj {person_}");
			person_ = p;

			enabled_ = new W.VamBoolParameter(p, "ClockwiseSilver.BJ", "enabled");
			active_ = new W.VamBoolParameter(p, "ClockwiseSilver.BJ", "isActive");
			running_ = new W.VamBoolParameterRO(p, "ClockwiseSilver.BJ", "isBJRoutine");
			male_ = new W.VamStringChooserParameter(p, "ClockwiseSilver.BJ", "Atom");
			sfxVolume_ = new W.VamFloatParameter(p, "ClockwiseSilver.BJ", "SFX Volume");
			moanVolume_ = new W.VamFloatParameter(p, "ClockwiseSilver.BJ", "Moan Volume");
			volumeScaling_ = new W.VamFloatParameter(p, "ClockwiseSilver.BJ", "Volume Scaling");
			headX_ = new W.VamFloatParameter(p, "ClockwiseSilver.BJ", "Head Side/Side");
			headY_ = new W.VamFloatParameter(p, "ClockwiseSilver.BJ", "Head Up/Down");
			headZ_ = new W.VamFloatParameter(p, "ClockwiseSilver.BJ", "Head Fwd/Bkwd");

			active_.Value = false;
			sfxVolume_.Value = 0;
			moanVolume_.Value = 0;
			volumeScaling_.Value = 0;
			headX_.Value = 0.01f;
			headY_.Value = 0.12f;
			headZ_.Value = 0.03f;
		}

		public bool Active
		{
			get { return running_.Value; }
		}

		public void Start(Person target)
		{
			enabled_.Value = true;

			if (target != null)
				male_.Value = target.ID;

			person_.Gaze.LookAt(target, false);

			active_.Value = true;
		}

		public void Stop()
		{
			active_.Value = false;
			person_.Gaze.LookAtDefault();
		}

		public void Update(float s)
		{
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
