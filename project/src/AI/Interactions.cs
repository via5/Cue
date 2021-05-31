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
		private Person person_;
		private Logger log_;
		private float elapsed_ = 0;
		private IObject cig_ = null;
		private bool inited_ = false;
		private bool enabled_ = true;

		public SmokingInteraction(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Interaction, p, "SmokeInt");
			enabled_ = p.Smoker;

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
				log_.Info("initing");
				inited_ = true;
				elapsed_ = 0;
				enabled_ = true;
				CreateCigarette();
			}
			else if (cig_ != null)
			{
				var ia = person_.Body.RightHand.Index.Intermediate;
				var ib = person_.Body.RightHand.Middle.Intermediate;
				var ip = ia.Position + (ib.Position - ia.Position) / 2;

				var da = person_.Body.RightHand.Index.Distal;
				var db = person_.Body.RightHand.Middle.Distal;
				var dp = da.Position + (db.Position - da.Position) / 2;

				var p = ip + (dp - ip) / 2;
				var r = person_.Body.RightHand.Middle.Intermediate.Rotation;

				cig_.Position = p + Vector3.RotateEuler(new Vector3(0, -0.025f, 0), r);
				cig_.Rotation = r;
			}
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

			Cue.Instance.Sys.CreateObject(ObjectFactory.Cigarette, CigaretteID, (o) =>
			{
				SetCigarette(o);
			});
		}

		private void SetCigarette(W.IAtom a)
		{
			if (a == null)
			{
				log_.Error("failed to create cigarette, disabling");
				enabled_ = false;
				return;
			}

			cig_ = new BasicObject(-1, a);

			a.Collisions = false;
			a.Physics = false;
			a.Hidden = true;

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
