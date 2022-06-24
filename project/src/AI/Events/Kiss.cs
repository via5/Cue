using SimpleJSON;
using System;

namespace Cue
{
	public interface IEventData
	{
		IEventData Clone();
	}


	class KissEventData : IEventData
	{
		public float startDistance;
		public float startDistanceWithPlayer;
		public float stopDistance;
		public float stopDistanceWithPlayer;
		public Duration duration;
		public Duration wait;


		public IEventData Clone()
		{
			var d = new KissEventData();
			d.CopyFrom(this);
			return d;
		}

		private void CopyFrom(KissEventData d)
		{
			startDistance = d.startDistance;
			startDistanceWithPlayer = d.startDistanceWithPlayer;
			stopDistance = d.stopDistance;
			stopDistanceWithPlayer = d.stopDistanceWithPlayer;
			duration = d.duration.Clone();
			wait = d.wait.Clone();
		}
	}


	class KissEvent : BasicEvent
	{
		private const float MinWait = 2;
		private const float MinDurationAfterGrab = 30;

		private string lastResult_ = "";
		private BodyPartLock[] locks_ = null;
		private Person target_ = null;
		private bool leading_ = false;
		private bool wasGrabbed_ = false;
		private KissEventData d_ = null;

		private float elapsed_ = MinWait;
		private float minDuration_ = 0;
		private bool durationFinished_ = false;
		private bool waitFinished_ = false;
		private bool waitFinishedBecauseGrab_ = false;


		public KissEvent()
			: base("kiss")
		{
		}

		protected override IEventData DoParseEventData(JSONClass o)
		{
			var d = new KissEventData();

			d.startDistance = J.ReqFloat(o, "startDistance");
			d.startDistanceWithPlayer = J.ReqFloat(o, "startDistanceWithPlayer");
			d.stopDistance = J.ReqFloat(o, "stopDistance");
			d.stopDistanceWithPlayer = J.ReqFloat(o, "stopDistanceWithPlayer");
			d.duration = Duration.FromJSON(o, "duration", true);
			d.wait = Duration.FromJSON(o, "interval", true);

			if (d.duration.Minimum < MinWait)
			{
				Cue.LogWarning(
					$"kiss: duration minimum below hard minimum of " +
					$"{MinWait}, overriding");

				d.duration.SetRange(MinWait, d.duration.Maximum);
			}

			if (d.wait.Minimum < MinWait)
			{
				Cue.LogWarning(
					$"kiss: wait minimum below hard minimum of " +
					$"{MinWait}, overriding");

				d.wait.SetRange(MinWait, d.wait.Maximum);
			}

			return d;
		}

		protected override void DoInit()
		{
			OnPersonalityChanged();
			person_.PersonalityChanged += OnPersonalityChanged;
			Next();
		}

		private void OnPersonalityChanged()
		{
			d_ = person_.Personality.CloneEventData(Name) as KissEventData;
		}

		public bool Active
		{
			get { return target_ != null; }
		}

		public bool Leading
		{
			get { return leading_; }
		}

		public Person Target
		{
			get { return target_; }
		}

		public override void Debug(DebugLines debug)
		{
			debug.Add("startDistance", $"{d_.startDistance:0.00}");
			debug.Add("startDistanceWithPlayer", $"{d_.startDistanceWithPlayer:0.00}");
			debug.Add("stopDistance", $"{d_.stopDistance:0.00}");
			debug.Add("stopDistanceWithPlayer", $"{d_.stopDistanceWithPlayer:0.00}");
			debug.Add("duration", $"{d_.duration.ToLiveString()}");
			debug.Add("wait", $"{d_.wait.ToLiveString()}");
			debug.Add("state", $"{(Active ? "active" : "waiting")}");
			debug.Add("durationFinished", $"{durationFinished_}");
			debug.Add("waitFinished", $"{waitFinished_}");
			debug.Add("elapsed", $"{elapsed_:0.00}");
			debug.Add("last", $"{lastResult_}");
			debug.Add("minDuration", $"{minDuration_}");
			debug.Add("waitFinishedBecauseGrab", $"{waitFinishedBecauseGrab_}");
		}

		public override void Update(float s)
		{
			if (!person_.Body.Exists)
				return;

			if (!person_.Options.CanKiss)
			{
				if (Active)
					Stop();

				return;
			}

			elapsed_ += s;

			if (Active)
			{
				d_.duration.Update(s, Mood.MultiMovementEnergy(person_, target_));
				if (d_.duration.Finished)
					durationFinished_ = true;

				if (elapsed_ >= MinWait)
					UpdateActive();
			}
			else
			{
				d_.wait.Update(s, person_.Mood.MovementEnergy);
				if (d_.wait.Finished)
					waitFinished_ = true;

				if (elapsed_ >= MinWait)
					UpdateInactive();
			}
		}

