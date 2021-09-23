using System;

namespace Cue
{
	class ClockwiseSilverKiss : IKisser
	{
		public const float Cooldown = 10;

		private Logger log_;
		private Person person_;
		private Sys.Vam.BoolParameter enabled_ = null;
		private Sys.Vam.BoolParameter active_ = null;
		private Sys.Vam.BoolParameterRO running_ = null;
		private Sys.Vam.StringChooserParameter atom_ = null;
		private Sys.Vam.StringChooserParameter target_ = null;
		private Sys.Vam.BoolParameter trackPos_ = null;
		private Sys.Vam.BoolParameter trackRot_ = null;
		private Sys.Vam.FloatParameter headAngleX_ = null;
		private Sys.Vam.FloatParameter headAngleY_ = null;
		private Sys.Vam.FloatParameter headAngleZ_ = null;
		private Sys.Vam.FloatParameter lipDepth_ = null;
		private Sys.Vam.FloatParameter morphDuration_ = null;
		private Sys.Vam.FloatParameter morphSpeed_ = null;
		private Sys.Vam.FloatParameter tongueLength_ = null;
		private Sys.Vam.BoolParameter closeEyes_ = null;
		private bool wasKissing_ = false;
		private bool randomMovements_ = false;
		private bool randomSpeeds_ = false;
		private float elapsed_ = 0;
		private float cooldownRemaining_ = 0;


		private const float MinDistance = 0.001f;
		private const float MaxDistance = 0.008f;
		private const float MaxLipDepth = 0.14f;

		private const float HeadAngleXMin = -10;
		private const float HeadAngleXMax = 10;
		private const float HeadAngleYMin = -10;
		private const float HeadAngleYMax = 10;
		private const float HeadAngleZMin = -10;
		private const float HeadAngleZMax = 10;

		private const float StartHeadAngleXWithPlayer = -45;
		private const float StartHeadAngleYWithPlayer = 0;
		private const float StartHeadAngleZWithPlayer = 0;
		private const float StartHeadAngleXLeader = -10;
		private const float StartHeadAngleYLeader = 0;
		private const float StartHeadAngleZLeader = -40;
		private const float StartHeadAngleX = -10;
		private const float StartHeadAngleY = 0;
		private const float StartHeadAngleZ = -40;
		private const float StartLipDepthLeader = 0.02f;
		private const float StartLipDepth = 0;

		// this was trying to fix a probably non-existent problem, where lip
		// triggers are not positioned properly after moving colliders with
		// AB's collider editor until a reload
		//
		// trying to change the lip depth dynamically just makes everything move
		// weird
		private bool EnableDepthCheck = false;


		// random angle changes can be nice, but cw doesn't always handle them
		// well, heads have trouble syncing
		private bool EnableRandomMovements = false;


		private float startAngleX_ = 0;
		private InterpolatedRandomRange randomHeadAngleX_ =
			new InterpolatedRandomRange(
				new Pair<float, float>(HeadAngleXMin, HeadAngleXMax),
				new Pair<float, float>(0, 5),
				new Pair<float, float>(1, 3));

		private float startAngleY_ = 0;
		private InterpolatedRandomRange randomHeadAngleY_ =
			new InterpolatedRandomRange(
				new Pair<float, float>(HeadAngleYMin, HeadAngleYMax),
				new Pair<float, float>(0, 5),
				new Pair<float, float>(1, 3));

		private float startAngleZ_ = 0;
		private InterpolatedRandomRange randomHeadAngleZ_ =
			new InterpolatedRandomRange(
				new Pair<float, float>(HeadAngleZMin, HeadAngleZMax),
				new Pair<float, float>(0, 5),
				new Pair<float, float>(1, 3));

		private float startLipDepth_ = 0;
		private InterpolatedRandomRange randomLipDepth_ =
			new InterpolatedRandomRange(
				new Pair<float, float>(0, 0.02f),
				new Pair<float, float>(0, 5),
				new Pair<float, float>(1, 3));

