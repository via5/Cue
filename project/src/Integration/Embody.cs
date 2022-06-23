namespace Cue.AcidBubbles
{
	public class Embody : IPossesser
	{
		Sys.Vam.VamAtom atom_ = null;
		private bool enabled_ = false;
		private Sys.Vam.BoolParameter active_ = null;
		private bool possessed_ = false;

		public Embody(Sys.IAtom atom)
		{
			atom_ = atom as Sys.Vam.VamAtom;

			if (atom_ != null && atom_.IsPerson)
			{
				enabled_ = true;
				active_ = new Sys.Vam.BoolParameter(atom_, "Embody", "Active");
			}
		}

		public bool Possessed
		{
			get
			{
				if (!enabled_)
					return false;

				bool b = active_.Value;

				if (!possessed_ && b)
					atom_.Log.Info("embody: possession started");
				else if (possessed_ && !b)
					atom_.Log.Info("embody: possession ended");

				possessed_ = b;

				return possessed_;
			}
		}
	}
}
