namespace Cue
{
	class Physiology : EnumValueManager
	{
		public struct SpecificModifier
		{
			public int bodyPart;
			public int sourceBodyPart;
			public float modifier;

			public override string ToString()
			{
				return
					$"{BP.ToString(bodyPart)}=>" +
					$"{BP.ToString(sourceBodyPart)}   " +
					$"{modifier}";
			}
		}


		private string name_;
		private SpecificModifier[] specificModifiers_ = new SpecificModifier[0];

		public Physiology(string name)
			: base(new PE())
		{
			name_ = name;
		}

		public string Name
		{
			get { return name_; }
		}

		public Physiology Clone(Person p)
		{
			var pp = new Physiology(name_);
			pp.CopyFrom(this);
			return pp;
		}

		private void CopyFrom(Physiology p)
		{
			base.CopyFrom(p);
			specificModifiers_ = p.specificModifiers_;
		}

		public void Set( SpecificModifier[] sms)
		{
			specificModifiers_ = sms;
		}

		public float GetSpecificModifier(int part, Sys.TriggerInfo t)
		{
			for (int i = 0; i < specificModifiers_.Length; ++i)
			{
				var sm = specificModifiers_[i];
				if (sm.bodyPart == part && sm.sourceBodyPart == t.sourcePartIndex)
					return sm.modifier;
			}

			return 0;
		}

		public SpecificModifier[] SpecificModifiers
		{
			get { return specificModifiers_; }
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
