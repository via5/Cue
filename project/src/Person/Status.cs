namespace Cue
{
	class PersonStatus
	{
		private readonly Person person_;
		private readonly Body body_;

		public PersonStatus(Person p)
		{
			person_ = p;
			body_ = p.Body;
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
			if (InsidePersonalSpace(other) || PenetratedBy(other) || GropedBy(other))
				return true;

			// special case for unpossessed, because it's just the camera and
			// the mouse pointer grab is not handled
			if (!other.Body.Exists && other.IsPlayer)
			{
				for (int i = 0; i < BP.Count; ++i)
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
				if (GropedBy(p, BodyParts.GropedParts))
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

			return false;
		}

		public bool Penetrating()
		{
			if (!body_.HasPenis)
				return false;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p.Status.PenetratedBy(person_))
					return true;
			}

			return false;
		}

		public Body.PartResult GropedByAny(int triggerBodyPart)
		{
			return GropedByAny(new int[] { triggerBodyPart });
		}

		public Body.PartResult GropedByAny(int[] triggerBodyParts)
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				var pr = GropedBy(p, triggerBodyParts);
				if (pr.Valid)
					return pr;
			}

			return Body.PartResult.None;
		}

		public Body.PartResult GropedBy(Person p)
		{
			return GropedBy(p, BodyParts.GropedParts);
		}

		public Body.PartResult GropedBy(Person p, int triggerBodyPart)
		{
			return GropedBy(p, new int[] { triggerBodyPart });
		}

		public Body.PartResult GropedBy(Person p, int[] triggerBodyParts)
		{
			if (p == person_)
				return Body.PartResult.None;

			return body_.CheckParts(p, triggerBodyParts, BodyParts.GropedByParts);
		}

		public bool PenetratedBy(Person p)
		{
			if (p == person_)
				return false;

			return body_.CheckParts(
				p, BodyParts.PenetratedParts, BodyParts.PenetratedByParts);
		}
	}
}
