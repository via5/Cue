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
		private bool cleanedUp_ = false;

		public SmokeEvent()
			: base("Smoke")
		{
			wait_ = new Duration(15, 40);
		}

		public override bool Active
		{
			get { return false; }
			set { }
		}

		public override bool CanToggle { get { return false; } }
		public override bool CanDisable { get { return false; } }

		public override void Debug(DebugLines debug)
		{
			string canRunReason = "";
			string canRun = "";

			if (!CanRun(ref canRunReason))
				canRun = "no: " + canRunReason;
			else
				canRun = "yes";

			debug.Add("enabled", $"{enabled_}");
			debug.Add("cig", $"{cig_}");
			debug.Add("smoke", $"{smoke_}");
			debug.Add("checkElapsed", $"{checkElapsed_:0.00}");
			debug.Add("wait", $"{wait_.ToLiveString()}");
			debug.Add("canRun", $"{canRun}");
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

			if (!cleanedUp_)
			{
				// cleanup leftovers
				var a = Cue.Instance.Sys.GetAtom(CigaretteID);
				if (a != null)
					a.Destroy();

				a = Cue.Instance.Sys.GetAtom(SmokeID);
				if (a != null)
					a.Destroy();

				cleanedUp_ = true;
			}
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

		protected override void DoUpdate(float s)
		{
			CheckEnabled(s);
			if (!enabled_)
				return;

			wait_.Update(s, 0);

			if (wait_.Finished && CanRun())
			{
				if (cig_ != null)
					cig_.Visible = true;

				person_.Animator.PlayType(AnimationType.Smoke);
			}


			if (cig_ != null)
			{
				if (!person_.Animator.IsPlayingType(AnimationType.Smoke))
				{
					if (person_.Body.Get(BP.RightHand).LockedFor(BodyPartLock.Anim))
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

			return p;
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
			string s = null;
			return CanRun(ref s);
		}

		private bool CanRun(ref string reason)
		{
			if (person_.Possessed)
			{
				if (reason != null) reason = "possessed";
				return false;
			}

			var b = person_.Body;
			var head = b.Get(BP.Head);
			var lips = b.Get(BP.Lips);

			if (person_.Status.Groped())
			{
				if (reason != null) reason = "groped";
				return false;
			}

			if (person_.Body.Get(BP.RightHand).LockedFor(BodyPartLock.Anim))
			{
				if (reason != null) reason = "right hand busy";
				return false;
			}

			if (head.LockedFor(BodyPartLock.Move))
			{
				if (reason != null) reason = "head locked";
				return false;
			}

			if (lips.LockedFor(BodyPartLock.Morph))
			{
				if (reason != null) reason = "lips locked";
				return false;
			}

			return true;
		}

		private void CreateCigarette()
		{
			var oc = Resources.Objects.Get("cigarette");
			if (oc == null)
			{
				person_.Log.Error("no cigarette object creator");
				return;
			}

			oc.Create(person_.Atom, CigaretteID, (o) =>
			{
				if (o == null)
				{
					person_.Log.Error("failed to create cigarette");
					return;
				}

				SetCigarette(o);
			});
		}

		private void SetCigarette(IObject o)
		{
			cig_ = o;
			cig_.Atom.Collisions = false;
			cig_.Atom.Physics = false;
			cig_.Atom.Hidden = true;
			cig_.Visible = true;

			o.Atom.Scale = person_.Atom.Scale * 0.8f;
		}
	}
}
