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
		public const float MaxWait = 40;
		public const float MinDuration = 2;
		public const float MaxDuration = 30;

		private float elapsed_ = 0;
		private float wait_ = 0;
		private float duration_ = 0;
		private string lastResult_ = "";

		public KissEvent(Person p)
			: base("kiss", p)
		{
			person_ = p;
			Next();
		}

		public override string[] Debug()
		{
			return new string[]
			{
				$"elapsed    {elapsed_:0.00}",
				$"wait       {wait_:0.00}",
				$"duration   {duration_:0.00}",
				$"state      {(person_.Kisser.Active ? "active" : "waiting")}",
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
				if (person_.Kisser.Active)
					person_.Kisser.Stop();

				return;
			}


			elapsed_ += s;

			if (person_.Kisser.Active)
			{
				if (elapsed_ >= duration_ || MustStop())
				{
					Next();

					var t = person_.Kisser.Target;
					if (t != null)
						t.AI.GetEvent<KissEvent>().Next();

					person_.Kisser.Stop();
				}
			}
			else
			{
				// todo: this always starts when a head is grabbed, which is
				//       probably fine if the target is the player, but it makes
				//       any head adjustments start kissing if there's a target
				//       available
				//
				//       maybe keep the delay if the player is not in range?

				if (elapsed_ > wait_ || person_.Body.Get(BP.Head).GrabbedByPlayer)
				{
					Next();
					TryStart();
				}
			}
		}

		private void Next()
		{
			wait_ = U.RandomGaussian(MinWait, MaxWait);
			duration_ = U.RandomGaussian(MinDuration, MaxDuration);
			elapsed_ = 0;
		}

		private bool TryStart()
		{
			lastResult_ = "";

			if (!SelfCanStart(person_))
				return false;

			var srcLips = person_.Body.Get(BP.Lips).Position;
			bool foundClose = false;

			foreach (var target in Cue.Instance.ActivePersons)
			{
				if (target == person_)
					continue;

				var targetLips = target.Body.Get(BP.Lips);
				if (targetLips == null)
					continue;

				if (Vector3.Distance(srcLips, targetLips.Position) < StartDistance)
				{
					foundClose = true;

					if (!OtherCanStart(target))
						continue;

					log_.Verbose($"starting for {person_} and {target}");

					if (person_.Kisser.StartReciprocal(target))
					{
						target.AI.GetEvent<KissEvent>().Next();
						return true;
					}
					else
					{
						log_.Error($"kisser failed to start");
					}
				}
			}

			if (!foundClose)
				lastResult_ = "nobody in range";

			return false;
		}

		private bool SelfCanStart(Person p)
		{
			if (!p.Options.CanKiss)
			{
				lastResult_ = "kissing disabled";
				return false;
			}

			if (!p.CanMoveHead)
			{
				lastResult_ = "can't move head";
				return false;
			}

			if (p.Kisser.Active)
			{
				lastResult_ = "kissing already active";
				return false;
			}

			return true;
		}

		private bool OtherCanStart(Person p)
		{
			if (!p.Options.CanKiss)
			{
				if (lastResult_ != "")
					lastResult_ += ",";
				lastResult_ += $"{p.ID} kissing disabled";

				return false;
			}

			if (!p.CanMoveHead)
			{
				if (lastResult_ != "")
					lastResult_ += ",";
				lastResult_ += $"{p.ID} can't move head";

				return false;
			}

			if (p.Kisser.Active)
			{
				if (lastResult_ != "")
					lastResult_ += ",";
				lastResult_ += $"{p.ID} kissing already active";

				return false;
			}

			return true;
		}

		private bool MustStop()
		{
			var target = person_.Kisser.Target;
			if (target == null)
				return false;

			var srcLips = person_.Body.Get(BP.Lips);
			var targetLips = target.Body.Get(BP.Lips);

			if (srcLips == null || targetLips == null)
				return false;

			var d = Vector3.Distance(srcLips.Position, targetLips.Position);

			var hasPlayer = (
				person_ == Cue.Instance.Player ||
				target == Cue.Instance.Player);

			var sd = (hasPlayer ? PlayerStopDistance : StopDistance);

			return (d >= sd);
		}
	}
}
