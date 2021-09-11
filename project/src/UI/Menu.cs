namespace Cue
{
	static class UIActions
	{
		public static void HJ(Person p, bool b)
		{
			if (p != null && p != Cue.Instance.Player)
			{
				if (b)
					p.Handjob.Start();
				else
					p.Handjob.Stop();
			}
		}

		public static void BJ(Person p, bool b)
		{
			if (p != null && p != Cue.Instance.Player)
			{
				if (b)
					p.Blowjob.Start();
				else
					p.Blowjob.Stop();
			}
		}

		public static void Thrust(Person p, bool b)
		{
			if (p != null)
				p.AI.GetEvent<SexEvent>().Active = b;
		}

		public static void CanKiss(Person p, bool b)
		{
			if (p != null)
				p.Options.CanKiss = b;
		}

		public static void Genitals(Person p)
		{
			if (p != null)
				p.Clothing.GenitalsVisible = !p.Clothing.GenitalsVisible;
		}

		public static void Breasts(Person p)
		{
			if (p != null)
				p.Clothing.BreastsVisible = !p.Clothing.BreastsVisible;
		}
	}
}