		public ClockwiseSilverKiss(Person p)
		{
			log_ = new Logger(Logger.Integration, p, "ClockwiseKiss");

			person_ = p;

			enabled_ = new Sys.Vam.BoolParameter(
				p, "ClockwiseSilver.Kiss", "enabled");

			active_ = new Sys.Vam.BoolParameter(
				p, "ClockwiseSilver.Kiss", "isActive");

			running_ = new Sys.Vam.BoolParameterRO(
				p, "ClockwiseSilver.Kiss", "Is Kissing");

			atom_ = new Sys.Vam.StringChooserParameter(
				p, "ClockwiseSilver.Kiss", "atom");

			target_ = new Sys.Vam.StringChooserParameter(
				p, "ClockwiseSilver.Kiss", "kissTargetJSON");

			trackPos_ = new Sys.Vam.BoolParameter(
				p, "ClockwiseSilver.Kiss", "trackPosition");

			trackRot_ = new Sys.Vam.BoolParameter(
				p, "ClockwiseSilver.Kiss", "trackRotation");

			headAngleX_ = new Sys.Vam.FloatParameter(
				p, "ClockwiseSilver.Kiss", "Head Angle X");

			headAngleY_ = new Sys.Vam.FloatParameter(
				p, "ClockwiseSilver.Kiss", "Head Angle Y");

			headAngleZ_ = new Sys.Vam.FloatParameter(
				p, "ClockwiseSilver.Kiss", "Head Angle Z");

			lipDepth_ = new Sys.Vam.FloatParameter(
				p, "ClockwiseSilver.Kiss", "Lip Depth");

			morphDuration_ = new Sys.Vam.FloatParameter(
				p, "ClockwiseSilver.Kiss", "Morph Duration");

			morphSpeed_ = new Sys.Vam.FloatParameter(
				p, "ClockwiseSilver.Kiss", "Morph Speed");

			tongueLength_ = new Sys.Vam.FloatParameter(
				p, "ClockwiseSilver.Kiss", "Tongue Length");

			closeEyes_ = new Sys.Vam.BoolParameter(
				p, "ClockwiseSilver.Kiss", "closeEyes");

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

		public bool IsBusy(int bodyPart)
		{
			switch (bodyPart)
			{
				case BP.Head:
				case BP.Lips:
				case BP.Mouth:
					return wasKissing_;

				default:
					return false;
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

			if (k && randomMovements_ && active_.Value && EnableRandomMovements)
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

					if (EnableDepthCheck)
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

			if (k && randomSpeeds_ && active_.Value)
			{
				var ps = person_.Personality;

				// don't go too low
				var range =
					(morphDuration_.DefaultValue - morphDuration_.Minimum) * 0.6f;

				morphDuration_.Value =
					morphDuration_.DefaultValue -
					range * person_.Mood.MovementEnergy;

				range = morphSpeed_.Maximum - morphSpeed_.DefaultValue;
				morphSpeed_.Value =
					morphSpeed_.DefaultValue +
					range * person_.Mood.MovementEnergy;
			}

			if (k && active_.Value && EnableDepthCheck)
			{
				var t = Target;
				if (t != null)
				{
					var l1 = person_.Body.Get(BP.Lips);
					var l2 = t.Body.Get(BP.Lips);
					var d = Vector3.Distance(l1.Position, l2.Position);

					if (trackPos_.Value)
					{
						if (d > MaxDistance)
							startLipDepth_ += 0.03f * s;
						else if (d < MinDistance)
							startLipDepth_ -= 0.03f * s;

						startLipDepth_ = U.Clamp(startLipDepth_, 0, MaxLipDepth);
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
				headAngleX_.Value = StartHeadAngleXWithPlayer;
				headAngleY_.Value = StartHeadAngleYWithPlayer;
				headAngleZ_.Value = StartHeadAngleZWithPlayer;
				lipDepth_.Value = 0;

				closeEyes_.Value = !person_.Personality.GetBool(PSE.AvoidGazePlayer);

				randomMovements_ = false;
				randomSpeeds_ = true;
			}
			else
			{
				if (leader)
				{
					startLipDepth_ = StartLipDepthLeader;
					startAngleX_ = StartHeadAngleXLeader;
					startAngleY_ = StartHeadAngleYLeader;
					startAngleZ_ = StartHeadAngleZLeader;
				}
				else
				{
					startLipDepth_ = StartLipDepth;
					startAngleX_ = StartHeadAngleX;
					startAngleY_ = StartHeadAngleY;
					startAngleZ_ = StartHeadAngleZ;
				}


				headAngleX_.Value = startAngleX_;
				headAngleY_.Value = startAngleY_;
				headAngleZ_.Value = startAngleZ_;
				lipDepth_.Value = startLipDepth_;

				closeEyes_.Value = true;

				randomHeadAngleX_.Reset();
				randomHeadAngleY_.Reset();
				randomHeadAngleZ_.Reset();
				randomLipDepth_.Reset();

				randomMovements_ = leader;
				randomSpeeds_ = true;
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
					log_.Info($"now kissing {target}");
			}
			else
			{
				log_.Info($"kiss stopped");
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
		private Sys.Vam.BoolParameter enabled_ = null;
		private Sys.Vam.BoolParameter active_ = null;
		private Sys.Vam.BoolParameterRO running_ = null;
		private Sys.Vam.StringChooserParameter male_ = null;
		private Sys.Vam.StringChooserParameter hand_ = null;
		private Sys.Vam.FloatParameter handX_ = null;
		private Sys.Vam.FloatParameter handY_ = null;
		private Sys.Vam.FloatParameter handZ_ = null;
		private Sys.Vam.FloatParameter closeMax_ = null;
		private Sys.Vam.FloatParameter speed_ = null;
		private Sys.Vam.FloatParameter zStrokeMax_ = null;
		private Sys.Vam.FloatParameter hand2Side_ = null;
		private Sys.Vam.FloatParameter hand2UpDown_ = null;
		private Sys.Vam.FloatParameter topOnlyChance_ = null;

		private float elapsed_ = 0;
		private bool closedHand_ = false;
		private bool wasActive_ = false;
		private bool leftUsed_ = false;
		private bool rightUsed_ = false;

		public ClockwiseSilverHandjob(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Integration, p, "ClockwiseHJ");
			enabled_ = new Sys.Vam.BoolParameter(p, "ClockwiseSilver.HJ", "enabled");
			active_ = new Sys.Vam.BoolParameter(p, "ClockwiseSilver.HJ", "isActive");
			running_ = new Sys.Vam.BoolParameterRO(p, "ClockwiseSilver.HJ", "isHJRoutine");
			male_ = new Sys.Vam.StringChooserParameter(p, "ClockwiseSilver.HJ", "Atom");
			hand_ = new Sys.Vam.StringChooserParameter(p, "ClockwiseSilver.HJ", "handedness");
			handX_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Hand Side/Side");
			handY_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Hand Fwd/Bkwd");
			handZ_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Hand Shift Up/Down");
			closeMax_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Hand Close Max");
			speed_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Overall Speed");
			zStrokeMax_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Z Stroke Max");
			hand2Side_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Hand2 Side/Side");
			hand2UpDown_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Hand2 Shift Up/Down");
			topOnlyChance_ = new Sys.Vam.FloatParameter(p, "ClockwiseSilver.HJ", "Top Only Chance");

			active_.Value = false;
		}

		public bool Active
		{
			get { return running_.Value; }
		}

		public bool LeftUsed
		{
			get { return (wasActive_ && leftUsed_); }
		}

		public bool RightUsed
		{
			get { return (wasActive_ && rightUsed_); }
		}

		public Person[] Targets
		{
			get
			{
				if (wasActive_)
				{
					var id = male_.Value;
					if (id != "")
					{
						var p = Cue.Instance.FindPerson(id);
						if (p != null)
							return new Person[] { p };
					}
				}

				return null;
			}
		}

		public bool IsBusy(int bodyPart)
		{
			switch (bodyPart)
			{
				case BP.LeftArm:
				case BP.LeftForearm:
				case BP.LeftHand:
					return wasActive_ && LeftUsed;

				case BP.RightArm:
				case BP.RightForearm:
				case BP.RightHand:
					return wasActive_ && RightUsed;

				default:
					return false;
			}
		}

		public bool StartBoth(Person p)
		{
			StartCommon(p, "Both");

			leftUsed_ = true;
			rightUsed_ = true;

			zStrokeMax_.Value = 10;
			hand2Side_.Value = -0.05f;
			hand2UpDown_.Value = 0.15f;
			topOnlyChance_.Value = 0;

			return true;
		}

		public bool StartLeft(Person p)
		{
			StartCommon(p, "Left");
			StartSingleHandCommon(p);

			leftUsed_ = true;
			rightUsed_ = false;

			return true;
		}

		public bool StartRight(Person p)
		{
			StartCommon(p, "Right");
			StartSingleHandCommon(p);

			leftUsed_ = false;
			rightUsed_ = true;

			return true;
		}

		private void StartCommon(Person p, string hand)
		{
			hand_.Value = hand;
			enabled_.Value = true;
			male_.Value = p.ID;

			handX_.Value = 0;
			handY_.Value = 0;
			handZ_.Value = 0;
			closeMax_.Value = 0.5f;

			elapsed_ = 0;
			closedHand_ = false;

			active_.Value = true;
		}

		private void StartSingleHandCommon(Person p)
		{
			zStrokeMax_.Value = zStrokeMax_.DefaultValue;
			topOnlyChance_.Value = 0.1f;
		}

		public void Stop()
		{
			active_.Value = false;
		}

		public void StopLeft()
		{
			// todo
			Stop();
		}

		public void StopRight()
		{
			// todo
			Stop();
		}

		public void Update(float s)
		{
			wasActive_ = Active;
			if (!wasActive_)
				return;

			if (!closedHand_)
			{
				elapsed_ += s;
				if (elapsed_ >= 2)
				{
					closedHand_ = true;
					//closeMax_.Value = 0.75f;
				}
			}

			// speed
			{
				// not too fast
				var range = (speed_.Maximum - speed_.DefaultValue) * 0.9f;

				var v = speed_.DefaultValue + range * person_.Mood.MovementEnergy;
				speed_.Value = v;
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

		private const float MaxDistanceToStart = 0.4f;

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

		public Person[] Targets
		{
			get
			{
				if (Active)
				{
					var id = male_.Value;
					if (id != "")
					{
						var p = Cue.Instance.FindPerson(id);
						if (p != null)
							return new Person[] { p };
					}
				}

				return null;
			}
		}

		public bool IsBusy(int bodyPart)
		{
			switch (bodyPart)
			{
				case BP.Head:
				case BP.Lips:
				case BP.Mouth:
				case BP.Eyes:   // cw also handles eyes
					return wasActive_;

				default:
					return false;
			}
		}

		private Person FindTarget()
		{
			var head = person_.Body.Get(BP.Head);

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_ || !p.Body.HasPenis)
					continue;

				var g = p.Body.Get(BP.Penis);
				var d = Vector3.Distance(head.Position, g.Position);

				Cue.LogInfo($"{person_.ID} {p.ID} {d}");

				if (d < MaxDistanceToStart)
					return p;
			}

			return null;
		}

		public bool Start()
		{
			var p = FindTarget();
			if (p == null)
				return false;

			enabled_.Value = true;
			male_.Value = p.ID;
			active_.Value = true;

			return true;
		}

		public void Stop()
		{
			active_.Value = false;
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
