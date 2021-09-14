using System;
using System.Collections.Generic;

namespace Cue.Proc
{
	class BuiltinAnimations
	{
		public static List<Animation> Get()
		{
			var list = new List<Animation>();

			list.Add(Stand(PersonState.Walking));
			list.Add(Stand(PersonState.Standing));
			list.Add(Sex());
			list.Add(Smoke());
			list.Add(Suck());
			list.Add(Penetrated());

			return list;
		}

		private static Animation Stand(int from)
		{
			var a = new ProcAnimation("backToNeutral");

			var s = new ElapsedSync(1);

			a.AddTarget(new Controller("headControl", new Vector3(0, 1.6f, 0), new Vector3(0, 0, 0), s.Clone()));
			a.AddTarget(new Controller("chestControl", new Vector3(0, 1.4f, 0), new Vector3(20, 0, 0), s.Clone()));
			a.AddTarget(new Controller("hipControl", new Vector3(0, 1.1f, 0), new Vector3(340, 10, 0), s.Clone()));
			a.AddTarget(new Controller("lHandControl", new Vector3(-0.2f, 0.9f, 0), new Vector3(0, 10, 90), s.Clone()));
			a.AddTarget(new Controller("rHandControl", new Vector3(0.2f, 0.9f, 0), new Vector3(0, 0, 270), s.Clone()));
			a.AddTarget(new Controller("lFootControl", new Vector3(-0.1f, 0, 0), new Vector3(20, 10, 0), s.Clone()));
			a.AddTarget(new Controller("rFootControl", new Vector3(0.1f, 0, -0.1f), new Vector3(20, 10, 0), s.Clone()));

			return new Animation(
				Animations.Transition,
				from, PersonState.Standing,
				PersonState.None, MovementStyles.Any, a);
		}

		private static Animation Sex()
		{
			var a = new SexProcAnimation();

			return new Animation(
				Animations.Sex,
				PersonState.None, PersonState.None,
				PersonState.None, MovementStyles.Any, a);
		}

		private static Animation Smoke()
		{
			var a = new SmokeProcAnimation();

			return new Animation(
				Animations.Smoke,
				PersonState.None, PersonState.None,
				PersonState.None, MovementStyles.Any, a);
		}

		private static Animation Suck()
		{
			var a = new SuckProcAnimation();

			return new Animation(
				Animations.Suck,
				PersonState.None, PersonState.None,
				PersonState.None, MovementStyles.Any, a);
		}

		private static Animation Penetrated()
		{
			var a = new PenetratedAnimation();

			return new Animation(
				Animations.Penetrated,
				PersonState.None, PersonState.None,
				PersonState.None, MovementStyles.Any, a);
		}
	}


	class PenetratedAnimation : BasicProcAnimation
	{
		private const float Time = 20;
		private float elapsed_ = 0;

		public PenetratedAnimation()
			: base("procPenetrated", false)
		{
		}

		public override BasicProcAnimation Clone()
		{
			var a = new PenetratedAnimation();
			a.CopyFrom(this);
			return a;
		}

		public override bool Done
		{
			get { return (elapsed_ >= Time); }
		}

		public override void Start(Person p)
		{
			base.Start(p);
			elapsed_ = 0;

			foreach (var e in (person_.Expression as Proc.Expression).All)
			{
				foreach (var g in e.Groups)
				{
					if (e.Type == Expressions.Pleasure && g.Name == "pleasure")
					{
						g.Force(MorphTarget.ForceToRangePercent, 70, 1);
					}
					else
					{
						g.Force(MorphTarget.ForceToZero, 7, 1);
					}
				}
			}
		}

		public override void Reset()
		{
			base.Reset();
			elapsed_ = 0;
		}

		public override void FixedUpdate(float s)
		{
			elapsed_ += s;

			if (elapsed_ < Time)
			{
				var p = elapsed_ / Time;

				person_.Body.Get(BP.RightHand).AddRelativeForce(
					new Vector3(0, 500, 0) * p);
			}
			else
			{
				person_.Gaze.Picker.ForcedTarget = null;

				foreach (var e in (person_.Expression as Proc.Expression).All)
				{
					foreach (var g in e.Groups)
					{
						g.Force(MorphTarget.NoForceTarget, 0, 0);
					}
				}
			}
		}
	}


	class SuckProcAnimation : BasicProcAnimation
	{
		private float durationMin_ = 0.8f;
		private float durationMax_ = 1.5f;
		private float durationWin_ = 0;
		private float durationInterval_ = 5;

