namespace Cue
{
	class SmokeEvent : BasicEvent
	{
		private const float EnableCheckInterval = 2;

		private bool enabled_ = false;
		private IObject cig_ = null;
		private ISmoke smoke_ = null;
		private float checkElapsed_ = EnableCheckInterval;

		public SmokeEvent(Person p)
			: base("smoke", p)
		{
			CheckEnabled();
		}

		private void CheckEnabled()
		{
			if (checkElapsed_ < EnableCheckInterval)
				return;

			checkElapsed_ = 0;

			bool e = person_.HasTrait("smoker");

			if (!enabled_ && e)
				Start();
			else if (!e)
				Destroy();

			enabled_ = e;
		}

		private void Start()
		{
			CreateCigarette();
			smoke_ = Integration.CreateSmoke(SmokeID);
		}

		private void Destroy()
		{
			if (cig_?.Atom != null)
			{
				cig_.Destroy();
				cig_ = null;
			}

			if (smoke_ != null)
			{
				smoke_.Destroy();
				smoke_ = null;
			}

			// cleanup leftovers
			var a = Cue.Instance.Sys.GetAtom(CigaretteID);
			if (a != null)
				a.Destroy();

			a = Cue.Instance.Sys.GetAtom(SmokeID);
			if (a != null)
				a.Destroy();
		}

		public static string MakeCigaretteID(Person p)
		{
			return $"cue!{p.ID}_cigarette";
		}

		public static string MakeSmokeID(Person p)
		{
			return $"cue!{p.ID}_cigarette_smoke";
		}

		private string CigaretteID
		{
			get { return MakeCigaretteID(person_); }
		}

		private string SmokeID
		{
			get { return MakeSmokeID(person_); }
		}

		public override void Update(float s)
		{
			checkElapsed_ += s;
			CheckEnabled();

			if (!enabled_)
				return;

			if (CanRun())
			{
				if (person_.Animator.CanPlayType(Animations.Smoke))
					person_.Animator.PlayType(Animations.Smoke);
			}
		}

		private bool CanRun()
		{
			var b = person_.Body;
			var head = b.Get(BP.Head);
			var lips = b.Get(BP.Lips);

			bool busy =
				person_.Body.Get(BP.RightHand).Busy ||
				head.Busy || head.Triggered ||
				lips.Busy || lips.Triggered;

			if (busy)
				return false;

			if (b.GropedByAny(BP.Head))
				return false;

			return true;
		}

		private void CreateCigarette()
		{
			var a = Cue.Instance.Sys.GetAtom(CigaretteID);

			if (a != null)
			{
				person_.Log.Info("cig already exists, taking");
				SetCigarette(new BasicObject(-1, a));
			}
			else
			{
				person_.Log.Info("creating cigarette");

				var oc = Resources.Objects.Get("cigarette");
				if (oc == null)
				{
					person_.Log.Error("no cigarette object creator");
					return;
				}

				oc.Create(CigaretteID, (o) =>
				{
					if (o == null)
					{
						person_.Log.Error("failed to create cigarette");
						return;
					}

					SetCigarette(o);
				});
			}
		}

		private void SetCigarette(IObject o)
		{
			cig_ = o;
			cig_.Atom.Collisions = false;
			cig_.Atom.Physics = false;
			cig_.Atom.Hidden = true;
		}
	}
}
