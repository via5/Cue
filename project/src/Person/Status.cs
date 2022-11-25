namespace Cue
{
	public class PersonStatus
	{
		public struct PartResult
		{
			public BodyPartType ownBodyPart;
			public BodyPartType byBodyPart;
			public int byObjectIndex;

			public PartResult(
				BodyPartType ownBodyPart, int byObjectIndex,
				BodyPartType byBodyPart)
			{
				this.ownBodyPart = ownBodyPart;
				this.byObjectIndex = byObjectIndex;
				this.byBodyPart = byBodyPart;
			}

			public static PartResult None
			{
				get { return new PartResult(BP.None, -1, BP.None); }
			}

			public bool Valid
			{
				get { return (ownBodyPart != BP.None); }
			}

			public override string ToString()
			{
				if (Valid)
				{
					return
						$"{BodyPartType.ToString(ownBodyPart)} by " +
						$"{Cue.Instance.GetObject(byObjectIndex)?.ID ?? "?"}" +
						$"." +
						$"{BodyPartType.ToString(byBodyPart)}";
				}
				else
				{
					return "false";
				}
			}

			public static implicit operator bool(PartResult pr)
			{
				return pr.Valid;
			}
		}


		private readonly Person person_;
		private readonly Body body_;

		public PersonStatus(Person p)
		{
			person_ = p;
			body_ = p.Body;
		}

		public void Update(float s)
		{
		}

		public bool AnyInsidePersonalSpace()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				if (InsidePersonalSpace(p))
					return true;
			}

			return false;
		}

		public bool InsidePersonalSpace(Person other)
		{
			if (other == person_)
				return false;

			var checkParts = BodyParts.PersonalSpaceParts;

			for (int i = 0; i < checkParts.Length; ++i)
			{
				var a = body_.Get(checkParts[i]);

				for (int j = 0; j < checkParts.Length; ++j)
				{
					var b = other.Body.Get(checkParts[j]);
					if (a.CloseTo(b))
						return true;
				}
			}

			return false;
		}

		public bool InteractingWith(Person other)
		{
			if (InsidePersonalSpace(other) || PenetratedBy(other) || HeadTouchedBy(other) || GropedBy(other))
				return true;

			// special case for unpossessed, because it's just the camera and
			// the mouse pointer grab is not handled
			if (!other.Body.Exists && other.IsPlayer)
			{
				foreach (BodyPartType i in BodyPartType.Values)
				{
					if (body_.Get(i).GrabbedByPlayer)
						return true;
				}
			}

			return false;
		}

		public bool Groped()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (GropedBy(p))
					return true;
			}

			return false;
		}

		public Person PenetratedBy()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (PenetratedBy(p))
					return p;
			}

			return null;
		}

		public bool Penetrated()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (PenetratedBy(p))
					return true;
			}

			if (body_.Get(BP.Vagina).Triggered)
				return true;

			if (body_.Get(BP.Anus).Triggered)
				return true;

			return false;
		}

		public bool Penetrating()
		{
			if (!body_.HasPenis)
				return false;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (Penetrating(p))
					return true;
			}

			return false;
		}

		public bool Penetrating(Person p)
		{
			if (!body_.HasPenis)
				return false;

			if (p.Status.PenetratedBy(person_))
				return true;

			return false;
		}

		public static bool EitherPenetrating(Person a, Person b)
		{
			return a.Status.PenetratedBy(b) || b.Status.PenetratedBy(a);
		}

		public bool HeadTouchedBy(Person p)
		{
			return CheckParts(p, new BodyPartType[] { BP.Head }, BodyParts.GropingParts);
		}

		public bool GropedBy(Person p)
		{
			return
				GropedBy(p, SS.Mouth) ||
				GropedBy(p, SS.Breasts) ||
				GropedBy(p, SS.Genitals);
		}

		public bool GropedBy(Person p, ZoneType z)
		{
			if (p == person_)
				return false;

			return person_.Body.Zone(z).Sources[p.PersonIndex].Active;
		}

		public bool PenetratedBy(Person p)
		{
			if (p == person_ || !p.Body.HasPenis)
				return false;

			var src = person_.Body.Zone(SS.Penetration).Sources[p.PersonIndex];
			return src.IsStrictlyActive(BP.Penis);
		}

		public bool FingeredBy(Person p)
		{
			return person_.Body.Zone(SS.Genitals).Sources[p.PersonIndex].Active;
		}

		private PartResult CheckParts(
			Person by, BodyPartType[] triggerParts, BodyPartType[] checkParts)
		{
			for (int i = 0; i < triggerParts.Length; ++i)
			{
				var triggerPart = body_.Get(triggerParts[i]);

				for (int j = 0; j < checkParts.Length; ++j)
				{
					var byPart = by.Body.Get(checkParts[j]);

					var pr = TriggeredBy(triggerPart, byPart);
					if (pr)
						return pr;
				}

				if (by.IsPlayer)
				{
					var pr = triggerPart.GrabbedByPlayer;
					if (pr)
						return pr;
				}
			}

			return PartResult.None;
		}

		private PartResult TriggeredBy(BodyPart p, BodyPart by)
		{
			if (!p.Exists || !by.Exists)
				return PartResult.None;

			var ts = p.GetTriggers();

			if (ts != null)
			{
				for (int i = 0; i < ts.Length; ++i)
				{
					if (ts[i].BodyPart != BP.None)
					{
						var pp = Cue.Instance.GetPerson(ts[i].PersonIndex);
						var bp = pp.Body.Get(ts[i].BodyPart);

						if (bp == by)
						{
							return new PartResult(
								p.Type,
								pp.ObjectIndex, ts[i].BodyPart);
						}
					}
				}
			}

			return PartResult.None;
		}
	}
}