		public SuckProcAnimation()
			: base("procSuck", false)
		{
			var g = new ConcurrentTargetGroup(
				"g", new Duration(), new Duration(), true,
				new SlidingDurationSync(
					new SlidingDuration(
						durationMin_, durationMax_,
						durationInterval_, durationInterval_,
						durationWin_, new CubicOutEasing()),
					new SlidingDuration(
						durationMin_, durationMax_,
						durationInterval_, durationInterval_,
						durationWin_, new CubicOutEasing()),
					new Duration(0, 0), new Duration(0, 0),
					SlidingDurationSync.Loop));

			g.AddTarget(new MorphTarget(
				BP.Lips, "Lips Pucker",
				0, 1, new ParentTargetSync()));

			AddTarget(g);
		}

		public override BasicProcAnimation Clone()
		{
			var a = new SuckProcAnimation();
			a.CopyFrom(this);
			return a;
		}
	}


	class SexProcAnimation : BasicProcAnimation
	{
		private const float DirectionChangeMaxDistance = 0.05f;
		private const float ForceFarDistance = 0.07f;
		private const float ForceCloseDistance = 0.04f;
		private const float MinimumForce = 0.4f;
		private const float ForceChangeMaxAmount = 0.02f;

		private float hipForceMin_ = 300;
		private float hipForceMax_ = 1200;
		private float hipTorqueMin_ = 0;
		private float hipTorqueMax_ = -20;
		private float chestTorqueMin_ = -10;
		private float chestTorqueMax_ = -50;
		private float headTorqueMin_ = 0;
		private float headTorqueMax_ = -10;
		private float durationMin_ = 1;
		private float durationMax_ = 0.1f;
		private float durationWin_ = 0.15f;
		private float durationInterval_ = 10;
		private Force[] forces_ = null;

		private float lastForceFactor_ = 0;
		private Vector3 lastDir_ = Vector3.Zero;

		public SexProcAnimation()
			: base("procSex", false)
		{
			var g = new ConcurrentTargetGroup(
				"g", new Duration(), new Duration(), true,
				new SlidingDurationSync(
					new SlidingDuration(
						durationMin_, durationMax_,
						durationInterval_, durationInterval_,
						durationWin_, new CubicOutEasing()),
					new SlidingDuration(
						durationMin_, durationMax_,
						durationInterval_, durationInterval_,
						durationWin_, new CubicOutEasing()),
					new Duration(0, 0), new Duration(0, 0),
					SlidingDurationSync.Loop | SlidingDurationSync.ResetBetween));


			g.AddTarget(new Force(
				Force.AbsoluteForce, BP.Hips, "hip",
				new SlidingMovement(
					Vector3.Zero, Vector3.Zero,
					0, 0, new Vector3(0, 0, 0), new LinearEasing()),
				new LinearEasing(), new ParentTargetSync(),
				new LinearEasing(), new LinearEasing()));

			g.AddTarget(new Force(
				Force.RelativeTorque, BP.Hips, "hip",
				new SlidingMovement(
					new Vector3(hipTorqueMin_, 0, 0),
					new Vector3(hipTorqueMax_, 0, 0),
					0, 0, new Vector3(0, 0, 0), new LinearEasing()),
				new LinearEasing(), new ParentTargetSync(),
				new LinearEasing(), new LinearEasing()));

			g.AddTarget(new Force(
				Force.RelativeTorque, BP.Chest, "chest",
				new SlidingMovement(
					new Vector3(chestTorqueMin_, 0, 0),
					new Vector3(chestTorqueMax_, 0, 0),
					0, 0, new Vector3(0, 0, 0), new LinearEasing()),
				new LinearEasing(), new ParentTargetSync(),
				new LinearEasing(), new LinearEasing()));

			g.AddTarget(new Force(
				Force.RelativeTorque, BP.Head, "head",
				new SlidingMovement(
					new Vector3(headTorqueMin_, 0, 0),
					new Vector3(headTorqueMax_, 0, 0),
					0, 0, new Vector3(0, 0, 0), new LinearEasing()),
				new LinearEasing(), new ParentTargetSync(),
				new LinearEasing(), new LinearEasing()));

			AddTarget(g);
		}

		public override BasicProcAnimation Clone()
		{
			var a = new SexProcAnimation();
			a.CopyFrom(this);
			return a;
		}

