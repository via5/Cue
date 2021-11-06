using System;

namespace Cue
{
	class KissEvent : BasicEvent
	{
		public const float StartDistance = 0.15f;
		public const float StopDistance = 0.1f;
		public const float PlayerStopDistance = 0.15f;
		public const float MinimumActiveTime = 3;
		public const float MinWait = 2;
		public const float MaxWait = 120;
		public const float MinDuration = 2;
		public const float MaxDuration = 120;
		public const float WaitWhenFailed = 1;

		private float elapsed_ = 0;
		private float wait_ = 0;
		private float duration_ = 0;
		private string lastResult_ = "";
		private BodyPartLock[] locks_ = null;
		private Person target_ = null;
		private bool wasGrabbed_ = false;

		public KissEvent(Person p)
			: base("kiss", p)
		{
			person_ = p;
			Next();
		}

		public bool Active
		{
			get { return target_ != null; }
		}

		public Person Target
		{
			get { return target_; }
		}

		public override string[] Debug()
		{
			return new string[]
			{
				$"elapsed    {elapsed_:0.00}",
				$"wait       {wait_:0.00}",
				$"duration   {duration_:0.00}",
				$"state      {(Active ? "active" : "waiting")}",
				$"last       {lastResult_}"
			};
		}

		public override void Update(float s)
		{
			if (!person_.Body.Exists)
				return;

			if (!person_.Options.CanKiss)
			{
				elapsed_ = 0;
				if (Active)
					Stop();

				return;
			}


			elapsed_ += s;

			if (Active)
			{
				if (elapsed_ >= MinWait)
					UpdateActive();
			}
			else
			{
				if (elapsed_ >= MinWait)
					UpdateInactive();
			}
		}

		private void UpdateActive()
		{
			bool tooLong = false;

			// never stop with player
			if (target_ == null || !target_.IsPlayer)
				tooLong = (elapsed_ >= duration_);

			if (tooLong || MustStop())
				Stop();
		}

		private void UpdateInactive()
		{
			if (TryStart(true))
			{
				Next();
				return;
			}

			var elapsed = (elapsed_ > wait_);
			var grabbed = person_.Body.Get(BP.Head).GrabbedByPlayer;

			if (elapsed || (wasGrabbed_ && !grabbed))
			{
				lastResult_ = "";

				if (SelfCanStart(person_))
				{
					Next();
					if (!TryStart(false))
						wait_ = WaitWhenFailed;
				}
			}

			wasGrabbed_ = grabbed;
		}

		private void Stop()
		{
			if (target_ != null)
				target_.AI.GetEvent<KissEvent>().StopSelf();

			StopSelf();
		}

		private void StopSelf()
		{
			Unlock();
			Next();
			person_.Animator.StopType(Animations.Kiss);
			target_ = null;
		}

		private void Next()
		{
			wait_ = U.RandomGaussian(MinWait, MaxWait);
			duration_ = U.RandomGaussian(MinDuration, MaxDuration);
			elapsed_ = 0;
		}

		private bool TryStart(bool playerOnly)
		{
			var srcLips = person_.Body.Get(BP.Lips).Position;
			bool foundClose = false;

			foreach (var target in Cue.Instance.ActivePersons)
			{
				if (target == person_)
					continue;

				if (playerOnly && !target.IsPlayer)
					continue;

				var targetLips = target.Body.Get(BP.Lips);
				if (targetLips == null)
					return false;

				var d = Vector3.Distance(srcLips, targetLips.Position);

				if (d <= StartDistance)
				{
					foundClose = true;

					if (TryStartWith(target))
						return true;

					Unlock();
				}
			}

			if (!foundClose)
				lastResult_ = "nobody in range";

			return false;
		}

		private bool TryStartWith(Person target)
		{
			log_.Verbose($"starting for {person_} and {target}");

			if (!Lock())
			{
				lastResult_ = "self lock failed";
				return false;
			}

			var sf = target.AI.GetEvent<KissEvent>().TryStartFrom(person_);
			if (sf != "")
			{
				lastResult_ = $"other failed to start: " + sf;
				return false;
			}

			if (!person_.Animator.PlayType(
				Animations.Kiss, new AnimationContext(target, locks_[0].Key)))
			{
				lastResult_ = $"kiss animation failed to start";
				target.AI.GetEvent<KissEvent>().Unlock();
				return false;
			}

			if (!target.AI.GetEvent<KissEvent>().StartedFrom(person_))
			{
				lastResult_ = $"kiss animation failed to start anim";
				person_.Animator.StopType(Animations.Kiss);
				target.AI.GetEvent<KissEvent>().Unlock();
				return false;
			}

			target_ = target;

			return true;
		}

		private string TryStartFrom(Person initiator)
		{
			if (!person_.Options.CanKiss)
				return $"target {person_.ID} kissing disabled";

			if (target_ != null)
				return $"target {person_.ID} kissing already active";

			if (!Lock())
				return $"target {person_.ID} lock failed";

			return "";
		}

		private bool StartedFrom(Person initiator)
		{
			// animations will always fail on player
			if (!person_.IsPlayer)
			{
				if (!person_.Animator.PlayType(
					Animations.Kiss, new AnimationContext(
						initiator, locks_[0].Key)))
				{
					lastResult_ = $"kiss animation failed to start";
					return false;
				}
			}

			target_ = initiator;
			Next();

			return true;
		}

		private bool Lock()
		{
			Unlock();
			locks_ = person_.Body.LockMany(
				new int[] { BP.Head, BP.Lips, BP.Mouth },
				BodyPartLock.Anim, "kiss");

			return (locks_ != null);
		}

		private void Unlock()
		{
			if (locks_ != null)
			{
				for (int i = 0; i < locks_.Length; ++i)
					locks_[i].Unlock();

				locks_ = null;
			}
		}

		private bool SelfCanStart(Person p)
		{
			if (!p.Options.CanKiss)
			{
				lastResult_ = "kissing disabled";
				return false;
			}

			if (Active)
			{
				lastResult_ = "kissing already active";
				return false;
			}

			return true;
		}

		private bool MustStop()
		{
			// animations are never playing on player
			if (!person_.IsPlayer)
			{
				if (!person_.Animator.IsPlayingType(Animations.Kiss))
				{
					log_.Info("must stop: animation is not playing");
					return true;
				}
			}

			var srcLips = person_.Body.Get(BP.Lips);
			var targetLips = target_.Body.Get(BP.Lips);

			if (srcLips == null || targetLips == null)
			{
				log_.Info("must stop: no lips");
				return true;
			}

			var d = Vector3.Distance(srcLips.Position, targetLips.Position);
			var hasPlayer = (person_.IsPlayer || target_.IsPlayer);
			var sd = (hasPlayer ? PlayerStopDistance : StopDistance);

			if (d >= sd)
			{
				log_.Info($"must stop: too far, {d}");
				return true;
			}

			return false;
		}
	}
}
