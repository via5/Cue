using System;

namespace Cue.Proc
{
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
		private const float LipsPuckerMax = 0.3f;
		private const float LipsPuckerTime = 0.2f;

		private const float ExhaleMouthOpenMin = 0;
		private const float ExhaleMouthOpenMax = 0;
		private const float ExhaleMorphsTime = 0.2f;
		private const float ExhaleLipsPuckerMin = 0;
		private const float ExhaleLipsPuckerMax = 0.4f;
		private const float ExhaleLipsPartMin = 0;
		private const float ExhaleLipsPartMax = 0.2f;
		private const float ExhaleLipsPuckerWideMin = 0;
		private const float ExhaleLipsPuckerWideMax = 0;
		private const float ExhaleMouthNarrowMin = 0;
		private const float ExhaleMouthNarrowMax = 0.15f;

		private const float MidPointDistance = 0.3f;

		private const float HeadUpTorque = -15;
		private const float HeadUpTime = 1;

		private const float SmokeOpacityMax = 0.15f;


		private float elapsed_ = 0;
		private IObject unsafeCig_ = null;
		private ISmoke unsafeSmoke_ = null;
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
		private bool busySet_ = false;

		private Morph mouthOpen_;
		private Morph lipsPucker_, mouthNarrow_, lipsPart_, lipsPuckerWide_;

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

		public override bool Start(Person p, object ps)
		{
			if (!base.Start(p, ps))
				return false;

			hand_ = person_.Body.RightHand;
			handPart_ = person_.Body.Get(BP.RightHand);

			mouthOpen_ = new Morph(p.Atom.GetMorph("Mouth Open Wide"));
			lipsPucker_ = new Morph(p.Atom.GetMorph("Lips Pucker"));
			mouthNarrow_ = new Morph(p.Atom.GetMorph("Mouth Narrow"));
			lipsPart_ = new Morph(p.Atom.GetMorph("Lips Part"));
			lipsPuckerWide_ = new Morph(p.Atom.GetMorph("Lips Pucker Wide"));

			unsafeCig_ = FindCigarette();
			unsafeSmoke_ = FindSmoke();

			if (unsafeCig_ == null)
				return false;

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
			return true;
		}

		public override void Reset()
		{
			base.Reset();

			if (busySet_)
				SetBusy(false);

			mouthOpen_?.Reset();
			lipsPucker_?.Reset();
			mouthNarrow_?.Reset();
			lipsPart_?.Reset();
			lipsPuckerWide_?.Reset();
		}

		private IObject FindCigarette()
		{
			var a = Cue.Instance.Sys.GetAtom(
				SmokeEvent.MakeCigaretteID(person_));

			if (a == null)
				return null;

			return new BasicObject(-1, a);
		}

		private ISmoke FindSmoke()
		{
			return Integration.CreateSmoke(
				SmokeEvent.MakeSmokeID(person_), true);
		}

		public override bool Done
		{
			get { return (state_ == Finished); }
		}

		public override void FixedUpdate(float s)
		{
			if (unsafeCig_ == null)
			{
				state_ = Finished;
				return;
			}

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

			SmokeEvent.SetCigaretteTransform(hand_, unsafeCig_);

			if (DoRender)
			{
				var mouth = person_.Body.Get(BP.Lips);
				render_.hand.Position = handPart_.Position;

				try
				{
					render_.cig.Position = unsafeCig_.Position;
				}
				catch (Exception)
				{
					// eat it
				}

				render_.targetHand.Position = targetPos_;
				render_.targetHandMid.Position = targetMidPos_;
				render_.targetCig.Position =
					targetPos_ + targetRot_.Rotate(
						handPart_.Rotation.RotateInv(
							SmokeEvent.MakeCigarettePosition(hand_) - handPart_.Position));
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
				SmokeEvent.MakeCigarettePosition(hand_) - handPart_.ControlPosition);

			targetRot_ = GetTargetRotation();

			targetPos_ =
				mouth.Position - targetRot_.Rotate(d) +
				head.Rotation.Rotate(new Vector3(0, 0, 0.005f));
		}

		private void StartMoveToMouth()
		{
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

			SetBusy(true);
		}

		private void SetBusy(bool b)
		{
			person_.Body.Get(BP.Head)?.ForceBusy(b);
			person_.Body.Get(BP.Mouth)?.ForceBusy(b);
			handPart_?.ForceBusy(b);
			busySet_ = b;
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
			var cig = SmokeEvent.MakeCigarettePosition(hand_);

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

				float mf = morphEasing_.Magnitude(U.Clamp(e / ExhaleMorphsTime, 0, 1));
				SetExhaleMorphs(mf);

				{
					float f = moveToMouthEasing_.Magnitude(U.Clamp(e / HeadUpTime, 0, 1));
					head.AddRelativeTorque(new Vector3(HeadUpTorque * f, 0, 0));
				}

				try
				{
					unsafeSmoke_.Position = head.Position;
					unsafeSmoke_.Opacity = SmokeOpacityMax;
				}
				catch (Exception)
				{
					// eat it
				}
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
				SetBusy(false);
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

			float mf = morphEasing_.Magnitude(U.Clamp(elapsed_ / ResetTime, 0, 1));
			SetExhaleMorphs(1 - mf);

			{
				float f = moveToMouthEasing_.Magnitude(U.Clamp(elapsed_ / HeadUpTime, 0, 1));
				head.AddRelativeTorque(new Vector3(HeadUpTorque * (1 - f), 0, 0));
			}

			float opacity = SmokeOpacityMax / 2;

			if (elapsed_ >= ResetTime)
			{
				state_ = Finished;
				opacity = 0;
			}

			try
			{
				unsafeSmoke_.Opacity = opacity;
			}
			catch (Exception)
			{
				// eat it
			}
		}

		private void SetExhaleMorphs(float f)
		{
			mouthOpen_.Value = ExhaleMouthOpenMin + (ExhaleMouthOpenMax - ExhaleMouthOpenMin) * f;
			lipsPucker_.Value = ExhaleLipsPuckerMin + (ExhaleLipsPuckerMax - ExhaleLipsPuckerMin) * f;
			mouthNarrow_.Value = ExhaleMouthNarrowMin + (ExhaleMouthNarrowMax - ExhaleMouthNarrowMin) * f;
			lipsPart_.Value = ExhaleLipsPartMin + (ExhaleLipsPartMax - ExhaleLipsPartMin) * f;
			lipsPuckerWide_.Value = ExhaleLipsPuckerWideMin + (ExhaleLipsPuckerWideMax - ExhaleLipsPuckerWideMin) * f;
		}
	}
}