		public override void Start(Person p)
		{
			base.Start(p);

			if (forces_ == null)
			{
				var list = new List<Force>();
				GatherForces(list, Targets);
				forces_ = list.ToArray();
			}

			UpdateForces(true);
		}

		private void UpdateForces(bool alwaysUpdate = false)
		{
			for (int i=0; i<forces_.Length; ++i)
			{
				var f = forces_[i];

				if (f.Done || alwaysUpdate)
					UpdateForce(f);
			}
		}

		// gets the direction between the genitals so the forces go that way
		//
		// changes in direction need to be dampened because if the hips are
		// to the side of the target, they'll come down at an angle and
		// bounce back in the opposite direction
		//
		// this would reverse the direction for the next thrust, which would
		// just compound the direction changes infinitely
		//
		// dampening the direction changes eventually centers the movement
		//
		private Vector3 GetDirection()
		{
			if (receiver_ == null)
			{
				var rot = person_.Body.Get(BP.Hips).Rotation;
				return rot.Rotate(new Vector3(0, 0, 1)).Normalized;
			}

			var thisBP = person_.Body.Get(person_.Body.GenitalsBodyPart);
			var targetBP = receiver_.Body.Get(receiver_.Body.GenitalsBodyPart);

			// direction between genitals
			var currentDir = (targetBP.Position - thisBP.Position).Normalized;

			Vector3 dir;

			if (lastDir_ == Vector3.Zero)
			{
				dir = currentDir;
			}
			else
			{
				dir = Vector3.MoveTowards(
					lastDir_, currentDir, DirectionChangeMaxDistance);
			}

			lastDir_ = dir;

			return dir;
		}

		// gets a [0, 1] factor that multiplies the maximum force applied, based
		// on the distance between genitals
		//
		// this avoids forces that are too large when the genitals are close
		//
		// changes in forces need to be dampened because the hips don't always
		// have time to fully move back up before the distance is checked, which
		// would constantly alternate between different force factors
		//
		private float GetForceFactor()
		{
			if (receiver_ == null)
				return 0.7f;

			var thisBP = person_.Body.Get(person_.Body.GenitalsBodyPart);
			var targetBP = receiver_.Body.Get(receiver_.Body.GenitalsBodyPart);

			var range = ForceFarDistance - ForceCloseDistance;

			var dist = Vector3.Distance(thisBP.Position, targetBP.Position);
			var cdist = U.Clamp(dist, ForceCloseDistance, ForceFarDistance);
			var currentP = Math.Max((cdist - ForceCloseDistance) / range, MinimumForce);

			float p;

			if (lastForceFactor_ == 0)
			{
				p = currentP;
			}
			else
			{
				if (currentP > lastForceFactor_)
					p = Math.Min(lastForceFactor_ + ForceChangeMaxAmount, currentP);
				else
					p = Math.Max(lastForceFactor_ - ForceChangeMaxAmount, currentP);
			}

			lastForceFactor_ = p;

			return p;
		}

		private void UpdateForce(Force f)
		{
			var p = GetForceFactor();
			var fmin = hipForceMin_ * p;
			var fmax = hipForceMax_ * p;

			var dir = GetDirection();

			Cue.LogError($"{p} {dir}");

			f.Movement.SetRange(dir * fmin, dir * fmax);
		}

		private void GatherForces(List<Force> list, List<ITarget> targets)
		{
			for (int i = 0; i < targets.Count; ++i)
			{
				var t = targets[i];

				if (t is ITargetGroup)
				{
					GatherForces(list, ((ITargetGroup)t).Targets);
				}
				else if (t is Force)
				{
					var f = (Force)t;

					if (f.Type == Force.AbsoluteForce)
					{
						f.BeforeNextAction = () => UpdateForce(f);
						list.Add(f);
					}
				}
			}
		}
	}


	class SmokeProcAnimation : BasicProcAnimation
	{
		struct Render
		{
			public Sys.IGraphic hand, cig, targetHand, targetHandMid, targetCig, mouth;
		}


		private const int NoState = 0;
		private const int MovingToMouth = 1;
		private const int Adjusting = 2;
		private const int Pulling = 3;
		private const int Inhaling = 4;
		private const int Exhaling = 5;
		private const int Resetting = 6;
		private const int Finished = 7;

