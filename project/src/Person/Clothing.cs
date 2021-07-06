namespace Cue
{
	class Clothing
	{
		private Person person_;

		public Clothing(Person p)
		{
			person_ = p;
		}

		public void Init()
		{
			person_.Atom.Clothing.Init();
		}

		public float HeelsAngle
		{
			get { return person_.Atom.Clothing.HeelsAngle; }
		}

		public float HeelsHeight
		{
			get { return person_.Atom.Clothing.HeelsHeight; }
		}

		public bool GenitalsVisible
		{
			get { return person_.Atom.Clothing.GenitalsVisible; }
			set { person_.Atom.Clothing.GenitalsVisible = value; }
		}

		public bool BreastsVisible
		{
			get { return person_.Atom.Clothing.BreastsVisible; }
			set { person_.Atom.Clothing.BreastsVisible = value; }
		}

		public void Dump()
		{
			person_.Atom.Clothing.Dump();
		}

		public override string ToString()
		{
			return person_.Atom.Clothing.ToString();
		}
	}
}
