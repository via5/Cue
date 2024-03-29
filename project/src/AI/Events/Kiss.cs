﻿using SimpleJSON;

namespace Cue
{
	class KissEventData : BasicEventData
	{
		public bool canInitiate = true;
		public float startDistance = 0;
		public float startDistanceWithPlayer = 0;
		public float stopDistance = 0;
		public float stopDistanceWithPlayer = 0;
		public float stopHeadDistance = 0;
		public Duration duration = null;
		public Duration wait = null;


		public override BasicEventData Clone()
		{
			var d = new KissEventData();
			d.CopyFrom(this);
			return d;
		}

		private void CopyFrom(KissEventData d)
		{
			base.CopyFrom(d);
			canInitiate = d.canInitiate;
			startDistance = d.startDistance;
			startDistanceWithPlayer = d.startDistanceWithPlayer;
			stopDistance = d.stopDistance;
			stopDistanceWithPlayer = d.stopDistanceWithPlayer;
			stopHeadDistance = d.stopHeadDistance;
			duration = d.duration?.Clone();
			wait = d.wait?.Clone();
		}
	}


	class KissEvent : BasicEvent<KissEventData>
	{
		private const float MinWait = 2;
		private const float MinDurationAfterGrab = 30;

		private string lastResult_ = "";
		private string lastPlayerResult_ = "";
		private BodyPartLock[] locks_ = null;
		private Person target_ = null;
		private bool leading_ = false;
		private bool wasGrabbed_ = false;

		private float elapsed_ = MinWait;
		private float minDuration_ = 0;
		private bool durationFinished_ = false;
		private bool waitFinished_ = false;
		private bool waitFinishedBecauseGrab_ = false;
		private Vector3 startingHeadPos_;
		private bool debugEnabled_ = false;

		public KissEvent()
			: base("Kiss")
		{
		}

		protected override void DoParseEventData(JSONClass o, KissEventData d)
		{
			d.canInitiate = J.ReqBool(o, "canInitiate");
			d.startDistance = J.ReqFloat(o, "startLipsDistance");
			d.startDistanceWithPlayer = J.ReqFloat(o, "startLipsDistanceWithPlayer");
			d.stopDistance = J.ReqFloat(o, "stopLipsDistance");
			d.stopDistanceWithPlayer = J.ReqFloat(o, "stopLipsDistanceWithPlayer");
			d.stopHeadDistance = J.ReqFloat(o, "stopHeadDistanceFromStart");
			d.duration = Duration.FromJSON(o, "duration", true);
			d.wait = Duration.FromJSON(o, "interval", true);

			if (d.duration.Minimum < MinWait)
			{
				Logger.Global.Warning(
					$"kiss: duration minimum below hard minimum of " +
					$"{MinWait}, overriding");

				d.duration.SetRange(MinWait, d.duration.Maximum);
			}

			if (d.wait.Minimum < MinWait)
			{
				Logger.Global.Warning(
					$"kiss: wait minimum below hard minimum of " +
					$"{MinWait}, overriding");

				d.wait.SetRange(MinWait, d.wait.Maximum);
			}
		}

		protected override void DoInit()
		{
			Next();
		}

		public override bool Active
		{
			get
			{
				return target_ != null;
			}

			set
			{
				if (Active && !value)
					Stop();
			}
		}

		public override bool CanToggle { get { return false; } }
		public override bool CanDisable { get { return true; } }

		public bool Leading
		{
			get { return leading_; }
		}

		public Person Target
		{
			get { return target_; }
		}

		protected override void DoDebug(DebugLines debug)
		{
			debugEnabled_ = true;

			var d = Data;

			debug.Add("canInitiate", $"{d.canInitiate}");
			debug.Add("startDistance", $"{d.startDistance:0.00}");
			debug.Add("startDistanceWithPlayer", $"{d.startDistanceWithPlayer:0.00}");
			debug.Add("stopDistance", $"{d.stopDistance:0.00}");
			debug.Add("stopDistanceWithPlayer", $"{d.stopDistanceWithPlayer:0.00}");
			debug.Add("stopHeadDistance", $"{d.stopHeadDistance:0.00}");
			debug.Add("duration", $"{d.duration.ToLiveString()}");
			debug.Add("wait", $"{d.wait.ToLiveString()}");
			debug.Add("state", $"{(Active ? "active" : "waiting")}");
			debug.Add("durationFinished", $"{durationFinished_}");
			debug.Add("waitFinished", $"{waitFinished_}");
			debug.Add("elapsed", $"{elapsed_:0.00}");
			debug.Add("last", $"{lastResult_}");
			debug.Add("last for player", $"{lastPlayerResult_}");
			debug.Add("minDuration", $"{minDuration_}");
			debug.Add("waitFinishedBecauseGrab", $"{waitFinishedBecauseGrab_}");
		}