		private const float MoveTime = 2;
		private const float RotationTime = 2;
		private const float AdjustTime = 2;
		private const float AdjustPerSecond = 0.05f;
		private const float PullTime = 2;
		private const float InhaleTime = 0.3f;
		private const float HoldInTime = 1;
		private const float ExhaleTime = 5;
		private const float ResetTime = 1;

		private const float MouthOpenMin = 0;
		private const float MouthOpenMax = 0.3f;
		private const float MouthOpenTime = 0.3f;
		private const float MouthCloseTime = 0.2f;

		private const float LipsPuckerMin = 0;
		private const float LipsPuckerMax = 0.5f;
		private const float LipsPuckerTime = 0.2f;

		private const float MidPointDistance = 0.3f;

		private const float HeadUpTorque = -15;
		private const float HeadUpTime = 1;

		private const float SmokeOpacityMax = 0.15f;


		private float elapsed_ = 0;
		private IObject cig_ = null;
		private ISmoke smoke_ = null;
		private Hand hand_ = null;
		private BodyPart handPart_ = null;
		private int state_ = NoState;
		private Vector3 startPos_ = Vector3.Zero;
		private Quaternion startRot_ = Quaternion.Zero;
		private Vector3 targetMidPos_ = Vector3.Zero;
		private Vector3 targetPos_ = Vector3.Zero;
		private Quaternion targetRot_ = Quaternion.Zero;
		private Render render_ = new Render();
		private float startFist_ = 0;
		private float targetFist_ = 0;
		private Morph mouthOpen_;
		private Morph lipsPucker_;

		private IEasing moveToMouthEasing_ = new SinusoidalEasing();
		private IEasing moveBackEasing_ = new SinusoidalEasing();
		private IEasing morphEasing_ = new SinusoidalEasing();

		private bool DoRender = false;

		public SmokeProcAnimation()
			: base("smoking", false)
		{
		}

		public override BasicProcAnimation Clone()
		{
			var a = new SmokeProcAnimation();
			a.CopyFrom(this);
			return a;
		}

		public override void Start(Person p)
		{
			base.Start(p);

			hand_ = person_.Body.RightHand;
			handPart_ = person_.Body.Get(BP.RightHand);

			mouthOpen_ = new Morph(p.Atom.GetMorph("Mouth Open Wide"));
			lipsPucker_ = new Morph(p.Atom.GetMorph("Lips Pucker"));

			if (DoRender)
			{
				var b = new Box(Vector3.Zero, new Vector3(0.01f, 0.01f, 0.01f));

				render_.hand = Cue.Instance.Sys.CreateBoxGraphic("smokingHand", b, new Color(0, 0, 1, 0.5f));
				render_.cig = Cue.Instance.Sys.CreateBoxGraphic("smokingCig", b, new Color(0, 1, 0, 0.5f));
				render_.targetHand = Cue.Instance.Sys.CreateBoxGraphic("smokingTargetHand", b, new Color(1, 0, 0, 0.5f));
				render_.targetHandMid = Cue.Instance.Sys.CreateBoxGraphic("smokingTargetHandMid", b, new Color(0, 1, 0, 0.5f));

				render_.targetCig = Cue.Instance.Sys.CreateBoxGraphic("smokingTargetCig", b, new Color(1, 0, 1, 0.5f));
				render_.mouth = Cue.Instance.Sys.CreateBoxGraphic("smokingMouth", b, new Color(0, 1, 1, 0.5f));
			}

			elapsed_ = 0;

			CreateCigarette();
			smoke_ = Integration.CreateSmoke(SmokeID);
		}

		private string CigaretteID
		{
			get { return person_.ID + "_cue_cigarette"; }
		}

		private string SmokeID
		{
			get { return person_.ID + "_cue_cigarette_smoke"; }
		}

		public override bool Done
		{
			get { return (state_ == Finished); }
		}

		public override void FixedUpdate(float s)
		{
			if (cig_ == null)
				return;

			elapsed_ += s;
			hand_.InOut = -1;

			switch (state_)
			{
				case NoState:
				{
					StartMoveToMouth();
					break;
				}

				case MovingToMouth:
				{
					MoveToMouth();
					break;
				}

				case Adjusting:
				{
					Adjust(s);
					break;
				}

				case Pulling:
				{
					Pull();
					break;
				}

				case Inhaling:
				{
					Inhale();
					break;
				}

				case Exhaling:
				{
					Exhale();
					break;
				}

				case Resetting:
				{
					DoReset();
					break;
				}

				case Finished:
				{
					break;
				}
			}

			SetPosition();

			if (DoRender)
			{
				var mouth = person_.Body.Get(BP.Lips);
				render_.hand.Position = handPart_.Position;
				render_.cig.Position = cig_.Position;
				render_.targetHand.Position = targetPos_;
				render_.targetHandMid.Position = targetMidPos_;
				render_.targetCig.Position =
					targetPos_ + targetRot_.Rotate(handPart_.Rotation.RotateInv(CigarettePosition() - handPart_.Position));
				render_.mouth.Position = mouth.Position;
			}
		}

