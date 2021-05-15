namespace Cue
{
	interface IInteraction
	{
		void Update(float s);
	}


	class KissingInteraction : IInteraction
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

		public void Update(float s)
		{
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
