namespace Cue
{
	class Morph
	{
		private Sys.IMorph m_;

		public Morph(Sys.IMorph m)
		{
			m_ = m;
		}

		public Morph(Person p, string id)
			: this(p.Atom.GetMorph(id))
		{
		}

		public string Name
		{
			get { return m_?.Name ?? "?"; }
		}

		public float Value
		{
			get
			{
				return m_?.Value ?? 0;
			}

			set
			{
				if (m_ != null)
					m_.Value = value;
			}
		}

		public float DefaultValue
		{
			get { return m_?.DefaultValue ?? 0; }
		}

		public void Reset()
		{
			m_?.Reset();
		}

		public override string ToString()
		{
			return m_?.ToString() ?? "no morph";
		}
	}
}