		private Quaternion GetTargetRotation()
		{
			var head = person_.Body.Get(BP.Head);

			if (hand_ == person_.Body.RightHand)
			{
				return Quaternion.FromEuler(
					head.Rotation.Euler.X,
					head.Rotation.Euler.Y + 90,
					head.Rotation.Euler.Z + 90);
			}
			else
			{
				return Quaternion.FromEuler(
					head.Rotation.Euler.X,
					head.Rotation.Euler.Y + 270,
					head.Rotation.Euler.Z + 270);
			}
		}

		private void CheckTarget()
		{
			var head = person_.Body.Get(BP.Head);
			var mouth = person_.Body.Get(BP.Lips);

			var d = handPart_.ControlRotation.RotateInv(
				CigarettePosition() - handPart_.ControlPosition);

			targetRot_ = GetTargetRotation();

			targetPos_ =
				mouth.Position - targetRot_.Rotate(d) +
				head.Rotation.Rotate(new Vector3(0, 0, 0.005f));
		}

		private void StartMoveToMouth()
		{
			var head = person_.Body.Get(BP.Head);
			var chest = person_.Body.Get(BP.Chest);
			var mouth = person_.Body.Get(BP.Lips);

			startPos_ = handPart_.Position;
			startRot_ = handPart_.Rotation;
			startFist_ = hand_.Fist;

			CheckTarget();

			targetMidPos_ =
				startPos_ + (targetPos_ - startPos_) / 2 +
				chest.Rotation.Rotate(new Vector3(0, 0, MidPointDistance));

			targetFist_ = 0;

			elapsed_ = 0;
			state_ = MovingToMouth;

			head.ForceBusy(true);
			handPart_.ForceBusy(true);
		}

		public static Vector3 Bezier(
			Vector3 s,
			Vector3 p,
			Vector3 e,
			float t)
		{
			float rt = 1 - t;
			return rt * rt * s + 2 * rt * t * p + t * t * e;
		}

		private Quaternion GetRotationPoint(float f, bool xNeg, bool yNeg, bool zNeg)
		{
			var s = startRot_.Euler;
			var t = targetRot_.Euler;

			float x, y, z;

			{
				var x1 = s.X;
				var x2 = t.X;

				if (Math.Abs(Vector3.AngleBetweenBearings(x1, x2)) > 50)
				{
					if (xNeg)
					{
						if (x2 > x1)
							x1 += 360;
					}
					else
					{
						if (x1 > x2)
							x2 += 360;
					}

					x = x1 + (x2 - x1) * f;
				}
				else
				{
					x = x1 + Vector3.AngleBetweenBearings(x1, x2) * f;
				}
			}

			{
				var y1 = s.Y;
				var y2 = t.Y;

				if (Math.Abs(Vector3.AngleBetweenBearings(y1, y2)) > 50)
				{
					if (yNeg)
					{
						if (y2 > y1)
							y1 += 360;
					}
					else
					{
						if (y1 > y2)
							y2 += 360;
					}

					y = y1 + (y2 - y1) * f;
				}
				else
				{
					y = y1 + Vector3.AngleBetweenBearings(y1, y2) * f;
				}
			}

			{
				var z1 = s.Z;
				var z2 = t.Z;

				if (Math.Abs(Vector3.AngleBetweenBearings(z1, z2)) > 50)
				{
					if (zNeg)
					{
						if (z2 > z1)
							z1 += 360;
					}
					else
					{
						if (z1 > z2)
							z2 += 360;
					}

					z = z1 + (z2 - z1) * f;
				}
				else
				{
					z = z1 + Vector3.AngleBetweenBearings(z1, z2) * f;
				}
			}

			return Quaternion.FromEuler(x, y, z);
		}

