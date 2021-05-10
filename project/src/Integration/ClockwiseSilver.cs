using System;

namespace Cue
{
	struct Range
	{
		public float first, second;

		public Range(float first, float second)
		{
			this.first = first;
			this.second = second;
		}
	}

	class RandomRange
	{
		private Range valuesRange_;
		private Range changeIntervalRange_;
		private Range interpolateTimeRange_;

		private float nextElapsed_ = 0;
		private float nextInterval_ = 0;

		private float nextValue_ = 0;
		private float currentValue_ = 0;
		private float valueElapsed_ = 0;
		private float valueTime_ = 0;
		private float lastValue_ = 0;

		private IEasing easing_ = new SinusoidalEasing();

		public RandomRange(Range values, Range changeInterval, Range interpolateTime)
		{
			valuesRange_ = values;
			changeIntervalRange_ = changeInterval;
			interpolateTimeRange_ = interpolateTime;
		}

		public float Value
		{
			get { return currentValue_; }
		}

		public void Reset()
		{
			nextElapsed_ = 0;
			nextInterval_ = NextInterval();

			nextValue_ = NextValue();
			currentValue_ = 0;
			valueElapsed_ = 0;
			valueTime_ = ValueTime();
			lastValue_ = 0;
		}

		public bool Update(float s)
		{
			nextElapsed_ += s;

			if (nextElapsed_ >= nextInterval_)
			{
				lastValue_ = currentValue_;
				nextValue_ = NextValue();
				nextElapsed_ = 0;
				valueElapsed_ = 0;
				nextInterval_ = NextInterval();
				valueTime_ = ValueTime();
			}
			else if (valueElapsed_ < valueTime_)
			{
				valueElapsed_ = U.Clamp(valueElapsed_ + s, 0, valueTime_);
				currentValue_ = Interpolate(lastValue_, nextValue_, valueElapsed_ / valueTime_);

				return true;
			}

			return false;
		}

		private float NextValue()
		{
			return U.RandomFloat(valuesRange_.first, valuesRange_.second);
		}

		private float NextInterval()
		{
			return U.RandomFloat(changeIntervalRange_.first, changeIntervalRange_.second);
		}

		private float ValueTime()
		{
			return U.RandomFloat(interpolateTimeRange_.first, interpolateTimeRange_.second);
		}

		private float Interpolate(float start, float end, float f)
		{
			return start + (end - start) * easing_.Magnitude(f);
		}
	}


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
		private W.VamFloatParameter lipDepth_ = null;
		private bool wasKissing_ = false;
		private bool random_ = false;
		private float elapsed_ = 0;
		private float cooldownRemaining_ = 0;

		private float startAngleX_ = 0;
		private RandomRange randomHeadAngleX_ = new RandomRange(
			new Range(-10, 10), new Range(0, 5), new Range(1, 3));

		private float startAngleY_ = 0;
		private RandomRange randomHeadAngleY_ = new RandomRange(
			new Range(-10, 10), new Range(0, 5), new Range(1, 3));

		private float startAngleZ_ = 0;
		private RandomRange randomHeadAngleZ_ = new RandomRange(
			new Range(-10, 10), new Range(0, 5), new Range(1, 3));

		private float startLipDepth_ = 0;
		private RandomRange randomLipDepth_ = new RandomRange(
			new Range(0, 0.02f), new Range(0, 5), new Range(1, 3));

		public ClockwiseSilverKiss(Person p)
		{
			log_ = new Logger(Logger.Integration, p, "ClockwiseKiss");

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

			lipDepth_ = new W.VamFloatParameter(
				p, "ClockwiseSilver.Kiss", "Lip Depth");

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

			if (k && random_ && active_.Value)
			{
				bool changed = false;

				changed = changed || randomHeadAngleX_.Update(s);
				changed = changed || randomHeadAngleY_.Update(s);
				changed = changed || randomHeadAngleZ_.Update(s);
				changed = changed || randomLipDepth_.Update(s);

				if (changed)
				{
					headAngleX_.Value = startAngleX_ + randomHeadAngleX_.Value;
					headAngleY_.Value = startAngleY_ + randomHeadAngleY_.Value;
					headAngleZ_.Value = startAngleZ_ + randomHeadAngleZ_.Value;
					lipDepth_.Value = startLipDepth_ + randomLipDepth_.Value;

					var t = Target?.Kisser as ClockwiseSilverKiss;
					if (t != null)
					{
						t.headAngleX_.Value = t.startAngleX_ + randomHeadAngleX_.Value;
						t.headAngleY_.Value = t.startAngleY_ + randomHeadAngleY_.Value;
						t.headAngleZ_.Value = t.startAngleZ_ + randomHeadAngleZ_.Value;
					}
				}
			}
		}

		public void OnPluginState(bool b)
		{
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

			bool leader = true;
			if (person_ == Cue.Instance.Player)
				leader = false;

			DoKiss(target, leader);

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
					tcw.DoKiss(person_, !leader);
				}
			}
		}

		private void DoKiss(Person target, bool leader)
		{
			enabled_.Value = true;

			// force reset
			atom_.Value = "";
			target_.Value = "";
			atom_.Value = target.ID;
			target_.Value = "LipTrigger";

			if (target == Cue.Instance.Player)
			{
				headAngleX_.Value = 0;
				headAngleY_.Value = 0;
				headAngleZ_.Value = 0;
				lipDepth_.Value = 0;

				random_ = false;
			}
			else
			{
				startAngleX_ = -10;
				startAngleY_ = 0;

				if (leader)
					startAngleZ_ = -30;
				else
					startAngleZ_ = -20;

				startLipDepth_ = 0;

				headAngleX_.Value = startAngleX_;
				headAngleY_.Value = startAngleY_;
				headAngleZ_.Value = startAngleZ_;
				lipDepth_.Value = startLipDepth_;

				randomHeadAngleX_.Reset();
				randomHeadAngleY_.Reset();
				randomHeadAngleZ_.Reset();
				randomLipDepth_.Reset();

				random_ = leader;
			}

			trackPos_.Value = leader;
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
		private Person person_;
		private Logger log_;
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
			person_ = p;
			log_ = new Logger(Logger.Integration, p, "ClockwiseHJ");
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
		private Person person_;
		private Logger log_;
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
			person_ = p;
			log_ = new Logger(Logger.Integration, p, "ClockwiseBJ");
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
