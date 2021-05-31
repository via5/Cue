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
		private const int NoState = 0;
		private const int MovingToMouth = 1;
		private const int Pull = 2;
		private const int MovingBack = 3;
		private const int Stop = 4;

		private const float MoveTime = 2;

		private Person person_;
		private Logger log_;
		private float elapsed_ = 0;
		private IObject cig_ = null;
		private bool inited_ = false;
		private bool enabled_ = true;
		private int state_ = NoState;
		private Vector3 startPos_ = Vector3.Zero;
		private Quaternion startRot_ = Quaternion.Zero;
		private Vector3 targetPos_ = Vector3.Zero;
		private Quaternion targetRot_ = Quaternion.Zero;

		public SmokingInteraction(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Interaction, p, "SmokeInt");
			enabled_ = p.HasTrait("smoker");

			if (!enabled_)
				log_.Info("not a smoker");
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

				case Pull:
				{
					if (elapsed_ > 2)
					{
						elapsed_ = 0;
						state_ = MovingBack;
					}

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
		}

		private void StartMoveToMouth()
		{
			var mouth = person_.Body.Get(BodyParts.Mouth);
			var hand = person_.Body.Get(BodyParts.RightHand);

			startPos_ = hand.Position;
			startRot_ = hand.Rotation;

			targetPos_ =
				mouth.Position +
				mouth.Rotation.Rotate(new Vector3(0, 0, 0.1f));

			targetRot_ = Quaternion.FromEuler(0, 90, 90);

			elapsed_ = 0;
			state_ = MovingToMouth;
		}

		private void MoveToMouth()
		{
			var f = U.Clamp(elapsed_ / MoveTime, 0, 1);
			var p = Vector3.Lerp(startPos_, targetPos_, f);
			var r = Quaternion.Lerp(startRot_, targetRot_, f);

			var hand = person_.Body.Get(BodyParts.RightHand);

			hand.Position = p;
			hand.Rotation = r;

			if (elapsed_ >= MoveTime)
			{
				elapsed_ = 0;
				state_ = Pull;
			}
		}

		private void MoveBack()
		{
			var f = U.Clamp(elapsed_ / MoveTime, 0, 1);
			var p = Vector3.Lerp(targetPos_, startPos_, f);
			var r = Quaternion.Lerp(targetRot_, startRot_, f);

			var hand = person_.Body.Get(BodyParts.RightHand);
			hand.Position = p;
			hand.Rotation = r;

			if (elapsed_ >= MoveTime)
			{
				elapsed_ = 0;
				state_ = Stop;
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

		private void SetPosition()
		{
			var ia = person_.Body.RightHand.Index.Intermediate;
			var ib = person_.Body.RightHand.Middle.Intermediate;
			var ip = ia.Position + (ib.Position - ia.Position) / 2;

			var da = person_.Body.RightHand.Index.Distal;
			var db = person_.Body.RightHand.Middle.Distal;
			var dp = da.Position + (db.Position - da.Position) / 2;

			var p = ip + (dp - ip) / 2;
			var r = person_.Body.RightHand.Middle.Intermediate.Rotation;

			cig_.Position = p + r.Rotate(new Vector3(0, -0.025f, 0));
			cig_.Rotation = r;
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