		private Quaternion LerpRotation(float f)
		{
			var s = startRot_.Euler;
			var t = targetRot_.Euler;

			bool xNeg, yNeg, zNeg;

			if (hand_ == person_.Body.RightHand)
			{
				xNeg = true;
				yNeg = false;
				zNeg = true;
			}
			else
			{
				xNeg = true;
				yNeg = true;
				zNeg = false;
			}

			float midF = 0.3f;
			var mid = GetRotationPoint(midF, xNeg, yNeg, zNeg);

			if (f <= midF)
				return Quaternion.Lerp(startRot_, mid, f / midF);
			else
				return Quaternion.Lerp(mid, targetRot_, (f - midF) / (1 - midF));
		}

		private void MoveToMouth()
		{
			if (elapsed_ < (MoveTime - 0.5f))
				CheckTarget();

			var f = moveToMouthEasing_.Magnitude(U.Clamp(elapsed_ / MoveTime, 0, 1));
			var p = Bezier(startPos_, targetMidPos_, targetPos_, f);

			var ff = moveToMouthEasing_.Magnitude(U.Clamp(elapsed_ / RotationTime, 0, 1));
			var r = LerpRotation(ff);

			handPart_.ControlPosition = p;
			handPart_.ControlRotation = r;
			hand_.Fist = startFist_ + (targetFist_ - startFist_) * f;

			if (elapsed_ >= MoveTime && elapsed_ >= RotationTime)
				StartAdjust();
		}

		private void StartAdjust()
		{
			elapsed_ = 0;
			state_ = Adjusting;
		}

		private void Adjust(float s)
		{
			var mouth = person_.Body.Get(BP.Lips);
			var cig = CigarettePosition();

			{
				float f = morphEasing_.Magnitude(U.Clamp(elapsed_ / MouthOpenTime, 0, 1));
				mouthOpen_.Value = MouthOpenMin + (MouthOpenMax - MouthOpenMin) * f;
			}

			var d = Vector3.Distance(cig, mouth.Position);
			if (d <= 0.003f || elapsed_ >= AdjustTime)
			{
				StartPull();
				return;
			}

			var offset = mouth.Position - cig;
			var maxD = (s * AdjustPerSecond);

			targetPos_ = handPart_.ControlPosition + offset;

			var p = Vector3.MoveTowards(
				handPart_.ControlPosition, targetPos_, maxD);

			handPart_.ControlPosition = p;
		}

		private void StartPull()
		{
			elapsed_ = 0;
			state_ = Pulling;
		}

		private void Pull()
		{
			{
				float f = morphEasing_.Magnitude(U.Clamp(elapsed_ / MouthCloseTime, 0, 1));
				mouthOpen_.Value = MouthOpenMin + (MouthOpenMax - MouthOpenMin) * (1 - f);
			}

			{
				float f = morphEasing_.Magnitude(U.Clamp(elapsed_ / LipsPuckerTime, 0, 1));
				lipsPucker_.Value = LipsPuckerMin + (LipsPuckerMax - LipsPuckerMin) * f;
			}

			if (elapsed_ > PullTime)
				StartInhale();
		}

		private void StartInhale()
		{
			elapsed_ = 0;
			state_ = Inhaling;
		}

		private void Inhale()
		{
			{
				float f = morphEasing_.Magnitude(U.Clamp(elapsed_ / MouthOpenTime, 0, 1));
				mouthOpen_.Value = MouthOpenMin + (MouthOpenMax - MouthOpenMin) * f;
			}

			{
				float ff = morphEasing_.Magnitude(U.Clamp(elapsed_ / LipsPuckerTime, 0, 1));
				lipsPucker_.Value = LipsPuckerMin + (LipsPuckerMax - LipsPuckerMin) * (ff - 1);
			}

			if (elapsed_ >= InhaleTime)
				StartExhale();
		}

		private void StartExhale()
		{
			var chest = person_.Body.Get(BP.Chest);

			targetMidPos_ =
				startPos_ + (targetPos_ - startPos_) / 2 +
				chest.Rotation.Rotate(new Vector3(0, 0, MidPointDistance));

			elapsed_ = 0;
			state_ = Exhaling;
		}