		public override void ForceStop()
		{
			if (Active)
				Stop();
		}

		private void UpdateActive()
		{
			bool tooLong = false;

			// never stop with player
			if (target_ == null || !target_.IsPlayer)
				tooLong = (durationFinished_ && (elapsed_ >= minDuration_));

			if (tooLong || MustStop())
				Stop();
		}

		private void UpdateInactive()
		{
			if (TryStartWithPlayer())
			{
				Next();
				return;
			}

			if (waitFinished_)
			{
				lastResult_ = "";

				if (SelfCanStart(person_))
				{
					Next();
					if (!TryStartWithAnyone())
						elapsed_ = 0;

					//Log.Info(lastResult_);
				}
			}

			var grabbed = person_.Body.Get(BP.Head).GrabbedByPlayer;
			if (wasGrabbed_ && !grabbed)
			{
				waitFinished_ = true;
				waitFinishedBecauseGrab_ = true;
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
			SetExcitement(false);
			person_.Animator.StopType(Animations.Kiss);
			target_ = null;
			leading_ = false;
		}

		private void Next()
		{
			if (waitFinishedBecauseGrab_)
				minDuration_ = MinDurationAfterGrab;
			else
				minDuration_ = 0;

			elapsed_ = 0;
			durationFinished_ = false;
			waitFinished_ = false;
			waitFinishedBecauseGrab_ = false;

			d_.duration.Reset(1);
			d_.wait.Reset(1);
		}

		private bool TryStartWithPlayer()
		{
			return TryStart(true);
		}

		private bool TryStartWithAnyone()
		{
			return TryStart(false);
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
				{
					lastResult_ = "no lips";
					return false;
				}

				var d = Vector3.Distance(srcLips, targetLips.Position);

				float startDistance;

				if (target.IsPlayer)
					startDistance = d_.startDistanceWithPlayer;
				else
					startDistance = d_.startDistance;

				if (d <= startDistance)
				{
					foundClose = true;

					if (TryStartWith(target))
					{
						leading_ = true;
						return true;
					}

					Unlock();
				}
			}

			if (!foundClose)
				lastResult_ = "nobody in range";

			return false;
		}

		private bool TryStartWith(Person target)
		{
			Log.Verbose($"try start for {person_} and {target}");

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

			if (!target.AI.GetEvent<KissEvent>().StartedFrom(person_, minDuration_))
			{
				lastResult_ = $"kiss animation failed startfrom";
				person_.Animator.StopType(Animations.Kiss);
				target.AI.GetEvent<KissEvent>().Unlock();
				return false;
			}

			target_ = target;
			leading_ = false;
			SetExcitement(true);

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

		private bool StartedFrom(Person initiator, float minDuration)
		{
			if (!person_.Animator.PlayType(
				Animations.Kiss, new AnimationContext(
					initiator, locks_[0].Key)))
			{
				// animations can fail on player
				if (!person_.IsPlayer)
				{
					lastResult_ = $"kiss animation failed to start";
					return false;
				}
			}

			target_ = initiator;
			Next();
			SetExcitement(true);
			minDuration_ = minDuration;

			return true;
		}

		private void SetExcitement(bool b)
		{
			if (b)
			{
				person_.Excitement.GetSource(SS.Mouth).AddEnabledFor(target_);

				if (target_ != null)
					person_.Body.Get(BP.Mouth).AddForcedTrigger(target_.PersonIndex, BP.Mouth);
			}
			else
			{
				person_.Excitement.GetSource(SS.Mouth).RemoveEnabledFor(target_);

				if (target_ != null)
					person_.Body.Get(BP.Mouth).RemoveForcedTrigger(target_.PersonIndex, BP.Mouth);
			}
		}

		private bool Lock()
		{
			Unlock();
			locks_ = BodyPartLock.LockMany(
				person_,
				new int[] { BP.Head, BP.Lips, BP.Mouth, BP.Chest },
				BodyPartLock.Anim, "kiss", BodyPartLock.Strong);

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
			if (!person_.Animator.IsPlayingType(Animations.Kiss))
			{
				// not all animations can run on player
				if (!person_.IsPlayer)
				{
					Log.Info("must stop: animation is not playing");
					return true;
				}
			}

			var srcLips = person_.Body.Get(BP.Lips);
			var targetLips = target_.Body.Get(BP.Lips);

			if (srcLips == null || targetLips == null)
			{
				Log.Info("must stop: no lips");
				return true;
			}

			var d = Vector3.Distance(srcLips.Position, targetLips.Position);
			var hasPlayer = (person_.IsPlayer || target_.IsPlayer);
			var sd = (hasPlayer ? d_.stopDistanceWithPlayer : d_.stopDistance);

			if (d >= sd)
			{
				Log.Info($"must stop: too far, {d}");
				return true;
			}

			return false;
		}
	}
}
