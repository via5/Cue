namespace Cue
{
	interface IInteraction
	{
		void OnPluginState(bool b);
		void FixedUpdate(float s);
		void Update(float s);
	}


	abstract class BasicInteraction : IInteraction
	{
		public static IInteraction[] All(Person p)
		{
			return new IInteraction[]
			{
				new FingerSuckInteraction(p),
				new KissingInteraction(p),
				new SmokingInteraction(p)
			};
		}

		public virtual void OnPluginState(bool b)
		{
			// no-op
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


	class FingerSuckInteraction : BasicInteraction
	{
		private Person person_;
		private Logger log_;
		private bool busy_ = false;

		public FingerSuckInteraction(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Interaction, p, "FSuckInt");
		}

		public override void Update(float s)
		{
			var mouthTriggered = person_.Body.Get(BodyParts.Mouth).Triggered;
			var head = person_.Body.Get(BodyParts.Head);

			if (!busy_ && mouthTriggered && !head.Busy)
			{
				busy_ = true;
				head.ForceBusy(true);
				person_.Animator.PlayType(Animation.SuckType, Animator.Loop);
			}
			else if (busy_ && !mouthTriggered)
			{
				busy_ = false;
				head.ForceBusy(false);
				person_.Animator.StopType(Animation.SuckType);
			}
		}
	}


	class SmokingInteraction : BasicInteraction
	{
		private Person person_;
		private Logger log_;
		private bool enabled_ = true;

		public SmokingInteraction(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Interaction, p, "SmokeInt");
			enabled_ = p.HasTrait("smoker");

			if (!enabled_)
			{
				//log_.Info("not a smoker");
				return;
			}
		}

		public override void FixedUpdate(float s)
		{
			if (!enabled_)
				return;

			if (CanRun())
			{
				if (!person_.Animator.Playing)
					person_.Animator.PlayType(Animation.SmokeType);
			}
		}

		private bool CanRun()
		{
			var b = person_.Body;
			var head = b.Get(BodyParts.Head);
			var lips = b.Get(BodyParts.Lips);

			bool busy =
				person_.Body.Get(BodyParts.RightHand).Busy ||
				head.Busy || head.Triggered ||
				lips.Busy || lips.Triggered;

			if (busy)
				return false;

			if (b.GropedByAny(BodyParts.Head))
				return false;

			return true;
		}
	}


	class KissingInteraction : BasicInteraction
	{
		public const float StartDistance = 0.15f;
		public const float StopDistance = 0.1f;
		public const float PlayerStopDistance = 0.15f;
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

			foreach (var target in Cue.Instance.ActivePersons)
			{
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