		private void Exhale()
		{
			var head = person_.Body.Get(BP.Head);

			if (elapsed_ >= HoldInTime)
			{
				var e = elapsed_ - HoldInTime;

				{
					float f = morphEasing_.Magnitude(U.Clamp(e / MouthOpenTime, 0, 1));
					mouthOpen_.Value = MouthOpenMin + (MouthOpenMax - MouthOpenMin) * f;
				}

				{
					float f = morphEasing_.Magnitude(U.Clamp(e / LipsPuckerTime, 0, 1));
					lipsPucker_.Value = LipsPuckerMin + (LipsPuckerMax - LipsPuckerMin) * f;
				}

				{
					float f = moveToMouthEasing_.Magnitude(U.Clamp(e / HeadUpTime, 0, 1));
					head.AddRelativeTorque(new Vector3(HeadUpTorque * f, 0, 0));
				}

				smoke_.Position = head.Position;
				smoke_.Opacity = SmokeOpacityMax;
			}

			{
				var f = moveBackEasing_.Magnitude(U.Clamp((MoveTime - elapsed_) / MoveTime, 0, 1));
				var p = Bezier(startPos_, targetMidPos_, targetPos_, f);

				var ff = moveToMouthEasing_.Magnitude(U.Clamp((RotationTime - elapsed_) / RotationTime, 0, 1));
				var r = LerpRotation(ff);

				handPart_.ControlPosition = p;
				handPart_.ControlRotation = r;
				hand_.Fist = startFist_ + (targetFist_ - startFist_) * f;
			}

			if (elapsed_ >= ExhaleTime && elapsed_ >= MoveTime && elapsed_ >= RotationTime && elapsed_ >= HeadUpTime)
			{
				head.ForceBusy(false);
				handPart_.ForceBusy(false);
				StartReset();
			}
		}

		private void StartReset()
		{
			elapsed_ = 0;
			state_ = Resetting;
		}

		private void DoReset()
		{
			var head = person_.Body.Get(BP.Head);

			{
				float f = morphEasing_.Magnitude(U.Clamp(elapsed_ / ResetTime, 0, 1));
				mouthOpen_.Value = MouthOpenMin + (MouthOpenMax - MouthOpenMin) * (1 - f);
			}

			{
				float f = morphEasing_.Magnitude(U.Clamp(elapsed_ / ResetTime, 0, 1));
				lipsPucker_.Value = LipsPuckerMin + (LipsPuckerMax - LipsPuckerMin) * (1 - f);
			}

			{
				float f = moveToMouthEasing_.Magnitude(U.Clamp(elapsed_ / HeadUpTime, 0, 1));
				head.AddRelativeTorque(new Vector3(HeadUpTorque * (1 - f), 0, 0));
			}

			{
				smoke_.Opacity = SmokeOpacityMax / 2;
			}

			if (elapsed_ >= ResetTime)
			{
				state_ = Finished;
				smoke_.Opacity = 0;
			}
		}

		private void CreateCigarette()
		{
			var a = Cue.Instance.Sys.GetAtom(CigaretteID);

			if (a != null)
			{
				person_.Log.Info("cig already exists, taking");
				SetCigarette(new BasicObject(-1, a));
			}
			else
			{
				person_.Log.Info("creating cigarette");

				var oc = Resources.Objects.Get("cigarette");
				if (oc == null)
				{
					person_.Log.Error("no cigarette object creator");
					return;
				}

				oc.Create(CigaretteID, (o) =>
				{
					if (o == null)
					{
						person_.Log.Error("failed to create cigarette");
						return;
					}

					SetCigarette(o);
				});
			}
		}

		private void SetCigarette(IObject o)
		{
			cig_ = o;
			cig_.Atom.Collisions = false;
			cig_.Atom.Physics = false;
			cig_.Atom.Hidden = true;
		}

		private Vector3 CigarettePosition()
		{
			var ia = hand_.Index.Intermediate;
			var ib = hand_.Middle.Intermediate;
			var ip = ia.Position + (ib.Position - ia.Position) / 2;

			var da = hand_.Index.Distal;
			var db = hand_.Middle.Distal;
			var dp = da.Position + (db.Position - da.Position) / 2;

			var p = ip + (dp - ip) / 2;
			var r = hand_.Middle.Intermediate.Rotation;

			float vertOffset;

			if (hand_ == person_.Body.RightHand)
				vertOffset = 0.02f;
			else
				vertOffset = -0.01f;

			return p + r.Rotate(new Vector3(vertOffset, -0.025f, 0));
		}

		private void SetPosition()
		{
			var e = hand_.Middle.Intermediate.Rotation.Euler;
			var q = Quaternion.FromEuler(e.X, e.Y, e.Z + 10);

			cig_.Position = CigarettePosition();
			cig_.Rotation = q;
		}
	}
}
