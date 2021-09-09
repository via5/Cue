namespace Cue
{
	interface IEvent
	{
		void OnPluginState(bool b);
		void FixedUpdate(float s);
		void Update(float s);
	}


	abstract class BasicEvent : IEvent
	{
		protected Person person_;
		protected Logger log_;

		protected BasicEvent(string name, Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Event, p, "int." + name);
		}

		public static IEvent[] All(Person p)
		{
			return new IEvent[]
			{
				new FingerSuckEvent(p),
				new KissingEvent(p),
				new SmokingEvent(p),
				new SexEvent(p)
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


	class FingerSuckEvent : BasicEvent
	{
		private bool busy_ = false;

		public FingerSuckEvent(Person p)
			: base("fsuck", p)
		{
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


	class SmokingEvent : BasicEvent
	{
		private bool enabled_ = true;

		public SmokingEvent(Person p)
			: base("smoke", p)
		{
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
				if (person_.Animator.CanPlayType(Animation.SmokeType))
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


	class KissingEvent : BasicEvent
	{
		public const float StartDistance = 0.15f;
		public const float StopDistance = 0.1f;
		public const float PlayerStopDistance = 0.15f;
		public const float MinimumActiveTime = 3;
		public const float MinimumStoppedTime = 2;

		private float elapsed_ = 0;

		public KissingEvent(Person p)
			: base("kiss", p)
		{
			person_ = p;
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


	class SexEvent : BasicEvent
	{
		public const int NoState = 0;
		public const int PlayState = 1;

		private Person receiver_ = null;
		private bool active_ = false;
		private bool running_ = false;

		public SexEvent(Person p)
			: base("sex", p)
		{
		}

		public bool Active
		{
			get { return active_; }
			set { active_ = value; }
		}

		public override void Update(float s)
		{
			if (!active_)
			{
				if (running_)
				{
					person_.Animator.StopType(Animation.SexType);
					running_ = false;
				}

				return;
			}

			if (receiver_ == null)
			{
				receiver_ = FindReceiver();
				if (receiver_ == null)
				{
					person_.Log.Error($"cannot start sex, no valid receiver");
					active_ = false;
					return;
				}

				log_.Info($"starting sex, receiver={receiver_.ID}");

				person_.Clothing.GenitalsVisible = true;
				receiver_.Clothing.GenitalsVisible = true;
				person_.Atom.SetBodyDamping(Sys.BodyDamping.Sex);

				if (person_.Animator.CanPlayType(Animation.SexType) && person_.Mood.State == Mood.NormalState)
					person_.Animator.PlaySex(person_.State.Current, receiver_);

				running_ = true;
			}
		}

		private Person FindReceiver()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				if (person_.Body.PenetratedBy(p) || p.Body.PenetratedBy(person_))
					return p;
			}

			return null;
		}
	}
}
