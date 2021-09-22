using System;

namespace Cue
{
	class SmokeEvent : BasicEvent
	{
		private const float EnableCheckInterval = 2;

		private bool enabled_ = false;
		private IObject cig_ = null;
		private ISmoke smoke_ = null;
		private float checkElapsed_ = 0;
		private Duration wait_;

		public SmokeEvent(Person p)
			: base("smoke", p)
		{
			wait_ = new Duration(15, 40);
		}

		private void CheckEnabled(float s)
		{
			checkElapsed_ += s;
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
			CheckEnabled(s);
			if (!enabled_)
				return;

			wait_.Update(s);

			if (wait_.Finished && CanRun())
			{
				if (person_.Animator.CanPlayType(Animations.Smoke))
				{
					if (cig_ != null)
						cig_.Visible = true;

					person_.Animator.PlayType(Animations.Smoke);
				}
			}


			if (cig_ != null)
			{
				if (!person_.Animator.IsPlayingType(Animations.Smoke))
				{
					if (person_.Body.Get(BP.RightHand).Busy)
						cig_.Visible = false;

					if (cig_.Visible)
						SetCigaretteTransform(person_.Body.RightHand, cig_);
				}
			}
		}

		public static Vector3 MakeCigarettePosition(Hand hand)
		{
			var ia = hand.Index.Intermediate;
			var ib = hand.Middle.Intermediate;
			var ip = ia.Position + (ib.Position - ia.Position) / 2;

			var da = hand.Index.Distal;
			var db = hand.Middle.Distal;
			var dp = da.Position + (db.Position - da.Position) / 2;

			var p = ip + (dp - ip) / 2;
			var r = hand.Middle.Intermediate.Rotation;

			float vertOffset;

			if (hand == hand.Person.Body.RightHand)
				vertOffset = 0.02f;
			else
				vertOffset = -0.01f;

			return p + r.Rotate(new Vector3(vertOffset, -0.025f, 0));
		}

		public static void SetCigaretteTransform(Hand hand, IObject cig)
		{
			var e = hand.Middle.Intermediate.Rotation.Euler;
			var q = Quaternion.FromEuler(e.X, e.Y, e.Z + 10);

			try
			{
				cig.Position = MakeCigarettePosition(hand);
				cig.Rotation = q;
			}
			catch (Exception)
			{
				// eat them
			}
		}

		private bool CanRun()
		{
			var b = person_.Body;
			var head = b.Get(BP.Head);
			var lips = b.Get(BP.Lips);

			bool busy =
				person_.Body.AnyInsidePersonalSpace() ||
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
			cig_.Visible = true;
		}
	}
}
