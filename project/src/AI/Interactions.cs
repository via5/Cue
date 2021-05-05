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
		private float elapsed_ = 0;

		public KissingInteraction(Person p)
		{
			person_ = p;
		}

		public void Update(float s)
		{
			if (person_.Body.Lips == null)
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

			var srcLips = person_.Body.Lips.Position;

			for (int i = 0; i < Cue.Instance.Persons.Count; ++i)
			{
				var target = Cue.Instance.Persons[i];
				if (target == person_)
					continue;

				if (target.Body.Lips == null || target.Kisser.Active)
					continue;

				if (!CanStart(target))
					continue;

				// todo: check rotations
				var targetLips = target.Body.Lips.Position;

				if (Vector3.Distance(srcLips, targetLips) < StartDistance)
				{
					Cue.LogInfo($"starting kiss for {person_} and {target}");
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

			if (p.Blowjob.Active)
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

			if (target.Body.Lips == null)
				return false;

			var srcLips = person_.Body.Lips.Position;
			var targetLips = target.Body.Lips.Position;
			var d = Vector3.Distance(srcLips, targetLips);

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
