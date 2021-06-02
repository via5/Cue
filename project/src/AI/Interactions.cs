using Cue.W;
using System;
using System.Collections;

namespace Cue
{
	interface IInteraction
	{
		void FixedUpdate(float s);
		void Update(float s);
	}


	abstract class BasicInteraction : IInteraction
	{
		public static IInteraction[] All(Person p)
		{
			return new IInteraction[]
			{
				new SmokingInteraction(p),
				new KissingInteraction(p)
			};
		}

		public virtual void FixedUpdate(float s)
		{
			// no-op
		}

		public virtual void Update(float s)
		{
			// no-op
		}
	}


	class SmokingInteraction : BasicInteraction
	{
		struct Render
		{
			public IGraphic hand, cig, targetHand, targetHandMid, targetCig, mouth;
		}

		private const int NoState = 0;
		private const int MovingToMouth = 1;
		private const int Adjusting = 2;
		private const int Pulling = 3;
		private const int Inhaling = 4;
		private const int Exhaling = 5;
		private const int Resetting = 6;
		private const int Stop = 7;

		private const float MoveTime = 2;
		private const float RotationTime = 2;
		private const float AdjustTime = 2;
		private const float AdjustPerSecond = 0.07f;
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

		private Person person_;
		private Logger log_;
		private float elapsed_ = 0;
		private IObject cig_ = null;
		private Hand hand_ = null;
		private BodyPart handPart_ = null;
		private bool inited_ = false;
		private bool enabled_ = true;
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


		public SmokingInteraction(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Interaction, p, "SmokeInt");
			enabled_ = p.HasTrait("smoker");

			if (!enabled_)
			{
				log_.Info("not a smoker");
				return;
			}

			hand_ = person_.Body.RightHand;
			handPart_ = person_.Body.Get(BodyParts.RightHand);

			mouthOpen_ = new Morph(p.Atom.GetMorph("Mouth Open Wide"));
			lipsPucker_ = new Morph(p.Atom.GetMorph("Lips Pucker"));

			//var b = new Box(Vector3.Zero, new Vector3(0.01f, 0.01f, 0.01f));
			//
			//render_.hand = Cue.Instance.Sys.CreateBoxGraphic("smokingHand", b, new Color(0, 0, 1, 0.5f));
			//render_.cig = Cue.Instance.Sys.CreateBoxGraphic("smokingCig", b, new Color(0, 1, 0, 0.5f));
			//render_.targetHand = Cue.Instance.Sys.CreateBoxGraphic("smokingTargetHand", b, new Color(1, 0, 0, 0.5f));
			//render_.targetHandMid = Cue.Instance.Sys.CreateBoxGraphic("smokingTargetHandMid", b, new Color(0, 1, 0, 0.5f));
			//
			//render_.targetCig = Cue.Instance.Sys.CreateBoxGraphic("smokingTargetCig", b, new Color(1, 0, 1, 0.5f));
			//render_.mouth = Cue.Instance.Sys.CreateBoxGraphic("smokingMouth", b, new Color(0, 1, 1, 0.5f));
		}

		private string CigaretteID
		{
			get
			{
				return person_.ID + "_cue_cigarette";
			}
		}

		public override void FixedUpdate(float s)
		{
			if (!enabled_)
				return;

			if (!inited_)
			{
				Init();
				if (!enabled_)
					return;
			}

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
					Reset();
					break;
				}

				case Stop:
				{
					break;
				}
			}

			SetPosition();