		protected override void DoUpdate(float s)
		{
			if (person_.Body.Exists)
			{
				if (Enabled)
				{
					elapsed_ += s;

					if (Active)
					{
						Data.duration.Update(s, Mood.MultiMovementEnergy(person_, target_));
						if (Data.duration.Finished)
							durationFinished_ = true;

						if (elapsed_ >= MinWait)
							UpdateActive();
					}
					else
					{
						Data.wait.Update(s, person_.Mood.MovementEnergy);
						if (Data.wait.Finished)
							waitFinished_ = true;

						if (elapsed_ >= MinWait)
							UpdateInactive();
					}
				}
				else
				{
					if (Active)
						Stop();

					return;
				}
			}

			debugEnabled_ = false;
		}

		protected override void DoForceStop()
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
			if (SelfCanStart() && TryStartWithPlayer())
			{
				Next();
				return;
			}

			if (waitFinished_)
			{
				lastResult_ = "";

				if (SelfCanStart())
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
			person_.Animator.StopType(AnimationType.Kiss);
			person_.Atom.SetCollidersForKiss(false, target_.Atom);
			target_ = null;
			leading_ = false;

			person_.Options.GetAnimationOption(AnimationType.Kiss).Trigger(false);
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

			Data.duration.Reset(1);
			Data.wait.Reset(1);
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
					startDistance = Data.startDistanceWithPlayer;
				else
					startDistance = Data.startDistance;

				if (playerOnly && debugEnabled_)
				{
					lastPlayerResult_ =
						$"srcLips={srcLips} pLips={targetLips.Position} " +
						$"d={d} sd={startDistance}";
				}

				if (d > startDistance)
				{
					if (playerOnly && debugEnabled_)
						lastPlayerResult_ += ", too far";
				}
				else
				{
					foundClose = true;

					if (TryStartWith(target))
					{
						if (playerOnly && debugEnabled_)
							lastPlayerResult_ += ", ok";

						person_.Atom.SetCollidersForKiss(true, target.Atom);
						target.Atom.SetCollidersForKiss(true, person_.Atom);

						leading_ = true;
						startingHeadPos_ = person_.Body.Get(BP.Head).Position;

						person_.Options.GetAnimationOption(AnimationType.Kiss).Trigger(true);
						target_.Options.GetAnimationOption(AnimationType.Kiss).Trigger(true);

						return true;
					}
					else
					{
						if (playerOnly && debugEnabled_)
							lastPlayerResult_ += ", trystart failed, " + lastResult_;
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

			if (person_.Options.GetAnimationOption(AnimationType.Kiss).Play)
			{
				if (!person_.Animator.PlayType(
					AnimationType.Kiss, new AnimationContext(target, locks_[0].Key)))
				{
					elapsed_ = 0;
					lastResult_ = $"kiss animation failed to start";
					target.AI.GetEvent<KissEvent>().Unlock();
					return false;
				}
			}

			if (!target.AI.GetEvent<KissEvent>().StartedFrom(person_, minDuration_))
			{
				lastResult_ = $"kiss animation failed startfrom";
				person_.Animator.StopType(AnimationType.Kiss);
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
			if (!Enabled)
				return $"target {person_.ID} kissing disabled";

			if (target_ != null)
				return $"target {person_.ID} kissing already active";

			if (!Lock())
				return $"target {person_.ID} lock failed";

			return "";
		}

		private bool StartedFrom(Person initiator, float minDuration)
		{
			if (person_.Options.GetAnimationOption(AnimationType.Kiss).Play)
			{
				if (!person_.Animator.PlayType(
					AnimationType.Kiss, new AnimationContext(
						initiator, locks_[0].Key)))
				{
					// animations can fail on player
					if (!person_.IsPlayer)
					{
						lastResult_ = $"kiss animation failed to start";
						return false;
					}
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
				new BodyPartType[] { BP.Head, BP.Lips, BP.Mouth, BP.Chest },
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

		private bool SelfCanStart()
		{
			if (!Enabled)
			{
				lastResult_ = "kissing disabled";
				return false;
			}

			if (Active)
			{
				lastResult_ = "kissing already active";
				return false;
			}

			if (!Data.canInitiate)
			{
				lastResult_ = "cannot initiate";
				return false;
			}

			return true;
		}

		private bool MustStop()
		{
			if (!person_.Animator.IsPlayingType(AnimationType.Kiss))
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
			var sd = (hasPlayer ? Data.stopDistanceWithPlayer : Data.stopDistance);

			if (d >= sd)
			{
				Log.Info($"must stop: lips too far, {d}");
				return true;
			}

			if (leading_ && target_.IsPlayer)
			{
				var hd = Vector3.Distance(person_.Body.Get(BP.Head).Position, startingHeadPos_);
				if (hd >= Data.stopHeadDistance)
				{
					Log.Info($"must stop: head to far, {hd}");
					return true;
				}
			}

			return false;
		}
	}
}
