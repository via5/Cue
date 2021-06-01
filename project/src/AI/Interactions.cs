using Cue.W;
using System;
using System.Collections;

namespace Cue
{
	interface IInteraction
	{
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

		public abstract void Update(float s);
	}


	class SmokingInteraction : BasicInteraction
	{
		struct Render
		{
			public IGraphic hand, cig, targetHand, targetCig, mouth;
		}

		private const int NoState = 0;
		private const int MovingToMouth = 1;
		private const int Adjusting = 2;
		private const int Pulling = 3;
		private const int Inhaling = 4;
		private const int MovingBack = 5;
		private const int Stop = 6;

		private const float MoveTime = 1;
		private const float AdjustTime = 2;
		private const float AdjustPerSecond = 0.07f;
		private const float PullTime = 2;
		private const float InhaleTime = 0.3f;

		private const float MouthOpenMin = 0;
		private const float MouthOpenMax = 0.3f;
		private const float MouthOpenTime = 0.3f;
		private const float MouthCloseTime = 0.2f;

		private const float LipsPuckerMin = 0;
		private const float LipsPuckerMax = 0.5f;
		private const float LipsPuckerTime = 0.2f;

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
		private Vector3 targetPos_ = Vector3.Zero;
		private Quaternion targetRot_ = Quaternion.Zero;
		private Render render_ = new Render();
		private float startFist_ = 0;
		private float targetFist_ = 0;
		private Morph mouthOpen_;
		private Morph lipsPucker_;


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

		public override void Update(float s)
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

				case MovingBack:
				{
					MoveBack();
					break;
				}

				case Stop:
				{
					break;
				}
			}

			SetPosition();


			//var mouth = person_.Body.Get(BodyParts.Lips);
			//render_.hand.Position = handPart_.Position;
			//render_.cig.Position = cig_.Position;
			//render_.targetHand.Position = targetPos_;
			//render_.targetCig.Position =
			//	targetPos_ + targetRot_.Rotate(handPart_.Rotation.RotateInv(CigarettePosition() - handPart_.Position));
			//render_.mouth.Position = mouth.Position;
		}

		private void StartMoveToMouth()
		{
			var head = person_.Body.Get(BodyParts.Head);
			var mouth = person_.Body.Get(BodyParts.Lips);

			var d = handPart_.Rotation.RotateInv(
				CigarettePosition() - handPart_.Position);

			startPos_ = handPart_.Position;
			startRot_ = handPart_.Rotation;
			startFist_ = hand_.Fist;

			targetRot_ = Quaternion.FromEuler(
				head.Rotation.Euler.X,
				head.Rotation.Euler.Y + 90,
				head.Rotation.Euler.Z + 90);

			targetPos_ =
				mouth.Position - targetRot_.Rotate(d);

			targetFist_ = 0;

			elapsed_ = 0;
			state_ = MovingToMouth;

			head.ForceBusy(true);
			handPart_.ForceBusy(true);
		}

		private void MoveToMouth()
		{
			var f = U.Clamp(elapsed_ / MoveTime, 0, 1);
			var p = Vector3.Lerp(startPos_, targetPos_, f);
			var r = Quaternion.Lerp(startRot_, targetRot_, f);

			handPart_.ControlPosition = p;
			handPart_.ControlRotation = r;
			hand_.Fist = startFist_ + (targetFist_ - startFist_) * f;

			if (elapsed_ >= MoveTime)
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
				float f = U.Clamp(elapsed_ / MouthOpenTime, 0, 1);
				mouthOpen_.Value = MouthOpenMin + (MouthOpenMax - MouthOpenMin) * f;
			}

			var d = Vector3.Distance(cig, mouth.Position);
			if (d <= 0.003f || elapsed_ >= AdjustTime)
			{
				StartPull();
				return;
			}

			var offset = mouth.Position - cig;
			var maxD = s * AdjustPerSecond;

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
				float f = U.Clamp(elapsed_ / MouthCloseTime, 0, 1);
				mouthOpen_.Value = MouthOpenMin + (MouthOpenMax - MouthOpenMin) * (1 - f);
			}

			{
				float f = U.Clamp(elapsed_ / LipsPuckerTime, 0, 1);
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
				float f = U.Clamp(elapsed_ / MouthOpenTime, 0, 1);
				mouthOpen_.Value = MouthOpenMin + (MouthOpenMax - MouthOpenMin) * f;
			}

			{
				float ff = U.Clamp(elapsed_ / LipsPuckerTime, 0, 1);
				lipsPucker_.Value = LipsPuckerMin + (LipsPuckerMax - LipsPuckerMin) * (ff - 1);
			}

			if (elapsed_ >= InhaleTime)
				StartMoveBack();
		}

		private void StartMoveBack()
		{
			elapsed_ = 0;
			state_ = MovingBack;
		}

		private void MoveBack()
		{
			var f = U.Clamp((MoveTime - elapsed_) / MoveTime, 0, 1);
			var p = Vector3.Lerp(startPos_, targetPos_, f);
			var r = Quaternion.Lerp(startRot_, targetRot_, f);

			var head = person_.Body.Get(BodyParts.Head);

			handPart_.ControlPosition = p;
			handPart_.ControlRotation = r;
			hand_.Fist = startFist_ + (targetFist_ - startFist_) * f;

			{
				float ff = U.Clamp(elapsed_ / MouthOpenTime, 0, 1);
				mouthOpen_.Value = MouthOpenMin + (MouthOpenMax - MouthOpenMin) * (1 - ff);
			}


			if (elapsed_ >= MoveTime)
			{
				elapsed_ = 0;
				head.ForceBusy(false);
				handPart_.ForceBusy(false);
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

			return p + r.Rotate(new Vector3(0.01f, -0.025f, 0));
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