//			var mouth = person_.Body.Get(BodyParts.Lips);
//			render_.hand.Position = handPart_.Position;
//			render_.cig.Position = cig_.Position;
//			render_.targetHand.Position = targetPos_;
//			render_.targetHandMid.Position = targetMidPos_;
//			render_.targetCig.Position =
//				targetPos_ + targetRot_.Rotate(handPart_.Rotation.RotateInv(CigarettePosition() - handPart_.Position));
//			render_.mouth.Position = mouth.Position;
		}

		private void StartMoveToMouth()
		{
			var head = person_.Body.Get(BodyParts.Head);
			var chest = person_.Body.Get(BodyParts.Chest);
			var mouth = person_.Body.Get(BodyParts.Lips);

			var d = handPart_.ControlRotation.RotateInv(
				CigarettePosition() - handPart_.ControlPosition);

			startPos_ = handPart_.Position;
			startRot_ = handPart_.Rotation;
			startFist_ = hand_.Fist;

			targetRot_ = Quaternion.FromEuler(
				head.Rotation.Euler.X,
				head.Rotation.Euler.Y + 90,
				head.Rotation.Euler.Z + 90);

			targetPos_ =
				mouth.Position - targetRot_.Rotate(d) +
				head.Rotation.Rotate(new Vector3(0, 0, 0.005f));

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

		private Quaternion GetRotationPoint(float f)
		{
			var s = startRot_.Euler;
			var t = targetRot_.Euler;

			float x, y, z;

			{
				var x1 = s.X;
				var x2 = t.X;

				if (Math.Abs(Vector3.AngleBetweenBearings(x1, x2)) > 50)
				{
					if (x2 > x1)
						x1 += 360;

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
					if (y1 > y2)
						y2 += 360;

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
					if (z2 > z1)
						z1 += 360;

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

			float midF = 0.3f;
			var mid = GetRotationPoint(midF);

			if (f <= midF)
				return Quaternion.Lerp(startRot_, mid, f / midF);
			else
				return Quaternion.Lerp(mid, targetRot_, (f - midF) / (1 - midF));
		}

		private void MoveToMouth()
		{
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
			var mouth = person_.Body.Get(BodyParts.Lips);
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
			var chest = person_.Body.Get(BodyParts.Chest);

			targetMidPos_ =
				startPos_ + (targetPos_ - startPos_) / 2 +
				chest.Rotation.Rotate(new Vector3(0, 0, MidPointDistance));

			elapsed_ = 0;
			state_ = Exhaling;
		}

		private void Exhale()
		{
			var head = person_.Body.Get(BodyParts.Head);

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

		private void Reset()
		{
			var head = person_.Body.Get(BodyParts.Head);

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

			if (elapsed_ >= ResetTime)
			{
				StartMoveToMouth();
			}
		}

		private void Init()
		{
			log_.Info("initing");
			inited_ = true;
			elapsed_ = 0;
			enabled_ = true;
			CreateCigarette();
		}

		private Vector3 CigarettePosition()
		{
			var ia = person_.Body.RightHand.Index.Intermediate;
			var ib = person_.Body.RightHand.Middle.Intermediate;
			var ip = ia.Position + (ib.Position - ia.Position) / 2;

			var da = person_.Body.RightHand.Index.Distal;
			var db = person_.Body.RightHand.Middle.Distal;
			var dp = da.Position + (db.Position - da.Position) / 2;

			var p = ip + (dp - ip) / 2;
			var r = person_.Body.RightHand.Middle.Intermediate.Rotation;

			return p + r.Rotate(new Vector3(0.02f, -0.025f, 0));
		}

		private void SetPosition()
		{
			var e = person_.Body.RightHand.Middle.Intermediate.Rotation.Euler;
			var q = Quaternion.FromEuler(e.X, e.Y, e.Z + 10);

			cig_.Position = CigarettePosition();
			cig_.Rotation = q;
		}

		private void CreateCigarette()
		{
			var a = Cue.Instance.Sys.GetAtom(CigaretteID);

			if (a != null)
			{
				log_.Info("already exists, destroying");
				a.Destroy();
			}

			log_.Info("creating cigarette");

			var oc = Resources.Objects.Get("cigarette");
			if (oc == null)
			{
				log_.Error("no cigarette object creator, disabling");
				enabled_ = false;
				return;
			}

			oc.Create(CigaretteID, (o) => { SetCigarette(o); });
		}

		private void SetCigarette(IObject o)
		{
			if (o == null)
			{
				log_.Error("failed to create cigarette, disabling");
				enabled_ = false;
				return;
			}

			cig_ = o;
			o.Atom.Collisions = false;
			o.Atom.Physics = false;
			o.Atom.Hidden = true;
		}
	}


	class KissingInteraction : BasicInteraction
	{
		public const float StartDistance = 0.15f;
		public const float StopDistance = 0.1f;
		public const float PlayerStopDistance = 0.2f;
		public const float MinimumActiveTime = 3;
		public const float MinimumStoppedTime = 2;

		private Person person_;
		private Logger log_;
		private float elapsed_ = 0;

		public KissingInteraction(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Interaction, p, "KissInt");
		}

		public override void Update(float s)
		{
			if (!person_.Options.CanKiss)
			{
				if (person_.Kisser.Active)
					person_.Kisser.Stop();

				return;
			}

			if (person_.Body.Get(BodyParts.Lips) == null)
				return;

			if (person_.Kisser.Active)
			{
				if (person_.Kisser.Elapsed >= MinimumActiveTime)
					TryStop();
			}
			else
			{
				elapsed_ += s;
				if (elapsed_ > 1)
				{
					TryStart();
					elapsed_ = 0;
				}
			}
		}

		private bool TryStart()
		{
			if (!CanStart(person_))
				return false;

			var srcLips = person_.Body.Get(BodyParts.Lips).Position;

			for (int i = 0; i < Cue.Instance.Persons.Count; ++i)
			{
				var target = Cue.Instance.Persons[i];
				if (target == person_)
					continue;

				var targetLips = target.Body.Get(BodyParts.Lips);

				if (targetLips == null || target.Kisser.Active)
					continue;

				if (!CanStart(target))
					continue;

				// todo: check rotations

				if (Vector3.Distance(srcLips, targetLips.Position) < StartDistance)
				{
					log_.Info($"starting for {person_} and {target}");
					person_.Kisser.StartReciprocal(target);
					return true;
				}
			}

			return false;
		}

		private bool CanStart(Person p)
		{
			if (!p.Options.CanKiss)
				return false;

			if (p.Kisser.OnCooldown)
				return false;

			if (!p.CanMoveHead)
				return false;

			if (p.State.Is(PersonState.Walking))
				return false;

			if (p.State.Transitioning)
				return false;

			return true;
		}

		private bool TryStop()
		{
			var target = person_.Kisser.Target;
			if (target == null)
				return false;

			var srcLips = person_.Body.Get(BodyParts.Lips);
			var targetLips = target.Body.Get(BodyParts.Lips);

			if (srcLips == null || targetLips == null)
				return false;

			var d = Vector3.Distance(srcLips.Position, targetLips.Position);

			var hasPlayer = (
				person_ == Cue.Instance.Player ||
				target == Cue.Instance.Player);

			var sd = (hasPlayer ? PlayerStopDistance : StopDistance);

			if (d >= sd)
			{
				person_.Kisser.Stop();
				target.Kisser.Stop();
				return true;
			}

			return false;
		}
	}
}
