namespace Cue
{
	class PE : PE_Enum
	{
	}


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
					$"{BodyParts.ToString(bodyPart)}=>" +
					$"{BodyParts.ToString(sourceBodyPart)}   " +
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
			pp.Init(p);

			return pp;
		}

		private void CopyFrom(Physiology p)
		{
			CopyFrom((EnumValueManager)this);
			specificModifiers_ = p.specificModifiers_;
		}

		private void Init(Person p)
		{
			if (Get(PE.VoicePitch) <  0)
			{
				Set(PE.VoicePitch, U.Clamp(
					Get(PE.NeutralVoicePitch) + (1 - p.Atom.Body.Scale),
					0, 1));
			}
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

			return 1;
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
